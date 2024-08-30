using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnapRotation : MonoBehaviour
{
    public float angleDeltaToSnap;

    Transform source;
    Transform local;
    Transform cam;
    float shipDivergenceAngle;
    Vector3 lastGoodRotation;

    Vector3 cameraAngleToShip;
    Vector3 lastGoodAngleToShip;
    
    Quaternion cameraOffset;
    
    float cameraDivergenceAngle;

    // Start is called before the first frame update
    void Start()
    {
        source = GetComponentInParent<Transform>();
        local = transform;
        cam = Camera.main.transform;

        cameraAngleToShip = Vector3.Normalize(source.position - cam.position);
        lastGoodAngleToShip = cameraAngleToShip;

    }

    // Update is called once per frame
    void Update()
    {
        local = transform;
        cam = Camera.main.transform;
        cameraAngleToShip = Vector3.Normalize(source.position - cam.position);
        cameraDivergenceAngle = Vector3.Angle(cameraAngleToShip, lastGoodAngleToShip);

        //Vector3.RotateTowards(local.rotation.eulerAngles, Quaternion.LookRotation(Vector3.Normalize(cameraAngleToShip - lastGoodAngleToShip), cam.up).eulerAngles, 1000f, 1000f);
        //Quaternion.LookRotation((lastGoodAngleToShip - cameraAngleToShip), local.up);

        cameraOffset = //Quaternion.LookRotation(Vector3.RotateTowards(cameraAngleToShip, lastGoodAngleToShip, 1000,1000), cam.up);
        Quaternion.FromToRotation(cameraAngleToShip,lastGoodAngleToShip);
       // cameraOffset = cameraOffset * source.rotation;

        if (cameraDivergenceAngle > angleDeltaToSnap)
        {
            lastGoodAngleToShip = cameraAngleToShip;
        }        
        
        local.rotation = Quaternion.Inverse(cameraOffset);

    }
}
