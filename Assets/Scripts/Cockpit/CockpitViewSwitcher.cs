using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CockpitViewSwitcher : MonoBehaviour
{
    ShipSettings shipMain;
    public bool RandomSwitch = false; 
    
    public Vector3 RearCamOffset;
    public float RearAngle;
    Transform MainView;
    public Transform RightView;
    public Transform LeftView;
    public Transform RearView;
    public GameObject CockpitBase;
    public GameObject RightBase;
    public GameObject LeftBase;
    public GameObject RearBase;
    public Vector3 DriftAmts = Vector3.zero;
    public float ShiftSmoothness = .2f;


    
    [HideInInspector] public bool isExternal = false;
    [HideInInspector] public bool isCockpitForward = true;

    Vector3 preCollideView;
    Vector3 recoverView;

    [HideInInspector] public Vector2 SmoothShift;
    [HideInInspector] public float SmoothSpin;
    Vector2 RefShift;
    public Vector2 TargetShift;
    
    float refSpin;

    // Start is called before the first frame update
    void Start()
    {
        MainView = transform;
        shipMain = (ShipSettings)gameObject.GetComponentInParent<ShipSettings>();
    }
    void DoChaseCam()
    {

        if(CockpitBase.activeInHierarchy)
        {
        CockpitBase.active = false;
        transform.localPosition = RearCamOffset;
        transform.localEulerAngles = new Vector3(RearAngle,0,0);
        }
        print(shipMain.deltaRot);
        
        float xShift =  shipMain.deltaRot.y;
        float yShift =  shipMain.deltaRot.x;
        
        //print (xShift);
        TargetShift = new Vector2(Mathf.Clamp(xShift,-1f,1f),-Mathf.Clamp(yShift,-1f,1f));
        
        SmoothShift = Vector2.SmoothDamp(SmoothShift,TargetShift,ref RefShift, ShiftSmoothness);
        
        if(shipMain.speed > shipMain.topSpeed)
        {
            SmoothShift += new Vector2(Random.Range(-1f,1f),Random.Range(-1f,1f))*.025f;
        }

        float zShift =  shipMain.deltaRot.z;
        float TargetSpin = Mathf.Clamp(zShift,-1f,1f);
        SmoothSpin = Mathf.SmoothDamp(SmoothSpin,TargetSpin,ref refSpin,ShiftSmoothness);

        transform.localEulerAngles = new Vector3(RearAngle+SmoothShift.x*DriftAmts.x,SmoothShift.y*DriftAmts.y,SmoothSpin*DriftAmts.z);
        transform.localPosition = RearCamOffset+(-Vector3.right*SmoothShift.x*10);
        recoverView = transform.forward;

       /* if(shipMain.recover >= 1)
        {
            preCollideView = transform.forward*555;   
        }
        else
        {
            transform.LookAt(Vector3.Slerp(preCollideView, recoverView, 1-shipMain.recover));
        }*/
    }

    // Update is called once per frame
    void LateUpdate()
    {
         if(RandomSwitch)
        {     DoChaseCam();
        }
    }
}
