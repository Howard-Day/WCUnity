using UnityEngine;

public class CockpitController : MonoBehaviour
{
  public float targetSpeed = 15f;

  [SerializeField] float turnRate = 50f;
  [SerializeField] bool invertYAxis = false;
  [SerializeField] float speedSelectionSpeed = 10f;
  [SerializeField] float topSpeed = 20f;

  void Update()
  {
    DoSteering();
    DoThrottle();
  }

  void DoSteering()
  {
    var yaw = ((Input.mousePosition.x / Screen.width) * 2f - 1f) * turnRate * Time.deltaTime;
    var pitch = ((Input.mousePosition.y / Screen.height) * 2f - 1f) * turnRate * Time.deltaTime;

    transform.localRotation *= Quaternion.AngleAxis(yaw, Vector3.up) * Quaternion.AngleAxis(pitch, invertYAxis ? Vector3.right : Vector3.left);
  }

  void DoThrottle()
  {
    var fullStop = Input.GetKey(KeyCode.Backspace);
    var accelerate = Input.GetKey(KeyCode.KeypadPlus);
    var decelerate = Input.GetKey(KeyCode.KeypadMinus);
    
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

    transform.position += transform.forward * targetSpeed * Time.deltaTime;
  }
}
