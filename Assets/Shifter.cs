using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shifter : MonoBehaviour
{
    ShipSettings shipMain;
    Vector3 RefShift;
    float refSpin;
    [HideInInspector] public Vector3 SmoothShift;
    [HideInInspector] public float SmoothSpin;
    public Vector2 TargetShift;
    public float ShiftSmoothness = .4f;
    public Vector2 ParallaxAmount = Vector2.zero;

    float smoothThrottle;
    float smoothBurn;
    Vector3 initialPos;

    // Start is called before the first frame update
    void Start()
    {
        shipMain = (ShipSettings)gameObject.GetComponentInParent<ShipSettings>();
        initialPos = transform.localPosition;
    }
    Vector3 SmoothedParallax;
    void DoParallax()
    {
        float xShift = shipMain.rotDelta.y;
        float yShift = shipMain.rotDelta.x;
        float zShift = shipMain.rotDelta.z;

        TargetShift = new Vector3(Mathf.Clamp(xShift, -1f, 1f), -Mathf.Clamp(yShift, -1f, 1f), Mathf.Clamp(zShift, -1f, 1f));
        SmoothShift = Vector3.SmoothDamp(SmoothShift, TargetShift, ref RefShift, ShiftSmoothness);

        Vector3 TargetParallax = new Vector3(SmoothShift.x * ParallaxAmount.x, SmoothShift.y * ParallaxAmount.y, 0f);
        SmoothedParallax = Vector3.Lerp(SmoothedParallax, TargetParallax, .2f);
        transform.localPosition = initialPos + SmoothedParallax;
    }

    // Update is called once per frame
    void Update()
    {
        DoParallax();
    }
}
