using UnityEngine;

public class LaserCannon : MonoBehaviour
{
  [HideInInspector] public bool fire = false;

  [SerializeField] Transform mountingPoint = null;
  [SerializeField] Transform laserBoltPrefab = null;
  [SerializeField] float fireRate = .4f;
  [SerializeField] float powerDrain = 2.1f;
  
  float cooldown = 0f;
  Ship MainShip;

  /// <summary>
  /// Start is called on the frame when a script is enabled just before
  /// any of the Update methods is called the first time.
  /// </summary>
  void Start()
  {
      MainShip = gameObject.GetComponent<Ship>();
  }
  // late update to give human or AI player scripts a chance to set values first
  void LateUpdate()
  {
    cooldown = Mathf.Max(cooldown - Time.deltaTime, 0f);

    if (fire && cooldown <= 0f && MainShip.capacitorLevel > powerDrain*2.1f)
    {
      Instantiate(laserBoltPrefab, mountingPoint.position, mountingPoint.rotation);
      MainShip.capacitorLevel -= powerDrain;
      cooldown = fireRate;
    }
  }
}
