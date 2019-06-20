using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ShipSettings))]
public class AIPlayer : MonoBehaviour
{
  public enum AIState {PATROL, SEARCH, WINGMAN, ENGAGE, HUNT, REPOSITION, FLEE, DEATH };
  public enum AILevel {CHUMP, NOVICE, DEFAULT, SKILLED, ACE, MASTER};

  public AIState ActiveAIState = AIState.PATROL;
  public AILevel AISkillLevel = AILevel.CHUMP;


  [HideInInspector]
  public Transform AITarget;
  [HideInInspector]
  public GameObjTracker Tracker;


  ShipSettings ship;
  LaserCannon[] laserCannons;


  void Start()
  {
    ship = GetComponent<ShipSettings>();
    laserCannons = GetComponentsInChildren<LaserCannon>();
    
  }
  Vector3 smoothDir = Vector3.zero;
  Vector3 refDir = Vector3.zero;
  Quaternion refRot = Quaternion.identity;
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
  bool rolling = false;
  float rollStart;
  float rollDir;
  float rollLength;

void FireGuns(bool fire)
{
  foreach (LaserCannon laserCannon in laserCannons)
  {
   laserCannon.fire = fire;
   bloodThirst = 0f; //Ahh, our bloodthirst is sated
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

  void ChumpAI()
  {
     ship.targetSpeed = ship.topSpeed*.75f;
    //Afterburn every so often.
    if(Mathf.Clamp01(Mathf.Sin(Time.time/2)*5f-4f) > 0)
    {
      ship.targetSpeed = ship.burnSpeed;
    }
    //Occasionally Fire the guns
    if(Mathf.Clamp01(Mathf.Sin(Time.time/3)*5f-4f) > 0)
    {
      FireGuns(true);
    }
    else
    {
     FireGuns(false);
    }
    //Steer back to origin!
    if (transform.position.magnitude > 250*Mathf.Clamp01(Mathf.Sin(Time.time*2)*6f-4f)+50)
    { 
      SteerTo (Vector3.zero, ship.turnRate);
    }
    //occasionally, but reguarly roll
    RollControl(Mathf.Clamp01(Mathf.Sin(Time.time+.1f)*6f-5.75f));
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
  
void NoviceAI()
{
  engageDist = 1500f;
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
  if (AITargetShip && Vector3.Distance(AITarget.position,gameObject.transform.position) <= engageDist && ActiveAIState != AIState.WINGMAN)
  { 
    if(ActiveAIState == AIState.PATROL || ActiveAIState == AIState.SEARCH )
    {
      ActiveAIState = AIState.ENGAGE;
    }
  }
  if (AITargetShip && Vector3.Distance(AITarget.position,gameObject.transform.position) <= 100)
  { 
    if(ActiveAIState == AIState.ENGAGE)
    {
      ActiveAIState = AIState.HUNT;
    }
  }
  
//print("Current target is" + AITargetShip.name);
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

void DoAIStates(){
switch(ActiveAIState)
{
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

void ForceRegister()
{
  gameObject.transform.SetParent(GameObject.FindWithTag("GamePlayObjs").transform);
  Tracker = gameObject.GetComponentInParent<GameObjTracker>();
  Tracker.RegisterAllShips();
  Tracker.RegisterTeams();
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
