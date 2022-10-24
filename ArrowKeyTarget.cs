using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrowKeyTarget : MonoBehaviour
{
    private Vector3 Displacement;
    public Transform LeftShoulder;
    public Transform RightShoulder;
    public Transform LeftTarget;
    public Transform RightTarget;
    private Transform Shoulder;
    private Transform Target;
    private bool Right = false;
    private float ArmLength=8f;
   // public float bounce=0f;


    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown("space"))
        {
            print("Switch!");
            Right = !Right;
        }

        Vector2 playerInput;
        playerInput.x = Input.GetAxis("Horizontal");
        playerInput.y = Input.GetAxis("Vertical");
        Displacement = new Vector3(0f, playerInput.y, playerInput.x);


        if (Right)
        {
            Vector3 newPosition = RightTarget.localPosition + Displacement;
            RightTarget.localPosition = newPosition;

            Vector3 direction = newPosition - RightShoulder.localPosition;
            float distance = (direction).sqrMagnitude;
            if (distance > ArmLength * ArmLength)
            {
                RightTarget.localPosition -= Displacement; //returns to previous position
                RightTarget.localPosition -= direction * .05f;
            }
        }
        else
        {
            Vector3 newPosition = LeftTarget.localPosition + Displacement;
            LeftTarget.localPosition = newPosition;

            Vector3 direction = newPosition - LeftShoulder.localPosition;
            float distance = (direction).sqrMagnitude;
            if (distance > ArmLength * ArmLength)
            {
                LeftTarget.localPosition -= Displacement; //returns to previous position
                LeftTarget.localPosition -= direction * .05f;
            }
        }
    }
}
