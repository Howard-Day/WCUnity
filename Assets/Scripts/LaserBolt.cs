using UnityEngine;

public class LaserBolt : MonoBehaviour
{
  [SerializeField] float speed = 100f;
  [SerializeField] float range = 1000f;

  float distanceTraveled = 0f;

  void Update()
  {
    transform.position += transform.forward * speed * Time.deltaTime;

    distanceTraveled += speed * Time.deltaTime;
    if (distanceTraveled > range)
    {
      Destroy(gameObject);
    }
  }
}
