using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurretSettings : MonoBehaviour
{
    public enum TEAM { CONFED, KILRATHI, NEUTRAL, PIRATE, ENV };
    [Header("Choose Team, Name, and filters")]
    [SerializeField] public TEAM AITeam = TEAM.CONFED;
    [SerializeField] public string DisplayName;
    [Header("Billboard")]
    [SerializeField] public GameObject Billboard;
    [Header("VDU Icon!")]
    [SerializeField] public Sprite VDUImage;
    [Header("Movement Settings")]
    [SerializeField] public float turnRate = 50f;
    [SerializeField] public float angleLimit = 60f;
    [SerializeField] public Vector3 initialVec = Vector3.back;
    [SerializeField] public Transform traverse;
    [SerializeField] public Transform elevation;
    [Header("Rotation Delta")]
    [SerializeField] public float deltaSmooth = .2f;
    [Header("Weapon Settings")]
    [SerializeField] public float capacitorSize = 50f;
    [SerializeField] float rechargeRate = 1f;
    [SerializeField] public ProjectileWeapon[] projWeapons;
    [Header("Health Settings")]
    [SerializeField] public float Armor;
    [Header("Death Effect")]
    [SerializeField] public GameObject[] DeathVFX;

    //Hidden Attributes
    [HideInInspector] public float _ArmorMax;
    [HideInInspector] public float CoreMax;
    [HideInInspector] Material billboardMat;
    [HideInInspector] ShipSettings shipMain;
    [HideInInspector] public float _CoreStrength;
    [HideInInspector] public int ShipID;
    [HideInInspector] public float targetSpeed;
    [HideInInspector] public float capacitorLevel;
    [HideInInspector] public bool isFiring = false;
    [HideInInspector] GameObjTracker Tracker;
    [HideInInspector] public bool isDead = false;
    [HideInInspector] public ShipSettings currentTarget;
    [HideInInspector] public bool currentLocked = false;
    [HideInInspector] public bool hitInternal = false;
    [HideInInspector] public Quaternion oldRot;
    [HideInInspector] public Vector3 rotDelta;
    [HideInInspector] public Quaternion initialRot;
    public Vector3 localRot;
    Pose lastTrans;

    // Start is called before the first frame update
    void Start()
    {
        oldRot = transform.localRotation;
        //Find the Ship we're attached to
        shipMain = gameObject.GetComponentInParent<ShipSettings>();
        //Get our ID
        GetId();
        //Atomic Batteries to power
        capacitorLevel = capacitorSize;
        //Power Weapons
        InitGuns();
        _ArmorMax = Armor; //Give us something to compare to later on
        _CoreStrength = Armor / 3; //Generalized fomula for the unarmored mechanical core of the turret
        CoreMax = _CoreStrength;
        //grab the display part of the billboard, for futher modification
        GetBillboardMat();
        
        //oldRot = Quaternion.Euler(transform.forward);
    }
    //Get the ShipId of the craft we're attached to
    public void GetId()
    {
        ShipID = shipMain.ShipID;
        while (ShipID == 0)
        {
            ShipID = shipMain.ShipID;
        }
    }
    //Get our Billboard Material
    public void GetBillboardMat()
    {
        if (Billboard != null)
        {
            billboardMat = Billboard.GetComponent<Renderer>().material;
        }
    }
    //Manage Targets
    void TargetManage()
    {
        if (currentTarget != null)
        {
            if (currentTarget.isLocked)
            {
                currentLocked = true;
            }
            else
            {
                currentLocked = false;
            }
        }
    }
    int countFireIndex = 0;
    int lastFireIndex = 0;
    //Find our Guns, Figure out what they are, sequence them and put them in a list! 
    void InitGuns()
    {
        projWeapons = GetComponentsInChildren<ProjectileWeapon>();
        foreach (ProjectileWeapon projWeapon in projWeapons)
        {
            //Init gun index
            if (projWeapon.index == 0)
            {
                projWeapon.index = countFireIndex;
                countFireIndex++;
            }
        }

    }
    //Fire Guns! 
    public void FireGuns(bool fire)
    {
        //loop through the guns
        foreach (ProjectileWeapon projWeapon in projWeapons)
        {
            if (capacitorLevel < projWeapon.powerDrain * (countFireIndex + 1))
            {
                if (shipMain.recover >= .99f && projWeapon.index != lastFireIndex) // Can the ship fire? Is this gun *not* the last to fire? Are we Cloaked? 
                {
                    projWeapon.fire = fire;
                    //increment through guns

                    // if(logDebug){print("aactually setting state to " + fire);}
                    //are we firing?
                    isFiring = fire; //Make sure our broadcast flag is set! 
                }
                if (shipMain.recover >= .99f && projWeapon.index == lastFireIndex) // Can the ship fire? Is this gun the last to fire? 
                {
                    projWeapon.fire = false;
                }
                if (shipMain.recover < .75f) //wait for recharge or return of control! 
                {
                    projWeapon.fire = false;
                    isFiring = false;
                }
            }
            else if (shipMain.recover >= .99f)
            {
                projWeapon.fire = fire;
                isFiring = fire;
            }

            if (fire == false)
            {
                projWeapon.fire = fire;
                isFiring = fire;
            }

            if (projWeapon.hasFired)
            {
                lastFireIndex = projWeapon.index;
            }
        }
    }
    //Handle Power Management
    void Power()
    {
        if (capacitorLevel < capacitorSize) //Charge Them Guns
        {
            capacitorLevel += rechargeRate * Time.deltaTime;
        }
    }
    //Helpful Utilities
    public static float GetSignedAngle(Quaternion A, Quaternion B, Vector3 axis)
    {
        float angle = 0f;
        Vector3 angleAxis = Vector3.zero;
        (A * Quaternion.Inverse(B)).ToAngleAxis(out angle, out angleAxis);
        if (Vector3.Angle(axis, angleAxis) > 90f)
        {
            angle = -angle;
        }
        return Mathf.DeltaAngle(0f, angle);
    }
    void DeltaRot()
    {
        Quaternion qLocal = Quaternion.Inverse(transform.rotation) * lastTrans.rotation;

        float newPitchDelta = (qLocal * Vector3.forward).y / Time.deltaTime;
        float newYawDelta = (qLocal * Vector3.right).z / Time.deltaTime;
        float newRollDelta = (qLocal * Vector3.up).x / Time.deltaTime;

        Vector3 rotDeltaRough = new Vector3(-newPitchDelta, -newYawDelta, newRollDelta);

        rotDelta = Vector3.Lerp(rotDelta, rotDeltaRough, deltaSmooth);

        lastTrans.position = transform.position;
        lastTrans.rotation = transform.rotation;
    }

    
   /* void LimitedAimAit(Vector3 target) //AutoAim!
    {
        //get the aim vector
        Vector3 lookDir = Vector3.Normalize(target - transform.position);
        //look at the aim vector
        Quaternion rotateTowards = Quaternion.LookRotation(lookDir, shipMain.gameObject.transform.up);
        //smoothly rotate towards the aim vector
        oldRot = Quaternion.RotateTowards(oldRot, rotateTowards, turnRate * Time.deltaTime);
        transform.rotation = oldRot;
        //limit the rotation to the turret bounds
        localRot = transform.localEulerAngles;
        //fix gimbal lock
        if (localRot.x > 360 + turnXLimit.x)
        {
            localRot.x -= 360;
        }
        if (localRot.x > 360 - turnXLimit.y)
        {
            localRot.x -= 360;
        }
        Vector3 shipRot = shipMain.gameObject.transform.localEulerAngles;
        if (localRot.x < turnXLimit.x)
        {
            transform.localEulerAngles = new Vector3(turnXLimit.x, localRot.y, localRot.z);
        }
        if (localRot.x > turnXLimit.y)
        {
            transform.localEulerAngles = new Vector3(turnXLimit.y, localRot.y, localRot.z);
        }
        
        if (localRot.y < turnYLimit.x)
        {
            transform.localEulerAngles = new Vector3(localRot.x, turnYLimit.x, localRot.z);
        }
        if (localRot.y > turnYLimit.y)
        {
            transform.localEulerAngles = new Vector3(localRot.x, turnYLimit.y, localRot.z);
        }

    }*/

    public void TryToAimAtTarget(Vector3 target)
    {
        if (target != null)
        {
            Vector3 relativePos = target - traverse.position;

            InterpolateTurretAim(relativePos);
        }
    }

    private void InterpolateTurretAim(Vector3 forward)
    {
        
        Quaternion baseRot = Quaternion.LookRotation(forward, shipMain.transform.up);
        //Set initial local rotation
        initialRot = shipMain.transform.rotation * oldRot;
        //clamp the rotation
        baseRot = ClampRotation(initialRot, baseRot, angleLimit);


        // first rotate completely towards target in world space
        traverse.rotation = Quaternion.Lerp(traverse.rotation, baseRot, Time.deltaTime * turnRate);


        // reset local roll & pitch for turret base
        traverse.localRotation = Quaternion.Euler(
            0,
            traverse.localRotation.eulerAngles.y,
            0
        );
        elevation.rotation = Quaternion.Lerp(elevation.rotation, baseRot, Time.deltaTime * turnRate);
        // reset local roll & yaw for turret cannon
        elevation.localRotation = Quaternion.Euler(
            elevation.localRotation.eulerAngles.x,
            0,
            0
        );
    }
    Quaternion ClampRotation(Quaternion baseRotation, Quaternion currentRotation, float maxAngle)
    {

        // Check if the current angle exceeds the limit
        var angle = Quaternion.Angle(baseRotation, currentRotation);
        if (angle <= maxAngle)
        {
            // Inside limit, return rotation unchanged
            return currentRotation;
        }

        // Clamp rotation by slerping from the base rotation to the limit,
        // keeping the direction of the current rotation.
        // Since slerp is uniform, the angle between the result and baseRotation will be (t * angle).
        return Quaternion.Slerp(baseRotation, currentRotation, maxAngle / angle);
    }



    // Update is called once per frame
    void Update()
    {
        //LimitedAimAit(Camera.main.transform.position);
        //TryToAimAtTarget(Camera.main.transform.position);
        if (projWeapons.Length == 0)
            InitGuns();
        Power();
        DeltaRot();

    }
}
