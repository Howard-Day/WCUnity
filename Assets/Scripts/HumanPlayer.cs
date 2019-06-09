using UnityEngine;

[RequireComponent(typeof(Ship))]
public class HumanPlayer : MonoBehaviour
{
  [SerializeField] float speedSelectionSpeed = 10f;

  Ship ship;
  LaserCannon[] laserCannons;

  void Start()
  {
    ship = GetComponent<Ship>();
    laserCannons = GetComponentsInChildren<LaserCannon>();
  }

  void Update()
  {
    Steer();
    Throttle();
    FireGuns();
  }

  void Steer()
  {
    ship.yaw = (Input.mousePosition.x / Screen.width) * 2f - 1f;
    ship.pitch = (Input.mousePosition.y / Screen.height) * 2f - 1f;
  }

  void Throttle()
  {
    var fullStop = Input.GetKey(KeyCode.Backspace);
    var accelerate = Input.GetKey(KeyCode.Equals) || Input.GetKey(KeyCode.KeypadPlus); // KeyCode.Equals is the plus key without modifier
    var decelerate = Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus);
    
    if (fullStop)
    {
      ship.targetSpeed = 0f;
    }
    else
    {
      if (accelerate && !decelerate)
      {
        ship.targetSpeed += speedSelectionSpeed * Time.deltaTime;
      }
      else if (decelerate && !accelerate)
      {
        ship.targetSpeed -= speedSelectionSpeed * Time.deltaTime;
      }
    }
  }

  void FireGuns()
  {
    var fire = Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0);
    foreach (LaserCannon laserCannon in laserCannons)
    {
      laserCannon.fire = fire;
    }
  }
}
