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
    public bool ChaseCam = false;
    public float DelaySwitchTime = 3f;
    public enum View { Main, Right, Left, Rear, Chase };//, Cinematic, Missile};
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
    public FrameController SpaceflightFrame;
    public GameObject CinematicFrame;
    public Vector3 DriftAmts = Vector3.zero;
    public float ShiftSmoothness = .2f;
    public bool deathCam = false;
    public float deathCamTime = 6f;
    public float deathCamDelay = 1f;
    public float deathCamActiveRange = 100f;

    [HideInInspector] public bool isExternal = false;
    [HideInInspector] public bool isCockpitForward = true;

    Vector3 preCollideView;
    Vector3 recoverView;

    [HideInInspector] public Vector2 SmoothShift;
    [HideInInspector] public float SmoothSpin;
    Vector2 RefShift;
    [HideInInspector] public Vector2 TargetShift;

    float refSpin;
    HUDRoot hud;
    float deathTime = 0f;
    Vector3 preDeathAngle = Vector3.zero;
    Vector3 smoothedInterestAngle;
    Vector3 refAngleVel;
    Vector3 chaseCamPos;
    Quaternion chaseCamRot;

    Vector3 interestPoint;
    Vector3 interestAngle;
    bool deathCamActive = false;
    // Start is called before the first frame update
    void Start()
    {
        MainPos = transform.localPosition;
        MainRot = transform.localEulerAngles;
        Billboard = gameObject.GetComponentInParent<SpritePicker>().billboard.gameObject;
        Shifter = gameObject.transform.parent.GetComponentInChildren<CockpitShift>();
        activeView = View.Main;
        shipMain = (ShipSettings)gameObject.GetComponentInParent<ShipSettings>();
        CockpitBase.SetActive(true);
        RightBase.SetActive(false);
        LeftBase.SetActive(false);
        RearBase.SetActive(false);
        Billboard.SetActive(false);
        hud = HoverUI.GetComponent<HUDRoot>();
        //Make our ship sounds more muted to start
        shipMain.MinMaxThrottlePitch *= .25f;
        shipMain.MinMaxThrottleVolume *= .25f;
        shipMain.AfterburnPitch *= .25f;
        shipMain.AfterburnVolume *= .25f;
    }
    void DoChaseCamAngles()
    {
        if (!deathCam)
        {
            chaseCamPos = transform.position;
            chaseCamRot = transform.rotation;
            chaseCamRot *= Quaternion.Euler(Vector3.right * ChaseAngle);
            chaseCamPos += chaseCamRot * ChaseCamOffset;
        }   
    }
    void DoChaseCam()
    {
        if (Billboard && !Billboard.activeInHierarchy)
        {
            HoverUI.SetActive(true);
            Billboard.SetActive(true);
            transform.localPosition = ChaseCamOffset;
            transform.localEulerAngles = new Vector3(ChaseAngle, 0, 0);
            CockpitBase.SetActive(false);
            RightBase.SetActive(false);
            LeftBase.SetActive(false);
            RearBase.SetActive(false);
            //Turn on the Spaceflight cinematic Frame!
            SpaceflightFrame.FrameActive = true;
            //Make our ship sounds default when switched to external cam
            shipMain.MinMaxThrottlePitch *= 4f;
            shipMain.MinMaxThrottleVolume *= 4f;
            shipMain.AfterburnPitch *= 4f;
            shipMain.AfterburnVolume *= 4f;
        }
        //disable hover UI if there's no billboard (IE, we've been destroyed)
        if (Billboard == null)
        {
            HoverUI.SetActive(false);
            transform.localPosition = ChaseCamOffset;
            transform.localEulerAngles = new Vector3(ChaseAngle, 0, 0);
            CockpitBase.SetActive(false);
            RightBase.SetActive(false);
            LeftBase.SetActive(false);
            RearBase.SetActive(false);
        }
        //print(shipMain.deltaRot);

        float xShift = -shipMain.rotDelta.y;
        float yShift = shipMain.rotDelta.x;

        //print (xShift);
        TargetShift = new Vector2(Mathf.Clamp(xShift, -1f, 1f), -Mathf.Clamp(yShift, -1f, 1f));
        if (shipMain.speed > shipMain.topSpeed)
        {
            TargetShift += new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * .025f;
        }

        SmoothShift = Vector2.SmoothDamp(SmoothShift, TargetShift, ref RefShift, ShiftSmoothness);
        float zShift = shipMain.rotDelta.z;
        float TargetSpin = Mathf.Clamp(zShift, -1f, 1f);
        SmoothSpin = Mathf.SmoothDamp(SmoothSpin, TargetSpin, ref refSpin, ShiftSmoothness);

        transform.localEulerAngles = new Vector3(ChaseAngle + SmoothShift.x * DriftAmts.x, SmoothShift.y * DriftAmts.y, SmoothSpin * DriftAmts.z);
        transform.localPosition = ChaseCamOffset + (-Vector3.right * SmoothShift.x * 10);
        recoverView = transform.forward;

        hud.reticle.chaseCamMode = true;

        if (deathCam)
        {  
            //Grab an interest angle to look at, and set the transform to the last chaseCam. Then set up a damping velocity controller
            if (!deathCamActive)
            {
                interestPoint = GameObjTracker.GetAverageShipLocInRange(transform.position, deathCamActiveRange, shipMain.ShipID);
                interestAngle = interestPoint - transform.position;
                transform.position = chaseCamPos;
                transform.rotation = chaseCamRot;
                preDeathAngle = transform.forward;
                DampInitVelocity dampVel = gameObject.AddComponent<DampInitVelocity>();
                dampVel.initDir = shipMain.DeathDir;
                dampVel.initVel = shipMain.DeathVel;
                dampVel.DampVel = .9f;
                deathCamActive = true;
            }
            smoothedInterestAngle = Vector3.SmoothDamp(smoothedInterestAngle, interestAngle, ref refAngleVel, .05f);
            float normalizedDeathTime = Mathf.Clamp01((deathTime - deathCamDelay) / deathCamTime);
            float smoothDeathTime = Mathf.SmoothStep(0,1,normalizedDeathTime);
            Vector3 smoothRotateToInterest = Vector3.Slerp(preDeathAngle, smoothedInterestAngle, smoothDeathTime);// Vector3.SmoothDamp(preDeathAngle, interestPoint, ref refAngleVel, .1f);
            transform.rotation = Quaternion.LookRotation(smoothRotateToInterest,Vector3.up);
            if(deathTime >= deathCamTime)
            {
                GameObjTracker.playerNeedsRespawn = true;
            }
            deathTime += Time.deltaTime;
        }
        else {
            deathTime = 0f;
            preDeathAngle = transform.forward;
        }

    }

    void DoMainCam()
    {
        if (!CockpitBase.activeInHierarchy)
        {
            transform.localPosition = MainPos;
            transform.localEulerAngles = MainRot;
            CockpitBase.SetActive(true);
            RightBase.SetActive(false);
            LeftBase.SetActive(false);
            RearBase.SetActive(false);
            Billboard.SetActive(false);
            HoverUI.SetActive(true);
            //Turn off the Spaceflight cinematic Frame!
            SpaceflightFrame.FrameActive = false;
            //Make our ship sounds more muted when switched to internal view
            shipMain.MinMaxThrottlePitch *= .25f;
            shipMain.MinMaxThrottleVolume *= .25f;
            shipMain.AfterburnPitch *= .25f;
            shipMain.AfterburnVolume *= .25f;
        }
        Shifter.xShift = 1 - shipMain.rotDelta.y;// .y;
        Shifter.yShift = 1 - shipMain.rotDelta.x;//.x;
        hud.reticle.chaseCamMode = false;
    }

    void DoRearCam()
    {
        if (!RearBase.activeInHierarchy)
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
        Shifter.xShift = 1 - shipMain.rotDelta.y;// .y;
        Shifter.yShift = shipMain.rotDelta.x;//.x;
        hud.reticle.chaseCamMode = false;
    }
    void DoRightCam()
    {
        if (!RightBase.activeInHierarchy)
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
        Shifter.xShift = 1 - shipMain.rotDelta.y;//.y;
        Shifter.yShift = 1 - shipMain.rotDelta.z;//.x;
        hud.reticle.chaseCamMode = false;
    }

    void DoLeftCam()
    {
        if (!LeftBase.activeInHierarchy)
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
        Shifter.xShift = shipMain.rotDelta.y;// .y;
        Shifter.yShift = shipMain.rotDelta.z;//.x;
        hud.reticle.chaseCamMode = false;
    }
    // Update is called once per frame


    float switchTime;
    void LateUpdate()
    {
        DoChaseCamAngles();
        switchTime += Time.deltaTime;

        if (RandomSwitch)
        {
            if (GameObjTracker.frames % Random.Range(120, 180) == 0)
            {
                activeView = (View)Random.Range(0, System.Enum.GetValues(typeof(View)).Length);
            }
        }
        if (ChaseSwitch)
        {
            if (GameObjTracker.frames % Random.Range(120, 180) == 0 && switchTime > DelaySwitchTime)
            {
                if (activeView == View.Main)
                {
                    activeView = View.Chase;
                }
                else
                {
                    activeView = View.Main;
                }
                switchTime = 0f;
            }
        }
        if (ChaseCam)
        {
            if (activeView == View.Main)
            {
                activeView = View.Chase;
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
