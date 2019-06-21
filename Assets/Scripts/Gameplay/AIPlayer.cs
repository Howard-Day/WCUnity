using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ShipSettings))]
public class AIPlayer : MonoBehaviour
{
  public enum AIState {PATROL, BREAK, SEARCH, WINGMAN, ENGAGE, HUNT, PROTECT, REPOSITION, FLEE, DEATH };
  public enum AILevel {CHUMP, NOVICE, DEFAULT, SKILLED, ACE, MASTER};
  public ShipSettings WingmanTo;
  public Transform[] PatrolPoints;

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

  bool rolling = false;
  float rollStart;
  float rollDir;
  float rollLength;


  void Start()
  {
    ship = GetComponent<ShipSettings>();
    laserCannons = GetComponentsInChildren<LaserCannon>();    
  }

void ForceRegister()
{
  gameObject.transform.SetParent(GameObject.FindWithTag("GamePlayObjs").transform);
  Tracker = gameObject.GetComponentInParent<GameObjTracker>();
  Tracker.RegisterAllShips();
  Tracker.RegisterTeams();
}

void FireGuns(bool fire)
{
  foreach (LaserCannon laserCannon in laserCannons)
  {
   laserCannon.fire = fire;
   bloodThirst = 0f; //Ahh, our bloodthirst is sated
  }
}
void SteerTo(Vector3 aimAt, float turnSpeed)
{
  //kill any manual steering
  ship.yaw = 0;//Mathf.SmoothStep(ship.yaw,0f,.1f);
  ship.pitch = 0;//Mathf.SmoothStep(ship.pitch,0f,.1f);
  //smooth autosteer
  Vector3 targetDir = aimAt - transform.position;
  smoothDir = Vector3.SmoothDamp(smoothDir,targetDir,ref refDir, .25f);
  
  Quaternion newDir = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(smoothDir,transform.up), turnSpeed * 6f * Time.fixedUnscaledDeltaTime);
  transform.rotation = QuaternionUtil.SmoothDamp(transform.rotation,newDir, ref refRot, .15f);
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
    foreach (ShipSettings shipTest in Tracker.Ships)
    {
      if(shipTest == null) //SOMEONE MUSTA DIED
      {
        Tracker.RegisterAllShips();
        Tracker.RegisterTeams();
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

  float engageDist = 150f;
  float aimAccuracy = 30f;
  int aimUpdate = 60;
  float followDist;
  float impatience;
  float bloodThirst;
  Vector3 randPos = Vector3.zero;
  ShipSettings AITargetShip;
  Vector3 formationPos;

  void ChumpAI()
  {
     ship.targetSpeed = ship.topSpeed*.75f;
  }  
void NoviceAI()
{
  engageDist = 150f;
  aimAccuracy = 15f;
  aimUpdate = 60;
  if(followDist == 0){
    followDist = Random.Range(250f,400f);
    //print(name + " has a follow distance of " +followDist);
  }
  AITargetShip = FindNearestShip(gameObject.transform,ship.AITeam);
  if(AITargetShip != null)
  {AITarget = AITargetShip.gameObject.GetComponent<Transform>();
  }
  /*if (AITargetShip && Vector3.Distance(AITarget.position,gameObject.transform.position) <= engageDist && ActiveAIState != AIState.WINGMAN)
  { 
    if(ActiveAIState == AIState.PATROL || ActiveAIState == AIState.SEARCH )
    {
      ActiveAIState = AIState.ENGAGE;
    }
  }*/
  if (AITargetShip && Vector3.Distance(AITarget.position,gameObject.transform.position) <= 100)
  { 
    if(ActiveAIState == AIState.ENGAGE)
    {
      ActiveAIState = AIState.HUNT;
    }
  }
  DoImpatience(1f);
  DoAIStates();
  RollControl(Random.Range(-1000f,1f));
}
//Ai frustration
public void DoImpatience(float maxImpatience) 
{
  if(ship.capacitorLevel <=2f) //if the AI can't shoot, increase impatience
  {
    impatience += Time.deltaTime;
  }
  if (impatience >= maxImpatience) //had enough, break off to recharge guns
  {
    ActiveAIState = AIState.REPOSITION;
  }
  ///but we gradually calm down
   impatience -= Time.deltaTime/4;
}
//AI bloodthirstyness
public void DoBloodThirsty(float maxThirsty)
{

}

Vector3 randDist = Vector3.zero;
public Vector3 DoTargeting(float Accuracy, float Update)
{    
  if(Tracker.frames % Update == 0)
  {
    randDist = Random.insideUnitSphere*Accuracy;
    }
    return randDist;  
}

public Vector3 DoAim(float aimRand)
{
      return AITarget.position + (Random.onUnitSphere * aimRand);
}
Vector3 patrolPoint;

bool hasWingLead = false;

//Init Wingman System vars
public void InitWingman(ShipSettings wingman)
{
  if(!hasWingLead) 
    {
      wingman = FindWingMan();
      if (wingman == null)//can't find anyone, bail
      {print("No Wingleaders Available"); return; }
      else //Make ourselves a slot to form up if we're just now finding a Wingleader
      {
        print("Found Wingleader, setting up Params, Setting Formation Pos");
        formationPos = ((Random.Range(-.5f,.5f)*wingman.gameObject.transform.up)+(Random.Range(-1f,1f)*wingman.gameObject.transform.right))*ship.shipRadius*4;
        wingman.numWingmen++;
        hasWingLead = true;
        WingmanTo = wingman;
      }
    }
    else // If we already know who our wingleader is, assume the current relational position is the desired formation position
    {
        print("Wingleader exists on Init, setting up Params, Taking Existing Formation Pos");
        Vector3 initLoc = wingman.gameObject.transform.position-transform.position;
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
    foreach (ShipSettings friendly in Tracker.Ships)
    {
      if(friendly == null) //SOMEONE MUSTA DIED
      {
        print("Someone Died");
        Tracker.RegisterAllShips();
        Tracker.RegisterTeams();
      }
      //Okay, check if we're still null, and if the ship we've found is *AKTUALLY* friendly, and looking for wingmen
      //AND isn't ourselves, AND doesn't already have 4 wingmen.
      if(friendly != null && friendly.isWingLead && friendly.AITeam == ship.AITeam && friendly != ship && friendly.numWingmen <= 4)
      {
        print("Found a Wingleader in the scene! His name is:" + friendly.name);
        Transform friendlyTrans = (Transform)friendly.gameObject.GetComponent<Transform>();
        float shipDist = Vector3.Distance(friendlyTrans.position, transform.position);
        //if there are multiple, find the closest wingleader.
        if (shipDist < distance )
        {
          distance = shipDist;
          wingMan = friendly;
          hasWingLead = true;
          print("Wingleader " + wingMan.name + " is the closest, and current");
        }
      }
    }
    //After going through all, return the closest.
    return wingMan;
  }



void DoAIStates(){
switch(ActiveAIState)
{
  case AIState.PATROL:
  {


  
  }
  break;
  case AIState.BREAK:
  {


  
  }
  break;
  case AIState.WINGMAN:
  {
    if(!hasWingLead)//Look for a wingleader in this state 
    {
      InitWingman(WingmanTo);
    }
    if (WingmanTo == null && !hasWingLead) // No wingleaders? Individual patrol mode! 
    {
      ActiveAIState = AIState.PATROL;
    }
    //See how far away and what direction we need to go
    var localFormPos = formationPos-(WingmanTo.gameObject.transform.forward*ship.shipRadius*3)+WingmanTo.transform.position;
    var leadDist = Vector3.Distance(localFormPos,gameObject.transform.position);
    var dirToPos = (localFormPos)-gameObject.transform.position;

    Debug.DrawLine(gameObject.transform.position,localFormPos, Color.green,.10f);
    
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
        transform.rotation = Quaternion.Lerp(transform.rotation,WingmanTo.transform.rotation,.025f);
        //QuaternionUtil.SmoothDamp(transform.rotation,WingmanTo.transform.rotation, ref refForm, .15f);
        ship.roll = WingmanTo.roll;
      }
    }
  break;

  case AIState.ENGAGE:
  {
    SteerTo(DoAim(15f), ship.turnRate);
    ship.targetSpeed = ship.topSpeed;
    if(Vector3.Distance(AITarget.position,gameObject.transform.position) < engageDist/4)
    {
      ship.targetSpeed = ship.burnSpeed;
    }
    if(Vector3.Distance(AITarget.position,gameObject.transform.position) < engageDist/16)
    {
      ActiveAIState = AIState.HUNT;
    }
    //print(gameObject.name + " Is engaging! Throttle set to " + ship.targetSpeed);
  }
  break;
  case AIState.HUNT:
  {    
    SteerTo(AITarget.position+(AITarget.forward*aimAccuracy*2) +DoTargeting(aimAccuracy,aimUpdate), ship.turnRate);
    ship.targetSpeed = Mathf.Max(AITargetShip.targetSpeed, ship.topSpeed/3);
    if(Vector3.Distance(AITarget.position,gameObject.transform.position) > followDist)
      ship.targetSpeed = ship.burnSpeed;
    if(Vector3.Distance(AITarget.position,gameObject.transform.position) < followDist/4)
      ship.targetSpeed = ship.topSpeed;
    float angleToTarget = Vector3.Angle(gameObject.transform.forward, AITarget.transform.position-gameObject.transform.position);
    if (angleToTarget < aimAccuracy*1.5f)
    {
       FireGuns(true);
    }
    else
    {
       FireGuns(false);
    }
    if(Vector3.Distance(AITarget.position,gameObject.transform.position) < followDist/8 || Random.Range(-1000000f,0.01f) > 0 )
    {
      ActiveAIState = AIState.REPOSITION;
    }
  }
  break;
  case AIState.REPOSITION:
  {
     FireGuns(false);
    if(randPos.magnitude == 0)
    {  randPos = transform.position+Random.insideUnitSphere*100f;
    }
    if(Vector3.Distance(AITarget.position,gameObject.transform.position) < 50f)
    {
      ship.targetSpeed = ship.burnSpeed;
    }
    else{
      ship.targetSpeed = ship.topSpeed*.75f;
    }
    SteerTo(randPos, ship.turnRate);
    if (Vector3.Distance(transform.position,randPos) < 50f || Vector3.Distance(AITarget.position,gameObject.transform.position) > 200f)
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
    if(!Tracker)
    {
      ForceRegister();
    }
    switch (AISkillLevel) {
      case (AILevel.CHUMP):{
        ChumpAI();
      }
      break;
      case (AILevel.NOVICE):{
        NoviceAI();
      }
      break;
    }
   
  }
}
