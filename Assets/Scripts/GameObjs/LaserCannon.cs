using UnityEngine;

public class LaserCannon : MonoBehaviour
{
  [HideInInspector] public bool fire = false;
  [HideInInspector] public int GunId = 0;
  public enum GunType {Laser, Neutron, MassDriver, ParticleCollisionEvent, Turret};
  public GunType Type;
  Transform mountingPoint = null;
  [SerializeField] Transform laserBoltPrefab = null;
  [SerializeField] float fireRate = .4f;
  [SerializeField] float powerDrain = 2.1f;
  public float gunRange;
  float cooldown = 0f;
  ShipSettings MainShip;

  /// <summary>
  /// Start is called on the frame when a script is enabled just before
  /// any of the Update methods is called the first time.
  /// </summary>
  void Start()
  {
      MainShip = gameObject.GetComponentInParent<ShipSettings>();
      mountingPoint = gameObject.transform;
      //Set the Gun ID to the firing Ship ID
      GunId = MainShip.ShipID;
  }
  // late update to give human or AI player scripts a chance to set values first
  void LateUpdate()
  {
    //Catch case in occurance of not having an ID
    if(GunId == 0)
      GunId = MainShip.ShipID;

    cooldown = Mathf.Max(cooldown - Time.deltaTime, 0f);

    if (fire && cooldown <= 0f && MainShip.capacitorLevel > powerDrain)
    {
      Transform Proj = Instantiate(laserBoltPrefab, mountingPoint.position, mountingPoint.rotation);
      //Set the Projectile ID to the GunID, and thus the ShipID. No shooting yourself.
      Proj.gameObject.GetComponent<LaserBolt>().ProjID = GunId;
      MainShip.capacitorLevel -= powerDrain;
      cooldown = fireRate;
    }
  }
}
