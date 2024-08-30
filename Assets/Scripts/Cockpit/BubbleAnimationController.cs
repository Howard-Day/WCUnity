using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BubbleAnimationController : MonoBehaviour
{
    ShipSettings shipMain;
    Material mat;
    Vector4 motionVecs;
    // Start is called before the first frame update
    void Start()
    {
        shipMain = GetComponentInParent<ShipSettings>();
        mat = GetComponent<MeshRenderer>().sharedMaterial;

    }

    // Update is called once per frame
    void Update()
    {
        float xShift = shipMain.rotDelta.y;
        float yShift = shipMain.rotDelta.x;
        float zShift = shipMain.rotDelta.z;

        //print (xShift);
        Vector4 targetVecs = new Vector4(Mathf.Clamp(xShift, -1f, 1f), -Mathf.Clamp(yShift, -1f, 1f), Mathf.Clamp(zShift, -1f, 1f),0);
        motionVecs = Vector4.Lerp(motionVecs, targetVecs, .5f);

        //Vector4 motionVecs = new Vector4(ship.pitch,ship.yaw,ship.roll, 0f);

        mat.SetVector("_MotionVectors", motionVecs);

    }
}
