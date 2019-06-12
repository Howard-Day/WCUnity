using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipSettings : MonoBehaviour
{
  [HideInInspector] public float yaw;
  [HideInInspector] public float pitch;
  [HideInInspector] public float roll;
  [HideInInspector] public float targetSpeed;
  [HideInInspector] public float capacitorLevel;
  [HideInInspector] public bool isAfterburning;
  [SerializeField] float turnRate = 50f;
  [SerializeField] bool invertYAxis = false;
  [SerializeField] public float topSpeed = 20f;
  [SerializeField] public float burnSpeed = 50f;
  [SerializeField] float acceleration = 1.5f;
  [SerializeField] float deceleration = 1f;

  [SerializeField] public float capacitorSize = 50f;
  [SerializeField] float rechargeRate = 1f;
  EngineFlare[] engineFlares;

  [HideInInspector]  public float speed = 0f;
  void Start()
  {
    engineFlares = GetComponentsInChildren<EngineFlare>();
    capacitorLevel = capacitorSize;
  }

  // late update to give human or AI player scripts a chance to set values first
  void LateUpdate()
  {
    Steer();
    Throttle();
    Power();
  }

  void Power()
  {
    if(capacitorLevel<capacitorSize)
    {
      capacitorLevel += rechargeRate*Time.deltaTime;
    }
    //print(gameObject.name + " capacitor level is " + capacitorLevel);
  }

  void Steer()
  {
    var yaw_ = Mathf.Clamp(yaw, -1f, 1f);
    var pitch_ = Mathf.Clamp(pitch, -1f, 1f);
    var roll_ = Mathf.Clamp(roll, -1f, 1f);
    yaw_ *= turnRate * Time.deltaTime;
    pitch_ *= turnRate * Time.deltaTime;
    roll_ *= turnRate * 2f * Time.deltaTime;
    transform.localRotation *= Quaternion.AngleAxis(roll_, Vector3.forward) * Quaternion.AngleAxis(yaw_, Vector3.up) * Quaternion.AngleAxis(pitch_, invertYAxis ? Vector3.right : Vector3.left);
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
    //print(speed);
    // also set the visible flare throttles
    foreach (EngineFlare flare in engineFlares)
    {
      flare.FlareThrottle = speed/topSpeed;
    }
  }
}
