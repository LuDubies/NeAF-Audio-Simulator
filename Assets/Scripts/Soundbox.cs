using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Soundbox : MonoBehaviour
{
    AudioSource audioSource;
    [Range(0f, 9f)]
    public float start_time = 1.6f;
    [Range(0f, 10f)]
    public float end_time = 2.8f;

    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {   
        if(!audioSource.isPlaying)
        { 
            audioSource.time = start_time; 
        }
        if(Input.GetButtonDown("Jump") && !audioSource.isPlaying)
        {
            Debug.Log("Plying");
            audioSource.Play();
        }
        if(audioSource.isPlaying && audioSource.time >= end_time)
        {
            audioSource.Stop();
        }
    }
}
