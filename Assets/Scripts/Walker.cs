using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(AudioListener))]
public class Walker : MonoBehaviour
{

    // params
    public float speed = 15.0f;
    public float rot_speed = 3.0f;

    public int score = 0;
    public float respawn_time_counter = 0.0f;

    public GameObject pizza;
    public TextMeshProUGUI scoreboard;

    // Start is called before the first frame update
    void Start()
    {
        pizza = GameObject.Find("Pizza");
    }

    // Update is called once per frame
    void Update()
    {   
        if(!pizza.activeSelf)
        {
            float new_x = (float)Random.Range(150, 250);
            float new_z = (float)Random.Range(140, 200);
            pizza.transform.position = new Vector3(new_x, 0.33f, new_z);
            pizza.SetActive(true);

        }
        transform.Rotate(0, Input.GetAxis("Horizontal") * rot_speed * Time.deltaTime * 100, 0);
        transform.Translate(Vector3.forward * Input.GetAxis("Vertical") * speed * Time.deltaTime);

        if ((pizza.transform.position - transform.position).magnitude <= 1.0f)
        {
            score += 100;
            scoreboard.text = "Score: " + score;
            pizza.SetActive(false);
            Debug.Log(score);
        }
    }            
}
