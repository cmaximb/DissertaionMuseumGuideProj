using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCamera : MonoBehaviour
{
    public Transform cameraLocation;

    // Update is called once per frame
    void Update()
    {
        transform.position = cameraLocation.position;
    }
}
