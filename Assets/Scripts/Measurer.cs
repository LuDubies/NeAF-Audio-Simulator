using System.Linq;
using UnityEditor;
using UnityEngine;


public enum Mode { Random, Picture };
public enum PictureMode { Translated, Rotated };

public class Measurer : MonoBehaviour
{
    public bool debugMode = false;
    public bool on = true;
    public bool reset = false;

    
    public Mode mode;
    public PictureMode pictureMode;

    // RANDOM how many positions with how many rotations to test
    [Range(1, 5000)]
    public int totalPositions = 1;
    [Range(1, 5000)]
    public int totalRotations = 1;
    private int totalMeasurements;

    // PICTURE
    [Range(1, 1000)]
    public int pictureCount = 1;
    public int pictureWidth = 100;
    public int pictureHeight = 50;
    public float pixelSize = 0.02f;
    private int measuredPixels;
    private int picturesTaken;
    private Camera camera;

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
    private bool measureInAudioThreat = false;
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
        measurementSamples = (int)Mathf.Ceil(AudioSettings.outputSampleRate * 2 * probingTime);
        measurement = new float[measurementSamples];
        measuredPixels = 0;
        picturesTaken = 0;

        camera = new Camera(listener.transform.position, listener.transform.forward, pictureWidth, pictureHeight, pictureMode);

        if (debugMode) { Debug.Log($"Sampling rate is {AudioSettings.outputSampleRate}." +
            $" For measurement time of {probingTime}s, we collect {measurementSamples} samples each measurement!"); }
    }


    // Update is called once per frame
    void Update(){
        if (reset){
            //manuall reset performed
            finished = false;
            measureInAudioThreat = false;
            waitForMeasure = false;
            Reset();
            return;
        }
        if (!on) { return; }

        handleTimer();

        // check if the generator is running
        if (generator.isRunning() && !running && !waitForMeasure && !finished)
        {
            waitForMeasure = true;
            timer = 0.5f;
        }

        // check if a measurement is finished
        if(!finished && running && !measureInAudioThreat)
        {
            finishMeasurement();
        }

    }

    private void Reset() {
        measuredPixels = 0;
        filledSamples = 0;
        reset = false;
        listener.transform.rotation = camera.getDirection();
        listener.transform.position = camera.getOrigin();
    }

    // callback to activate measuring (called some time after sound generation started)
    private void activateMeasureing() {
        running = true;
        waitForMeasure = false;
        camera.setOrigin(listener.transform.position);
        camera.setDirection(listener.transform.forward);
        if (debugMode) { Debug.Log("Starting measurements"); }
        startNextMeasurement();
    }

    private void startNextMeasurement() {
        if(mode == Mode.Random && debugMode) {
            Debug.Log($"Measuring position {measuredPositions + 1} of {totalPositions}. (Rotation {measuredRotations})");
        }
        if(mode == Mode.Picture) { 
            listener.transform.position = camera.getNPosition(measuredPixels, pixelSize);
            listener.transform.rotation = camera.getNRotation(measuredPixels);
            if(debugMode) { Debug.Log($"Pixel {measuredPixels} position is {listener.transform.position} in direction {listener.transform.forward}."); }
        }
        measureInAudioThreat = true;
    }

    private void finishMeasurement() {
        // get intensities
        float[] leftSideSquared = measurement.Where((x, i) => i % 2 == 0).Select(x => x*x).ToArray();
        float[] rightSideSquared = measurement.Where((x, i) => i % 2 == 1).Select(x => x * x).ToArray();
        float leftRMS = leftSideSquared.Sum() / leftSideSquared.Length;
        float rightRMS = rightSideSquared.Sum() / rightSideSquared.Length;
        if(debugMode && mode == Mode.Random) { Debug.Log($"RMS at {measuredPositions}/{measuredRotations} is: {leftRMS}/{rightRMS}.");
        } else if(debugMode && mode == Mode.Picture) { Debug.Log($"RMS at pixel {measuredPixels} is: {leftRMS}/{rightRMS}."); }

        if(mode == Mode.Random) {
            // get position and rotation for the next measurement
            measuredRotations++;
            if (measuredRotations == totalRotations) {
                measuredRotations = 0;
                measuredPositions++;
                // were done if we measured all positions
                if (measuredPositions == totalPositions) {
                    if (debugMode) { Debug.Log($"Finished all {totalMeasurements} measurements."); }
                    endMeasuring();
                    return;
                }
                setNewRandomListenerPosition();
            }
            setNewListenerRotation();
        } else if(mode == Mode.Picture) {
            camera.setNPixel(measuredPixels, leftRMS);
            measuredPixels++;
            if(measuredPixels >= pictureWidth * pictureHeight) { Debug.Log("Pic done"); camera.saveImage(); picturesTaken++;
                if (picturesTaken >= pictureCount) {
                    endMeasuring();
                    return;
                } else {
                    Reset();
                    setNextPictureParameters();
                }
            }
            
        }
        startNextMeasurement();
    }

    private void setNextPictureParameters() {
        (int, int) valid_x = (-5, 5);
        (int, int) valid_z = (5, 10);
        (int, int) valid_y = (0, 2);

        Vector3 look_to = generator.transform.position;
        Vector3 new_pos = new Vector3(Random.Range(valid_x.Item1, valid_x.Item2), Random.Range(valid_y.Item1, valid_y.Item2), Random.Range(valid_z.Item1, valid_z.Item2));
        camera.setOrigin(new_pos);
        camera.setDirection((look_to - new_pos).normalized);

        startNextMeasurement();
    }

    private void endMeasuring() {
        running = false;
        finished = true;
    }

    // set position to new random position
    private void setNewRandomListenerPosition() {
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
        if (measureInAudioThreat)
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
                filledSamples = 0;
                measureInAudioThreat = false;
            }
        }
    }
}
