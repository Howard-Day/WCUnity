using UnityEngine;

[RequireComponent(typeof(Ship))]
public class AIPlayer : MonoBehaviour
{
  Ship ship;
  LaserCannon[] laserCannons;

  void Start()
  {
    ship = GetComponent<Ship>();
    laserCannons = GetComponentsInChildren<LaserCannon>();

    // hardcoded to fly in circle for now
    ship.yaw = -0.5f;
    ship.pitch = 0.2f;
    ship.roll = .1f;
    ship.targetSpeed = 10f;

    // also never stop firing the guns
    foreach (LaserCannon laserCannon in laserCannons)
    {
      laserCannon.fire = true;
    }
  }
}
