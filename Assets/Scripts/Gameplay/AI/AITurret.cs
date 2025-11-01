using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class AITurret : AIUnit
{
    //public enum AILevel { CHUMP, NOVICE, DEFAULT, SKILLED, ACE, MASTER };
    [SerializeField] private AITurretSkillSettings skillSettings;
    [SerializeField] private bool autoSkillLevel = true;

    [HideInInspector] public GameObjTracker Tracker; 
    [HideInInspector] public ShipSettings WingmanTo;

    ShipSettings shipMain;
    AIPlayer AIPilot;
    PlayerController pilot;
    TurretSettings turret;
    Transform elevation;
    ShipSettings AITargetShip;

    Vector3 currentTargetPos;

    protected override Capacitor MainCapacitor => turret.MainCapacitor;

    // Start is called before the first frame update
    void Start()
    {

        shipMain = GetComponentInParent<ShipSettings>();
        AIPilot = GetComponentInParent<AIPlayer>();
        pilot = GetComponentInParent<PlayerController>();
        turret = GetComponent<TurretSettings>();
        weaponsSystem = GetComponent<WeaponsSystem>();
        Assert.IsNotNull(weaponsSystem);
        elevation = transform.FindRecursive("Elevation");
        DoSkillLevels();
    }


    //Set up skill levels
    void DoSkillLevels()
    {
        //if we're set to auto-match skill levels, do so
        if (autoSkillLevel && AIPilot != null)
        {
            skillSettings = AIPilot.SkillSettings.TurretSkillSettings;
        }
        //If we're on a human piloted ship, auto set skill level! 
        if (autoSkillLevel && pilot != null)
        {
            // NOTE: formerly, we automatically applied ACE skill level here, but
            // that isn't possible after moving AI skill settings into their own
            // files. Now, the turret skill settings should be selected and applied
            // by the script that spawns the player.
            OMEPLogger.Log(this, $"Using skill level {skillSettings.SkillLevel} for {gameObject.name} on player ship {shipMain.name}");
        }

        Assert.IsNotNull(skillSettings);

        //apply rotation modifiers 
        turret.turnRate *= skillSettings.RotationSpeed;
    }

    // Utility to find the nearest ship, ignoring one of the Teams, any cloaked ships, and the Ship looking.
    // Note this includes some extra logic that isn't found in AIPlayer.
    public ShipSettings FindNearestShip(Transform toObj,float angle, TEAM ignoreTEAM)
    {
        float distance = skillSettings.EngageDistance * 10f;

        ShipSettings nearestShip = null;
        foreach (ShipSettings ship in GameObjTracker.Instance.AllShips)
        {
            if (ship != null && !ship.isCloaked)
            {
                Vector3 shipVec = Vector3.Normalize(ship.transform.position - toObj.position);
                float shipAngle = Vector3.Angle(shipVec, transform.forward);

                if (ship.Team != TEAM.NEUTRAL && ship != shipMain && shipAngle <= angle)
                {
                    float shipDist = Vector3.Distance(ship.transform.position, toObj.position);
                    if (shipDist < distance && ship.Team != ignoreTEAM)
                    {
                        distance = shipDist;
                        nearestShip = ship;
                    }
                }
            }
        }
        if (nearestShip != null)
            return nearestShip;
        else
            return null;
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
        Vector3 aimAt = targetPos + (targetVelocity * timeToTarget * skillSettings.LeadAmount);

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
            Vector3 aimPoint = PredictV3Pos(transform.position, averageGunSpeed, AITarget.position, AITargetShip.MeasuredVelocity);
            //Anim at the target's future position
            turret.TryToAimAtTarget(aimPoint);

            //check if we're within a good angle to fire
            float angleToTarget = CustomAngleTo(elevation.transform.forward, aimPoint);
            //Debug.Log(angleToTarget + " is the current angle-to-target!");
            
            if (angleToTarget < skillSettings.AimAccuracyAngle)
            {
                weaponsSystem.FireGuns();
                //Debug.Log("trying to fire");
            }
            else {
                weaponsSystem.StopFiring();
            }
        }
        DoGunCooldown(1f, .2f);
    }

    void DoTargets()
    {
        //find the closest target, if we don't already have one, check at the skill level frequency
        if (!AITarget && GameObjTracker.Instance.CurrentFrame % skillSettings.ScanNewTargetFreq == 0)
        {
            AITargetShip = FindNearestShip(gameObject.transform, turret.angleLimit, shipMain.Team);
            //if there is no target in range, bail
            if (!AITargetShip)
            {
                return;
            }
            //if the target ship is out of engagement range, ignore it! 
            if (DistanceTo(AITargetShip.gameObject) > skillSettings.EngageDistance)
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
        if (!shipMain.IsDead)
        {
            DoGunSpeed();
            DoNoTargets();
            DoAttack();
            DoTargets();
            //turret.FireGuns(true);
        }
    }
}
