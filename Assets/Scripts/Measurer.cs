using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

public class Measurer : MonoBehaviour
{
    public bool debugMode = false;

    // how many positions with how many rotations to test
    public bool fixPosition = false;
    [Range(1, 5000)]
    public int totalPositions = 1;
    [Range(1, 5000)]
    public int totalRotations = 1;
    private int totalMeasurements;

    public FrequencyGenerator generator;
    public GameObject listener;

    // time spent sampling intesnsity
    private float probingTime = 0.1f;
    private float timer;

    // measuring
    private float[] measurement;
    private int measurementSamples;
    private int filledSamples = 0;

    // contol flow
    private bool measureing = false;
    private bool running = false;
    private bool waitForMeasure = false;
    private bool finished = false;

    // Max Vals for random position generation
    private int maxX = 25;
    private int maxZ = 25;


    // variables for generating and tracking positions and rotations
    private Quaternion startingRotation;
    private int measuredPositions = 0;
    private int measuredRotations = 0;


    private void Awake() {
        startingRotation = listener.transform.rotation;
        totalMeasurements = totalPositions * totalRotations;
        measurementSamples = AudioSettings.outputSampleRate * 2;
        measurement = new float[measurementSamples];

    }


    // Update is called once per frame
    void Update()
    {
        handleTimer();

        // check if the generator is running
        if (generator.isRunning() && !running && !waitForMeasure && !finished)
        {
            waitForMeasure = true;
            timer = 0.5f;
        }

        // check if a measurement is finished
        if(!finished && running && !measureing)
        {
            finishMeasurement();
        }

    }

    // callback to activate measuring (called some time after sound generation started)
    private void activateMeasureing() {
        running = true;
        waitForMeasure = false;
        startNextMeasurement();
    }

    private void startNextMeasurement() {
        if (debugMode) { Debug.Log($"Measuring position {measuredPositions + 1} of {totalPositions}. (Rotation {measuredRotations})"); }
        // set the new timer
        timer = (float)probingTime;
        measureing = true;
    }

    private void finishMeasurement() {
        // get intensities
        float[] leftSideSquared = measurement.Where((x, i) => i % 2 == 0).Select(x => x*x).ToArray();
        float[] rightSideSquared = measurement.Where((x, i) => i % 2 == 1).Select(x => x * x).ToArray();
        float leftRMS = leftSideSquared.Sum() / leftSideSquared.Length;
        float rightRMS = rightSideSquared.Sum() / rightSideSquared.Length;
        if(debugMode)
        {
            Debug.Log($"RMS at {measuredPositions}/{measuredRotations} is: {leftRMS}/{rightRMS}.");
        }


        // get position and rotation for the next measurement
        measuredRotations++;
        if (measuredRotations == totalRotations)
        {
            measuredRotations = 0;
            measuredPositions++;
            setNewListenerPosition();
            
        }
        setNewListenerRotation();

        // were done if we measured all positions
        if (measuredPositions == totalPositions)
        {
            if(debugMode) { Debug.Log($"Finished all {totalMeasurements} measurements."); }
            running = false;
            finished = true;
            return;
        }

        startNextMeasurement();
    }

    // set position to new random position
    private void setNewListenerPosition() {
        listener.transform.rotation = startingRotation;
        listener.transform.position = new Vector3(Random.Range(-maxX, maxX), 0, Random.Range(-maxZ, maxZ));
    }

    // set rotation to next rotation
    private void setNewListenerRotation() {
        listener.transform.rotation = startingRotation;
        listener.transform.RotateAround(listener.transform.position, Vector3.up, (measuredRotations * (360 / totalRotations)));
    }

    private void handleTimer() {
        timer -= Time.deltaTime;

        if (timer < 0)
        {
            if(waitForMeasure) { activateMeasureing(); }
        }
    }
    private void OnAudioFilterRead(float[] data, int channels) {
        if (measureing)
        {
            int currentSamples = data.Length;

            if (filledSamples + data.Length <= measurementSamples)
            {
                data.CopyTo(measurement, filledSamples);
                filledSamples += currentSamples;
            }
            else
            {
                data.Take(measurementSamples - filledSamples).ToArray().CopyTo(measurement, filledSamples);
                measureing = false;
            }
        }
    }
}
