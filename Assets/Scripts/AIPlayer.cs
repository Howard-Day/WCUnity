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
    ship.yaw = -1f;
    ship.pitch = .1f;
    ship.roll = 0f;
    ship.targetSpeed = 10f;

  }
  /// <summary>
  /// Update is called every frame, if the MonoBehaviour is enabled.
  /// </summary>
  void Update()
  {
    ship.targetSpeed = Mathf.Clamp01(Mathf.Sin(Time.time)*4f-3f)*ship.burnSpeed;
    //ship.roll = (1f-Mathf.Clamp01(Mathf.Sin(Time.time)*2.5f-1f))*.2f;

    //Occasionally Fire the guns
    if(Mathf.Clamp01(Mathf.Sin(Time.time/2)*5f-3f) > 0)
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
  }
}
