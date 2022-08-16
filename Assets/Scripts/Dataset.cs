
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;

public class Dataset : MonoBehaviour
{
    // general info
    public System.Guid guid;
    public System.DateTime date;
    public string version = "1.0";


    public GameObject producer;
    public GameObject listener;

    // recording count parameters
    [Range(1, 10000)]
    public int position_cnt;
    [Range(1, 1000)]
    public int rotation_cnt;
    public int total_recordings;
    public int finished_recordings;
    public List<Recording> allRecordings;
    public int percentageDone = 0;
    public long globalByteOffset = 0;

    [Range(0.2f, 2f)]
    public float rec_len;

    // scene information
    public Geometry geometry;
    public SoundSource sound;
    public Quaternion firstListenerRotation;

    // file information
    public string filename;

    // recording vars
    private int total_samples;
    private bool record;
    float[] recording;
    int recording_sample_cnt;

    public void Awake()
    {
        listener = this.gameObject;
        firstListenerRotation = listener.transform.rotation;
        guid = System.Guid.NewGuid();
        date = System.DateTime.Now;
        total_recordings = position_cnt * rotation_cnt;
        finished_recordings = 0;
        allRecordings = new List<Recording>();

        filename = "Dataset-" + date.ToString("MM-ddTHH-mm-ss") + "--" + position_cnt;
        recording_sample_cnt = Mathf.CeilToInt(AudioSettings.outputSampleRate * rec_len * 2);
        recording = new float[recording_sample_cnt];
    }

    public void Start()
    {
        // set Geometry and SoundSource
        geometry = new Geometry();
        sound = new SoundSource();

        Invoke("DetermineNextRecordingParameters", 1);
    }

    public void Save()
    {
        // put general info into json file
        string complete = $"{{\"Version\": \"{version}\",\n";
        complete += "\"Geometry\": " + geometry.BuildJSON() + ",\n\"Sound\": " + sound.BuildJSON();

        complete += ",\n\"Recordings\": [";
        string delim = "";
        foreach(Recording recording in allRecordings)
        {
            complete += delim + "\n" + recording.BuildJson();
            delim = ",";
        }
        complete += "\n]";

        complete += "}";
        File.WriteAllText(@"C:\Users\lucad\NeAF\data\" + filename + ".json", complete);
    }

    public void DetermineNextRecordingParameters() {
        if (finished_recordings == total_recordings)
        {
            Save();
            Debug.Log("Finished Dataset Generation!");
            return;
        }

        if (finished_recordings % rotation_cnt == 0)
        {
            // find new position
            listener.transform.position = new Vector3(
                producer.transform.position.x + (float)Random.Range(-25, 25),
                0f,
                producer.transform.position.z + (float)Random.Range(-25, 25));
            listener.transform.rotation = firstListenerRotation;
        } else
        {
            // rotate for next recording
            listener.transform.RotateAround(listener.transform.position, Vector3.up, 360 / rotation_cnt);
        }

        Invoke("GenerateRecording", 0.01f);
    }

    public void GenerateRecording()
    {
        // start recording and schedule saving the finished recording
        total_samples = 0;
        record = true;
        Invoke("BuildRecording", rec_len + 0.1f);
        AudioSource audioSource = producer.GetComponent<AudioSource>();
        audioSource.Play();
    }

    public void BuildRecording()
    {
        if (record)
        {
            Debug.Log("BuildRecording called to early!!");
            Invoke("BuildRecording", 0.1f);
            return;
        }
        Recording s = new (++finished_recordings, listener, recording, globalByteOffset);
        s.SaveBytes(@"C:\Users\lucad\NeAF\data\" + filename + "-recordings.bin");
        globalByteOffset += recording_sample_cnt;

        allRecordings.Add(s);

        // status update
        if (Mathf.FloorToInt(((float)finished_recordings / total_recordings) * 100) > percentageDone)
        {
            percentageDone = Mathf.FloorToInt(((float)finished_recordings / total_recordings) * 100);
            Debug.Log($"Finished {percentageDone}% of recordings.");
        }

        // generate the next recording
        DetermineNextRecordingParameters();
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        if(record)
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

public class PosRot
{
    public Vector3 position;
    public Quaternion rotation;

    public PosRot(GameObject go)
    {
        position = go.transform.position;
        rotation = go.transform.rotation;
    }
}

public class TransformEssentials
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;

    public TransformEssentials(GameObject go)
    {
        position = go.transform.position;
        rotation = go.transform.rotation;
        scale = go.transform.lossyScale;
    }
}

public class Geometry
{
    public List<TransformEssentials> geoEssentialCollection = new List<TransformEssentials>();
    public int oCount;

    public Geometry()
    {
        // get all objects with steam audio geometry
        var steamAudioGeometries = Object.FindObjectsOfType<SteamAudio.SteamAudioGeometry>();
        foreach (SteamAudio.SteamAudioGeometry sag in steamAudioGeometries)
        {
            geoEssentialCollection.Add(new TransformEssentials(sag.gameObject));
        }
        oCount = geoEssentialCollection.Count;
    }

    public string BuildJSON()
    {
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

public class SoundSource
{
    public TransformEssentials geo;
    public string audioFileName;
    public float start;
    public float end;

    public SoundSource()
    {
        GameObject sourceObject = Object.FindObjectOfType<SteamAudio.SteamAudioSource>().gameObject;
        geo = new TransformEssentials(sourceObject);
        AudioSource audioSource = sourceObject.GetComponent<AudioSource>();
        audioFileName = audioSource.clip.name;

        Soundbox soundbox = sourceObject.GetComponent<Soundbox>();
        start = soundbox.start_time;
        end = soundbox.end_time;
    }

    public string BuildJSON()
    {
        return JsonUtility.ToJson(this) + ",\n\"SoundLocation\": " + JsonUtility.ToJson(geo); 
    }
}

public class Recording
{
    public int number;
    public PosRot listenerLocation;
    public float[] samples;
    public long byteOffset;
    public bool saved;

    public Recording(int _num, GameObject listener, float[] _received)
    {
        number = _num;
        listenerLocation = new PosRot(listener);
        samples = _received;
        byteOffset = 0;
        saved = false;
    }

    public Recording(int _num, GameObject listener, float[] _received, long _off)
    {
        number = _num;
        listenerLocation = new PosRot(listener);
        samples = _received;
        byteOffset = _off;
        saved = false;
    }


    public string BuildJson()
    {
        string json = $"{{\"Number\": {number},\n\"Location\":" + JsonUtility.ToJson(listenerLocation) + $",\n\"Offset\":{byteOffset}}}";
        return json;
    }

    public void SaveBytes(string filename)
    {
        FileStream fileStream;
        if (byteOffset == 0)
        {
            fileStream = new FileStream(filename, FileMode.Create);
        } else
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

    public void NullSamples()
    {
        samples = null;
    }
    

}
