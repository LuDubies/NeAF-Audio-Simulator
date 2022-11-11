using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrequencyGenerator : MonoBehaviour
{
	// public sound generation parameters
	[Range(200, 1000)]
	public int frequency;
	[Range(0.1f, 1f)]
	public float amplitude;
	public bool on = true;

	//priate sound gerneration parameters
	private int samplingRate;

	private bool running = false;

	public void Awake()
	{
		running = true;
		samplingRate = AudioSettings.outputSampleRate;

    }

	private void OnAudioFilterRead(float[] data, int channels)
	{
		if (!running) { return; }

		int samples = data.Length / channels;

	}
}
