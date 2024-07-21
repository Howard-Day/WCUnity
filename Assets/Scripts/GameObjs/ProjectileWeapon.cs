using UnityEngine;

public class ProjectileWeapon : MonoBehaviour
{
    
    public enum GunType { Laser, Neutron, MassDriver, PhotonBlaster, Meson, Particle, ParticleCollisionEvent, TurretLaser, TurretNeutron, TurretMassDriver };
    public GunType Type;
    Transform mountingPoint = null;
    [SerializeField] public Transform projectilePrefab = null;
    [SerializeField] Transform muzzleflashPrefab = null;
    [SerializeField] float fireRate = .4f;
    [SerializeField] public float powerDrain = 2.1f;
    public float gunRange;
    float cooldown = 0f;
    ShipSettings MainShip;
    //[HideInInspector] 
    public int index;
    [HideInInspector] public bool fire = false;
    [HideInInspector] public int GunId = 0;
    [HideInInspector] public float speed = 0;
    public bool hasFired = false; //has the gun actually fired?
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
        speed = projectilePrefab.GetComponent<Projectile>().speed;
    }
    // late update to give human or AI player scripts a chance to set values first
    void LateUpdate()
    {
       
        //Catch case in occurance of not having an ID
        if (GunId == 0)
            GunId = MainShip.ShipID;

        cooldown = Mathf.Max(cooldown - Time.deltaTime, 0f);

        if (fire && cooldown <= 0f && MainShip.capacitorLevel > powerDrain)
        {
            Transform Proj = Instantiate(projectilePrefab, mountingPoint.position, mountingPoint.rotation);
            Instantiate(muzzleflashPrefab, mountingPoint.position, mountingPoint.rotation, transform.parent);

            //Set the Projectile ID to the GunID, and thus the ShipID. No shooting yourself.
            Proj.gameObject.GetComponent<Projectile>().ProjID = GunId;
            MainShip.capacitorLevel -= powerDrain;
            cooldown = fireRate;
            hasFired = true;
        }
        if (cooldown < .125f)
        {
            hasFired = false;
        }
    }
}
