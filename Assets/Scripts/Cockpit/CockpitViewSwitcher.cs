using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CockpitViewSwitcher : MonoBehaviour
{
    ShipSettings shipMain;
    GameObject Billboard;
    CockpitShift Shifter;
    public bool RandomSwitch = false; 
    public bool ChaseSwitch = false; 
    public enum View {Main, Right, Left, Rear, Chase};//, Cinematic, Missile};
    public View activeView = View.Main;
    public Vector3 ChaseCamOffset;
    public float ChaseAngle;
    Vector3 MainPos;
    Vector3 MainRot;

    public Transform RightView;
    public Transform LeftView;
    public Transform RearView;
    public GameObject CockpitBase;
    public GameObject RightBase;
    public GameObject LeftBase;
    public GameObject RearBase;
    public GameObject HoverUI;
    public Vector3 DriftAmts = Vector3.zero;
    public float ShiftSmoothness = .2f;


    
    [HideInInspector] public bool isExternal = false;
    [HideInInspector] public bool isCockpitForward = true;

    Vector3 preCollideView;
    Vector3 recoverView;

    [HideInInspector] public Vector2 SmoothShift;
    [HideInInspector] public float SmoothSpin;
    Vector2 RefShift;
    [HideInInspector] public Vector2 TargetShift;
    
    float refSpin;

    // Start is called before the first frame update
    void Start()
    {
        MainPos = transform.localPosition;
        MainRot = transform.localEulerAngles;
        Billboard = gameObject.GetComponentInParent<SpritePicker>().billboard.gameObject;
        Shifter = gameObject.transform.parent.GetComponentInChildren<CockpitShift>();
        activeView = View.Main;
        shipMain = (ShipSettings)gameObject.GetComponentInParent<ShipSettings>();
        CockpitBase.SetActive(false);
        RightBase.SetActive(false);
        LeftBase.SetActive(false);
        RearBase.SetActive(false);
        Billboard.SetActive(false);
    }
    void DoChaseCam()
    {
        if(!Billboard.activeInHierarchy)
        {
        HoverUI.SetActive(true);
        Billboard.SetActive(true);
        transform.localPosition = ChaseCamOffset;
        transform.localEulerAngles = new Vector3(ChaseAngle,0,0);
        CockpitBase.SetActive(true);
        RightBase.SetActive(true);
        LeftBase.SetActive(true);
        RearBase.SetActive(true);
        }
        //print(shipMain.deltaRot);
        
        float xShift =  -shipMain.deltaRot.y;
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

        transform.localEulerAngles = new Vector3(ChaseAngle+SmoothShift.x*DriftAmts.x,SmoothShift.y*DriftAmts.y,SmoothSpin*DriftAmts.z);
        transform.localPosition = ChaseCamOffset+(-Vector3.right*SmoothShift.x*10);
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

    void DoMainCam()
    {
        if(!CockpitBase.activeInHierarchy)
        {
        transform.localPosition = MainPos;
        transform.localEulerAngles = MainRot;
        CockpitBase.SetActive(true);
        RightBase.SetActive(false);
        LeftBase.SetActive(false);
        RearBase.SetActive(false);
        Billboard.SetActive(false);
        HoverUI.SetActive(true);
        }
        Shifter.xShift =  1-shipMain.deltaRot.y;// .y;
        Shifter.yShift =  1-shipMain.deltaRot.x;//.x;
    }

    void DoRearCam()
    {
        if(!RearBase.activeInHierarchy)
        {
        transform.localPosition = RearView.localPosition;
        transform.localEulerAngles = RearView.transform.localEulerAngles;
        CockpitBase.SetActive(false);
        RightBase.SetActive(false);
        LeftBase.SetActive(false);
        RearBase.SetActive(true);
        Billboard.SetActive(false);
        HoverUI.SetActive(false);
        }
        Shifter.xShift =  1-shipMain.deltaRot.y;// .y;
        Shifter.yShift =  shipMain.deltaRot.x;//.x;

    }
    void DoRightCam()
    {
        if(!RightBase.activeInHierarchy)
        {
        transform.localPosition = RightView.localPosition;
        transform.localEulerAngles = RightView.transform.localEulerAngles;
        CockpitBase.SetActive(false);
        RightBase.SetActive(true);
        LeftBase.SetActive(false);
        RearBase.SetActive(false);
        Billboard.SetActive(false);
        HoverUI.SetActive(false);
        }
        Shifter.xShift =  1-shipMain.deltaRot.y;// .y;
        Shifter.yShift =  1-shipMain.deltaRot.z;//.x;
    }

    void DoLeftCam()
    {
        if(!LeftBase.activeInHierarchy)
        {
                         
        transform.localPosition = LeftView.localPosition;
        transform.localEulerAngles = LeftView.transform.localEulerAngles;
        CockpitBase.SetActive(false);
        RightBase.SetActive(false);
        LeftBase.SetActive(true);
        RearBase.SetActive(false);
        Billboard.SetActive(false);
        HoverUI.SetActive(false);
        }
        Shifter.xShift =  shipMain.deltaRot.y;// .y;
        Shifter.yShift =  shipMain.deltaRot.z;//.x;
    }
    // Update is called once per frame
void LateUpdate()
{
    if(RandomSwitch)
    {     
        if(GameObjTracker.frames % Random.Range(240,360) == 0)
        {
            activeView = (View)Random.Range(0, System.Enum.GetValues(typeof(View)).Length);
        }
    }
    if(ChaseSwitch)
    {
        if(GameObjTracker.frames % Random.Range(480,360) == 0)
        {
            if (activeView == View.Main)
            {activeView = View.Chase;}
            else{ activeView =View.Main;}
        }
    }
    switch (activeView)
    {
        case View.Main:
        {
            DoMainCam();
        }
        break;
        case View.Chase:
        {
            DoChaseCam();
        }
        break;
        case View.Rear:
        {
            DoRearCam();
        }
        break;
        case View.Right:
        {
            DoRightCam();
        }
        break;
        case View.Left:
        {
            DoLeftCam();
        }
        break;
    }
  }
}
