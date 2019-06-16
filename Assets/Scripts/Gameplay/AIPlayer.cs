using UnityEngine;

[RequireComponent(typeof(ShipSettings))]
public class AIPlayer : MonoBehaviour
{
  ShipSettings ship;
  LaserCannon[] laserCannons;

  void Start()
  {
    ship = GetComponent<ShipSettings>();
    laserCannons = GetComponentsInChildren<LaserCannon>();
    // hardcoded to fly in circle for now
    ship.yaw = -.5f;
    ship.pitch = .1f;
    ship.roll = 1f;
    ship.targetSpeed = 10f;

  }
  Vector3 smoothDir = Vector3.zero;
  Vector3 refDir = Vector3.zero;
  void SteerTo(Vector3 aimAt, float turnSpeed)
  {
    //kill any manual steering
    ship.yaw = 0;//Mathf.SmoothStep(ship.yaw,0f,.1f);
    ship.pitch = 0;//Mathf.SmoothStep(ship.pitch,0f,.1f);
    //ship.roll = 0;//Mathf.SmoothStep(ship.roll,0f,.1f);
    //print("killed manual rotations from "+ ship.pitch +" "+ ship.yaw +" "+ ship.roll +"!");
    //ship.roll = 0;
    //autosteers
    Vector3 targetDir = aimAt - transform.position;
    Vector3 newDir = Vector3.RotateTowards(transform.forward,targetDir,turnSpeed*Time.deltaTime,0.0f);
    //smoothDir = Vector3.SmoothDamp(smoothDir,newDir, ref refDir,.2f);
    //print("turning towards"+ newDir +"with a smoothed vector of "+ smoothDir);
    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(newDir,transform.up), .75f); 
  }

  /// <summary>
  /// Update is called every frame, if the MonoBehaviour is enabled.
  /// </summary>
  void Update()
  {
    ship.targetSpeed = Mathf.Clamp01(Mathf.Sin(Time.time*3)*.75f+.5f)*ship.topSpeed;
    //Afterburn every so often.
    if(Mathf.Clamp01(Mathf.Sin(Time.time/2)*5f-4f) > 0)
    {
      ship.targetSpeed = ship.burnSpeed;
    }
    //Occasionally Fire the guns
    if(Mathf.Clamp01(Mathf.Sin(Time.time/3)*5f-3f) > 0)
    {
      foreach (LaserCannon laserCannon in laserCannons)
      {
        laserCannon.fire = true;
      }
    }
    else
    {
      foreach (LaserCannon laserCannon in laserCannons)
      {
        laserCannon.fire = false;
      }
    }
    //Steer back to origin!
    if (transform.position.magnitude > 250*Mathf.Clamp01(Mathf.Sin(Time.time)*6f-4f)+50)
    { 
      SteerTo (Vector3.zero, ship.turnRate * (Mathf.PI/180) );
    }
    //occasionally spin! 
    if(Mathf.Clamp01(Mathf.Sin(Time.time/1.5f+.25f)*6f-4f) > 0)
    {
      ship.roll = Mathf.SmoothStep(ship.roll,1f,.05f);
      //print("Rolling to "+ ship.roll +"!");
    }
    else
    {
      ship.roll = Mathf.SmoothStep(ship.roll,0f,.05f);
    }
  //print(ship.roll);
  }
}
