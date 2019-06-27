using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DampInitVelocity : MonoBehaviour
{
    public float DampVel;
    [HideInInspector] public Vector3 initDir;
    [HideInInspector] public float initVel;

    // Update is called once per frame
    void Update()
    {
        transform.position += initDir * initVel * DampVel * Time.deltaTime;        
    }
}
