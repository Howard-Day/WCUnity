using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions.Must;

[RequireComponent(typeof(ShipSettings))]
public class AIPlayer : MonoBehaviour
{

    public enum AIState { PATROL, BREAK, SEARCH, WINGMAN, ENGAGE, HUNT, EVADE, PROTECT, REPOSITION, FLEE, DEATH, VICTORY };
    public enum AILevel { CHUMP, NOVICE, DEFAULT, SKILLED, ACE, MASTER };


    [Header("Settings")]
    public MessageHandler messageHandler;
    public AIState ActiveAIState = AIState.PATROL;
    public AILevel AISkillLevel = AILevel.CHUMP;

    [Header("Patrol Pattern")]
    public List<Vector3> PatrolPoints;

    [Header("Debug Options")]
    public bool logDebug = false;
    public bool doDebugOrient = false;
    public GameObject debugOrient;

    [HideInInspector] public Transform AITarget;
    [HideInInspector] public GameObjTracker Tracker;
    [HideInInspector] public ShipSettings WingmanTo;
    [HideInInspector] float barrelRoll;
    [HideInInspector] public float impatience;
    [HideInInspector] public float angleToTarget;

    //Internal settings and flags
    ShipSettings ship;
    //ProjectileWeapon[] projWeapons;
    Vector3 smoothDir = Vector3.zero;
    Vector3 refDir = Vector3.zero;
    Quaternion refRot = Quaternion.identity;
    Quaternion refForm = Quaternion.identity;

    float averageGunSpeed = 0f;

    Vector3 smoothAimAt = Vector3.forward;
    Vector3 smoothVel = Vector3.forward;

    float turnSpeed = .05f;
    float engageDist = 150f;
    float aimAccuracy = 30f;
    int aimIterations = 3;
    int aimUpdate = 60;
    float followDist;
    float bloodThirst;
    Vector3 randPos = Vector3.zero;

    ShipSettings AITargetShip;
    Vector3 formationPos;
    float evadeLength;
    float evadeAmt;
    float AILeadAmt = 1f;

    bool rolling = false;
    float rollStart;
    float rollDir;
    float rollLength;
    float barrelRef = 0f;

    float avoidAngle;
    float avoidDist;
    float avoidSpeed;
    bool isAvoiding = false;
    float avoidTimer = 0f;
    float avoidTime;

    float forceFireAngle;
    float forceFireDist;

    float friendlyFireAvoidAngle;
    float friendlyFireTime;
    float friendlyFireTimer = 0f;

    Vector3 randApproach = Vector3.zero;
    Vector3 EvadeSteer = Vector3.zero;
    bool hasWingLead = false;

    int nextPatrolPoint = 0;
    float evadeTimer = 0f;

    float cooldownWait;
    bool cooldownWaiting = false;
    Vector3 patrolPoint = Vector3.zero;

    Vector3 randDist = Vector3.zero;

    Vector3 currentTargetPos;

    //Initial Conditions
    void Start()
    {
        ship = GetComponent<ShipSettings>();
        ForceRegister();
    }
     //Make sure the game knows we're here!
    void ForceRegister()
    {
        gameObject.transform.SetParent(GameObject.FindWithTag("GamePlayObjs").transform);
        GameObjTracker.RegisterAllShips();
        GameObjTracker.RegisterTeams();
    }
    //Figure out the average velocity of our guns, for predictive aiming
    void DoGunSpeed()
    {
        //loop through our guns, if they're initialized, and we haven't figured this out yet
        if (averageGunSpeed == 0)
        {
            float tempGunSpeed = 0f;
            //loop through our guns, and add all their speeds together
            if (logDebug) { print("the number of found weapons is " + ship.projWeapons.Length); }
            foreach (ProjectileWeapon gun in ship.projWeapons)
            {
            tempGunSpeed += gun.speed;
            }
            //return the cumulative gunspeeds by the number of guns, set the value so this only runs once.  
            averageGunSpeed = tempGunSpeed / ship.projWeapons.Length;
        }
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
            ship.yaw = Mathf.Lerp(ship.yaw, newYawDest, turnSpeed);//Mathf.SmoothStep(ship.yaw,0f,.1f);
            ship.pitch = Mathf.Lerp(ship.pitch, newPitchDest, turnSpeed);//Mathf.SmoothStep(ship.pitch,0f,.1f);
            ship.roll = Mathf.Lerp(ship.roll, newRollDest, turnSpeed);
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
        barrelRoll += barrelRef * ship.turnRate * Time.deltaTime;
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
        foreach (ShipSettings tarShip in GameObjTracker.Ships)
        {
            //verify the reference isnt null
            if (tarShip != null)
            {
                Vector3 targetShip = tarShip.gameObject.transform.position;
                //Cull by distance and Ourselves!
                if (Vector3.Distance(targetShip, ship.transform.position) <= avoidDist*.5f && tarShip != ship)
                {
                    //Cull by forward angle from the AI's ship
                    if (AngleTo(targetShip) < avoidAngle)
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
        if (avoidBurn == 1 && avoidTimer > avoidTime/3 && avoidTimer < avoidTime *.9f)
        {
            ship.speed = ship.burnSpeed;
        }
        if (isAvoiding && avoidTimer <= avoidTime)
        {
            avoidTimer += Time.deltaTime;
            SteerTo(ship.transform.position - (tarShipDir * 4f));
        }
        if (isAvoiding && avoidTimer > avoidTime)
        {
            isAvoiding = false;
            avoidBurn = 0;
            avoidTimer = 0f;
        }
    }
    //Force firing if we're within close range, within specified angle
    void DoForceFire()
    {
        //San check we have a target
        if (AITargetShip && AITarget)
        {
            //check if the target is within the forward vector angle and distance
            if (Vector3.Distance(ship.transform.position, AITargetShip.transform.position) <= forceFireDist)
            {
                if (AngleTo(AITargetShip.transform.position) <= forceFireAngle)
                {
                    ship.FireGuns(true);
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
                    if (hitShip && hitShip.AITeam == ship.AITeam)
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
                ship.FireGuns(false);
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
        ShipSettings shootingShip = GameObjTracker.GetShipByID(ship.lastHitID);
        //check if we're being deliberately shot at!
        if (ship.isBeingShot)
        {
            //check if the last shot was from a ship other than our target, and *Isn't* a friendly.
            if (shootingShip !=  null && shootingShip != AITargetShip && shootingShip.AITeam != ship.AITeam)
            {
                //check if our shields are low
                if (ship.Shield.x < ship._ShieldMax.x / 3 || ship.Shield.y < ship._ShieldMax.y / 3)
                {
                    //check if the firing ship is behind us!
                    if (AngleTo(shootingShip.transform.position) > 200f)
                    {
                        //change our target over to the firing ship! 
                        AITarget = shootingShip.transform;
                        AITargetShip = shootingShip;
                    }
                }
            }
        }
        //if we're not deliberately being shot, check for that and then lower the threashold for action 
        //check if the last shot was from a ship other than our target, and *Isn't* a friendly.
        if (shootingShip != null && shootingShip != AITargetShip && shootingShip.AITeam != ship.AITeam)
        {
            //check if our shields are low
            if (ship.Shield.x < ship._ShieldMax.x / 5 || ship.Shield.y < ship._ShieldMax.y / 5)
            {
                //check if the firing ship is behind us!
                if (AngleTo(shootingShip.transform.position) > 200f)
                {
                    //change our target over to the firing ship! 
                    AITarget = shootingShip.transform;
                    AITargetShip = shootingShip;
                }
            }
        }
        //check if the last shot was from a ship other than our target, and *Is* a friendly. Higher threshold for a reposition.
        if (shootingShip != null && shootingShip != AITargetShip && shootingShip.AITeam == ship.AITeam)
        {
            //check if our shields are low
            if (ship.Shield.x < ship._ShieldMax.x / 2 || ship.Shield.y < ship._ShieldMax.y / 2)
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

    //Handle Target Cloaking
    void DoCloakedTarget()
    {
        if (AITarget && AITargetShip && AITargetShip.isCloaked)
        {
            AITargetShip = FindNearestShip(gameObject.transform, ship.AITeam);
            if (AITargetShip != null)
            {
                AITarget = AITargetShip.gameObject.GetComponent<Transform>();
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
    //Handle Gun Cooldown wait
    void DoGunCooldown(float waitTime, float minCapacitorLevel)
    {
        float normalizedCapacitorLevel = ship.capacitorLevel / ship.capacitorSize;
        // if the capacitors are low, add wait time
        if (ship.capacitorLevel < .1f && !cooldownWaiting)
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
            ship.FireGuns(false);
            if (normalizedCapacitorLevel >= minCapacitorLevel)
            {
                cooldownWait = 0;
                cooldownWaiting = false;
            }
        }
    }
    //Utility to find the nearest ship, ignoring one of the Teams, any cloaked ships, and the Ship looking
    public ShipSettings FindNearestShip(Transform toObj, ShipSettings.TEAM ignoreTEAM)
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
                if (shipTest.AITeam != ShipSettings.TEAM.NEUTRAL && shipTest != ship)
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
    //Ai frustration and wait-for-gun recharge
    public void DoImpatience(float maxImpatience, float howImpatient, float waitTime)
    {
        if (AITarget)
        {
            float distToTarget = Vector3.Distance(AITarget.position, transform.position);
            if (ship.capacitorLevel <= 2f) //if the AI can't shoot full blasts, increase impatience
            {
                impatience += Time.deltaTime * howImpatient * 4;
            }
            if (distToTarget < 60 && ship.capacitorLevel >= ship.capacitorSize / 4) //If we're close to oue close to our target, but CANT fire, increase Impatience, albiet at a slower rate 
            {
                impatience += Time.deltaTime * howImpatient;
            }
            if (distToTarget < engageDist / 2 && angleToTarget < 10f && ship.capacitorLevel / ship.capacitorSize >= .666f)
            {
                impatience += Time.deltaTime * howImpatient * 2;
            }
            if (impatience >= maxImpatience && ship.capacitorLevel > ship.capacitorSize / 3) //had enough, break off 
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
        if (GameObjTracker.frames % Update == 5)
        {
            randDist = Random.insideUnitSphere * Accuracy;
        }
        return randDist;
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
        Vector3 aimAt = targetPos + targetVelocity * timeToTarget;

        return aimAt;
    }


    //Init Wingman logic
    public void InitWingman(ShipSettings wingman)
    {
        if (!hasWingLead)
        {
            wingman = FindWingMan();
            if (wingman == null)//can't find anyone, bail
            {//print("No Wingleaders Available"); 
                return;
            }
            else //Make ourselves a slot to form up if we're just now finding a Wingleader
            {
                //print("Found Wingleader, setting up Params, Setting Formation Pos");
                formationPos = ((Random.Range(-.5f, .5f) * wingman.transform.up) + (Random.Range(-1f, 1f) * wingman.transform.right)) * ship.shipRadius * 8;
                wingman.numWingmen++;
                hasWingLead = true;
                WingmanTo = wingman;
            }
        }
        else // If we already know who our wingleader is, assume the current relational position is the desired formation position
        {
            //print("Wingleader exists on Init, setting up Params, Taking Existing Formation Pos");
            Vector3 initLoc = wingman.transform.position - transform.position;
            formationPos = initLoc;
            wingman.numWingmen++;
            hasWingLead = true;
            WingmanTo = wingman;
        }
    }
    //Find Wingmen
    public ShipSettings FindWingMan()
    {
        float distance = 100000f;
        ShipSettings wingMan = null;
        foreach (ShipSettings friendly in GameObjTracker.Ships)
        {
            if (friendly == null) //SOMEONE MUSTA DIED
            {
                // print("Someone Died");
                GameObjTracker.RegisterAllShips();
                GameObjTracker.RegisterTeams();
            }
            //Okay, check if we're still null, and if the ship we've found is *AKTUALLY* friendly, and looking for wingmen
            //AND isn't ourselves, AND doesn't already have 4 wingmen.
            if (friendly != null && friendly.isWingLead && friendly.AITeam == ship.AITeam && friendly != ship && friendly.numWingmen <= 4)
            {
                //print("Found a Wingleader in the scene! His name is:" + friendly.name);
                Transform friendlyTrans = (Transform)friendly.gameObject.GetComponent<Transform>();
                float shipDist = Vector3.Distance(friendlyTrans.position, transform.position);
                //if there are multiple, find the closest wingleader.
                if (shipDist < distance)
                {
                    distance = shipDist;
                    wingMan = friendly;
                    hasWingLead = true;
                    //print("Wingleader " + wingMan.name + " is the closest, and current");
                }
            }
        }
        //After going through all, return the closest.
        return wingMan;
    }
    //Define AI States
    void DoAIStates()
    {
        switch (ActiveAIState)
        {

            case AIState.PATROL:
                {
                    //stop firing, if we are
                    ship.FireGuns(false);
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
                        ship.targetSpeed = ship.topSpeed * .75f; //Cruise speed! No rush, juuust loooking for baddies. 
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
                    if (AITarget != null && DistanceTo(AITarget.gameObject) <= engageDist * 1.5f)
                    {//If we're withing the engage envelope, let's go check it out! 
                        ActiveAIState = AIState.ENGAGE;
                    }
                    //It's an Ambush! 
                    if (AITarget && AITargetShip && ship.lastHitID != 0)
                    {
                        AITargetShip = FindShipByID(ship.lastHitID, ship.AITeam);
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
                    if (WingmanTo != null && WingmanTo.currentTarget != null && AITargetShip)
                    {
                        AITargetShip = WingmanTo.currentTarget;
                    }
                    ActiveAIState = AIState.ENGAGE;
                }
                break;
            case AIState.WINGMAN:
                {
                    if (!hasWingLead)//Look for a wingleader in this state 
                    {
                        InitWingman(WingmanTo);
                    }
                    if (WingmanTo == null) // No wingleaders? Individual patrol mode! 
                    {
                        ActiveAIState = AIState.PATROL;
                    }
                    if (WingmanTo != null && hasWingLead)
                    {
                        //See how far away and what direction we need to go
                        var localFormPos = formationPos - (WingmanTo.transform.forward * WingmanTo.shipRadius * 5) + WingmanTo.transform.position;
                        var leadDist = Vector3.Distance(localFormPos, transform.position);
                        var dirToPos = (localFormPos) - transform.position;

                        Debug.DrawLine(gameObject.transform.position, localFormPos, Color.green, .10f);
                        if (leadDist != 0)
                        {
                            if (leadDist > 120)//If we're a ways off, aim right at the formation point and afterburn into position.
                            {
                                ship.targetSpeed = ship.burnSpeed;
                                SteerTo(localFormPos);
                            }
                            if (leadDist <= 120 && leadDist > 20) //If we're a moderate distance away, set speed to the lead ship +25%, aim at the formation position.
                            {
                                ship.targetSpeed = WingmanTo.speed + ship.topSpeed / 4;
                                SteerTo(localFormPos);
                            }
                            if (leadDist <= 20) //If we're close, Match speed, and aim at a point parallel to the direction of the lead ship
                            {
                                ship.targetSpeed = WingmanTo.speed;
                                SteerTo(localFormPos + WingmanTo.transform.forward * ship.shipRadius * 4f);
                                //A gentle push, like the avoidance system, to nudge us into place
                                float formPush = (dirToPos.magnitude / 10) * .5f;
                                transform.position += dirToPos * formPush * Time.deltaTime;
                            }
                            if (leadDist <= 30) // attempt to match roll once we get close-ish
                            {
                                transform.rotation = Quaternion.Lerp(transform.rotation, WingmanTo.transform.rotation, .005f);
                                //QuaternionUtil.SmoothDamp(transform.rotation,WingmanTo.transform.rotation, ref refForm, .15f);
                                ship.roll = WingmanTo.roll;
                            }
                            //AITarget is already the closest known enemy - let's use that! 
                            if (AITarget != null && Vector3.Distance(AITarget.position, transform.position) <= engageDist * .333f)
                            {//hold formation until we're very close
                                ActiveAIState = AIState.REPOSITION;
                            }
                        }
                    }
                    else
                    {
                        ActiveAIState = AIState.PATROL;
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

                        ship.targetSpeed = ship.topSpeed;

                        if (!AITarget)
                        {
                            ActiveAIState = AIState.PATROL;
                        }
                        if (Vector3.Distance(AITarget.position, transform.position) > engageDist)
                        {
                            ship.targetSpeed = ship.burnSpeed;
                        }
                        if (Vector3.Distance(AITarget.position, transform.position) <= engageDist)
                        {
                            randApproach = Vector3.zero;
                            ActiveAIState = AIState.HUNT;
                        }
                        //Does the ship have a cloaking device? If so, engage it!
                        if (ship.hasCloak)
                        {
                            ship.Cloak = true;
                        }

                    }
                    //Bail if there's no targetable enemies
                    if (!AITargetShip || !AITarget)
                    {
                        ActiveAIState = AIState.PATROL;
                    }
                    //print(gameObject.name + " Is engaging! Throttle set to " + ship.targetSpeed);
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
                        if (ship.hasCloak)
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
                            randApproach = Random.onUnitSphere * AITargetShip.shipRadius * aimAccuracy;
                        }

                        float angleToTarget = AngleTo(AITarget.position);
                        float distToTarget = Vector3.Distance(AITarget.position, transform.position);

                        //Closest Target is infront of us
                        if (angleToTarget < 140)
                        {
                            //If we're too far away to match speed to the target, get closer
                            if (distToTarget > followDist)
                            {
                                ship.targetSpeed = ship.topSpeed;
                            }
                            //match the target's speed
                            else
                            {
                                ship.targetSpeed = Mathf.Max(Mathf.Min(AITargetShip.targetSpeed, ship.topSpeed), ship.topSpeed / 4);
                            }
                            //Try and turn toward the target! 
                            if (distToTarget > engageDist)
                            {
                                if (angleToTarget < 60)
                                {
                                    ship.targetSpeed = ship.burnSpeed;
                                }
                                else
                                {
                                    ship.targetSpeed = ship.topSpeed;
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
                        Vector3 shootAt = DoRandomOffset(aimAccuracy, aimUpdate);
                        currentTargetPos = AITarget.position;// + shootAt;
                        Vector3 targetVelocity = AITargetShip.velocity;

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
                        if (angleToShoot < aimAccuracy *2f )
                        {
                            if (logDebug) { print("attempting to fire"); }
                            AITargetShip.isBeingShot = true;
                            ship.FireGuns(true);
                        }
                        //otherwise, stop firing
                        else 
                        {
                            AITargetShip.isBeingShot = false;
                            ship.FireGuns(false);

                            //unless we're *very* close, take the chance!
                            if (distToTarget < engageDist / 8)
                            {
                                if (angleToShoot < aimAccuracy * 4f)
                                {
                                    AITargetShip.isBeingShot = true;
                                    if (logDebug) { print("attempting to fire"); }
                                    ship.FireGuns(true);
                                }
                                //disable firing
                                else
                                {
                                    AITargetShip.isBeingShot = false;
                                    ship.FireGuns(false);
                                }
                            }
                            //disable firing
                            else
                            {
                                AITargetShip.isBeingShot = false;
                                ship.FireGuns(false);
                            }
                        }
                        //Too far away to shoot, or the angle is too much!
                        if (distToTarget > engageDist * 2 || AngleTo(AITarget.position) > 180)
                        {
                            AITargetShip.isBeingShot = false;
                            ship.FireGuns(false);
                        }
                        //we've gotten too far away, go back into engage mode
                        if (distToTarget > engageDist * 1.5f)
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
                        ship.FireGuns(false);

                        ship.targetSpeed = ship.burnSpeed;
                        if (EvadeSteer == Vector3.zero)// have we chosen where to steer? 
                        {
                            EvadeSteer = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));
                        }
                        //Does the ship have a cloaking device? If so, engage it!
                        if (ship.hasCloak)
                        {
                            ship.Cloak = true;
                        }
                    }
                    if (GameObjTracker.frames % Random.Range(60, 120) == 0) // every few second jerk around wildly! 
                    {
                        EvadeSteer = new Vector3(Random.Range(-2f, 2f), Random.Range(-2f, 2f), Random.Range(-2f, 2f));
                    }
                    ship.pitch = Mathf.Lerp(ship.pitch, EvadeSteer.x * evadeAmt, .001f);
                    ship.yaw = Mathf.Lerp(ship.yaw, EvadeSteer.y * evadeAmt, .001f);
                    ship.roll = Mathf.Lerp(ship.roll, EvadeSteer.z * evadeAmt, .001f);
                    //count down the time to return to normal combat. If we have a cloaking device, increase the wait time to be extra sneaky! 
                    if (!ship.hasCloak)
                    {
                        evadeTimer += Time.deltaTime;
                    }
                    else
                    {
                        evadeTimer += Time.deltaTime / 4f;
                    }
                    if (evadeTimer >= evadeLength)
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
                        ship.FireGuns(false);

                        randPos = Vector3.zero;
                        if (randPos.magnitude == 0)
                        {
                            randPos = transform.position + Random.onUnitSphere * engageDist;
                        }
                        if (distToTarget < 80f && distToTarget > 40f)
                        {
                            ship.targetSpeed = ship.burnSpeed;
                        }
                        else
                        {
                            ship.targetSpeed = ship.topSpeed;
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
                    if (AISkillLevel != AILevel.CHUMP || AISkillLevel != AILevel.NOVICE)
                    {
                        ship.FireGuns(false);
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
        avoidAngle = 10f;
        avoidDist = 20f;
        avoidSpeed = .05f;
        avoidTime = 4f;
        forceFireAngle = 30f;
        forceFireDist = engageDist / 5f;
        ship.targetSpeed = ship.topSpeed * .75f;
        SteerTo(new Vector3(0, 50, 200));
        RollControl(Random.Range(-4000f, 1f));
    }
    //Novice AI Settings
    void NoviceAI()
    {
        avoidAngle = 15f;
        avoidDist = 75f;
        avoidSpeed = .125f;
        avoidTime = 3f;
        AILeadAmt = 1f;
        engageDist = 150f;
        aimAccuracy = 6f;
        aimUpdate = 20;
        evadeLength = .25f;
        evadeAmt = 1f;
        forceFireAngle = 20f;
        forceFireDist = engageDist / 5f;

        if (followDist == 0)
        {
            followDist = Random.Range(75f, 125f);
            //print(name + " has a follow distance of " +followDist);
        }
        if (!AITargetShip)
        {
            AITargetShip = FindNearestShip(gameObject.transform, ship.AITeam);
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
            if (ship.hitInAss && ship.Shield.y <= .5f && ship.lastHit == ShipSettings.HitLoc.B) //WE're being hit from behind, shields low, HOLY SHIT, EVADE!
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
        avoidAngle = 15f;
        avoidDist = 50f;
        avoidSpeed = .15f;
        avoidTime = 2.5f;
        AILeadAmt = 1.25f;
        engageDist = 175;
        aimAccuracy = 4f;
        aimUpdate = 10;
        evadeLength = .5f;
        evadeAmt = 1.5f;
        forceFireAngle = 17.5f;
        forceFireDist = engageDist / 4.5f;
        if (followDist == 0)
        {
            followDist = Random.Range(65f, 100f);
        }
        if (!AITargetShip)
        {
            AITargetShip = FindNearestShip(gameObject.transform, ship.AITeam);
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
            if (ship.hitInAss && ship.Shield.y <= .6f && ship.lastHit == ShipSettings.HitLoc.B) //WE're being hit from behind, shields low, HOLY SHIT, EVADE!
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
        avoidAngle = 20f;
        avoidDist = 40f;
        avoidSpeed = .2f;

        avoidTime = 2f;
        AILeadAmt = 1.5f;
        engageDist = 200;
        aimAccuracy = 3f;
        aimUpdate = 2;
        evadeLength = 2f;
        evadeAmt = 3f;
        forceFireAngle = 15f;
        forceFireDist = engageDist / 4f;
        if (followDist == 0)
        {
            followDist = Random.Range(55f, 80f);
        }
        if (!AITargetShip)
        {
            AITargetShip = FindNearestShip(gameObject.transform, ship.AITeam);
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
            if (ship.hitInAss && ship.Shield.y <= .9f && ship.lastHit == ShipSettings.HitLoc.B) //WE're being hit from behind, shields low, HOLY SHIT, EVADE!
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
        switch (AISkillLevel)
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
        }
        DoCloakedTarget();
        DoNoTargets();
        DoCollisionAvoidance();
        DoBeingShot();
        DoForceFire();
        DoFriendlyFire();
    }


}
