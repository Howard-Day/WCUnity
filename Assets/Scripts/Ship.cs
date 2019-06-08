using UnityEngine;

public class Ship : MonoBehaviour
{
  [HideInInspector] public float yaw;
  [HideInInspector] public float pitch;
  [HideInInspector] public float targetSpeed;

  [SerializeField] float turnRate = 50f;
  [SerializeField] bool invertYAxis = false;
  [SerializeField] float topSpeed = 20f;
  [SerializeField] float acceleration = 1.5f;
  [SerializeField] float deceleration = 1f;

  float speed;

  void Start()
  {
    speed = 0f;
  }

  // late update to give human or AI player scripts a chance to set values first
  void LateUpdate()
  {
    Steer();
    Throttle();
  }

  void Steer()
  {
    var yaw_ = Mathf.Clamp(yaw, -1f, 1f);
    var pitch_ = Mathf.Clamp(pitch, -1f, 1f);

    yaw_ *= turnRate * Time.deltaTime;
    pitch_ *= turnRate * Time.deltaTime;

    transform.localRotation *= Quaternion.AngleAxis(yaw_, Vector3.up) * Quaternion.AngleAxis(pitch_, invertYAxis ? Vector3.right : Vector3.left);
  }

  void Throttle()
  {
    var targetSpeed_ = Mathf.Clamp(targetSpeed, 0f, topSpeed);
    
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
  }
}
