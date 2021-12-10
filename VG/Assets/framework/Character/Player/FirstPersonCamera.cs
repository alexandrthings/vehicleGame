using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstPersonCamera : MonoBehaviour
{
    public Transform target;

    public Vector3 offset;

    public float upLimit;
    public float downLimit;

    private float xRot;
    private float yRot;

    // Update is called once per frame
    void Update()
    {
        transform.position = target.position + offset;

        xRot = Mathf.Clamp(xRot - Input.GetAxis("Mouse Y"), downLimit, upLimit);
        yRot += Input.GetAxis("Mouse X");

        transform.eulerAngles = new Vector3(xRot, yRot, 0);
    }
}
