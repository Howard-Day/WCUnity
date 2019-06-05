using UnityEngine;

public class CockpitController : MonoBehaviour
{
  public float targetSpeed = 15f;

  [SerializeField] float turnRate = 50f;
  [SerializeField] bool invertYAxis = false;
  [SerializeField] float speedSelectionSpeed = 10f;
  [SerializeField] float topSpeed = 20f;
  [SerializeField] float acceleration = 1.5f;
  [SerializeField] float deceleration = 1f;

  float speed;

  void Start()
  {
    speed = targetSpeed;
  }

  void Update()
  {
    Steer();
    SetTargetSpeed();
    MoveForwards();
  }

  void Steer()
  {
    var yaw = ((Input.mousePosition.x / Screen.width) * 2f - 1f) * turnRate * Time.deltaTime;
    var pitch = ((Input.mousePosition.y / Screen.height) * 2f - 1f) * turnRate * Time.deltaTime;

    transform.localRotation *= Quaternion.AngleAxis(yaw, Vector3.up) * Quaternion.AngleAxis(pitch, invertYAxis ? Vector3.right : Vector3.left);
  }

  void SetTargetSpeed()
  {
    var fullStop = Input.GetKey(KeyCode.Backspace);
    var accelerate = Input.GetKey(KeyCode.Equals) || Input.GetKey(KeyCode.KeypadPlus); // KeyCode.Equals is the plus key without modifier
    var decelerate = Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus);
    
    if (fullStop)
    {
      targetSpeed = 0f;
    }
    else
    {
      if (accelerate && !decelerate)
      {
        targetSpeed = Mathf.Min(targetSpeed + speedSelectionSpeed * Time.deltaTime, topSpeed);
      }
      else if (decelerate && !accelerate)
      {
        targetSpeed = Mathf.Max(targetSpeed - speedSelectionSpeed * Time.deltaTime, 0f);
      }
    }
  }

  void MoveForwards()
  {
    if (speed < targetSpeed)
    // accelerating
    {
      speed = Mathf.Lerp(speed, targetSpeed, acceleration * Time.deltaTime);
    }
    else if (speed > targetSpeed)
    // decelerating
    {
      speed = Mathf.Lerp(speed, targetSpeed, deceleration * Time.deltaTime);
    }

    Debug.Log("Speed: " + speed);

    transform.position += transform.forward * speed * Time.deltaTime;
  }
}
