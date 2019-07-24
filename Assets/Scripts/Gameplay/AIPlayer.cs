using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ShipSettings))]
public class AIPlayer : MonoBehaviour
{
  public bool logDebug = false;
  public enum AIState {PATROL, BREAK, SEARCH, WINGMAN, ENGAGE, HUNT, EVADE, PROTECT, REPOSITION, FLEE, DEATH };
  public enum AILevel {CHUMP, NOVICE, DEFAULT, SKILLED, ACE, MASTER};
  public ShipSettings WingmanTo;
  public List<Vector3> PatrolPoints;

  public AIState ActiveAIState = AIState.PATROL;
  public AILevel AISkillLevel = AILevel.CHUMP;


  [HideInInspector]
  public Transform AITarget;
  [HideInInspector]
  public GameObjTracker Tracker;

  ShipSettings ship;
  LaserCannon[] laserCannons;
  Vector3 smoothDir = Vector3.zero;
  Vector3 refDir = Vector3.zero;
  Quaternion refRot = Quaternion.identity;
  Quaternion refForm = Quaternion.identity;
  void SteerTo(Vector3 aimAt, float turnSpeed)
  {
    //kill any manual steering
    ship.yaw = Mathf.Lerp(ship.yaw,0,.02f);//Mathf.SmoothStep(ship.yaw,0f,.1f);
    ship.pitch =  Mathf.Lerp(ship.pitch,0,.025f);;//Mathf.SmoothStep(ship.pitch,0f,.1f);
    //smooth autosteer
    Vector3 targetDir = aimAt - transform.position;
    smoothDir = Vector3.SmoothDamp(smoothDir,targetDir,ref refDir, .1f);
    
    Quaternion newDir = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(smoothDir,transform.up), turnSpeed * 7.5f * Time.fixedUnscaledDeltaTime);
    transform.rotation = QuaternionUtil.SmoothDamp(transform.rotation,newDir, ref refRot, .1f);
  }

  bool rolling = false;
  float rollStart;
  float rollDir;
  float rollLength;

  void Start()
  {
    ship = GetComponent<ShipSettings>(); 
    laserCannons = GetComponentsInChildren<LaserCannon>();    
    ForceRegister();
  }

void ForceRegister()
{
  gameObject.transform.SetParent(GameObject.FindWithTag("GamePlayObjs").transform);
  GameObjTracker.RegisterAllShips();
  GameObjTracker.RegisterTeams();
}

void FireGuns(bool fire)
{
  foreach (LaserCannon laserCannon in laserCannons)
  {
    if(ship.recover >= .99f && rechargeWait <= 0.01f) // Can the ship fire? 
    {
       laserCannon.fire = fire;
      // if(logDebug){print("aactually setting state to " + fire);}
      if(fire)//are we firing?
      { bloodThirst = 0f; //Ahh, our bloodthirst is sated
        ship.isFiring = true; //Make sure our broadcast flag is set! 
      }
      else
      {
        ship.isFiring = false; //Make sure our broadcast flag is set! 
      }
    }
    if (rechargeWait >= 0.01f || ship.recover < .75f) //wait for recharge or return of control! 
    {
      laserCannon.fire = false;
      ship.isFiring = false;
    }
  }
}


void DoABarrelRoll(float direction, float length)
{
  if (!rolling)
    return;
    if(Time.time <= rollStart+length)
    {
      ship.roll = Mathf.SmoothStep(ship.roll,direction,.05f);
    }
    else 
    {
      rolling = false;
    }
}
void StopRoll(){
  ship.roll = Mathf.SmoothStep(ship.roll,0,.05f);
}
void RollControl(float rollOn)
{
  //occasionally spin! 
  if(rollOn > 0 && !rolling)
  {
    rolling = true;
    rollDir = Random.Range(-1,2);
    if(rollDir == 0)
      rollDir = Random.Range(-1,2);
    if(rollDir == 0)
      rollDir = Random.Range(-1,2);
    if(rollDir == 0)
      rollDir = Random.Range(-1,2);
    if(rollDir == 0)
      rollDir = 1;
    
    rollLength = Random.Range(.5f,6f);
    rollStart = Time.time;
    //print(gameObject.name+" starting to " + rollDir +" roll for: "+rollLength +"sec");
  }
  DoABarrelRoll(rollDir,rollLength);
  if(!rolling)
  {
    StopRoll();
  }
}

  //Find the nearest ship, ignoring one of the Teams, and the Ship looking
  public ShipSettings FindNearestShip(Transform toObj, ShipSettings.TEAM ignoreTEAM)
  {
    float distance = 100000f;
    ShipSettings nearestShip = null;
    foreach (ShipSettings shipTest in GameObjTracker.Ships)
    {
      if(shipTest == null) //SOMEONE MUSTA DIED
      {
        GameObjTracker.RegisterAllShips();
        GameObjTracker.RegisterTeams();
      }
      if(shipTest != null)
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
    if(nearestShip != null)
      return nearestShip;
    else
      return null;
  }
  public float DistanceTo(GameObject obj)
  {
    float dist = Vector3.Distance(obj.transform.position,transform.position);
    return dist;
  }

  float engageDist = 150f;
  float aimAccuracy = 30f;
  int aimUpdate = 60;
  float followDist;
  public float impatience;
  float bloodThirst;
  Vector3 randPos = Vector3.zero;
  public float angleToThing;
  ShipSettings AITargetShip;
  Vector3 formationPos;
  float evadeLength;
  float evadeAmt;
  float AILeadAmt = 1f;

  void ChumpAI()
  {
     ship.targetSpeed = ship.topSpeed*.75f;
  } 

void NoviceAI()
{
  AILeadAmt = .25f;
  engageDist = 150f;
  aimAccuracy = 10f;
  aimUpdate = 60;
  evadeLength = .25f;
  evadeAmt = 1f;
  if(followDist == 0){
    followDist = Random.Range(75f,125f);
    //print(name + " has a follow distance of " +followDist);
  }
  if(!AITargetShip)
  {
  AITargetShip = FindNearestShip(gameObject.transform,ship.AITeam);
  }
  if(AITargetShip != null)
  {AITarget = AITargetShip.gameObject.GetComponent<Transform>();
  }

  if (AITargetShip && Vector3.Distance(AITarget.position,transform.position) <= 100 )
  { 
    if(ActiveAIState == AIState.ENGAGE)
    {
      ActiveAIState = AIState.HUNT;
    }
  }
  if(ship.hitInAss && ship.Shield.y <= .5f && ship.lastHit == ShipSettings.HitLoc.B) //WE're being hit from behind, shields low, HOLY SHIT, EVADE!
  {
    
    ship.hitInAss = false;
    if( AngleTo(AITarget.position) > 30)  //If our target is in front of us , just reposition, otherwise evade
    { evadeTimer = 0f;
      ActiveAIState = AIState.EVADE;
    }
    else{
      ActiveAIState = AIState.REPOSITION;
    }
  }
  DoImpatience(3f, 2f, 1f);
  DoAIStates();
  RollControl(Random.Range(-4000f,1f));
  angleToThing = AngleTo(AITarget.position);
  ship.currentTarget = AITargetShip;
}

void DefaultAI()
{
  AILeadAmt = .5f;
  engageDist = 175;
  aimAccuracy = 6f;
  aimUpdate = 30;
  evadeLength = .5f;
  evadeAmt = 1.5f;
  if(followDist == 0){
    followDist = Random.Range(65f,100f);
  }
  if(!AITargetShip)
  {
  AITargetShip = FindNearestShip(gameObject.transform,ship.AITeam);
  }
  if(AITargetShip != null)
  {AITarget = AITargetShip.gameObject.GetComponent<Transform>();
  }

  if (AITargetShip && Vector3.Distance(AITarget.position,transform.position) <= 100 )
  { 
    if(ActiveAIState == AIState.ENGAGE)
    {
      ActiveAIState = AIState.HUNT;
    }
  }
  if(ship.hitInAss && ship.Shield.y <= .6f && ship.lastHit == ShipSettings.HitLoc.B) //WE're being hit from behind, shields low, HOLY SHIT, EVADE!
  {
    ship.hitInAss = false;
    if( AngleTo(AITarget.position) > 20)  //If our target is in front of us , just reposition, otherwise evade
    { evadeTimer = 0f;
      ActiveAIState = AIState.EVADE;
    }
    else{
      ActiveAIState = AIState.REPOSITION;
    }
  }
  DoImpatience(2.5f, 1f,2f);
  DoAIStates();
  RollControl(Random.Range(-2500f,1f));
  angleToThing = AngleTo(AITarget.position);
  ship.currentTarget = AITargetShip;
}
void AceAI()
{
  AILeadAmt = 1f;
  engageDist = 200;
  aimAccuracy = 4f;
  aimUpdate = 5;
  evadeLength = 1f;
  evadeAmt = 3f;
  if(followDist == 0){
    followDist = Random.Range(55f,80f);
  }
  if(!AITargetShip)
  {
  AITargetShip = FindNearestShip(gameObject.transform,ship.AITeam);
  }
  if(AITargetShip != null)
  {AITarget = AITargetShip.gameObject.GetComponent<Transform>();
  }

  if (AITargetShip && Vector3.Distance(AITarget.position,transform.position) <= 100 )
  { 
    if(ActiveAIState == AIState.ENGAGE)
    {
      ActiveAIState = AIState.HUNT;
    }
  }
  if(ship.hitInAss && ship.Shield.y <= .9f && ship.lastHit == ShipSettings.HitLoc.B) //WE're being hit from behind, shields low, HOLY SHIT, EVADE!
  {    
    ship.hitInAss = false;
    if( AngleTo(AITarget.position) > 15)  //If our target is in front of us , just reposition, otherwise evade
    { evadeTimer = 0f;
      ActiveAIState = AIState.EVADE;
    }
    else{
      ActiveAIState = AIState.REPOSITION;
    }
  }
  
  DoImpatience(2f, .75f, 2.5f);
  DoAIStates();
  RollControl(Random.Range(-1500f,1f));
  angleToThing = AngleTo(AITarget.position);
  ship.currentTarget = AITargetShip;
}


float rechargeWait = 0f;

//Ai frustration and wait-for-gun recharge
public void DoImpatience(float maxImpatience, float howImpatient, float waitTime) 
{
  float distToTarget = Vector3.Distance(AITarget.position,transform.position);
  if(ship.capacitorLevel <=2f) //if the AI can't shoot full blasts, increase impatience
  {
    impatience += Time.deltaTime*howImpatient*4;
  }
  if (distToTarget < 60 && ship.capacitorLevel >= ship.capacitorSize/4) //If we're close to oue close to our target, but CANT fire, increase Impatience, albiet at a slower rate 
  {
    impatience += Time.deltaTime*howImpatient;
  }
  if (distToTarget < engageDist/2 && angleToThing < 10f && ship.capacitorLevel/ship.capacitorSize >= .666f)
  {
    impatience += Time.deltaTime*howImpatient*2;
  }
  if (impatience >= maxImpatience && ship.capacitorLevel > ship.capacitorSize/3) //had enough, break off 
  {
    impatience = 0f; //We did something about it, calm down
    ActiveAIState = AIState.REPOSITION;
  }  
  if (impatience >= maxImpatience && ship.capacitorLevel <= ship.capacitorSize/3) //we need to wait for our guns to recharge. wait.
  {
    impatience = 0f; //We did something about it, calm down
    rechargeWait = waitTime;
  }
  ///but we *also gradually calm down
  if(impatience > 0)
  {
    impatience -= Time.deltaTime;
  }
  if (rechargeWait >= 0) //wait for recharge! 
  {
    rechargeWait -= Time.deltaTime;
  }
}
//AI bloodthirstyness
public void DoBloodThirsty(float maxThirsty)
{

}

Vector3 randDist = Vector3.zero;

public Vector3 DoAiming(float Accuracy, float Update)
{    
  if(GameObjTracker.frames % Update == 0)
  {
    randDist = Random.insideUnitSphere*Accuracy;
    }
    return randDist;  
}
public float AngleTo(Vector3 target) //Handy thing -since the cockpits can have offset pitches to line up the reticles, we need to adust our forward angle if it's a player ship.
{
  if(target == null)
    return 0f;

  Vector3 tempForward;
  tempForward = transform.forward;
  /*Camera isPlayer = GetComponentInChildren<Camera>();
  if (isPlayer != null)
  {

  }*/
  
  return Vector3.Angle(tempForward, target-transform.position);
}
public Vector3 DoAim(float aimRand)
{
      return AITarget.position + (Random.onUnitSphere * aimRand);
}
Vector3 patrolPoint;
Vector3 randApproach = Vector3.zero;
Vector3 EvadeSteer = Vector3.zero;
bool hasWingLead = false;

//Init Wingman System vars
public void InitWingman(ShipSettings wingman)
{
  if(!hasWingLead) 
    {
      wingman = FindWingMan();
      if (wingman == null)//can't find anyone, bail
      {//print("No Wingleaders Available"); 
      return; }
      else //Make ourselves a slot to form up if we're just now finding a Wingleader
      {
        //print("Found Wingleader, setting up Params, Setting Formation Pos");
        formationPos = ((Random.Range(-.5f,.5f)*wingman.transform.up)+(Random.Range(-1f,1f)*wingman.transform.right))*ship.shipRadius*8;
        wingman.numWingmen++;
        hasWingLead = true;
        WingmanTo = wingman;
      }
    }
    else // If we already know who our wingleader is, assume the current relational position is the desired formation position
    {
        //print("Wingleader exists on Init, setting up Params, Taking Existing Formation Pos");
        Vector3 initLoc = wingman.transform.position-transform.position;
        formationPos = initLoc;
        wingman.numWingmen++;
        hasWingLead = true;
        WingmanTo = wingman;
    }
}

  public ShipSettings FindWingMan()
  {
    float distance = 100000f;
    ShipSettings wingMan = null;
    foreach (ShipSettings friendly in GameObjTracker.Ships)
    {
      if(friendly == null) //SOMEONE MUSTA DIED
      {
       // print("Someone Died");
        GameObjTracker.RegisterAllShips();
        GameObjTracker.RegisterTeams();
      }
      //Okay, check if we're still null, and if the ship we've found is *AKTUALLY* friendly, and looking for wingmen
      //AND isn't ourselves, AND doesn't already have 4 wingmen.
      if(friendly != null && friendly.isWingLead && friendly.AITeam == ship.AITeam && friendly != ship && friendly.numWingmen <= 4)
      {
        //print("Found a Wingleader in the scene! His name is:" + friendly.name);
        Transform friendlyTrans = (Transform)friendly.gameObject.GetComponent<Transform>();
        float shipDist = Vector3.Distance(friendlyTrans.position, transform.position);
        //if there are multiple, find the closest wingleader.
        if (shipDist < distance )
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


int nextPatrolPoint = 0;
float evadeTimer = 0f;

void DoAIStates(){
switch(ActiveAIState)
{

  case AIState.PATROL:
  {
    //Check to see if we've got any patrol points assigned already! 
    if(PatrolPoints.Count == 0)
    {
      //Oh no! We need some to patrol, let's generate some, lessay 4
      int pp = 0;
      int numPoints = 4;
      while (pp <= numPoints)
      {
        PatrolPoints.Add(Random.onUnitSphere*Random.Range(320f,500f));
        pp++;
      }
    }
    float ppDist = Vector3.Distance(transform.position,PatrolPoints[nextPatrolPoint]);
    if(ppDist > 10)
    {
      SteerTo(PatrolPoints[nextPatrolPoint], ship.turnRate);
      ship.targetSpeed = ship.topSpeed*.75f; //Cruise speed! No rush, juuust loooking for baddies. 
    }
    else
    {
      patrolPoint = PatrolPoints[nextPatrolPoint];
      print(gameObject.name + " Reached patrol point " + nextPatrolPoint + " going to the next!");
      nextPatrolPoint++;
    }
    if(nextPatrolPoint > PatrolPoints.Count-1) //Cycle the patrol point list
    { print(gameObject.name + " is Loooping patrol points!");
      nextPatrolPoint = 0;
    }
   //AITarget is already the closest known enemy - let's use that! 
   if (AITarget != null && DistanceTo(AITarget.gameObject) <= engageDist*1.5f)
   {//If we're withing the engage envelope, let's go check it out! 
     ActiveAIState = AIState.ENGAGE;
   }
  
  }
  break;
  
  case AIState.BREAK: //Break and Attack!
  {
    if(WingmanTo !=null && WingmanTo.currentTarget != null)
    {
      AITargetShip = WingmanTo.currentTarget;
    }
    ActiveAIState = AIState.REPOSITION;
  }
  break;

  case AIState.WINGMAN:
  {
    if(!hasWingLead)//Look for a wingleader in this state 
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
      var localFormPos = formationPos-(WingmanTo.transform.forward*WingmanTo.shipRadius*5)+WingmanTo.transform.position;
      var leadDist = Vector3.Distance(localFormPos,transform.position);
      var dirToPos = (localFormPos)-transform.position;
      
      Debug.DrawLine(gameObject.transform.position,localFormPos, Color.green,.10f);
      if(leadDist != 0)
      {
        if (leadDist > 120)//If we're a ways off, aim right at the formation point and afterburn into position.
        {
          ship.targetSpeed = ship.burnSpeed;
          SteerTo(localFormPos, ship.turnRate);
        }
        if (leadDist <= 120 && leadDist > 20) //If we're a moderate distance away, set speed to the lead ship +25%, aim at the formation position.
          {
            ship.targetSpeed = WingmanTo.speed+ship.topSpeed/4;
            SteerTo(localFormPos, ship.turnRate);
          }
        if (leadDist <= 20) //If we're close, Match speed, and aim at a point parallel to the direction of the lead ship
          {
            ship.targetSpeed = WingmanTo.speed;
            SteerTo(localFormPos+WingmanTo.transform.forward*ship.shipRadius*4f, ship.turnRate);
            //A gentle push, like the avoidance system, to nudge us into place
            float formPush = (dirToPos.magnitude/10) * .5f;
            transform.position += dirToPos * formPush * Time.deltaTime;
          }
        if (leadDist <= 30) // attempt to match roll once we get close-ish
          {        
            transform.rotation = Quaternion.Lerp(transform.rotation,WingmanTo.transform.rotation,.005f);
            //QuaternionUtil.SmoothDamp(transform.rotation,WingmanTo.transform.rotation, ref refForm, .15f);
            ship.roll = WingmanTo.roll;
          }
          //AITarget is already the closest known enemy - let's use that! 
          if (AITarget != null && Vector3.Distance(AITarget.position,transform.position) <= engageDist*.666f)
          {//hold formation until we're very close
            ActiveAIState = AIState.REPOSITION;
          }
        }
      }
      else {
        ActiveAIState = AIState.PATROL;
      }
    }
  break;



  case AIState.ENGAGE:
  {
    float angleToTarget = AngleTo(AITarget.position);
    if(randApproach.magnitude == 0)
    {
      randApproach = Random.onUnitSphere*AITargetShip.shipRadius*2f;
    }
    SteerTo(AITarget.position+randApproach, ship.turnRate);
    ship.targetSpeed = ship.topSpeed;
    if(!AITarget)
    {
      ActiveAIState = AIState.PATROL;
    }

    if(Vector3.Distance(AITarget.position,transform.position) < engageDist*1.5f)
    {
      ship.targetSpeed = ship.burnSpeed;
    }
    if(Vector3.Distance(AITarget.position,transform.position) < engageDist)
    {
      randApproach = Vector3.zero;
      ActiveAIState = AIState.HUNT;
    }
    //print(gameObject.name + " Is engaging! Throttle set to " + ship.targetSpeed);
  }
  break;
  case AIState.HUNT:
  {     
    if(!AITarget)
    {
      ActiveAIState = AIState.PATROL;
    } 
    if(randApproach.magnitude == 0)
    {
      randApproach = Random.onUnitSphere*AITargetShip.shipRadius*aimAccuracy;
    }  
    float angleToTarget = AngleTo(AITarget.position);
    Vector3 dirToTarget = AITarget.transform.position-transform.position;
    float distToTarget = Vector3.Distance(AITarget.position,transform.position);

    //Closest Target is infront of us
    if(angleToTarget < 90 )
    {
      if(distToTarget > followDist)
      {
        ship.targetSpeed = ship.topSpeed;
      }
      else
      {
        ship.targetSpeed = Mathf.Max(Mathf.Min(AITargetShip.targetSpeed,ship.topSpeed), ship.topSpeed/4);
      }
      if(distToTarget > engageDist) //Try and turn toward the target! 
      {
        if(angleToTarget < 60)
        {
        ship.targetSpeed = ship.burnSpeed;
        
        }
        else {
          ship.targetSpeed = ship.topSpeed;
        }
        
      }
    }
    else 
    {//OH NOES, WE BEIN HUNTED SON
      ActiveAIState = AIState.REPOSITION;
    }
    float nearDodgeBlend = 1-Mathf.Clamp01((distToTarget)/40);
    if(logDebug){print(nearDodgeBlend + " near dodge blending!");}
    Vector3 shootAt = AITarget.forward*AITargetShip.shipRadius*AILeadAmt*2*AITargetShip.throttle+DoAiming(aimAccuracy,aimUpdate);

    Vector3 huntDir = AITarget.position;//+Vector3.Lerp(shootAt,randApproach,nearDodgeBlend);
    Debug.DrawLine(transform.position,shootAt+AITarget.position,Color.red,.01f);
    Debug.DrawLine(transform.position,transform.position+transform.forward*25,Color.yellow,.01f);


    SteerTo(huntDir, ship.turnRate*2);
    float angleToShoot = AngleTo(huntDir);

    if (angleToShoot < aimAccuracy*2)
    {
       //if(logDebug){print("attempting to fire");}
       AITargetShip.isBeingShot = true;
       FireGuns(true);
    }

    else
    {
      if (distToTarget < engageDist/5) //we're close, take the chance! 
      { 
        if(AngleTo(AITarget.position) < aimAccuracy*2)
        {
        rechargeWait = 0;
        AITargetShip.isBeingShot = true;
        //if(logDebug){print("attempting to fire");}
        FireGuns(true);
        }
        else{
          AITargetShip.isBeingShot = false;
          FireGuns(false);
        }
        
      }
    }

    if (distToTarget > engageDist*2 || AngleTo(AITarget.position) > 45)
    {
      AITargetShip.isBeingShot = false;
      FireGuns(false);
    }
    if (distToTarget > engageDist*1.5f)
    {
      ActiveAIState = AIState.ENGAGE;
    }

  }
  break;



  case AIState.EVADE:
  {
    if(evadeTimer == 0) //We're starting to evade
    {//Punch it, Chewie! 
      FireGuns(false);

      ship.targetSpeed = ship.burnSpeed;
      if(EvadeSteer == Vector3.zero)// have we chosen where to steer? 
      {
        EvadeSteer = new Vector3(Random.Range(-1f,1f),Random.Range(-1f,1f),Random.Range(-1f,1f));
      }
      
    }
    if(GameObjTracker.frames % Random.Range(30,60) == 0) // every few second jerk around wildly! 
    {
      EvadeSteer = new Vector3(Random.Range(-2f,2f),Random.Range(-2f,2f),Random.Range(-2f,2f));
    }
    ship.pitch = Mathf.Lerp(ship.pitch, EvadeSteer.x*evadeAmt,.001f);
    ship.yaw = Mathf.Lerp(ship.yaw, EvadeSteer.y*evadeAmt,.001f);
    ship.roll = Mathf.Lerp(ship.roll, EvadeSteer.z*evadeAmt,.001f);
    //count down the time to return to normal combat. 
    evadeTimer += Time.deltaTime;
    if (evadeTimer >= evadeLength)
    {
      EvadeSteer = Vector3.zero;
      ActiveAIState = AIState.HUNT;
    }
  }
  break;

  case AIState.REPOSITION:
  {   
    if(!AITarget)
    {
      ActiveAIState = AIState.PATROL;
    }
    float angleToTarget = AngleTo(AITarget.position);
    Vector3 dirToTarget = AITarget.transform.position-transform.position;
    float distToTarget = Vector3.Distance(AITarget.position,transform.position);
    FireGuns(false);
    if(randPos.magnitude == 0)
    {  randPos = transform.position+Random.onUnitSphere*50f;
    }
    if(distToTarget < 60f && distToTarget > 30f )
    {
      ship.targetSpeed = ship.burnSpeed;
    }
    else{
      ship.targetSpeed = ship.topSpeed;
    }
    SteerTo(randPos, ship.turnRate);
    if (Vector3.Distance(transform.position,randPos) < 20f || distToTarget > 100f)
    {
      randPos = Vector3.zero;
      ActiveAIState = AIState.HUNT;
    }

  }
  break;

  default:
  {

  }
  break;
}

}



  /// <summary>
  /// Update is called every frame, if the MonoBehaviour is enabled.
  /// </summary>
  void Update()
  {
    switch (AISkillLevel) {
      case (AILevel.CHUMP):{
        ChumpAI();
      }
      break;
      case (AILevel.NOVICE):{
        NoviceAI();
      }
      break;
      case (AILevel.DEFAULT):{
        DefaultAI();
      }
      break;
      case (AILevel.ACE):{
        AceAI();
      }
      break;
    }
   
  }
}
