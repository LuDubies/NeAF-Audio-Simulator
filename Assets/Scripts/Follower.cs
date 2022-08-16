using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Follower : MonoBehaviour
{
    public GameObject knight;

    private float height = 15.0f;

    // Start is called before the first frame update
    void Start()
    {
        knight = GameObject.Find("Knighty");
    }

    // Update is called once per frame
    void Update()
    {
        if(knight == null)
        {
            Debug.Log("shityyyy");
        }

        transform.position = knight.transform.position + Vector3.up * height;

    }
}
