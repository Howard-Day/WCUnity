using UnityEngine;

[RequireComponent(typeof(Ship))]
public class AIPlayer : MonoBehaviour
{
  Ship ship;

  void Start()
  {
    ship = GetComponent<Ship>();

    // hardoded to fly in circle for now
    ship.yaw = -0.5f;
    ship.pitch = 0.2f;
    ship.roll = .1f;
    ship.targetSpeed = 10f;
  }
}
