using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrequencyGenerator : MonoBehaviour
{
	// public sound generation parameters
	[Range(200, 1000)]

	public int frequency = 440;
	[Range(0.0f, 1f)]
	public float amplitude = 0.1f;
	public bool on = true;
	public bool debugMode = false;

	//priate sound gerneration parameters
	private int samplingRate;
	private float sampleInPeriod = .0f;
	private float samplesPerPeriod;

	private bool running = false;

	public void Awake()
	{
		running = true;
		samplingRate = AudioSettings.outputSampleRate;
		if(debugMode) { Debug.Log($"Global sampling rate is {samplingRate}"); }
    }

	public void Update() {
		// recalculate samplesPerPeriod if frequency changed
		double old_spp = samplesPerPeriod;
		samplesPerPeriod = samplingRate / (float)frequency;
		if(debugMode && (old_spp != samplesPerPeriod))
		{
			Debug.Log($"Frequency changed to {frequency}. {samplesPerPeriod} samples per period.");
		}
	}

	private void OnAudioFilterRead(float[] data, int channels)
	{
		if (!running || !on) { return; }

		int samplesToFill = data.Length / channels;

		// calculate the amplitude for every sample
		for(int i = 0; i < samplesToFill; i++) {
			float phase = (sampleInPeriod + i) / samplesPerPeriod;
			float amp = amplitude * Mathf.Sin(2 * Mathf.PI * phase);
			for(int j = 0; j < channels; j++) {
				data[i * channels + j] = amp;
			}
		}

		// update sampleInPeriod
		sampleInPeriod = (sampleInPeriod + samplesToFill) % samplesPerPeriod;
	}

	public bool isRunning() {
		return running && on;
	}
}
