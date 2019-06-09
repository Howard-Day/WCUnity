using UnityEngine;

public class LaserCannon : MonoBehaviour
{
  [HideInInspector] public bool fire = false;

  [SerializeField] Transform mountingPoint = null;
  [SerializeField] Transform laserBoltPrefab = null;
  [SerializeField] float fireRate = 1f;
  
  float cooldown = 0f;

  // late update to give human or AI player scripts a chance to set values first
  void LateUpdate()
  {
    cooldown = Mathf.Max(cooldown - Time.deltaTime, 0f);

    if (fire && cooldown <= 0f)
    {
      Instantiate(laserBoltPrefab, mountingPoint.position, mountingPoint.rotation);
      cooldown = fireRate;
    }
  }
}
