using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipSettings : MonoBehaviour
{
  
  public enum TEAM {CONFED, KILRATHI, NEUTRAL, PIRATE};
  
  [Header("Choose Team!")]
  [SerializeField] public TEAM AITeam = TEAM.CONFED;
  [Header("Movement Settings")]
  [SerializeField] public float turnRate = 50f;
  [SerializeField] bool invertYAxis = false;
  [SerializeField] public float topSpeed = 20f;
  [SerializeField] public float burnSpeed = 50f;
  [SerializeField] float acceleration = 1.5f;
  [SerializeField] float deceleration = 1f;
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
  [SerializeField] public GameObject DeathVFX;


  //Hidden Attributes
  EngineFlare[] engineFlares;
  Vector4 _ArmorMax;
  Vector2 _ShieldMax;
  float _CoreStrength;

  [HideInInspector] public int ShipID;
  [HideInInspector] public float yaw;
  [HideInInspector] public float pitch;
  [HideInInspector] public float roll;
  [HideInInspector] public float targetSpeed;
  [HideInInspector] public float capacitorLevel;
  [HideInInspector] public bool isAfterburning;
  [HideInInspector]  public float speed = 0f;
  [HideInInspector] GameObjTracker Tracker;

  void Start()
  {
    //assign a random ID
    ShipID = Random.Range(-32000,32000);
    //Organize the scene
    gameObject.transform.SetParent(GameObject.FindWithTag("GamePlayObjs").transform); 
    //Make sure everyone knows we're here
    Tracker = gameObject.GetComponentInParent<GameObjTracker>();
    Tracker.RegisterAllShips();
    Tracker.RegisterTeams();
    //grab the sub-object engine flares to control them
    engineFlares = GetComponentsInChildren<EngineFlare>();
    //Atomic Batteries to power
    capacitorLevel = capacitorSize;
    //Turbines to speed
    burnSpeed = burnSpeed * Tracker.speedMultiplier;
    topSpeed = topSpeed * Tracker.speedMultiplier;

    _ArmorMax = Armor; //Give us something to compare to later on
    _ShieldMax = Shield; //same
    _CoreStrength = (Armor.x+Armor.y+Armor.z+Armor.w+(Shield.x+Shield.y)/2)/3; //Generalized fomula for the unarmored mechanical core of the ship
  }

  // late update to give human or AI player scripts a chance to set values first
  void LateUpdate()
  {
    Steer();
    Throttle();
    Power();
    DoHealth();
  }

  public void DoDamage(Vector3 hitLoc, float damage)
  {
      //Where'd the hit come from, to the center of the ship?
      Vector3 damageAngle = hitLoc-transform.position;
      //Check font/back hit of the impact, apply that to the shields
      if(Vector3.Angle(transform.forward, damageAngle) < 90) //Hit from the front
      {
        print("hit from the front! Angle of" + Vector3.Angle(transform.forward, damageAngle));
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
            }
            else  //armor takes what it can, passes the rest onto internal damage;
              {
                damage -= Armor.x;
                Armor.x = 0;
                _CoreStrength -= damage;
              }
          }
          else if(Vector3.Angle(-transform.right, damageAngle) <= 45) // left armor hit!)
          {
            if(Armor.z > damage) //can the armor take the hit? 
            {  Armor.z -= damage;
            }
            else  //armor takes what it can, passes the rest onto internal damage;
              {
                damage -= Armor.z;
                Armor.z = 0;
                _CoreStrength -= damage;
              }
          }
          else if(Vector3.Angle(transform.right, damageAngle) <= 45) // right armor hit!)
          {
            if(Armor.w > damage) //can the armor take the hit? 
            {  Armor.w -= damage;
            }
            else  //armor takes what it can, passes the rest onto internal damage;
              {
                damage -= Armor.w;
                Armor.w = 0;
                _CoreStrength -= damage;
              }
          }
          else //HUH, no armor seems to have been hit. That's a dirty lie, so let's make them all suffer, plus a liiitle bit of core damage for fibbing.
          {
            Armor -= new Vector4(1,0,1,1)*damage/3;
            _CoreStrength -= damage/4;
          }
        }
      }
      else //Hit from the back
      {
        print("hit from the back!");
        if(Shield.y > damage)//if shields can take the hit, let them
        {  Shield.y -= damage;
          print("Shields damaged for "+ damage);
        }
        else //oh no! the armor needs to take the hit, minus whatever damage the shield can absorb.
        {
          damage -= Shield.y;
          Shield.y = 0;
          print("damage is now "+ damage);
          //check front/left/right armor quadrants, apply damage
          if(Vector3.Angle(-transform.forward, damageAngle) <= 45) // back armor hit!
          {
            if(Armor.y > damage) //can the armor take the hit? 
            {   Armor.y -= damage;
            }
            else  //armor takes what it can, passes the rest onto internal damage;
              {
                damage -= Armor.y;
                Armor.y = 0;
                _CoreStrength -= damage;
              }
          }
          else if(Vector3.Angle(-transform.right, damageAngle) <= 45) // left armor hit!)
          {
            if(Armor.z > damage) //can the armor take the hit? 
            {  Armor.z -= damage;
            }
            else  //armor takes what it can, passes the rest onto internal damage;
              {
                damage -= Armor.z;
                Armor.z = 0;
                _CoreStrength -= damage;
              }
          }
          else if(Vector3.Angle(transform.right, damageAngle) <= 45) // right armor hit!)
          {
            if(Armor.w > damage) //can the armor take the hit? 
            {  Armor.w -= damage;
            }
            else  //armor takes what it can, passes the rest onto internal damage;
              {
                damage -= Armor.w;
                Armor.w = 0;
                _CoreStrength -= damage;
              }
          }
          else //HUH, no armor seems to have been hit. That's a dirty lie, so let's make them all suffer, plus a liiitle bit of core damage for fibbing.
          {
            Armor -= new Vector4(0,1,1,1)*damage/3;
            _CoreStrength -= damage/4;
          }
      }
    }
  }
  GameObject Boom;
  void DoHealth()
  {
    //Constantly recharge the shields till full
    if(Shield.x < _ShieldMax.x)
      Shield.x += shieldRechargeRate*Time.deltaTime/20;
    if(Shield.y < _ShieldMax.y)
      Shield.y += shieldRechargeRate*Time.deltaTime/20;
    //Should do component damage here when the corestrength is low. Ignore for now
    if(_CoreStrength <= 0) //We dead, son
    { 
      if (!Boom)
      { Boom = Instantiate(DeathVFX,transform.position,Quaternion.identity,transform.parent);}
      Destroy(gameObject, .25f);
      //Makre sure everyone knows we're gone
      Tracker.RegisterAllShips();
      Tracker.RegisterTeams();
    }
  }

  void Power()
  {
    if(capacitorLevel<capacitorSize) //Charge Them Guns
    {
      capacitorLevel += rechargeRate*Time.deltaTime;
    }
  }

  [HideInInspector] public Vector3 refTurn;
  Vector3 oldRot;
  void Steer() //Autopilot!
  {
    var yaw_ = Mathf.Clamp(yaw, -1f, 1f);
    var pitch_ = Mathf.Clamp(pitch, -1f, 1f);
    var roll_ = Mathf.Clamp(roll, -1f, 1f);
    yaw_ *= turnRate * Time.deltaTime;
    pitch_ *= turnRate * Time.deltaTime;
    roll_ *= turnRate * 2f * Time.deltaTime;
    transform.localRotation *= Quaternion.AngleAxis(roll_, Vector3.forward) * Quaternion.AngleAxis(yaw_, Vector3.up) * Quaternion.AngleAxis(pitch_, invertYAxis ? Vector3.right : Vector3.left);
    refTurn = (transform.localEulerAngles-oldRot);
    oldRot = transform.localEulerAngles;
  }

  void Throttle()
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
    if(targetSpeed > topSpeed+1)
     isAfterburning = true;
     else
     isAfterburning = false;
    // also set the visible flare throttles
    foreach (EngineFlare flare in engineFlares)
    {
      flare.FlareThrottle = speed/(topSpeed);
    }
  }
}
