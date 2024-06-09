using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LockToCamera : MonoBehaviour
{
    public GameObject Override;
    // Update is called once per frame
    void LateUpdate()
    {
        if (Camera.main && !Override)
        {
            transform.position = Camera.main.transform.position;
        }
        if (Override)
        {
            transform.position = Override.transform.position;
        }
    }
}
