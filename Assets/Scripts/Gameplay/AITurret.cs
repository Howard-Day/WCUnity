using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AITurret : MonoBehaviour
{
    //public enum AILevel { CHUMP, NOVICE, DEFAULT, SKILLED, ACE, MASTER };
    public AIPlayer.AILevel AISkillLevel = AIPlayer.AILevel.CHUMP;
    public bool autoSkillLevel = true;

    [Header("Debug Options")]
    public bool logDebug = false;

    [HideInInspector] public Transform AITarget;
    [HideInInspector] public GameObjTracker Tracker; 
    [HideInInspector] public ShipSettings WingmanTo;

    ShipSettings shipMain;
    AIPlayer AIPilot;
    HumanPlayer pilot;
    TurretSettings turret;
    Transform elevation;
    ShipSettings AITargetShip;

    float cooldownWait;
    bool cooldownWaiting = false;
    float averageGunSpeed = 0f;
    float engageDist = 150f;

    //Combat modifiers 
    float leadAMount = 1f;
    float rotationSpeed = 1f;
    float aimAccuracyAngle = 15f;
    int scanNewTargetFreq;
    bool alwaysTargetPrimary;


    Vector3 currentTargetPos;

    // Start is called before the first frame update
    void Start()
    {

        shipMain = GetComponentInParent<ShipSettings>();
        AIPilot = GetComponentInParent<AIPlayer>();
        pilot = GetComponentInParent<HumanPlayer>();
        turret = GetComponent<TurretSettings>();
        elevation = transform.FindRecursive("Elevation");
        DoSkillLevels();
    }


    //Set up skill levels
    void DoSkillLevels()
    {
        //if we're set to auto-match skill levels, do so
        if (autoSkillLevel && AIPilot != null)
        {
            AISkillLevel = AIPilot.AISkillLevel;
        }
        //If we're on a human piloted ship, auto set skill level! 
        if (autoSkillLevel && pilot != null)
        {
            AISkillLevel = AIPlayer.AILevel.ACE;
        }

        switch (AISkillLevel)
        {
            case (AIPlayer.AILevel.CHUMP):
                {
                    leadAMount = .5f;
                    rotationSpeed = .5f;
                    scanNewTargetFreq = 120;
                    aimAccuracyAngle = 15f;
                    alwaysTargetPrimary = false;
                }
                break;
            case (AIPlayer.AILevel.NOVICE):
                {
                    leadAMount = .75f;
                    rotationSpeed = .85f;
                    scanNewTargetFreq = 90;
                    aimAccuracyAngle = 10f;
                    alwaysTargetPrimary = false;
                }
                break;
            case (AIPlayer.AILevel.DEFAULT):
                {
                    leadAMount = .9f;
                    rotationSpeed = .9f;
                    scanNewTargetFreq = 60;
                    aimAccuracyAngle = 8f;
                    alwaysTargetPrimary = false;
                }
                break;
            case (AIPlayer.AILevel.SKILLED):
                {
                    leadAMount = .95f;
                    rotationSpeed = 1f;
                    scanNewTargetFreq = 50;
                    aimAccuracyAngle = 5f;
                    alwaysTargetPrimary = false;
                }
                break;
            case (AIPlayer.AILevel.ACE):
                {
                    leadAMount = 1f;
                    rotationSpeed = 1.25f;
                    scanNewTargetFreq = 20;
                    aimAccuracyAngle = 3f;
                    alwaysTargetPrimary = false;
                }
                break;
            case (AIPlayer.AILevel.MASTER):
                {
                    leadAMount = 1f;
                    rotationSpeed = 1.35f;
                    scanNewTargetFreq = 10;
                    aimAccuracyAngle = 4f;
                    alwaysTargetPrimary = false;
                }
                break;

        }
        //apply rotation modifiers 
        turret.turnRate *= rotationSpeed;
    }

    //Find our average attached gun speed for leading targets
    void DoGunSpeed()
    {
        //loop through our guns, if they're initialized, and we haven't figured this out yet
        if (averageGunSpeed == 0 || averageGunSpeed == float.NaN)
        {
            float tempGunSpeed = 0f;
            //loop through our guns, and add all their speeds together
            if (logDebug) { print("the number of found weapons is " + turret.projWeapons.Length); }
            foreach (ProjectileWeapon gun in turret.projWeapons)
            {
                tempGunSpeed += gun.speed;
            }
            //return the cumulative gunspeeds by the number of guns, set the value so this only runs once. Modify by skill level  
            averageGunSpeed = tempGunSpeed / turret.projWeapons.Length * leadAMount;
        }
    }

    //Handle Gun Cooldown wait
    void DoGunCooldown(float waitTime, float minCapacitorLevel)
    {
        float normalizedCapacitorLevel = turret.capacitorLevel / turret.capacitorSize;
        // if the capacitors are low, add wait time
        if (turret.capacitorLevel < .1f && !cooldownWaiting)
        {
            cooldownWait += Time.deltaTime * 10;
        }
        //if the wait time has triggered, go into cooldown mode
        if (cooldownWait > waitTime && !cooldownWaiting)
        {
            cooldownWaiting = true;
        }
        //cooldown mode, disable firing till the capacitors are to a minimum level
        if (cooldownWaiting)
        {
            turret.FireGuns(false);
            if (normalizedCapacitorLevel >= minCapacitorLevel)
            {
                cooldownWait = 0;
                cooldownWaiting = false;
            }
        }
    }
    //Utility to find the nearest ship, ignoring one of the Teams, any cloaked ships, and the Ship looking
    public ShipSettings FindNearestShip(Transform toObj,float angle, ShipSettings.TEAM ignoreTEAM)
    {
        float distance = engageDist * 10f;

        ShipSettings nearestShip = null;
        foreach (ShipSettings shipTest in GameObjTracker.Ships)
        {
            if (shipTest == null) //SOMEONE MUSTA DIED...  pick a new target!
            {
                GameObjTracker.RegisterAllShips();
                GameObjTracker.RegisterTeams();
            }
            if (shipTest != null && !shipTest.isCloaked)
            {
                Transform shipTrans = (Transform)shipTest.gameObject.GetComponent<Transform>();

                float shipDist = Vector3.Distance(shipTrans.position, toObj.position);
                Vector3 shipVec = Vector3.Normalize(shipTrans.position - toObj.position);
                float shipAngle = Vector3.Angle(shipVec, transform.forward);

                if (shipTest.AITeam != ShipSettings.TEAM.NEUTRAL && shipTest != shipMain && shipAngle <= angle)
                {
                    if (shipDist < distance && shipTest.AITeam != ignoreTEAM)
                    {
                        distance = shipDist;
                        nearestShip = shipTest;
                    }
                }
            }
        }
        if (nearestShip != null)
            return nearestShip;
        else
            return null;
    }
    //Utility to Get a ship by ID
    public ShipSettings FindShipByID(int id, ShipSettings.TEAM team)
    {

        ShipSettings foundShip = GameObjTracker.GetShipByID(id);
        if (foundShip != null && foundShip.AITeam != team)
        {
            return foundShip;
        }
        else
        {
            return null;
        }
    }
    //Utility to do a Simple Distance Calc
    public float DistanceTo(GameObject obj)
    {
        float dist = Vector3.Distance(obj.transform.position, transform.position);
        return dist;
    }
    //Handy thing -since the cockpits can have offset pitches to line up the reticles, we need to adust our forward angle if it's a player ship.
    public float AngleTo(Vector3 target)
    {
        if (target == null)
            return 0f;
        Vector3 tempForward;
        tempForward = transform.forward;
        return Vector3.Angle(tempForward, target - transform.position);
    }
    //Custom Angle-to-Target test
    public float CustomAngleTo(Vector3 testVec, Vector3 target)
    {
        if (target == null)
            return 0f;
        return Vector3.Angle(testVec, target - transform.position);
    }
    //Handle angle to Aim at
    public Vector3 DoAim(float aimRand)
    {
        return AITarget.position + (Random.onUnitSphere * aimRand);
    }
    // Handy tool to predict where we need to Aim at our target! 
    public Vector3 PredictV3Pos(Vector3 muzzlePos, float bulletVelocity, Vector3 targetPos, Vector3 targetVelocity)
    {
        float dist = Vector3.Distance(muzzlePos, targetPos);
        float timeToTarget = dist / bulletVelocity;
        Vector3 aimAt = targetPos + (targetVelocity * timeToTarget * leadAMount);

        return aimAt;
    }
    //Handle no targets
    void DoNoTargets()
    {
        //release our target
        if (AITarget == null)
        {
            AITargetShip = null;
        }
        //reset aiming, to the rear if it's a center turret, and to either side if it's not!
        if (transform.localPosition.x == 0)
        {
            turret.TryToAimAtTarget(shipMain.transform.position - shipMain.transform.forward * 20f);
        }
        if (transform.localPosition.x < 0)
        {
            turret.TryToAimAtTarget(shipMain.transform.position - shipMain.transform.right * 20f);
        }
        if (transform.localPosition.x > 0)
        {
            turret.TryToAimAtTarget(shipMain.transform.position + shipMain.transform.right * 20f);
        }
        //check if our current target is out of our firing angle, if so, release it and look for others
        if (AITarget)
        {
            Vector3 shipVec = Vector3.Normalize(AITarget.position - transform.position);
            float shipAngle = Vector3.Angle(shipVec, transform.forward);
            if (shipAngle >= turret.angleLimit)
            {
                AITarget = null;
                AITargetShip = null;
            }
        }
    }
    //Handle attacking
    void DoAttack() 
    { 
        //check that we have a target
        if(AITarget)
        {
            //get the target's stats
            if (!AITargetShip)
                AITargetShip = AITarget.GetComponent<ShipSettings>();
            //lead the target 
            Vector3 aimPoint = PredictV3Pos(transform.position, averageGunSpeed, AITarget.position, AITargetShip.velocity);
            //Anim at the target's future position
            turret.TryToAimAtTarget(aimPoint);

            //check if we're within a good angle to fire
            float angleToTarget = CustomAngleTo(elevation.transform.forward, aimPoint);
            //Debug.Log(angleToTarget + " is the current angle-to-target!");
            
            if (angleToTarget < aimAccuracyAngle)
            {
                turret.FireGuns(true);
                //Debug.Log("trying to fire");

            }
            else {
                turret.FireGuns(false);
            }
        }
        DoGunCooldown(1f, .2f);
    }

    void DoTargets()
    {
        //find the closest target, if we don't already have one, check at the skill level frequency
        if (!AITarget && GameObjTracker.frames % scanNewTargetFreq == 0)
        {
            AITargetShip = FindNearestShip(gameObject.transform, turret.angleLimit, shipMain.AITeam);
            //if there is no target in range, bail
            if (!AITargetShip)
            {
                return;
            }
            //if the target ship is out of engagement range, ignore it! 
            if (DistanceTo(AITargetShip.gameObject) > engageDist)
            {
                AITargetShip = null;
                return;
            }

            AITarget = AITargetShip.gameObject.transform;
        }
    }


    


    // Update is called once per frame
    void Update()
    {
        if (!shipMain.isDead)
        {
            DoGunSpeed();
            DoNoTargets();
            DoAttack();
            DoTargets();
            //turret.FireGuns(true);
        }
    }
}
