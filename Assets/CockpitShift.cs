using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CockpitShift : MonoBehaviour
{
    public float MaxYShift = 0.0f;
    public float MaxXShift = 0.0f;
    public Vector2 TargetShift;
    public float ShiftSmoothness = .2f;

    Transform CockpitRoot;

    Vector2 RefShift;
    Vector2 SmoothShift;
    // Start is called before the first frame update
    void Start()
    {
        CockpitRoot = gameObject.transform;
    }

    // Update is called once per frame
    void Update()
    {
            SmoothShift = Vector2.SmoothDamp(SmoothShift,TargetShift,ref RefShift, ShiftSmoothness);
            CockpitRoot.localPosition = new Vector3(SmoothShift.x*MaxXShift,SmoothShift.y*MaxYShift, CockpitRoot.localPosition.z);
    }
}
