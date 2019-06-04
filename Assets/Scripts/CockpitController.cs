using UnityEngine;

public class CockpitController : MonoBehaviour
{
  public float targetSpeed;

  [SerializeField] float turnRate = 0.5f;
  [SerializeField] bool invertYAxis = false;

  void Update()
  {
    DoSteering();
    DoThrottle();
  }

  void DoSteering()
  {
    var yaw = ((Input.mousePosition.x / Screen.width) * 2f - 1f) * turnRate;
    var pitch = ((Input.mousePosition.y / Screen.height) * 2f - 1f) * turnRate;

    transform.localRotation *= Quaternion.AngleAxis(yaw, Vector3.up) * Quaternion.AngleAxis(pitch, invertYAxis ? Vector3.right : Vector3.left);
  }

  void DoThrottle()
  {
    transform.position += transform.forward * targetSpeed * Time.deltaTime; // TODO: this introduces a lot of shakiness
  }
}
