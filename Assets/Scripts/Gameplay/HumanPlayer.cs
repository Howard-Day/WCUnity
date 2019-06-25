using UnityEngine;

[RequireComponent(typeof(ShipSettings))]
public class HumanPlayer : MonoBehaviour
{
  [SerializeField] float speedSelectionSpeed = 10f;


  ShipSettings ship;
  LaserCannon[] laserCannons;
  

  void Start()
  {
    ship = GetComponent<ShipSettings>();
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
    ship.yaw = Mathf.Clamp((Input.mousePosition.x / Screen.width) * 2f - 1f,-1f,1f);
    ship.pitch = Mathf.Clamp((Input.mousePosition.y / Screen.height) * 2f - 1f,-1f,1f);;
  }

  void Throttle()
  {
    var fullStop = Input.GetKey(KeyCode.Backspace);
    var fullSpeed = Input.GetKey(KeyCode.Backslash);
    var afterBurn = Input.GetKey(KeyCode.Tab);    
    var afterBurnOff = Input.GetKeyUp(KeyCode.Tab);
    var accelerate = Input.GetKey(KeyCode.Equals) || Input.GetKey(KeyCode.KeypadPlus); // KeyCode.Equals is the plus key without modifier
    var decelerate = Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus);
    
    if(afterBurnOff)
      {
        ship.targetSpeed = ship.topSpeed/2;
      }   

    if (afterBurn)
    {
      ship.targetSpeed = ship.burnSpeed;
    }
    else {
    
      if (fullSpeed)
      {
        ship.targetSpeed = ship.topSpeed;
      }
      if (fullStop)
      {
        ship.targetSpeed = 0f;
      }
      else
      {      
        if (accelerate && !decelerate && ship.targetSpeed < ship.topSpeed )
        {
          ship.targetSpeed += speedSelectionSpeed * Time.deltaTime;
        }
        else if (decelerate && !accelerate && ship.targetSpeed > 0f)
        {
          ship.targetSpeed -= speedSelectionSpeed * Time.deltaTime;
        }
      }
    }

  }

  void FireGuns()
  {
    var fire = Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0);
  foreach (LaserCannon laserCannon in laserCannons)
  {
   laserCannon.fire = fire;
   if(fire)
   { 
    ship.isFiring = true;
   }
   else
   {
    ship.isFiring = false;
   }
  }
}
}
