using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;


public class Dataset : MonoBehaviour {
    // general info
    private System.Guid guid;
    private System.DateTime date;
    private string version = "2.0";

    [Range(0.2f, 10f)]
    public float timeScale;
    public GameObject producer;
    public GameObject listener;
    private SteamAudio.SteamAudioManager steamAudioManager;
    private int hrtfCount;

    // enum for data mode
    private enum Mode { RandomSampling, Fixed, Map };
    [SerializeField]
    private Mode mode;

    public bool saveRawRecordings;

    // recording count parameters
    [Range(1, 10000)]
    public int position_cnt;
    [Range(1, 1000)]
    public int rotation_cnt;
    private int total_recordings;
    private int finished_recordings;
    private List<Recording> allRecordings;
    private int percentageDone = 0;
    private long globalByteOffset = 0;

    [Range(0.2f, 2f)]
    public float rec_len;

    // Fixed Mode params
    private int vertical_degrees = 120;
    private int horizontal_degrees = 360;
    private float[,] intensity;


    // dB ref
    private float dbRefValue = 0.1f;
    private int lowerdbCap = -160;
    private int upperdbCap = 20;

    // scene information
    private Geometry geometry;
    private SoundSource sound;
    private Quaternion startingRotation;

    // file information
    private string filename;
    private string path;

    // recording vars
    private int total_samples;
    private bool record;
    float[] recording;
    int recording_sample_cnt;

    public void Awake() {
        listener = this.gameObject;
        startingRotation = listener.transform.rotation;
        guid = System.Guid.NewGuid();
        date = System.DateTime.Now;
        total_recordings = position_cnt * rotation_cnt;
        finished_recordings = 0;
        allRecordings = new List<Recording>();

        // adjust time
        Debug.Log(timeScale);
        float fixedDTimeBckup = Time.fixedDeltaTime;
        Time.timeScale = timeScale;
        Time.fixedDeltaTime = fixedDTimeBckup / Time.timeScale;

        // adjust pitch according to time
        AudioSource audioSource = producer.GetComponent<AudioSource>();
        audioSource.pitch = Time.timeScale;

        path = Path.GetFullPath(Path.Combine(Application.dataPath, "../"));
        filename = "Dataset-" + date.ToString("MM-ddTHH-mm-ss") + "--" + position_cnt;
        recording_sample_cnt = Mathf.CeilToInt((AudioSettings.outputSampleRate * rec_len * 2) / Time.timeScale);
        Debug.Log($"Sample count per recording is: {recording_sample_cnt}");
        recording = new float[recording_sample_cnt];

        if (mode == Mode.Fixed) {
            intensity = new float[position_cnt, rotation_cnt];
        }

        //find the SteamAudoManager
        steamAudioManager = GameObject.Find("Steam Audio Manager").GetComponent<SteamAudio.SteamAudioManager>();
        hrtfCount = steamAudioManager.hrtfNames.Length;
    }


    public void Start() {
        // set Geometry and SoundSource
        geometry = new Geometry();
        sound = new SoundSource(producer);

        Invoke("DetermineNextRecordingParameters", 3f);
    }

    void Save() {
        // put general info into json file
        string complete = $"{{\"Version\": \"{version}\",\n";
        complete += "\"Geometry\": " + geometry.BuildJSON() + ",\n\"Sound\": " + sound.BuildJSON();

        complete += ",\n\"Recordings\": [";
        string delim = "";
        foreach (Recording recording in allRecordings)
        {
            complete += delim + "\n" + recording.BuildJson();
            delim = ",";
        }
        complete += "\n]";

        complete += "}";

        File.WriteAllText(path + @"\data\" + filename + ".json", complete);

        if (mode == Mode.Fixed)
        {
            StreamWriter sw = new StreamWriter(path + @"\data\" + filename + "-intensities.csv", append: false);
            for (int i = 0; i < intensity.GetLength(0); i++) {
                sw.WriteLine(string.Join(",", Enumerable.Range(0, intensity.GetLength(1)).Select(x => intensity[i, x].ToString(System.Globalization.CultureInfo.InvariantCulture)).ToArray()));
            }
            sw.Close();
        }
    }

    void DetermineNextRecordingParameters() {
        if (finished_recordings == total_recordings)
        {
            Save();
            Debug.Log("Finished Dataset Generation!");
            return;
        }

        if (mode == Mode.RandomSampling)
        {
            if (finished_recordings % rotation_cnt == 0)
            {
                // find new random position
                listener.transform.position = new Vector3(
                    producer.transform.position.x + (float)Random.Range(-25, 25),
                    0f,
                    producer.transform.position.z + (float)Random.Range(-25, 25));
                listener.transform.rotation = startingRotation;
            }
            else
            {
                // rotate for next recording
                listener.transform.RotateAround(listener.transform.position, Vector3.up, 360 / rotation_cnt);
            }
        }
        else if (mode == Mode.Fixed)
        {
            
            // position is FIXED, change instead tilt of recording object
            if (finished_recordings % rotation_cnt == 0)
            {
                // tilt from - half the vertical_degrees to + half the vertical degrees
                int current_tilt = 0;
                if(position_cnt > 1){
                    current_tilt = -(vertical_degrees / 2) + (int)(((finished_recordings / rotation_cnt) / (float)(position_cnt - 1)) * vertical_degrees);
                }
                listener.transform.rotation = startingRotation;
                listener.transform.RotateAround(listener.transform.position, Vector3.forward, current_tilt);
            }
            else
            {
                listener.transform.RotateAround(listener.transform.position, Vector3.up, horizontal_degrees / rotation_cnt);
            }      
        }

        GenerateRecording();
    }

    void GenerateRecording() {
        // start recording and schedule saving the finished recording
        total_samples = 0;
        record = true;
        Invoke("BuildRecording", (rec_len + 0.1f) / timeScale);
        AudioSource audioSource = producer.GetComponent<AudioSource>();
        audioSource.Play();
    }

    void BuildRecording() {
        if (record)
        {
            Debug.Log("BuildRecording called to early!!");
            Invoke("BuildRecording", 0.1f);
            return;
        }

        Recording s = new(finished_recordings, listener, recording, globalByteOffset, dbRefValue);
        if(saveRawRecordings) { s.SaveBytes(@"C:\Users\lucad\NeAF\data\" + filename + "-recordings.bin"); }
        globalByteOffset += recording_sample_cnt;

        allRecordings.Add(s);

        if (mode == Mode.Fixed) {
            // record average decibels
            float[] dbsl = s.GetLoudness(0);
            intensity[(finished_recordings / rotation_cnt), finished_recordings % rotation_cnt] = (dbsl.Average() - lowerdbCap) / (upperdbCap - lowerdbCap);
        }

        // status update
        if (Mathf.FloorToInt(((float)finished_recordings / total_recordings) * 100) > percentageDone)
        {
            percentageDone = Mathf.FloorToInt(((float)finished_recordings / total_recordings) * 100);
            Debug.Log($"Finished {percentageDone}% of recordings.");
        }

        // generate the next recording
        finished_recordings++;
        DetermineNextRecordingParameters();
    }

    void OnAudioFilterRead(float[] data, int channels) {
        if (record)
        {
            int current_samples = data.Length;

            if (total_samples + current_samples <= recording_sample_cnt)
            {
                data.CopyTo(recording, total_samples);
                total_samples += current_samples;
            }
            else
            {
                data.Take(recording_sample_cnt - total_samples).ToArray().CopyTo(recording, total_samples);
                record = false;
            }
        }
    }
}

public class PosRot {
    public Vector3 position;
    public Quaternion rotation;

    public PosRot(GameObject go) {
        position = go.transform.position;
        rotation = go.transform.rotation;
    }
}

public class TransformEssentials {
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;

    public TransformEssentials(GameObject go) {
        position = go.transform.position;
        rotation = go.transform.rotation;
        scale = go.transform.lossyScale;
    }
}

public class Geometry {
    public List<TransformEssentials> geoEssentialCollection = new List<TransformEssentials>();
    public int oCount;

    public Geometry() {
        // get all objects with steam audio geometry
        var steamAudioGeometries = Object.FindObjectsOfType<SteamAudio.SteamAudioGeometry>();
        foreach (SteamAudio.SteamAudioGeometry sag in steamAudioGeometries)
        {
            geoEssentialCollection.Add(new TransformEssentials(sag.gameObject));
        }
        oCount = geoEssentialCollection.Count;
    }

    public string BuildJSON() {
        string json = $"{{\n\"Count\": {oCount},\n\"Objects\": [";
        string delim = "";
        foreach (TransformEssentials ge in geoEssentialCollection)
        {
            json += delim + "\n";
            json += JsonUtility.ToJson(ge);
            delim = ",";
        }
        json += "\n]\n}";
        return json;
    }
}

public class SoundSource {
    public TransformEssentials geo;
    public string audioFileName;
    public float start;
    public float end;

    public SoundSource(GameObject sourceObject) {
        geo = new TransformEssentials(sourceObject);
        AudioSource audioSource = sourceObject.GetComponent<AudioSource>();
        audioFileName = audioSource.clip.name;

        Soundbox soundbox = sourceObject.GetComponent<Soundbox>();
        start = soundbox.start_time;
        end = soundbox.end_time;
    }

    public string BuildJSON() {
        return JsonUtility.ToJson(this) + ",\n\"SoundLocation\": " + JsonUtility.ToJson(geo);
    }
}

public class Recording {
    public int number;
    public PosRot listenerLocation;
    public float[] samples;
    public long byteOffset;
    public bool saved;
    public float dbRefValue;

    public Recording(int _num, GameObject listener, float[] _received) {
        number = _num;
        listenerLocation = new PosRot(listener);
        samples = _received;
        byteOffset = 0;
        saved = false;
        dbRefValue = 0.1f;
    }

    public Recording(int _num, GameObject listener, float[] _received, long _off, float db_ref) {
        number = _num;
        listenerLocation = new PosRot(listener);
        samples = _received;
        byteOffset = _off;
        saved = false;
        dbRefValue = db_ref;
    }


    public string BuildJson() {
        string json = $"{{\"Number\": {number},\n\"Location\":" + JsonUtility.ToJson(listenerLocation) + $",\n\"Offset\":{byteOffset}}}";
        return json;
    }

    public void SaveBytes(string filename) {
        FileStream fileStream;
        if (byteOffset == 0)
        {
            fileStream = new FileStream(filename, FileMode.Create);
        }
        else
        {
            fileStream = new FileStream(filename, FileMode.Append, FileAccess.Write);
        }


        System.Int16[] intData = new System.Int16[samples.Length];
        System.Byte[] bytesData = new System.Byte[samples.Length * 2];

        int rescaleFactor = 32767; //to convert float to Int16

        for (int i = 0; i < samples.Length; i++)
        {
            intData[i] = (short)(samples[i] * rescaleFactor);
            System.Byte[] byteArr = new System.Byte[2];
            byteArr = System.BitConverter.GetBytes(intData[i]);
            byteArr.CopyTo(bytesData, i * 2);
        }

        fileStream.Write(bytesData, 0, bytesData.Length);
        fileStream.Close();
        saved = true;
    }

    public float[] GetLoudness(int side) {
        var sideonly_samples = samples.Where((x, i) => i % 2 == side).ToArray();


        int windowLength = 1024;
        float[] window = new float[windowLength];
        int totalWindows = Mathf.FloorToInt(sideonly_samples.Length / (float)windowLength);

        float[] loudness = new float[totalWindows];



        for (int window_id = 0; window_id < totalWindows; window_id++)
        {
            System.Array.Copy(sideonly_samples, window_id * windowLength, window, 0, windowLength);
            float sum = 0;
            for (int i = 0; i < windowLength; i++)
            {
                sum += window[i] * window[i];
            }
            float rmsValue = Mathf.Sqrt(sum / windowLength);
            float dbValue = 20 * Mathf.Log10(rmsValue / dbRefValue);
            loudness[window_id] = System.Math.Max(dbValue, -160);
        }
        return loudness;
    }

    public void NullSamples() {
        samples = null;
    }
}

