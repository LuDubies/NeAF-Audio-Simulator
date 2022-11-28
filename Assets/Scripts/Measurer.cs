using System.Linq;
using UnityEditor;
using UnityEngine;
using System.IO;

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

        camera = new Camera(listener.transform.position, listener.transform.forward, pictureWidth, pictureHeight, pictureMode);

        if (debugMode) { Debug.Log($"Sampling rate is {AudioSettings.outputSampleRate}." +
            $" For measurement time of {probingTime}s, we collect {measurementSamples} samples each measurement!"); }
    }


    // Update is called once per frame
    void Update(){
        if (reset){
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
        if(!finished && running && !measureing)
        {
            finishMeasurement();
        }

    }

    private void Reset() {
        measuredPixels = 0;
        filledSamples = 0;
        running = false;
        reset = false;
        finished = false;
        measureing = false;
        waitForMeasure = false;
        startingRotation = listener.transform.rotation;
        camera.setOrigin(listener.transform.position);
        camera.setDirection(listener.transform.forward);
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
            listener.transform.rotation = camera.getNRotation(measuredPixels);
            if(debugMode) { Debug.Log($"Pixel {measuredPixels} position is {listener.transform.position} in direction {listener.transform.forward}."); }
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

    private PictureMode picmode;

    private int width;
    private int height;

    private float[] image;

    public Camera(Vector3 origin, Vector3 direction, int width, int height, PictureMode pm)
    {
        this.origin = origin;
        this.direction = direction;
        this.width = width;
        this.height = height;
        this.picmode = pm;
        image = new float[width * height];
    }

    public Vector3 getPosition(int w, int h, float precision) {
        if(picmode == PictureMode.Translated){
            return origin +
            (width / (float)2 - w - 0.5f * precision) * Vector3.Cross(direction, Vector3.up).normalized * precision +
            (height / (float)2 - h - 0.5f * precision) * Vector3.up * precision;
        } else{
            return origin;
        } 
    }

    public Quaternion getRotation(int w, int h) {
        if(picmode == PictureMode.Translated){
            return Quaternion.LookRotation(direction);
        }
        else {
            float y_angle = -(width/2) + w;
            float x_angle = (height/2) - h;
            return Quaternion.LookRotation(Quaternion.Euler(x_angle, y_angle, 0) * direction);
        }
    }
    public Vector3 getNPosition(int n, float precision) {
        return getPosition(n % width, n / width, precision);
    }
    public Quaternion getNRotation(int n) {
        return getRotation(n % width, n / width);
    }

    public void setOrigin(Vector3 origin) { this.origin = origin; }
    public void setDirection(Vector3 direction) { this.direction = direction; }
    public void setWidthHeight(int width, int height) { this.width = width; this.height = height; image = new float[width * height]; }
    public void setPixel(int width, int height, float val) { image[width + height * this.width] = val; }
    public void setNPixel(int n, float val) { image[n] = val; }
    public void setPictureMode(PictureMode pm) { this.picmode = pm; }

    public void saveImage() {
        string dataDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../data/"));
        string filename= "capture-" + System.DateTime.Now.ToString("MM-ddTHH-mm-ss") + ".png";
        Debug.Log($"Saving image to {dataDir} as {filename}.");
        Texture2D imageTex = new Texture2D(width, height);
        var colorMap = image.Select(x => Color.Lerp(Color.black, Color.white, x));
        imageTex.SetPixels(colorMap.ToArray());
        File.WriteAllBytes(dataDir + filename, imageTex.EncodeToPNG());
    }


}
