using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipSettings : MonoBehaviour
{
  
  public enum TEAM {CONFED, KILRATHI, NEUTRAL, PIRATE};
  
  [Header("Choose Team!")]
  [SerializeField] public TEAM AITeam = TEAM.CONFED;
  [SerializeField] public bool isWingLead = false;  
  
  [SerializeField] public LayerMask CollidesWith;
  [Header("Movement Settings")]
  [SerializeField] public float turnRate = 50f;
  [SerializeField] public float maxFuel = 2500f;
  [SerializeField] public float fuelBurnRate = 2f;
  [SerializeField] bool invertYAxis = false;
  [SerializeField] public float topSpeed = 20f;
  [SerializeField] public float burnSpeed = 50f;
  [SerializeField] float acceleration = 1.5f;
  [SerializeField] float deceleration = 1f;
  [SerializeField] public LayerMask AutoAvoids;
  [Header("Weapon Settings")]
  [SerializeField] public float capacitorSize = 50f;
  [SerializeField] float rechargeRate = 1f;
  
  [Header("Armor - Front, Back, Left, Right")]
  [Header("Health Settings")]  
  [SerializeField] public Vector4 Armor;
  [Header("Shield - Front, Back")]
  [SerializeField] public Vector2 Shield;
  [SerializeField] public float shieldRechargeRate = 1;
  [Header("Death Effect")]
  [SerializeField] public GameObject[] DeathVFX;
  [SerializeField] public GameObject DeathTrailVFX;
  [SerializeField] public GameObject InternalDamageVFX;
  [SerializeField] public GameObject DamageVFX;
  [SerializeField] public GameObject DamageTrails;



  //Hidden Attributes
  [HideInInspector] public float shipRadius;
  EngineFlare[] engineFlares;
  [HideInInspector] public Vector4 _ArmorMax;
  [HideInInspector] public Vector2 _ShieldMax;
  [HideInInspector] public float Core;
  [HideInInspector] public float _CoreStrength;
  [SerializeField] public float _Fuel;
  [HideInInspector] public int ShipID;
  [HideInInspector] public float yaw;
  [HideInInspector] public float pitch;
  [HideInInspector] public float roll;
  [HideInInspector] public float targetSpeed;
  [HideInInspector] public float capacitorLevel;
  [HideInInspector] public bool isFiring = false;
  [HideInInspector] public bool isAfterburning;
  [HideInInspector]  public float speed = 0f;
  [HideInInspector] GameObjTracker Tracker;
  
  [HideInInspector] public int numWingmen = 0;
  [HideInInspector] public bool isDead = false;
  [HideInInspector] public bool isLocked = false;

  void Start()
  {
    //assign a random ID
    ShipID = Random.Range(-32000,32000);
    shipRadius = GetComponent<SphereCollider>().radius;
    //Organize the scene
    gameObject.transform.SetParent(GameObject.FindWithTag("GamePlayObjs").transform); 
    //Make sure everyone knows we're here
    GameObjTracker.RegisterAllShips();
    GameObjTracker.RegisterTeams();
    //grab the sub-object engine flares to control them
    engineFlares = GetComponentsInChildren<EngineFlare>();
    //Atomic Batteries to power
    capacitorLevel = capacitorSize;
    //Turbines to speed
    //check fuel Light
    _Fuel = maxFuel;

    _ArmorMax = Armor; //Give us something to compare to later on
    _ShieldMax = Shield; //same
    _CoreStrength = (Armor.x+Armor.y+Armor.z+Armor.w+(Shield.x+Shield.y)/2)/3; //Generalized fomula for the unarmored mechanical core of the ship
    Core = _CoreStrength;
  }

  // late update to give human or AI player scripts a chance to set values first
  void LateUpdate()
  {
    if(!isDead)
    {
    Steer();
    DoThrottle();
    Power();
    }
    DoHealth();
    DoFuel();
    AvoidObstacles(.75f,shipRadius*3f);
    //Collision Detecting, but make sure the full collision is only being used if the ship is afterburning, simple manuvers won't do it as much.
    //if(isAfterburning)
    //{ DoBounce(.5f,shipRadius/2);}
    //else
    DoBounce(.5f,shipRadius/16f);
  }

  void DoFuel()
  {
    var normalizedThrottle = Mathf.Clamp01(speed/topSpeed);
    if(_Fuel > 0 ) //WE've got fuel, let's go! 
    { 
      if(!isAfterburning)
      {//Do normal fuel drain based on throttle
        _Fuel -= normalizedThrottle*Time.deltaTime*2f;
      }
      else// Now we're burning fuel to GO VERY FAST
      {
        _Fuel -= fuelBurnRate*Time.deltaTime*2f;
      }
    }
    else //Fuck, basically just a max coasting speed. Good fucking luck, cowboy
    {
      speed = Mathf.Min(targetSpeed, .8f);
      isAfterburning = false;

    }
  }

  void InternalDamage(){
    //Show that internal damage has taken place! 
    Instantiate(InternalDamageVFX,transform.position,Quaternion.identity,transform);
  }
   void ArmorDamage(Vector3 pos){
    //Show that internal damage has taken place! 
    GameObject Debris = Instantiate(DamageVFX,pos,Quaternion.identity,transform.parent);
    Debris.GetComponent<DampInitVelocity>().initDir = DeathDir;
    Debris.GetComponent<DampInitVelocity>().initVel = DeathVel;
  }

  //Gently avoid obstacles!
  public void AvoidObstacles(float urgency, float avoidRadius)
  {
    //Check for all coliders within a reasonable distance
    Collider[] hitColliders = Physics.OverlapSphere(gameObject.transform.position, avoidRadius, AutoAvoids);
    int i = 0;
    while (i < hitColliders.Length)
    {
        if(hitColliders[i] != gameObject.GetComponent<SphereCollider>()) //Ignore it if we're.. LOOKING AT OURSELVES
        {
          
          //Find the direction to the obstacle
          Vector3 avoidDir = hitColliders[i].transform.position- gameObject.transform.position;
          //Inverse linear falloff to push out more strongly the closer they are
          float magPush = (1-(avoidDir.magnitude/avoidRadius))+.5f;
          //Gradually autofalloff to 0 intensity
          //magPush = Mathf.SmoothStep(magPush,0,.5f);
          //HEAVE AWAY, BOIS
          transform.position -= avoidDir * urgency * magPush * Time.deltaTime;
          //if(magPush > 0)
            //print(name +" is pushing away from "+ hitColliders[i].name +"with an intensity of " + magPush);
          
        }
        i++;
    }
    
  }
  //Violently Collide!
  [HideInInspector] public float recover = 1f;
  [HideInInspector] public Vector3 BounceDir;
   [HideInInspector] public Vector3 BounceSpin;
  [HideInInspector] public float BouncePush;
  public void DoBounce(float recoverRate, float bounceRadius)
  {
    Collider[] bounceColliders = Physics.OverlapSphere(gameObject.transform.position, bounceRadius, CollidesWith);
    int ib = 0;
    while (ib < bounceColliders.Length)
    {      
    if(bounceColliders[ib] != gameObject.GetComponent<SphereCollider>()) //Ignore it if we're.. LOOKING AT OURSELVES
        {
        //sprint("Bounce detected between "+ name +" and "+ bounceColliders[ib].name);
        ShipSettings hitShip = bounceColliders[ib].gameObject.GetComponent<ShipSettings>();
        if(recover >= 1f)// && hitShip != null)
        {
          //Find the direction to the collision
          Vector3 colDir = bounceColliders[ib].transform.position-gameObject.transform.position;
          //equally bounce each ship, damage is made from the rest of the momentum
          BouncePush = (speed+hitShip.speed)/2f;
          var weightDamageThem = ((speed+hitShip.speed)/hitShip.speed)/4f;
          var weightDamage = ((speed+hitShip.speed)/speed)/4f;
          DoDamage(bounceColliders[ib].transform.position,(speed+hitShip.speed)*.01f*weightDamage); 
          hitShip.DoDamage(transform.position,(speed+hitShip.speed)*.01f*weightDamageThem);
          print("RAM Detected:" + name +" has rammed " + hitShip.name + "at relative speeds of " + speed +" and " + hitShip.speed+
          " and will be damaged " + (speed+hitShip.speed)*.05f*weightDamage + "to " + (speed+hitShip.speed)*.05f*weightDamageThem);
          hitShip.recover = 0f;
          hitShip.BouncePush = BouncePush;
          hitShip.BounceDir = BounceDir;
          BounceDir = -BounceDir;
          BounceSpin = new Vector3(Random.Range(-3f,3f),Random.Range(-3f,3f),Random.Range(-3f,3f));
          hitShip.BounceSpin = new Vector3(Random.Range(-3f,3f),Random.Range(-3f,3f),Random.Range(-3f,3f));
          recover = 0f;          
        }       
      }

     ib++;
    }
    if(recover < 1)
    {
      var byaw_ = Mathf.Clamp(BounceSpin.y, -1f, 1f);
      var bpitch_ = Mathf.Clamp(BounceSpin.x, -1f, 1f);
      var broll_ = Mathf.Clamp(BounceSpin.z, -1f, 1f);
      byaw_ *= turnRate * 5f * Time.deltaTime* (1-recover);
      bpitch_ *= turnRate * 5f * Time.deltaTime* (1-recover);
      broll_ *= turnRate * 5f * Time.deltaTime* (1-recover);
      
      pitch *= recover;
      yaw *= recover;
      roll *= recover;
      speed *= recover;
      if (recover < .05f)
      {InternalDamage();          
      }
      transform.localRotation *= Quaternion.AngleAxis(broll_, Vector3.forward) * Quaternion.AngleAxis(byaw_, Vector3.up) * Quaternion.AngleAxis(bpitch_, invertYAxis ? Vector3.right : Vector3.left);
      transform.position += BounceDir * BouncePush * Time.deltaTime * (1-recover);
      //speed = Random.Range(-topSpeed,topSpeed/2);
      recover += recoverRate * Time.deltaTime;
      }
  }
  public enum HitLoc {F,R,L,U,D,B, NULL};
  public HitLoc lastHit;

  public void DoDamage(Vector3 hitLoc, float damage)
  {
      //Where'd the hit come from, to the center of the ship?
      Vector3 damageAngle = hitLoc-transform.position;
      //Check font/back hit of the impact, apply that to the shields
      if(Vector3.Angle(transform.forward, damageAngle) < 90) //Hit from the front
      {
        lastHit = HitLoc.F;
        //print("hit from the front! Angle of" + Vector3.Angle(transform.forward, damageAngle));
        if(Shield.x > damage)//if shields can take the hit, let them
          Shield.x -= damage;
        else //oh no! the armor needs to take the hit, minus whatever damage the shield can absorb.
        {
          damage -= Shield.x;
          Shield.x = 0;
          //check front/left/right armor quadrants, apply damage
          if(Vector3.Angle(transform.forward, damageAngle) <= 45) // front armor hit!
          {
            if(Armor.x > damage) //can the armor take the hit? 
            {  Armor.x -= damage;
               ArmorDamage(hitLoc);
            }
            else  //armor takes what it can, passes the rest onto internal damage;
              {
                damage -= Armor.x;
                Armor.x = 0;
                _CoreStrength -= damage;
                InternalDamage();
              }
          }
          else if(Vector3.Angle(-transform.right, damageAngle) <= 45) // left armor hit!)
          {
            lastHit = HitLoc.L;
            if(Armor.z > damage) //can the armor take the hit? 
            {  Armor.z -= damage;
               ArmorDamage(hitLoc);
            }
            else  //armor takes what it can, passes the rest onto internal damage;
              {
                damage -= Armor.z;
                Armor.z = 0;
                _CoreStrength -= damage;
                InternalDamage();
              }
          }
          else if(Vector3.Angle(transform.right, damageAngle) <= 45) // right armor hit!)
          {
            lastHit = HitLoc.R;
            if(Armor.w > damage) //can the armor take the hit? 
            {  Armor.w -= damage;
               ArmorDamage(hitLoc);
            }
            else  //armor takes what it can, passes the rest onto internal damage;
              {
                damage -= Armor.w;
                Armor.w = 0;
                _CoreStrength -= damage;
                InternalDamage();
              }
          }
          else //HUH, no armor seems to have been hit. That's a dirty lie, so let's make them all suffer, plus a liiitle bit of core damage for fibbing.
          {
            if(Vector3.Angle(transform.up, damageAngle) <= 45)
              {lastHit = HitLoc.U;}
            if(Vector3.Angle(-transform.up, damageAngle) <= 45)
              {lastHit = HitLoc.D;}
            Armor -= new Vector4(1,0,1,1)*damage/3;
            _CoreStrength -= damage/4;
          }
        }
      }
      else //Hit from the back
      {
        lastHit = HitLoc.B;
        //print("hit from the back!");
        if(Shield.y > damage)//if shields can take the hit, let them
        {  Shield.y -= damage;
          //print("Shields damaged for "+ damage);
        }
        else //oh no! the armor needs to take the hit, minus whatever damage the shield can absorb.
        {
          damage -= Shield.y;
          Shield.y = 0;
          //print("damage is now "+ damage);
          //check front/left/right armor quadrants, apply damage
          if(Vector3.Angle(-transform.forward, damageAngle) <= 45) // back armor hit!
          {
            if(Armor.y > damage) //can the armor take the hit? 
            {   Armor.y -= damage;
                ArmorDamage(hitLoc);
            }
            else  //armor takes what it can, passes the rest onto internal damage;
              {
                damage -= Armor.y;
                Armor.y = 0;
                _CoreStrength -= damage;
                InternalDamage();
              }
          }
          else if(Vector3.Angle(-transform.right, damageAngle) <= 45) // left armor hit!)
          {
            lastHit = HitLoc.L;
            if(Armor.z > damage) //can the armor take the hit? 
            {  Armor.z -= damage;
               ArmorDamage(hitLoc);
            }
            else  //armor takes what it can, passes the rest onto internal damage;
              {
                damage -= Armor.z;
                Armor.z = 0;
                _CoreStrength -= damage;
                InternalDamage();
              }
          }
          else if(Vector3.Angle(transform.right, damageAngle) <= 45) // right armor hit!)
          {
            lastHit = HitLoc.R;
            if(Armor.w > damage) //can the armor take the hit? 
            {  Armor.w -= damage;
              ArmorDamage(hitLoc);
            }
            else  //armor takes what it can, passes the rest onto internal damage;
              {
                damage -= Armor.w;
                Armor.w = 0;
                _CoreStrength -= damage;
                InternalDamage();
              }
          }
          else //HUH, no armor seems to have been hit. That's a dirty lie, so let's make them all suffer, plus a liiitle bit of core damage for fibbing.
          {
            if(Vector3.Angle(transform.up, damageAngle) <= 45)
              {lastHit = HitLoc.U;}
            if(Vector3.Angle(-transform.up, damageAngle) <= 45)
              {lastHit = HitLoc.D;}
            Armor -= new Vector4(0,1,1,1)*damage/3;
            _CoreStrength -= damage/4;
          }
      }
    }
  }
  GameObject Boom;

  Vector3 DeathDir = Vector3.zero;
  float DeathVel;
  Vector3 DeathSpin;
  int DeathType;
  float DeathLength;
  GameObject Trail;

  void DoHealth()
  {
    //Constantly recharge the shields till full
    if(Shield.x < _ShieldMax.x)
      Shield.x += shieldRechargeRate*Time.deltaTime/20;
    if(Shield.y < _ShieldMax.y)
      Shield.y += shieldRechargeRate*Time.deltaTime/20;
    //Should do component damage here when the corestrength is low. Ignore for now
    if(_CoreStrength < Core*.666f)
    {
      if(!Trail)
      {
        foreach(EngineFlare flare in engineFlares)
        {
          Trail = Instantiate(DamageTrails,flare.gameObject.transform.position+flare.gameObject.transform.forward*4,Quaternion.identity,flare.gameObject.transform);
        }
      }
    }
    if(_CoreStrength > 0) //We dead, son
    { 
    DeathDir = transform.forward;
    DeathVel = speed;
    }

    if(_CoreStrength <= 0) //We dead, son
    { 
      isDead = true;
      //Let's set up how we're going to die!
      if (DeathSpin == Vector3.zero) //this happens once, let's take advantage!
      {  
         DeathDir = transform.forward;
         DeathVel = speed;
         DeathSpin = new Vector3(Random.Range(-3f,3f),Random.Range(-3f,3f),Random.Range(-3f,3f));
         DeathType = Random.Range(0,2); //we've got three current deaths - immediate, short spin, and death tumble!
         DeathLength = Random.Range(2f,4f);
      }
      if(DeathType == 0) //Die Immediately.
      {
      if (!Boom)
      { 
        Boom = Instantiate(DeathVFX[Random.Range(0,DeathVFX.Length-1)],transform.position,Quaternion.identity,transform.parent);
        Boom.GetComponent<DampInitVelocity>().initDir = DeathDir;
        Boom.GetComponent<DampInitVelocity>().initVel = DeathVel;
      }
      Destroy(gameObject, .25f);
      transform.position += DeathDir * DeathVel* Time.deltaTime;
      }

      if(DeathType == 1)//Short burst of explosions, then Die
      {
        var dyaw_ = Mathf.Clamp(DeathSpin.y, -1f, 1f);
        var dpitch_ = Mathf.Clamp(DeathSpin.x, -1f, 1f);
        var droll_ = Mathf.Clamp(DeathSpin.z, -1f, 1f);
        dyaw_ *= turnRate * 2f * Time.deltaTime;
        dpitch_ *= turnRate * 2f * Time.deltaTime;
        droll_ *= turnRate * 3f * Time.deltaTime;
        transform.localRotation *= Quaternion.AngleAxis(droll_, Vector3.forward) * Quaternion.AngleAxis(dyaw_, Vector3.up) * Quaternion.AngleAxis(dpitch_, invertYAxis ? Vector3.right : Vector3.left);
        transform.position += DeathDir * DeathVel* Time.deltaTime;
        speed = 0f;
        DeathLength -= Time.deltaTime*5.5f;
        if (DeathLength > .5f)
        {
          GameObject DeathTrail = Instantiate(DeathTrailVFX,transform.position,Quaternion.identity,transform.parent);
          DeathTrail.GetComponent<DampInitVelocity>().initDir = DeathDir;
          DeathTrail.GetComponent<DampInitVelocity>().initVel = DeathVel/8;
        }
        if (DeathLength <= 0 && !Boom)
        { 
          Boom = Instantiate(DeathVFX[Random.Range(0,DeathVFX.Length-1)],transform.position,Quaternion.identity,transform.parent);
          Boom.GetComponent<DampInitVelocity>().initDir = DeathDir;
          Boom.GetComponent<DampInitVelocity>().initVel = DeathVel;
          Destroy(gameObject, .25f);
        }
      }

      if(DeathType == 2)
      {
      var dyaw_ = Mathf.Clamp(DeathSpin.y, -1f, 1f);
      var dpitch_ = Mathf.Clamp(DeathSpin.x, -1f, 1f);
      var droll_ = Mathf.Clamp(DeathSpin.z, -1f, 1f);
      dyaw_ *= turnRate * 2f * Time.deltaTime;
      dpitch_ *= turnRate * 2f * Time.deltaTime;
      droll_ *= turnRate * 3f * Time.deltaTime;
      transform.localRotation *= Quaternion.AngleAxis(droll_, Vector3.forward) * Quaternion.AngleAxis(dyaw_, Vector3.up) * Quaternion.AngleAxis(dpitch_, invertYAxis ? Vector3.right : Vector3.left);
      transform.position += DeathDir * DeathVel* Time.deltaTime;
      DeathLength -= Time.deltaTime;
        if (DeathLength > .25f)
        {
          GameObject DeathTrail = Instantiate(DeathTrailVFX,transform.position,Quaternion.identity,transform.parent);
          DeathTrail.GetComponent<DampInitVelocity>().initDir = DeathDir;
          DeathTrail.GetComponent<DampInitVelocity>().initVel = DeathVel/8;
        }
        if (DeathLength <= 0 && !Boom)
        { 
          Boom = Instantiate(DeathVFX[Random.Range(0,DeathVFX.Length-1)],transform.position,Quaternion.identity,transform.parent);
          Boom.GetComponent<DampInitVelocity>().initDir = DeathDir;
          Boom.GetComponent<DampInitVelocity>().initVel = DeathVel;
          Destroy(gameObject, .25f);
        }
      }



    }
  }

  void Power()
  {
    if(capacitorLevel<capacitorSize) //Charge Them Guns
    {
      capacitorLevel += rechargeRate*Time.deltaTime;
    }
  }

  Vector3 oldRotForward;
  Vector3 oldRotRight;
  Vector3 oldRotUp;
  [HideInInspector] public Quaternion oldRot;
  [HideInInspector] public Vector3 deltaRot;

  public static float GetSignedAngle(Quaternion A, Quaternion B, Vector3 axis) {
     float angle = 0f;
     Vector3 angleAxis = Vector3.zero;
     (B*Quaternion.Inverse(A)).ToAngleAxis(out angle, out angleAxis);
     if(Vector3.Angle(axis, angleAxis) > 90f) {
         angle = -angle;
     }
     return Mathf.DeltaAngle(0f, angle);
 }

  void Steer() //Autopilot!
  {
    var yaw_ = Mathf.Clamp(yaw, -1f, 1f);
    var pitch_ = Mathf.Clamp(pitch, -1f, 1f);
    var roll_ = Mathf.Clamp(roll, -1f, 1f);
    yaw_ *= turnRate * Time.deltaTime;
    pitch_ *= turnRate * Time.deltaTime;
    roll_ *= turnRate * 2f * Time.deltaTime;
    transform.localRotation *= Quaternion.AngleAxis(roll_, Vector3.forward) * Quaternion.AngleAxis(yaw_, Vector3.up) * Quaternion.AngleAxis(pitch_, invertYAxis ? Vector3.right : Vector3.left);
    
    var anglex = GetSignedAngle(transform.rotation,oldRot,transform.right);
    var angley = GetSignedAngle(transform.rotation,oldRot,transform.up);
    var anglez = GetSignedAngle(transform.rotation,oldRot,transform.forward);
    
    deltaRot = new Vector3(anglex,angley,anglez);
    
    
    oldRot = transform.rotation;     

  }

  void DoThrottle()
  {
    var targetSpeed_ = Mathf.Clamp(targetSpeed, 0f, burnSpeed);
    
    if (speed < targetSpeed_)
    // accelerating
    {
      speed = Mathf.Lerp(speed, targetSpeed_, acceleration * Time.deltaTime);
    }
    else if (speed > targetSpeed_)
    // decelerating
    {
      speed = Mathf.Lerp(speed, targetSpeed_, deceleration * Time.deltaTime);
    }

    transform.position += transform.forward * speed * Time.deltaTime;

    //set Afterburning flag
    if(targetSpeed > topSpeed+.1f)
     {isAfterburning = true;}
     else
     {isAfterburning = false;}
    // also set the visible flare throttles
    foreach (EngineFlare flare in engineFlares)
    {
      flare.FlareThrottle = speed/(topSpeed);
    }
  }
}
