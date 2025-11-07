using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

// TODO: rename to AIShip
[RequireComponent(typeof(ShipSettings))]
public partial class AIPlayer : AIUnit
{
    #region FIELDS
    [Header("Settings")]
    [SerializeField] private AIShipSkillSettings skillSettings;
    public MessageHandler messageHandler;
    [SerializeField] private AIState activeAIState = AIState.WINGMAN;
  
    [Header("Patrol Pattern")]
    [SerializeField] private List<Vector3> PatrolPoints;

    [Header("Debug Options")]
    [SerializeField] private bool doDebugOrient = false;
    [SerializeField] private GameObject debugOrient;
    [SerializeField] private Transform DEBUG_destination;

    [HideInInspector] public GameObjTracker Tracker;
    [HideInInspector] public float impatience;
    [HideInInspector] public float angleToTarget;

    //Internal settings and flags
    ShipSettings ship;

    Vector3 smoothAimAt = Vector3.forward;

    float followDist;
    Vector3 randPos = Vector3.zero;

    bool isRolling = false;
    double rollEnd;
    float rollDir;
    float barrelRoll;

    bool isAvoiding = false;
    float avoidTimer = 0f;

    float friendlyFireAvoidAngle;
    float friendlyFireTime;
    float friendlyFireTimer = 0f;

    Vector3 randApproach = Vector3.zero;
    Vector3 EvadeSteer = Vector3.zero;

    int nextPatrolPoint = 0;
    float evadeTimer = 0f;

    Vector3 randDist = Vector3.zero;
    Vector3 currentTargetPos;

    bool willOvershootDestination = false;
    #endregion

    #region PROPERTIES
    public AIState ActiveAIState
    {
        get => activeAIState;
        set
        {
            //OMEPLogger.Log(this, value);
            activeAIState = value;
        }
    }

    public AIShipSkillSettings SkillSettings
    {
        get => skillSettings;
        set => skillSettings = value;
    } 

    protected override Capacitor MainCapacitor => ship.MainCapacitor;

    public bool IsRolling => isRolling;

    /// <summary>
    /// <c>true</c> if our destination is within our turning radius but
    /// not in front of us; in other words, we'd end up circling around 
    /// it.
    /// </summary>
    public bool WillOvershootDestination => willOvershootDestination;
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

    [Range(0f, 1.5f), Tooltip("Scales our turning radius for the purpose of determining if we're going to overshoot " +
        "our destination. Higher values better prevent overshooting, but also reduce responsiveness.")]
    [SerializeField] private float turnRadiusPaddingFactor = 1.25f;
    bool CheckWillOvershootDestination(Vector3 destination)
    {
        float speed = ship.Velocity.magnitude;
        if (speed == 0) return false;

        Vector3 direction = destination - transform.position;
        float distance = direction.magnitude;
        if (distance < .1)
            return false; // Already at target

        var maxTurnRate = ship.Settings.TurnRate;
        Vector3 dirVel = ship.Velocity.normalized;
        float omega = maxTurnRate * Mathf.Deg2Rad;
        const float PADDING_FACTOR = 1.25f;
        float turnRadius = (speed / omega) * PADDING_FACTOR;

        float theta = Mathf.Acos(Mathf.Clamp(Vector3.Dot(dirVel, direction.normalized), -1f, 1f));
        bool willOvershoot = (distance < 2f * turnRadius * Mathf.Sin(theta * 0.5f)) && (theta > 0f);

        if (willOvershoot) Debug.DrawLine(transform.position, destination, new Color(.3f, 0f, 0f, 1f), .1f);

        return willOvershoot;
    }

    [Range(-1f, 1f), Tooltip("Scales how we turn when trying not to overshoot a destination")]
    [SerializeField] private float counterOvershootScale = -.07f;
    [SerializeField] private bool fancyRolling = true;
    [Range(0f, 1f), Tooltip("Min. time before we stop trying to overshoot. Higher values reduce twitchiness, but also reduce responsiveness.")]
    [SerializeField] private float overshootCooldown = .25f;
    double overshootCooldownEnd;
    void SteerTo(Vector3 aimAt)
    {
        smoothAimAt = Vector3.Lerp(smoothAimAt, aimAt, .25f);

        if (!isAvoiding) {
            // Check if we're going to overshoot the destination. If we determine that
            // we are going to overshoot, we apply a cooldown before we can check again.
            // This helps to prevent twitchy behavior.
            if (Time.timeAsDouble > overshootCooldownEnd)
            {
                willOvershootDestination = CheckWillOvershootDestination(smoothAimAt);
                if (willOvershootDestination)
                {
                    overshootCooldownEnd = Time.timeAsDouble + overshootCooldown;
                }
            }

            Vector3 localDir = transform.InverseTransformPoint(smoothAimAt);
            float pitchDist = -Vector3.SignedAngle(Vector3.forward, new Vector3(0, localDir.y, localDir.z), Vector3.right);
            float yawDist = Vector3.SignedAngle(Vector3.forward, new Vector3(localDir.x, 0, localDir.z), Vector3.up);

            if (willOvershootDestination)
            {
                pitchDist *= -counterOvershootScale;
                yawDist *= -counterOvershootScale;
                //rollDist *= -counterOvershootScale;
            }

            float turnRateFactor = ship.Settings.TurnRate / 2f;
            float newPitchDest = pitchDist / turnRateFactor;
            float newYawDest = yawDist / turnRateFactor;

            float newRollDest;
            if (fancyRolling)
            {
                float rollDist = Vector3.SignedAngle(Vector3.up, new Vector3(localDir.x, localDir.y, 0), Vector3.forward); // Roll to keep target above us
                newRollDest = rollDist;// * turnRateFactor + barrelRoll;
            } else
            {
                newRollDest = (-newYawDest / 2f) + barrelRoll;
            }

            newPitchDest = Mathf.Clamp(newPitchDest, -1f, 1f);
            newYawDest = Mathf.Clamp(newYawDest, -1f, 1f);
            newRollDest = Mathf.Clamp(newRollDest, -1f, 1f);

            float scaledT = skillSettings.TurnSpeed * 60 * Time.deltaTime; // Scale for framerate, assuming default framerate is 60fps
            ship.yaw = Mathf.Lerp(ship.yaw, newYawDest, scaledT);//Mathf.SmoothStep(ship.yaw,0f,.1f);
            ship.pitch = Mathf.Lerp(ship.pitch, newPitchDest, scaledT);//Mathf.SmoothStep(ship.pitch,0f,.1f);
            ship.roll = Mathf.Lerp(ship.roll, newRollDest, scaledT);
        }
    }

    //Roll the ship for more dynamic movement!
    void RollControl(float oddsAgainst)
    {
        //occasionally spin! 
        if (!isRolling && Random.Range(0, oddsAgainst) < 1f)
        {
            isRolling = true;
            rollDir = Random.Range(-1f, 1f);
            rollEnd = Time.timeAsDouble + Random.Range(.5f, 6f);
            //print(gameObject.name+" starting to " + rollDir +" roll for: "+rollLength +"sec");
        }
       
        if (isRolling)
        {
            barrelRoll += rollDir * ship.Settings.TurnRate * Time.deltaTime;
            if (Time.timeAsDouble > rollEnd)
            {
                isRolling = false;
                //bool wasNegative = barrelRoll < 0;
                //barrelRoll = Mathf.Repeat(Mathf.Abs(barrelRoll), 360f);
                //if (wasNegative) barrelRoll *= -1f;
                barrelRoll = 0;
            }
        } else
        {
            //barrelRoll = Mathf.MoveTowards(barrelRoll, 0, ship.Settings.TurnRate * Time.deltaTime);
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
                                if (verboseLogging){ print(ship.DisplayName + " is avoiding " + tarShip.DisplayName);}
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
        if (AITarget != null)
        {
            //check if the target is within the forward vector angle and distance
            if (Vector3.Distance(ship.transform.position, AITarget.transform.position) <= skillSettings.ForceFireDistance)
            {
                if (AngleTo(AITarget.transform.position) <= skillSettings.ForceFireAngle)
                {
                    weaponsSystem.FireGuns();
                    if (verboseLogging) { print(ship.DisplayName + " is forcing fire!"); }
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
        if (ship.isFiring && AITarget != null)
        {
            Vector3 us = ship.transform.position;
            Vector3 usForward = ship.transform.forward;
            Vector3 target = AITarget.transform.position;
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
                        if (verboseLogging) { print(ship.DisplayName + " is avoiding friendly fire!"); }
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
        if (AITarget != null)
        {
            if (ship.hitInAss && ship.Shield.Back <= skillSettings.ShieldLowThreshold && ship.lastHit == ShipSettings.HitLoc.B) //WE're being hit from behind, shields low, HOLY SHIT, EVADE!
            {
                ship.hitInAss = false;
                if (AngleTo(AITarget.transform.position) > skillSettings.InFrontAngleThreshold)  //If our target is in front of us , just reposition, otherwise evade
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

        // TODO: none of this logic is functional, because the angle can never be greater
        // than 180!
        const float BEHIND_US_ANGLE = 200;

        //track who's been shooting at us
        ShipSettings shootingShip = GameObjTracker.Instance.GetShipByID(ship.lastHitID);
        //check if we're being deliberately shot at!
        if (ship.isBeingShot)
        {
            //check if the last shot was from a ship other than our target, and *Isn't* a friendly.
            if (shootingShip !=  null && shootingShip != AITarget && shootingShip.Team != ship.Team)
            {
                CheckIfShieldsLow(shootingShip, 1 / 3f, BEHIND_US_ANGLE);
            }
        }
        //if we're not deliberately being shot, check for that and then lower the threashold for action 
        //check if the last shot was from a ship other than our target, and *Isn't* a friendly.
        if (shootingShip != null && shootingShip != AITarget && shootingShip.Team != ship.Team)
        {
            CheckIfShieldsLow(shootingShip, 1 / 5f, BEHIND_US_ANGLE);
        }
        //check if the last shot was from a ship other than our target, and *Is* a friendly. Higher threshold for a reposition.
        if (shootingShip != null && shootingShip != AITarget && shootingShip.Team == ship.Team)
        {
            //check if our shields are low
            const float LOW_FACTOR = 1 / 2f;
            if (ship.ShieldFrontNormalized <  LOW_FACTOR || ship.ShieldBackNormalized < LOW_FACTOR)
            {
                //check if the firing ship is behind us!
                if (AngleTo(shootingShip.transform.position) > BEHIND_US_ANGLE)
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
                AITarget = shootingShip;
            }
        }
    }

    //Handle Target Cloaking
    void DoCloakedTarget()
    {
        if (AITarget != null && AITarget.IsCloaked)
        {
            AITarget = FindNearestShip(gameObject.transform, ship.Team);
        }
    }
    //Handle no enemies
    void DoNoTargets()
    {
        if (AITarget == null)
        {
            GoToDefaultState();
        }
    }

    //Utility to find the nearest ship, ignoring one of the Teams, any cloaked ships, and the Ship looking
    public ShipSettings FindNearestShip(Transform toObj, TEAM ignoreTEAM)
    {
        float distance = skillSettings.EngageDistance * 10f;

        ShipSettings nearestShip = null;
        foreach (ShipSettings ship in GameObjTracker.Instance.AllShips)
        {
            if (ship != null && !ship.IsCloaked)
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
        if (AITarget != null)
        {
            float distToTarget = Vector3.Distance(AITarget.transform.position, transform.position);
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
        return AITarget.transform.position + (Random.onUnitSphere * aimRand);
    }

    // Handy tool to predict where we need to Aim at our target! 
    public Vector3 PredictV3Pos(Vector3 muzzlePos, float bulletVelocity, Vector3 targetPos, Vector3 targetVelocity)
    {
        float dist = Vector3.Distance(muzzlePos, targetPos);
        float timeToTarget = dist / bulletVelocity;
        Vector3 aimAt = targetPos + targetVelocity * timeToTarget;

        return aimAt;
    }

    private void TryJoinFlight()
    {
        FlightManager.Instance.TryJoinClosestFlight(ship, 10000, true);
    }

    //Define AI States
    void DoAIStates()
    {
        switch (ActiveAIState)
        {

            case AIState.PATROL:
                Patrol();
                break;

            case AIState.BREAK: //Break formation and Attack!
                {
                    if (ship.Flight != null && ship.Flight.Leader.CurrentTarget != null)
                    {
                        AITarget = ship.Flight.Leader.CurrentTarget;
                    }
                    ActiveAIState = AIState.CHASE;
                }
                break;

            case AIState.WINGMAN:
                Wingman();
                break;

            case AIState.CHASE: // If we have a target, follow it.
                Chase();
                break;

            case AIState.ATTACK: // If we're close to our target, attack it.
                Attack();
                break;

            case AIState.EVADE:
                Evade();
                break;

            case AIState.REPOSITION:
                Reposition();
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

    private void GoToDefaultState()
    {
        if (ship.Flight == null || ship.Flight.Leader == ship)
        {
            ActiveAIState = AIState.PATROL;
        } 
        else
        {
            ActiveAIState = AIState.WINGMAN;
        }
    }

    //Dumb as rocks AI
    void ChumpAI()
    {
        ship.Engines.TargetSpeed = ship.Settings.TopSpeed * .75f;
        SteerTo(new Vector3(0, 50, 200));
        RollControl(4000f);
    }
    //Novice AI Settings
    void NoviceAI()
    {
        if (followDist == 0)
        {
            followDist = Random.Range(75f, 125f);
            //print(name + " has a follow distance of " +followDist);
        }
        if (AITarget == null)
        {
            AITarget = FindNearestShip(gameObject.transform, ship.Team);
        }
        DoImpatience(3f, 2f, 1f);
        DoAIStates();
        RollControl(4000f);
        if (AITarget != null)
        {
            angleToTarget = AngleTo(AITarget.transform.position);
        }
        ship.CurrentTarget = AITarget;
        DoGunCooldown(1f, .125f);
    }

    //Default AI Settings!
    void DefaultAI()
    {
        if (DEBUG_destination != null)
        {
            ship.Engines.TargetSpeed = ship.Settings.TopSpeed;
            RollControl(400);
            SteerTo(DEBUG_destination.position);
            return;
        }

        if (followDist == 0)
        {
            followDist = Random.Range(65f, 100f);
        }
        if (AITarget == null)
        {
            AITarget = FindNearestShip(gameObject.transform, ship.Team);
        }
        DoImpatience(2.5f, 1f, 2f);
        DoAIStates();
        RollControl(2500f);
        if (AITarget != null)
        {
            angleToTarget = AngleTo(AITarget.transform.position);
        }
        ship.CurrentTarget = AITarget;
        DoGunCooldown(1f, .2f);
    }
    //Ace AI Settings!
    void AceAI()
    {
        if (followDist == 0)
        {
            followDist = Random.Range(55f, 80f);
        }
        if (AITarget == null)
        {
            AITarget = FindNearestShip(gameObject.transform, ship.Team);
        }
        if (AITarget != null)
        {
            // TODO: does this belong here?
            if (AITarget is ShipSettings targetShip)
            {
                if (ActiveAIState == AIState.ATTACK || ActiveAIState == AIState.REPOSITION)
                {
                    targetShip.IsLocked = true;
                }
                else
                {
                    targetShip.IsLocked = false;
                }
            }
        }
        DoImpatience(2f, .75f, 2.5f);
        DoAIStates();
        RollControl(1500f);
        if (AITarget != null)
        {
            angleToTarget = AngleTo(AITarget.transform.position);
        }
        ship.CurrentTarget = AITarget;
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
            Gizmos.color = new Color(1f, 0f, 0f, .5f);
            Gizmos.DrawLine(transform.position, AITarget.transform.position);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (AITarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, AITarget.transform.position);
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
                        UnityEditor.Selection.activeObject = (instance.AITarget);
                    }
                }

                UnityEditor.EditorGUILayout.LabelField("Will overshoot", instance.WillOvershootDestination.ToString());

                if (instance.DEBUG_destination == null)
                {
                    if (GUILayout.Button("Create debug destination"))
                    {
                        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        go.name = "DESTINATION";
                        go.transform.localScale = Vector3.one * 5f;
                        var distance = 50f;
                        go.transform.position = instance.transform.TransformPoint(new Vector3(distance, distance, distance));
                        instance.DEBUG_destination = go.transform;
                    }
                } else if (GUILayout.Button("Randomize destination"))
                {
                    instance.DEBUG_destination.transform.position = (Random.insideUnitSphere * 300) + instance.transform.position;
                }
            }
        }
    }
#endif
}
