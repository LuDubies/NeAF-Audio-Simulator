using System.Linq;
using UnityEditor;
using UnityEngine;
using System.IO;

public class Measurer : MonoBehaviour
{
    public bool debugMode = false;
    public bool on = true;

    public enum Mode { Random, Picture};
    public Mode mode;

    // RANDOM how many positions with how many rotations to test
    [Range(1, 5000)]
    public int totalPositions = 1;
    [Range(1, 5000)]
    public int totalRotations = 1;
    private int totalMeasurements;

    // PICTURE
    public int pictureCount = 1;
    public int pictureWidth = 100;
    public int pictureHeight = 50;
    public float pixelSize = 0.02f;
    private int measuredPixels;
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
        measurementSamples = (int)Mathf.Ceil(AudioSettings.outputSampleRate * 2 * probingTime);
        measurement = new float[measurementSamples];
        measuredPixels = 0;

        camera = new Camera(listener.transform.position, listener.transform.forward, pictureWidth, pictureHeight);

        if (debugMode) { Debug.Log($"Sampling rate is {AudioSettings.outputSampleRate}." +
            $" For measurement time of {probingTime}s, we collect {measurementSamples} samples each measurement!"); }
    }


    // Update is called once per frame
    void Update()
    {
        if (!on) { return; }

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
        if(debugMode) { Debug.Log("Starting measurements"); }
        startNextMeasurement();
    }

    private void startNextMeasurement() {
        if(mode == Mode.Random && debugMode) {
            Debug.Log($"Measuring position {measuredPositions + 1} of {totalPositions}. (Rotation {measuredRotations})");
        }
        if(mode == Mode.Picture) { 
            listener.transform.position = camera.getNPosition(measuredPixels, pixelSize);
            if(debugMode) { Debug.Log($"Pixel {measuredPixels} position is {listener.transform.position}."); }
        }
        measureing = true;
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
            if(measuredPixels >= pictureWidth * pictureHeight) { Debug.Log("Pic done"); endMeasuring(); camera.saveImage(); return; }
        }
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
                filledSamples = 0;
                measureing = false;
            }
        }
    }
}

public class Camera
{
    private Vector3 origin;
    private Vector3 direction;

    private int width;
    private int height;

    private float[] image;

    public Camera(Vector3 origin, Vector3 direction, int width, int height)
    {
        this.origin = origin;
        this.direction = direction;
        this.width = width;
        this.height = height;
        image = new float[width * height];
    }

    public Vector3 getPosition(int w, int h, float precision) {
        return origin +
            (width / (float)2 - w - 0.5f * precision) * Vector3.Cross(direction, Vector3.up).normalized * precision +
            (height / (float)2 - h - 0.5f * precision) * Vector3.up * precision;
    }
    public Vector3 getNPosition(int n, float precision) {
        return getPosition(n % width, n / width, precision);
    }

    public void setOrigin(Vector3 origin) { this.origin = origin; }
    public void setDirection(Vector3 direction) { this.direction = direction; }
    public void setWidthHeight(int width, int height) { this.width = width; this.height = height; image = new float[width * height]; }
    public void setPixel(int width, int height, float val) { image[width + height * this.width] = val; }
    public void setNPixel(int n, float val) { image[n] = val; }

    public void saveImage() {
        string filepath = @"C:\Users\lucad\Desktop\testy.png";
        Debug.Log($"Saving image to {filepath}.");
        Texture2D imageTex = new Texture2D(width, height);
        var colorMap = image.Select(x => Color.Lerp(Color.black, Color.white, x));
        imageTex.SetPixels(colorMap.ToArray());
        File.WriteAllBytes(filepath, imageTex.EncodeToPNG());
    }


}
