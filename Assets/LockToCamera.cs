using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LockToCamera : MonoBehaviour
{
    // Update is called once per frame
    void LateUpdate()
    {
        if(Camera.main)
        {
            transform.position = Camera.main.transform.position;
        }
    }
}
