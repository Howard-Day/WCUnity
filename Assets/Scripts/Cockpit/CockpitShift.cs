﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CockpitShift : MonoBehaviour
{
    
	private float shake_decay = 0.3f;	
	private float temp_shake_intensity = 0;

    public float MaxYShift = 0.0f;
    public float MaxXShift = 0.0f;
    public Vector2 TargetShift;
    public float ShiftSmoothness = .2f;

    ShipSettings shipMain;
    Transform CockpitRoot;
    Vector2 RefShift;
    Vector2 SmoothShift;
    Vector3 SmoothShake;
    Vector3 smoothRef;
    // Start is called before the first frame update
    void Start()
    {
        shipMain = (ShipSettings)Camera.main.gameObject.GetComponentInParent<ShipSettings>();
        CockpitRoot = gameObject.transform;
    }
    // Update is called once per frame
    void Update()
    {
        TargetShift = new Vector2(Mathf.Clamp(shipMain.refTurn.y,-1f,1f),Mathf.Clamp(shipMain.refTurn.x,-1f,1f));
        SmoothShift = Vector2.SmoothDamp(SmoothShift,TargetShift,ref RefShift, ShiftSmoothness);
        CockpitRoot.localPosition = new Vector3(SmoothShift.x*MaxXShift,SmoothShift.y*MaxYShift, CockpitRoot.localPosition.z);

        if (temp_shake_intensity > 0){
            SmoothShake = Vector3.SmoothDamp(SmoothShake,new Vector3(Random.insideUnitSphere.x,Random.insideUnitSphere.y,0) * temp_shake_intensity,ref smoothRef,.075f); 
            transform.localPosition += SmoothShake;
            temp_shake_intensity -= shake_decay;
        }
        if(shipMain.isAfterburning)
        {
            AfterburnShake();
        }
    }
	/*void OnGUI (){
		if (GUI.Button (new Rect (20,40,80,20), "Shake")){
			AfterburnShake ();
		}
	}*/
	void AfterburnShake(){
		temp_shake_intensity = .05f;
        shake_decay = 0.01f;
	}
}

