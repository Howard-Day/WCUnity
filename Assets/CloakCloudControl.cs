using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloakCloudControl : MonoBehaviour
{
    ShipSettings shipMain;
    Material Clouds;
    Material CloudsDistort;
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
    float smoothedCloakAmount = 0f;
    float throttleAnim;
    // Start is called before the first frame update
    void Start()
    {
        shipMain = (ShipSettings)gameObject.GetComponentInParent<ShipSettings>();
        initialPos = transform.localPosition;
        Clouds = gameObject.GetComponent<MeshRenderer>().materials[0];
        CloudsDistort = gameObject.GetComponent<MeshRenderer>().materials[1];
    }
    float refSteerX;
    float refSteerY;
    float refSteerZ;
    Vector3 SmoothedParallax;
    void DoParallax()
    {
        Vector3 TargetParallax = new Vector3(refSteerX * ParallaxAmount.x, refSteerY * ParallaxAmount.y, 0f);
        SmoothedParallax = Vector3.Lerp(SmoothedParallax, TargetParallax, .2f);
        transform.localPosition = initialPos + SmoothedParallax;
    }
    void DoCloudAnimation()
    {
        if (!shipMain)
        {
            shipMain = gameObject.GetComponent<CockpitViewSwitcher>().shipMain;
        }

        float xShift = shipMain.rotDelta.y;
        float yShift = shipMain.rotDelta.x;
        float zShift = shipMain.rotDelta.z;

        //print (xShift);
        TargetShift = new Vector3(Mathf.Clamp(xShift, -1f, 1f), -Mathf.Clamp(yShift, -1f, 1f), Mathf.Clamp(zShift, -1f, 1f));
        SmoothShift = Vector3.SmoothDamp(SmoothShift, TargetShift, ref RefShift, ShiftSmoothness);

        refSteerX = (SmoothShift.x + 1) / 2;
        refSteerY = (SmoothShift.y + 1) / 2;
        refSteerZ = Mathf.Clamp01((Mathf.Lerp(refSteerZ, zShift, .5f) + 1) / 2);

        float steerX = 1 - Mathf.Clamp01(refSteerX);
        float steerY = 1 - Mathf.Clamp01(refSteerY);
        float steerZ = refSteerZ - .5f;

        throttleAnim += shipMain.throttle/100f;

        smoothedCloakAmount = Mathf.Lerp(smoothedCloakAmount, shipMain.cloakedAmount, .125f) * 1.05f; 

        float clampedCloakAmount = Mathf.Clamp01(smoothedCloakAmount);

        Clouds.SetFloat("_Throttle", throttleAnim);
        Clouds.SetFloat("_Roll", refSteerZ);

        CloudsDistort.SetFloat("_Throttle", throttleAnim);
        CloudsDistort.SetFloat("_Roll", refSteerZ);

        float steppedCloakAmount = Mathf.FloorToInt(clampedCloakAmount * 8f) / 8f;
        Clouds.SetFloat("_CloakBlend", steppedCloakAmount);
        CloudsDistort.SetFloat("_CloakBlend", steppedCloakAmount);
    }
    // Update is called once per frame
    void Update()
    {
        DoCloudAnimation();
        DoParallax();
    }

}
