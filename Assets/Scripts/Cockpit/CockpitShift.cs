﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CockpitShift : MonoBehaviour
{

    private float shake_decay = 0.3f;
    private float temp_shake_intensity = 0;

    public int pixelDensity;
    public float MaxYShift = 0.0f;
    public float MaxXShift = 0.0f;
    public Vector2 TargetShift;
    public float ShiftSmoothness = .2f;

    ShipSettings shipMain;
    Transform CockpitRoot;
    Vector2 RefShift;
    [HideInInspector] public Vector2 SmoothShift;

    Vector3 SmoothShake;
    Vector3 smoothRef;

    Vector3 StartPos;

    Vector3 PrePixelLocked;
    Vector3 PixelLocked;
    [HideInInspector] public float xShift = 0;
    [HideInInspector] public float yShift = 0;

    // Start is called before the first frame update
    void Start()
    {
        shipMain = (ShipSettings)gameObject.GetComponentInParent<ShipSettings>();
        CockpitRoot = transform;

    }
    // Update is called once per frame
    void LateUpdate()
    {

        TargetShift = new Vector2(Mathf.Clamp(xShift - 1, -1f, 1f), -Mathf.Clamp(yShift - 1, -1f, 1f));
        SmoothShift = Vector2.SmoothDamp(SmoothShift, TargetShift, ref RefShift, ShiftSmoothness);
        PrePixelLocked = new Vector3(SmoothShift.x * MaxXShift, SmoothShift.y * MaxYShift, CockpitRoot.localPosition.z);

        if (temp_shake_intensity > 0)
        {
            SmoothShake = Vector3.SmoothDamp(SmoothShake, new Vector3(Random.insideUnitSphere.x, Random.insideUnitSphere.y, 0) * temp_shake_intensity, ref smoothRef, .125f);
            PrePixelLocked += SmoothShake;
            PrePixelLocked.x = Mathf.Clamp(PrePixelLocked.x, -1f, 1f);
            PrePixelLocked.y = Mathf.Clamp(PrePixelLocked.y, -1f, 1f);

            temp_shake_intensity -= shake_decay;
        }
        if (shipMain.isAfterburning)
        {
            AfterburnShake();
        }
        PixelLocked.x = (Mathf.Round(PrePixelLocked.x * pixelDensity) / pixelDensity);
        PixelLocked.y = (Mathf.Round(PrePixelLocked.y * pixelDensity) / pixelDensity);
        PixelLocked.z = CockpitRoot.localPosition.z;
        CockpitRoot.localPosition = PixelLocked;
    }
    /*void OnGUI (){
		if (GUI.Button (new Rect (20,40,80,20), "Shake")){
			AfterburnShake ();
		}
	}*/
    void AfterburnShake()
    {
        temp_shake_intensity = .1f;
        shake_decay = 0.01f;
    }
}
