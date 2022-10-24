using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CollisionTime : MonoBehaviour
{
    public float thrust = 5.0f;
    public Rigidbody rb;
    private Vector3 move= new Vector3(0, 10, 0);
    private int counter;
    private float timer;
    public Text Text_Time;
    public Text Score;
    public Text Game;
    private bool lose=true;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Text_Time.text = "Time:";
        Score.text = "Hits:";
        Game.text = "Space to Begin";
    }

    void OnCollisionEnter(Collision collision)
    {
        rb.AddForce(0, thrust, 0, ForceMode.Impulse);
        counter += 1;
        Debug.Log(counter);

    }

    void Update()
    {
        if (!lose)
        {
            timer += Time.deltaTime;
        }
        
        if (rb.position.y < -10)
        {
            lose = true;
        }
        Text_Time.text = "Time: "+timer.ToString("F2");
        Score.text = "Hits: "+counter;

        if (lose)
        {
            Game.text = "Ready to try again?";
            if (Input.GetKeyDown("space"))
            {
                rb.MovePosition(move);
                rb.AddForce(0, 10, 0, ForceMode.Impulse);
                lose = false;
                Game.text = "Begin!";
                timer = 0;
                counter = 0;
            }
        }
    }

}
