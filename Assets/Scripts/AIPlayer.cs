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
    ship.yaw = -0.5f;
    ship.pitch = .1f;
    ship.roll = .1f;
    ship.targetSpeed = 10f;

    // also never stop firing the guns
    foreach (LaserCannon laserCannon in laserCannons)
    {
      laserCannon.fire = true;
    }
  }
  /// <summary>
  /// Update is called every frame, if the MonoBehaviour is enabled.
  /// </summary>
  void Update()
  {
      ship.targetSpeed = Mathf.Clamp01(Mathf.Sin(Time.time)*2.5f-.5f)*ship.burnSpeed;
  }
}
