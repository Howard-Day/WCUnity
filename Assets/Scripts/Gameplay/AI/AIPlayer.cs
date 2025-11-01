using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

// TODO: rename to AIShip
[RequireComponent(typeof(ShipSettings))]
public class AIPlayer : AIUnit
{
    #region FIELDS
    [Header("Settings")]
    [SerializeField] private AIShipSkillSettings skillSettings;
    public MessageHandler messageHandler;
    public AIState ActiveAIState = AIState.PATROL;

    [Header("Patrol Pattern")]
    public List<Vector3> PatrolPoints;

    [Header("Debug Options")]
    public bool doDebugOrient = false;
    public GameObject debugOrient;

    [HideInInspector] public GameObjTracker Tracker;
    [HideInInspector] float barrelRoll;
    [HideInInspector] public float impatience;
    [HideInInspector] public float angleToTarget;

    //Internal settings and flags
    ShipSettings ship;

    Vector3 smoothAimAt = Vector3.forward;

    float followDist;
    Vector3 randPos = Vector3.zero;

    ShipSettings AITargetShip;

    bool rolling = false;
    float rollStart;
    float rollDir;
    float rollLength;
    float barrelRef = 0f;

    bool isAvoiding = false;
    float avoidTimer = 0f;

    float friendlyFireAvoidAngle;
    float friendlyFireTime;
    float friendlyFireTimer = 0f;

    Vector3 randApproach = Vector3.zero;
    Vector3 EvadeSteer = Vector3.zero;

    int nextPatrolPoint = 0;
    float evadeTimer = 0f;

    Vector3 patrolPoint = Vector3.zero;

    Vector3 randDist = Vector3.zero;

    Vector3 currentTargetPos;
    #endregion

    #region PROPERTIES
    public AIShipSkillSettings SkillSettings
    {
        get => skillSettings;
        set => skillSettings = value;
    } 

    protected override Capacitor MainCapacitor => ship.MainCapacitor;
    #endregion

    //Initial Conditions
    void Start()
    {
        ship = GetComponent<ShipSettings>();
        weaponsSystem = GetComponent<WeaponsSystem>();
        Assert.IsNotNull(weaponsSystem);
        ForceRegister();
    }
     //Make sure the game knows we're here!
    void ForceRegister()
    {
        gameObject.transform.SetParent(GameObject.FindWithTag("GamePlayObjs").transform);
    }

    //Control where we go
    void SteerTo(Vector3 aimAt)
    {       //Vector3 rollAdjust = Quaternion.AngleAxis(Time.time * 12f, Vector3.up).eulerAngles;
        //Quaternion tarQ = Quaternion.LookRotation(targetDir, Vector3.up);
        //tarQ *= Quaternion.AngleAxis(ship.turnRate * barrelRef, Vector3.forward);
        //destQ *= Quaternion.AngleAxis(Time.time * ship.turnRate * barrelRef, Vector3.forward );

        smoothAimAt =  Vector3.Lerp(smoothAimAt, aimAt, .25f);

        Vector3 targetDir = smoothAimAt - transform.position;

        Quaternion tarQ = Quaternion.LookRotation(targetDir);
        tarQ *= Quaternion.AngleAxis(barrelRoll, Vector3.forward);
        Quaternion destQ = Quaternion.Inverse(transform.rotation) * tarQ;


        /*
        Quaternion initQ = Quaternion.Inverse(transform.rotation);
        Quaternion tarQ = Quaternion.LookRotation(aimAt - transform.position);
        Quaternion destQ = Quaternion.Slerp(initQ, tarQ,.125f);
        */

        float newPitchDest = (destQ * Vector3.forward).y * 4;
        float newYawDest = (destQ * Vector3.right).z * 4;
        float newRollDest = (destQ * Vector3.up).x * 4;

        newPitchDest = Mathf.Clamp(newPitchDest, -1f, 1f);
        newYawDest = Mathf.Clamp(newYawDest, -1f, 1f);
        newRollDest = Mathf.Clamp(newRollDest, -1f, 1f);
        //quickly blend from any manual steering, only if we're not trying to avoid someone else!
        if (!isAvoiding)
        {
            //turnSpeed = 1f;
            ship.yaw = Mathf.Lerp(ship.yaw, newYawDest, skillSettings.TurnSpeed);//Mathf.SmoothStep(ship.yaw,0f,.1f);
            ship.pitch = Mathf.Lerp(ship.pitch, newPitchDest, skillSettings.TurnSpeed);//Mathf.SmoothStep(ship.pitch,0f,.1f);
            ship.roll = Mathf.Lerp(ship.roll, newRollDest, skillSettings.TurnSpeed);
        }


        if (doDebugOrient)
        {
            if (!debugOrient.activeInHierarchy)
            {
                debugOrient = Instantiate(debugOrient, transform.root);

            }
            else
            {
                debugOrient.transform.position = aimAt;
                debugOrient.transform.rotation = tarQ;
            }
        }
    }
    //Do a Random Barrel Roll for fun!
    void DoABarrelRoll(float direction, float length)
    {
        if (!rolling)
            return;
        if (Time.time <= rollStart + length)
        {
            barrelRef = Mathf.SmoothStep(barrelRef, direction, .05f);
        }
        else
        {
            barrelRef = Mathf.Lerp(barrelRef, 0f, .05f);
            rolling = false;
        }
        barrelRoll += barrelRef * ship.Settings.TurnRate * Time.deltaTime;
    }
    //Stop Rolling the ship
    void StopRoll()
    {
        ship.roll = Mathf.SmoothStep(ship.roll, 0, .05f);
    }
    //Roll the ship for more dynamic movement!
    void RollControl(float rollOn)
    {
        //occasionally spin! 
        if (rollOn > 0 && !rolling)
        {
            rolling = true;
            rollDir = Random.Range(-1, 2);
            if (rollDir == 0)
                rollDir = Random.Range(-1, 2);
            if (rollDir == 0)
                rollDir = Random.Range(-1, 2);
            if (rollDir == 0)
                rollDir = Random.Range(-1, 2);
            if (rollDir == 0)
                rollDir = 1;

            rollLength = Random.Range(.5f, 6f);
            rollStart = Time.time;
            //print(gameObject.name+" starting to " + rollDir +" roll for: "+rollLength +"sec");
        }
        DoABarrelRoll(rollDir, rollLength);
        if (!rolling)
        {
            StopRoll();
        }
    }

    int avoidBurn = 0;

    //Attempt avoid any ships in front of the AI with a random chance to afterburn
    void DoCollisionAvoidance()
    {
        Vector3 tarShipDir = Vector3.up;
        //loop through ships
        foreach (ShipSettings tarShip in GameObjTracker.Instance.AllShips)
        {
            //verify the reference isnt null
            if (tarShip != null)
            {
                Vector3 targetShip = tarShip.gameObject.transform.position;
                //Cull by distance and Ourselves!
                if ((Vector3.Distance(targetShip, ship.transform.position) <= skillSettings.AvoidDistance * .5f) && tarShip != ship)
                {
                    //Cull by forward angle from the AI's ship
                    if (AngleTo(targetShip) < skillSettings.AvoidAngle)
                    {
                        //Cull by only if the targetShip is coming towards the AI
                        if (CustomAngleTo(tarShip.transform.forward, ship.transform.position) <= 90f)
                        {
                            tarShipDir = ship.transform.position-tarShip.transform.position;
                            if (!isAvoiding)
                            {
                                if (logDebug){print(ship.DisplayName + " is avoiding " + tarShip.DisplayName);}
                            }
                            isAvoiding = true;
                            avoidBurn = 0;
                        }
                    }
                }
            }
        }
        if(avoidBurn == 0)
        {
            if (Random.Range(0f, 1f) > .5f)
            {
                avoidBurn = 1;
            }
            else 
            {
                avoidBurn = 2;
            }
        }
        if (avoidBurn == 1 && (avoidTimer > skillSettings.AvoidTime / 3) && (avoidTimer < skillSettings.AvoidTime * .9f))
        {
            ship.Engines.TargetSpeed = ship.Settings.BurnSpeed;
        }
        if (isAvoiding)
        {
            if (avoidTimer <= skillSettings.AvoidTime)
            {
                avoidTimer += Time.deltaTime;
                SteerTo(ship.transform.position - (tarShipDir * 4f));
            }
            else
            {
                isAvoiding = false;
                avoidBurn = 0;
                avoidTimer = 0f;
            }
        }
    }
    //Force firing if we're within close range, within specified angle
    void DoForceFire()
    {
        //San check we have a target
        if (AITargetShip && AITarget)
        {
            //check if the target is within the forward vector angle and distance
            if (Vector3.Distance(ship.transform.position, AITargetShip.transform.position) <= skillSettings.ForceFireDistance)
            {
                if (AngleTo(AITargetShip.transform.position) <= skillSettings.ForceFireAngle)
                {
                    weaponsSystem.FireGuns();
                    if (logDebug) { print(ship.DisplayName + " is forcing fire!"); }
                }
            }

        }
    }
    //Force us to Hold fire if a friendly is in front of us, between us and the target and then switch to Reposition if they're there too long! 
    void DoFriendlyFire()
    {
        //default flag to off
        bool holdFire = false;
        //check if we're trying to fire our guns!, and we have a target?
        if (ship.isFiring && AITarget && AITargetShip)
        {
            Vector3 us = ship.transform.position;
            Vector3 usForward = ship.transform.forward;
            Vector3 target = AITargetShip.transform.position;
            float angle = friendlyFireAvoidAngle;
            //Loop through 10 rays every frame within the fire avoidance angle
            for (int i = 0; i < 10; i++)
            {
                Quaternion angleOffset = Quaternion.Euler(Random.Range(angle, -angle), Random.Range(angle, -angle), Random.Range(angle, -angle));
                usForward = angleOffset * usForward;
                //cast a ray from us to the AITarget, and check if there's a friendly in between
                RaycastHit hit;
                if (Physics.Raycast(us, usForward, out hit, Vector3.Distance(us, target) * .9f))
                {
                    ShipSettings hitShip = hit.transform.gameObject.GetComponent<ShipSettings>();
                    //check if the hit object is a friendly
                    if (hitShip && hitShip.Team == ship.Team)
                    {
                        //increment the Reposition Timer
                        if (logDebug) { print(ship.DisplayName + " is avoiding friendly fire!"); }
                        friendlyFireTimer += Time.deltaTime;
                        //set the hold fire flag
                        holdFire = true;
                    }
                }
            }
            //if any of the rays hit, disable our guns!
            if (holdFire)
            {
                weaponsSystem.StopFiring();
            }
            //if we don't hit anything, decrease the timer! 
            else
            {
                if (friendlyFireTimer > 0)
                {
                    friendlyFireTimer -= Time.deltaTime / 2f;
                }
            }
        }
        //Reposition if the Timer has run out!
        if (friendlyFireTimer > friendlyFireTime)
        {
            ActiveAIState = AIState.REPOSITION;
            friendlyFireTimer = 0f;
        }
    }

    //Handle Being Shot
    void DoBeingShot()
    {
        //track who's been shooting at us
        ShipSettings shootingShip = GameObjTracker.Instance.GetShipByID(ship.lastHitID);
        //check if we're being deliberately shot at!
        if (ship.isBeingShot)
        {
            //check if the last shot was from a ship other than our target, and *Isn't* a friendly.
            if (shootingShip !=  null && shootingShip != AITargetShip && shootingShip.Team != ship.Team)
            {
                CheckIfShieldsLow(shootingShip, 1 / 3f, 200);
            }
        }
        //if we're not deliberately being shot, check for that and then lower the threashold for action 
        //check if the last shot was from a ship other than our target, and *Isn't* a friendly.
        if (shootingShip != null && shootingShip != AITargetShip && shootingShip.Team != ship.Team)
        {
            CheckIfShieldsLow(shootingShip, 1 / 5f, 200);
        }
        //check if the last shot was from a ship other than our target, and *Is* a friendly. Higher threshold for a reposition.
        if (shootingShip != null && shootingShip != AITargetShip && shootingShip.Team == ship.Team)
        {
            //check if our shields are low
            const float LOW_FACTOR = 1 / 2f;
            if (ship.ShieldFrontNormalized <  LOW_FACTOR || ship.ShieldBackNormalized < LOW_FACTOR)
            {
                //check if the firing ship is behind us!
                if (AngleTo(shootingShip.transform.position) > 200f)
                {
                    //Reposition to clear our lane of fire! 
                    ActiveAIState = AIState.REPOSITION;
                }
            }
        }
    }

    private void CheckIfShieldsLow(ShipSettings shootingShip, float shieldThreshold, float behindUsAngle)
    {
        if (ship.ShieldFrontNormalized < shieldThreshold || ship.ShieldBackNormalized < shieldThreshold)
        {
            //check if the firing ship is behind us!
            if (AngleTo(shootingShip.transform.position) > behindUsAngle)
            {
                //change our target over to the firing ship! 
                AITarget = shootingShip.transform;
                AITargetShip = shootingShip;
            }
        }
    }

    //Handle Target Cloaking
    void DoCloakedTarget()
    {
        if (AITarget && AITargetShip && AITargetShip.isCloaked)
        {
            AITargetShip = FindNearestShip(gameObject.transform, ship.Team);
            if (AITargetShip != null)
            {
                AITarget = AITargetShip.transform;
            }
        }
    }
    //Handle no enemies
    void DoNoTargets()
    {
        if (AITarget == null)
        {
            AITargetShip = null;
            ActiveAIState = AIState.PATROL;
        }
    }

    //Utility to find the nearest ship, ignoring one of the Teams, any cloaked ships, and the Ship looking
    public ShipSettings FindNearestShip(Transform toObj, TEAM ignoreTEAM)
    {
        float distance = skillSettings.EngageDistance * 10f;

        ShipSettings nearestShip = null;
        foreach (ShipSettings ship in GameObjTracker.Instance.AllShips)
        {
            if (ship != null && !ship.isCloaked)
            {
                if (ship.Team != TEAM.NEUTRAL && ship != this.ship)
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

    //Ai frustration and wait-for-gun recharge
    public void DoImpatience(float maxImpatience, float howImpatient, float waitTime)
    {
        if (AITarget)
        {
            float distToTarget = Vector3.Distance(AITarget.position, transform.position);
            if (ship.MainCapacitor.CurrentCharge <= 2f) //if the AI can't shoot full blasts, increase impatience
            {
                impatience += Time.deltaTime * howImpatient * 4;
            }
            // TODO: should this be <= ?
            if (distToTarget < 60 && ship.MainCapacitor.CurrentChargeNormalized >= .25f) //If we're close to oue close to our target, but CANT fire, increase Impatience, albiet at a slower rate
            {
                impatience += Time.deltaTime * howImpatient;
            }
            // TODO: should this be <= ?
            if ((distToTarget < skillSettings.EngageDistance / 2) && angleToTarget < 10f && ship.MainCapacitor.CurrentChargeNormalized > .666f)
            {
                impatience += Time.deltaTime * howImpatient * 2;
            }
            // TODO: should this be <= ?
            if (impatience >= maxImpatience && ship.MainCapacitor.CurrentChargeNormalized > .333f) //had enough, break off 
            {
                impatience = 0f; //We did something about it, calm down
                ActiveAIState = AIState.REPOSITION;
            }
            ///but we *also gradually calm down
            if (impatience > 0)
            {
                impatience -= Time.deltaTime;
            }
        }
    }
    //AI bloodthirstyness
    public void DoBloodThirsty(float maxThirsty)
    {


    }
    //Handle Randonm offset Aiming at a target
    public Vector3 DoRandomOffset(float Accuracy, float Update)
    {
        if (GameObjTracker.Instance.CurrentFrame % Update == 5)
        {
            randDist = Random.insideUnitSphere * Accuracy;
        }
        return randDist;
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
        Vector3 aimAt = targetPos + targetVelocity * timeToTarget;

        return aimAt;
    }

    private void TryJoinFormation()
    {
        FormationManager.Instance.TryJoinClosestFormation(ship, 10000, true);
    }

    //Define AI States
    void DoAIStates()
    {
        switch (ActiveAIState)
        {

            case AIState.PATROL:
                {
                    //stop firing, if we are
                    weaponsSystem.StopFiring();
                    //Check to see if we've got any patrol points assigned already! 
                    if (PatrolPoints.Count == 0)
                    {
                        //Oh no! We need some to patrol, let's generate some, lessay 4
                        int pp = 0;
                        int numPoints = 4;
                        while (pp <= numPoints)
                        {
                            PatrolPoints.Add(Random.onUnitSphere * Random.Range(320f, 500f));
                            pp++;
                        }
                    }
                    float ppDist = Vector3.Distance(transform.position, PatrolPoints[nextPatrolPoint]);
                    if (ppDist > 10)
                    {
                        SteerTo(PatrolPoints[nextPatrolPoint]);
                        ship.Engines.TargetSpeed = ship.Settings.TopSpeed * .75f; //Cruise speed! No rush, juuust loooking for baddies. 
                    }
                    else
                    {
                        patrolPoint = PatrolPoints[nextPatrolPoint];
                        print(gameObject.name + " Reached patrol point " + nextPatrolPoint + " going to the next!");
                        nextPatrolPoint++;
                    }
                    if (nextPatrolPoint > PatrolPoints.Count - 1) //Cycle the patrol point list
                    {
                        print(gameObject.name + " is Loooping patrol points!");
                        nextPatrolPoint = 0;
                    }
                    //AITarget is already the closest known enemy - let's use that! 
                    if (AITarget != null && DistanceTo(AITarget.gameObject) <= skillSettings.EngageDistance * 1.5f)
                    {//If we're withing the engage envelope, let's go check it out! 
                        ActiveAIState = AIState.ENGAGE;
                    }
                    //It's an Ambush! 
                    if (AITarget && AITargetShip && ship.lastHitID != 0)
                    {
                        AITargetShip = FindShipByID(ship.lastHitID, ship.Team);
                        if (AITargetShip)
                        {
                            AITarget = AITargetShip.gameObject.GetComponent<Transform>();
                        }
                    }
                    if (AITarget && AITargetShip)
                    {
                        ActiveAIState = AIState.ENGAGE;
                    }


                }
                break;
            case AIState.BREAK: //Break and Attack!
                {
                    if (ship.Formation != null && ship.Formation.Leader.currentTarget != null && AITargetShip)
                    {
                        AITargetShip = ship.Formation.Leader.currentTarget;
                    }
                    ActiveAIState = AIState.ENGAGE;
                }
                break;
            case AIState.WINGMAN:
                {
                    if (ship.Formation == null) //Look for a wingleader in this state 
                    {
                        TryJoinFormation();
                    }
                    if (ship.Formation == null || ship.Formation.Leader == ship) // No wingleaders? Individual patrol mode!
                    {
                        ActiveAIState = AIState.PATROL;
                    }
                    else // We are a wingman in a formation
                    {
                        var leader = ship.Formation.Leader;

                        //See how far away and what direction we need to go
                        var localFormationPose = ship.Formation.GetSlotPose(ship);
                        var leadDist = Vector3.Distance(localFormationPose.position, transform.position);
                        var dirToPos = localFormationPose.position - transform.position;

                        Debug.DrawLine(gameObject.transform.position, localFormationPose.position, Color.green, .10f);

                        if (leadDist != 0)
                        {
                            if (leadDist > 120)//If we're a ways off, aim right at the formation point and afterburn into position.
                            {
                                ship.Engines.TargetSpeed = ship.Settings.BurnSpeed;
                                SteerTo(localFormationPose.position);
                            }
                            if (leadDist <= 120 && leadDist > 20) //If we're a moderate distance away, set speed to the lead ship +25%, aim at the formation position.
                            {
                                ship.Engines.TargetSpeed = leader.Engines.Speed + ship.Settings.TopSpeed / 4;
                                SteerTo(localFormationPose.position);
                            }
                            if (leadDist <= 20) //If we're close, Match speed, and aim at a point parallel to the direction of the lead ship
                            {
                                ship.Engines.TargetSpeed = leader.Engines.Speed;
                                SteerTo(localFormationPose.position + leader.transform.forward * ship.shipRadius * 4f);
                                //A gentle push, like the avoidance system, to nudge us into place
                                float formPush = (dirToPos.magnitude / 10) * .5f;
                                transform.position += dirToPos * formPush * Time.deltaTime;
                            }
                            if (leadDist <= 30) // attempt to match roll once we get close-ish
                            {
                                // TODO: this is hacky and affected by framerate. use ship controls to rotate instead of lerping.
                                transform.rotation = Quaternion.Lerp(transform.rotation, localFormationPose.rotation, .005f);
                                //QuaternionUtil.SmoothDamp(transform.rotation,WingmanTo.transform.rotation, ref refForm, .15f);
                                ship.roll = leader.roll;
                            }
                            //AITarget is already the closest known enemy - let's use that! 
                            if (AITarget != null && Vector3.Distance(AITarget.position, transform.position) <= skillSettings.EngageDistance * .333f)
                            {//hold formation until we're very close
                                ActiveAIState = AIState.REPOSITION;
                            }
                        }
                    }
                }
                break;

            case AIState.ENGAGE:
                {
                    if (AITarget && AITargetShip)
                    {
                        float angleToTarget = AngleTo(AITarget.position);
                        if (randApproach.magnitude == 0)
                        {
                            randApproach = Random.onUnitSphere * AITargetShip.shipRadius * .5f;
                        }
                        
                        SteerTo(AITarget.position);// + (randApproach * (Vector3.Distance(AITarget.position, transform.position) / engageDist)));

                        ship.Engines.TargetSpeed = ship.Settings.TopSpeed;

                        if (!AITarget)
                        {
                            ActiveAIState = AIState.PATROL;
                        }
                        if (Vector3.Distance(AITarget.position, transform.position) > skillSettings.EngageDistance)
                        {
                            ship.Engines.TargetSpeed = ship.Settings.BurnSpeed;
                        }
                        if (Vector3.Distance(AITarget.position, transform.position) <= skillSettings.EngageDistance)
                        {
                            randApproach = Vector3.zero;
                            ActiveAIState = AIState.HUNT;
                        }
                        //Does the ship have a cloaking device? If so, engage it!
                        if (ship.Settings.HasCloak)
                        {
                            ship.Cloak = true;
                        }

                    }
                    //Bail if there's no targetable enemies
                    if (!AITargetShip || !AITarget)
                    {
                        ActiveAIState = AIState.PATROL;
                    }
                    //print(gameObject.name + " Is engaging! Throttle set to " + ship.Engines.TargetSpeed);
                }
                break;

            case AIState.HUNT:
                {
                    //Early bail, if no target                    
                    if (!AITargetShip || !AITarget)
                    {
                        ActiveAIState = AIState.PATROL;
                    }
                    else
                    {
                        //Does the ship have a cloaking device? If so, disengage it!
                        if (ship.Settings.HasCloak)
                        {
                            if (ship.isCloaked)
                            {
                                ship.Cloak = false;
                            }
                        }
                        //Track the target according to our ability
                        randApproach = Vector3.zero;

                        if (randApproach.magnitude == 0)
                        {
                            randApproach = Random.onUnitSphere * AITargetShip.shipRadius * skillSettings.AimAccuracy;
                        }

                        float angleToTarget = AngleTo(AITarget.position);
                        float distToTarget = Vector3.Distance(AITarget.position, transform.position);

                        //Closest Target is infront of us
                        if (angleToTarget < 140)
                        {
                            //If we're too far away to match speed to the target, get closer
                            if (distToTarget > followDist)
                            {
                                ship.Engines.TargetSpeed = ship.Settings.TopSpeed;
                            }
                            //match the target's speed
                            else
                            {
                                ship.Engines.TargetSpeed = Mathf.Max(Mathf.Min(AITargetShip.Engines.TargetSpeed, ship.Settings.TopSpeed), ship.Settings.TopSpeed / 4);
                            }
                            //Try and turn toward the target! 
                            if (distToTarget > skillSettings.EngageDistance)
                            {
                                if (angleToTarget < 60)
                                {
                                    ship.Engines.TargetSpeed = ship.Settings.BurnSpeed;
                                }
                                else
                                {
                                    ship.Engines.TargetSpeed = ship.Settings.TopSpeed;
                                }
                            }
                        }
                        //OH NOES, WE BEIN HUNTED SON
                        else
                        {
                            evadeTimer = 0;
                            ActiveAIState = AIState.EVADE;
                        }


                        //Get the target's velocity, adding a miss possibility
                        Vector3 shootAt = DoRandomOffset(skillSettings.AimAccuracy, skillSettings.AimUpdate);
                        currentTargetPos = AITarget.position;// + shootAt;
                        Vector3 targetVelocity = AITargetShip.MeasuredVelocity;

                        //Predict where we need to shoot in order to hit our target! 
                        Vector3 aimPoint = PredictV3Pos(ship.transform.position, averageGunSpeed, currentTargetPos, targetVelocity*2f);
                        //AITarget.position + Vector3.Lerp(AITargetShip.shipRadius * Vector3.one, shootAt, nearDodgeBlend);
                        //Debug.DrawLine(transform.position,shootAt+AITarget.position,Color.red,.01f);
                        //Debug.DrawLine(transform.position,transform.position+transform.forward*25,Color.yellow,.01f);

                        //Steer to the predicted aiming location! (Only if we're not trying to avoid something)
                        if (!isAvoiding)
                        {
                            SteerTo(aimPoint);
                        }
                    
                        
                        float angleToShoot = AngleTo(aimPoint);

                        //if we're within the aim accuracy angle start firing!
                        if (angleToShoot < skillSettings.AimAccuracy * 2f )
                        {
                            if (logDebug) { print("attempting to fire"); }
                            AITargetShip.isBeingShot = true;
                            weaponsSystem.FireGuns();
                        }
                        //otherwise, stop firing
                        else 
                        {
                            AITargetShip.isBeingShot = false;
                            weaponsSystem.StopFiring();

                            //unless we're *very* close, take the chance!
                            if (distToTarget < skillSettings.EngageDistance / 8f)
                            {
                                if (angleToShoot < skillSettings.AimAccuracy * 4f)
                                {
                                    AITargetShip.isBeingShot = true;
                                    if (logDebug) { print("attempting to fire"); }
                                    weaponsSystem.FireGuns();
                                }
                                //disable firing
                                else
                                {
                                    AITargetShip.isBeingShot = false;
                                    weaponsSystem.StopFiring();
                                }
                            }
                            //disable firing
                            else
                            {
                                AITargetShip.isBeingShot = false;
                                weaponsSystem.StopFiring();
                            }
                        }
                        //Too far away to shoot, or the angle is too much!
                        if (distToTarget > skillSettings.EngageDistance * 2 || AngleTo(AITarget.position) > 180)
                        {
                            AITargetShip.isBeingShot = false;
                            weaponsSystem.StopFiring();
                        }
                        //we've gotten too far away, go back into engage mode
                        if (distToTarget > skillSettings.EngageDistance * 1.5f)
                        {
                            ActiveAIState = AIState.ENGAGE;
                        }
                        //Oh no, we've crashed, reposition!
                        if (ship.recover < 1)
                        {
                            ActiveAIState = AIState.REPOSITION;
                        }
                    }
                }
                break;

            case AIState.EVADE:
                {
                    if (evadeTimer == 0) //We're starting to evade
                    {//Punch it, Chewie! 
                        weaponsSystem.StopFiring();

                        ship.Engines.TargetSpeed = ship.Settings.BurnSpeed;
                        if (EvadeSteer == Vector3.zero)// have we chosen where to steer? 
                        {
                            EvadeSteer = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));
                        }
                        //Does the ship have a cloaking device? If so, engage it!
                        if (ship.Settings.HasCloak)
                        {
                            ship.Cloak = true;
                        }
                    }
                    if (GameObjTracker.Instance.CurrentFrame % Random.Range(60, 120) == 0) // every few second jerk around wildly! 
                    {
                        EvadeSteer = new Vector3(Random.Range(-2f, 2f), Random.Range(-2f, 2f), Random.Range(-2f, 2f));
                    }
                    ship.pitch = Mathf.Lerp(ship.pitch, EvadeSteer.x * skillSettings.EvadeAmount, .001f);
                    ship.yaw = Mathf.Lerp(ship.yaw, EvadeSteer.y * skillSettings.EvadeAmount, .001f);
                    ship.roll = Mathf.Lerp(ship.roll, EvadeSteer.z * skillSettings.EvadeAmount, .001f);
                    //count down the time to return to normal combat. If we have a cloaking device, increase the wait time to be extra sneaky! 
                    if (!ship.Settings.HasCloak)
                    {
                        evadeTimer += Time.deltaTime;
                    }
                    else
                    {
                        evadeTimer += Time.deltaTime / 4f;
                    }
                    if (evadeTimer >= skillSettings.EvadeLength)
                    {
                        EvadeSteer = Vector3.zero;
                        ActiveAIState = AIState.HUNT;
                    }
                    if (!AITargetShip || !AITarget)
                    {
                        ActiveAIState = AIState.PATROL;
                    }
                }
                break;

            case AIState.REPOSITION:
                {
                    //Early Bail if no target
                    if (!AITargetShip || !AITarget)
                    {
                        ActiveAIState = AIState.PATROL;
                    }
                    //Basic State setup
                    if (AITarget)
                    {
                        float angleToTarget = AngleTo(AITarget.position);
                        Vector3 dirToTarget = AITarget.transform.position - transform.position;
                        float distToTarget = Vector3.Distance(AITarget.position, transform.position);

                        //No shootie
                        weaponsSystem.StopFiring();

                        randPos = Vector3.zero;
                        if (randPos.magnitude == 0)
                        {
                            randPos = transform.position + Random.onUnitSphere * skillSettings.EngageDistance;
                        }
                        if (distToTarget < 80f && distToTarget > 40f)
                        {
                            ship.Engines.TargetSpeed = ship.Settings.BurnSpeed;
                        }
                        else
                        {
                            ship.Engines.TargetSpeed = ship.Settings.TopSpeed;
                        }
                        SteerTo(randPos);
                        if (Vector3.Distance(transform.position, randPos) < 20f || distToTarget > 100f)
                        {
                            randPos = Vector3.zero;
                            ActiveAIState = AIState.HUNT;
                        }
                    }
                }
                break;

            case AIState.VICTORY:
                {
                    if (skillSettings.SkillLevel > AILevel.NOVICE)
                    {
                        weaponsSystem.StopFiring();
                    }
                }
                break;

            default:
                {
                }
                break;
        }

    }
    //Dumb as rocks AI
    void ChumpAI()
    {
        ship.Engines.TargetSpeed = ship.Settings.TopSpeed * .75f;
        SteerTo(new Vector3(0, 50, 200));
        RollControl(Random.Range(-4000f, 1f));
    }
    //Novice AI Settings
    void NoviceAI()
    {
        if (followDist == 0)
        {
            followDist = Random.Range(75f, 125f);
            //print(name + " has a follow distance of " +followDist);
        }
        if (!AITargetShip)
        {
            AITargetShip = FindNearestShip(gameObject.transform, ship.Team);
        }
        if (AITargetShip != null)
        {
            AITarget = AITargetShip.gameObject.GetComponent<Transform>();


            if (AITargetShip && Vector3.Distance(AITarget.position, transform.position) <= 100)
            {
                if (ActiveAIState == AIState.ENGAGE)
                {
                    ActiveAIState = AIState.HUNT;
                }
            }
            //TODO: this looks like a mistake:
            if (ship.hitInAss && ship.Shield.Back <= .5f && ship.lastHit == ShipSettings.HitLoc.B) //WE're being hit from behind, shields low, HOLY SHIT, EVADE!
            {

                ship.hitInAss = false;
                if (AngleTo(AITarget.position) > 30)  //If our target is in front of us , just reposition, otherwise evade
                {
                    evadeTimer = 0f;
                    ActiveAIState = AIState.EVADE;
                }
                else
                {
                    ActiveAIState = AIState.REPOSITION;
                }
            }
        }
        DoImpatience(3f, 2f, 1f);
        DoAIStates();
        RollControl(Random.Range(-4000f, 1f));
        if (AITarget)
        {
            angleToTarget = AngleTo(AITarget.position);
        }
        ship.currentTarget = AITargetShip;
        DoGunCooldown(1f, .125f);
    }
    //Default AI Settings!
    void DefaultAI()
    {
        if (followDist == 0)
        {
            followDist = Random.Range(65f, 100f);
        }
        if (!AITargetShip)
        {
            AITargetShip = FindNearestShip(gameObject.transform, ship.Team);
        }
        if (AITargetShip != null)
        {
            AITarget = AITargetShip.gameObject.GetComponent<Transform>();

            if (AITargetShip && Vector3.Distance(AITarget.position, transform.position) <= 100)
            {
                if (ActiveAIState == AIState.ENGAGE)
                {
                    ActiveAIState = AIState.HUNT;
                }
            }
            //TODO: this looks like a mistake:
            if (ship.hitInAss && ship.Shield.Back <= .6f && ship.lastHit == ShipSettings.HitLoc.B) //WE're being hit from behind, shields low, HOLY SHIT, EVADE!
            {
                ship.hitInAss = false;
                if (AngleTo(AITarget.position) > 20)  //If our target is in front of us , just reposition, otherwise evade
                {
                    evadeTimer = 0f;
                    ActiveAIState = AIState.EVADE;
                }
                else
                {
                    ActiveAIState = AIState.REPOSITION;
                }
            }
        }
        DoImpatience(2.5f, 1f, 2f);
        DoAIStates();
        RollControl(Random.Range(-2500f, 1f));
        if (AITarget)
        {
            angleToTarget = AngleTo(AITarget.position);
        }
        ship.currentTarget = AITargetShip;
        DoGunCooldown(1f, .2f);
    }
    //Ace AI Settings!
    void AceAI()
    {
        if (followDist == 0)
        {
            followDist = Random.Range(55f, 80f);
        }
        if (!AITargetShip)
        {
            AITargetShip = FindNearestShip(gameObject.transform, ship.Team);
        }
        if (AITargetShip != null)
        {
            AITarget = AITargetShip.gameObject.GetComponent<Transform>();


            if (AITargetShip && Vector3.Distance(AITarget.position, transform.position) <= 100)
            {
                if (ActiveAIState == AIState.ENGAGE)
                {
                    ActiveAIState = AIState.HUNT;
                }
            }
            //TODO: this looks like a mistake:
            if (ship.hitInAss && ship.Shield.Back <= .9f && ship.lastHit == ShipSettings.HitLoc.B) //WE're being hit from behind, shields low, HOLY SHIT, EVADE!
            {
                ship.hitInAss = false;
                if (AngleTo(AITarget.position) > 15)  //If our target is in front of us , just reposition, otherwise evade
                {
                    evadeTimer = 0f;
                    ActiveAIState = AIState.EVADE;
                }
                else
                {
                    ActiveAIState = AIState.REPOSITION;
                }
            }
            if (ActiveAIState == AIState.HUNT || ActiveAIState == AIState.REPOSITION)
            {
                AITargetShip.isLocked = true;
            }
            else
            {
                AITargetShip.isLocked = false;
            }
        }
        DoImpatience(2f, .75f, 2.5f);
        DoAIStates();
        RollControl(Random.Range(-1500f, 1f));
        if (AITarget)
        {
            angleToTarget = AngleTo(AITarget.position);
        }
        ship.currentTarget = AITargetShip;
        DoGunCooldown(1f, .2f);
    }

    void Update()
    {
        DoGunSpeed();
        switch (skillSettings.SkillLevel)
        {
            case (AILevel.CHUMP):
                {
                    ChumpAI();
                }
                break;
            case (AILevel.NOVICE):
                {
                    NoviceAI();
                }
                break;
            case (AILevel.DEFAULT):
                {
                    DefaultAI();
                }
                break;
            case (AILevel.ACE):
                {
                    AceAI();
                }
                break;
            default:
                Debug.LogError($"Unsupported skill level {skillSettings.SkillLevel}");
                break;
        }
        DoCloakedTarget();
        DoNoTargets();
        DoCollisionAvoidance();
        DoBeingShot();
        DoForceFire();
        DoFriendlyFire();
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (AITarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, AITarget.position);
        }
    }

    [UnityEditor.CustomEditor(typeof(AIPlayer))]
    private class AIPlayerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (Application.isPlaying)
            {
                var instance = (AIPlayer)target;
                
                using (var hlayout = new UnityEditor.EditorGUILayout.HorizontalScope())
                {
                    UnityEditor.EditorGUILayout.LabelField("Target", GUILayout.Width(100));
                    string name = (instance.AITarget == null) ? "None" : instance.AITarget.name;
                    if (GUILayout.Button(name))
                    {
                        UnityEditor.Selection.activeObject = instance.AITarget;
                    }
                }
            }
        }
    }
#endif
}
