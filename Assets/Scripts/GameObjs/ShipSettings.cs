using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

public class ShipSettings : MonoBehaviour
{
	[SerializeField] private ShipSettingsAsset settings;

    [SerializeField] public bool isWingLead = false;
    [SerializeField] public LayerMask CollidesWith;
    [Header("Billboard")]
    [SerializeField] public GameObject Billboard;
    [Header("VDU Icon!")]
    [SerializeField] public Sprite VDUImage;

    [Header("Movement Settings")]
    [SerializeField] public LayerMask AutoAvoids;
    [SerializeField] public bool bingoFuel = false;
    [SerializeField] public bool invertYAxis = false;

    [Header("Death Effect")]
    [SerializeField] public GameObject[] DeathVFX;
    [SerializeField] public GameObject DeathTrailVFX;
    [SerializeField] public GameObject InternalDamageVFX;
    [SerializeField] public GameObject DamageVFX;
    [SerializeField] public GameObject DamageTrails;
    [Header("Special Abilities")]
    [HideInInspector] public float cloakCapacitorLevel;

    [SerializeField] public GameObject[] turrets;
    [SerializeField] public List<ProjectileWeapon> projWeapons;

    [Header("SFX")]
    [SerializeField] public AudioClip EngineSound;
    [SerializeField] public Vector2 MinMaxThrottlePitch = Vector2.one;
    [SerializeField] public Vector2 MinMaxThrottleVolume = Vector2.one;
    [SerializeField] public AudioClip AfterburnSound;
    [SerializeField] public float AfterburnPitch = 1f;
    [SerializeField] public float AfterburnVolume = .25f;
    [SerializeField] public float AfterburnSmoothness = .25f;
    [SerializeField] public AudioClip CloakOnSound;
    [SerializeField] public AudioClip CloakOffSound;

    //Hidden Attributes
    [HideInInspector] public bool isPlayer = false;
    [HideInInspector] public GameObject playerUI;
    [HideInInspector] public float shipRadius;
    [HideInInspector] public float CoreMax;
    [HideInInspector] public bool hitInAss = false; //this is important information, for a lot of reasons.
    EngineFlare[] engineFlares;
    Material billboardMat;
    [HideInInspector] public AudioSource EngineSFX;
    [HideInInspector] public AudioSource AfterburnSFX;
    [HideInInspector] public AudioSource CloakSFX;
    [HideInInspector] public float AfterburnBlend = 0f;
    public class DamageComponents
    {
        public float IonDrive = 0f;
        public float PowerPlant = 0f;
        public float ShieldGen = 0f;
        public float CompSys = 0f;
        public float ComUnit = 0f;
        public float Track = 0f;
        public float AccelAbs = 0f;
        public float EjectSys = 0f;
        public float RepairSys = 0f;
        public float Jets = 0f;
    }
    [HideInInspector] public DamageComponents componentDamage = new DamageComponents();
    [HideInInspector] public float _CoreStrength;
    [SerializeField] public float _Fuel;
    [HideInInspector] public int ShipID;
    [HideInInspector] public float yaw;
    [HideInInspector] public float pitch;
    [HideInInspector] public float roll;
    [HideInInspector] public float targetSpeed;
    [HideInInspector] public float capacitorLevel;
    [HideInInspector] public bool isFiring = false;
    [HideInInspector] public bool isAfterburning;
    [HideInInspector] public float speed = 0f;
    [HideInInspector] GameObjTracker Tracker;

    [HideInInspector] public int numWingmen = 0;
    [HideInInspector] public bool isDead = false;
    [HideInInspector] public bool isBeingShot = false;
    [HideInInspector] public bool isLocked = false;
    [HideInInspector] public ShipSettings currentTarget;
    [HideInInspector] public bool currentLocked = false;

    [HideInInspector] public bool hitInternal = false;

    [HideInInspector] public float recover = 1f;
    [HideInInspector] public Vector3 BounceDir;
    [HideInInspector] public Vector3 BounceSpin;
    [HideInInspector] public float BouncePush;

    [HideInInspector] public enum HitLoc { F, R, L, U, D, B, NULL };
    [HideInInspector] public HitLoc lastHit;
    [HideInInspector] public int lastHitID;

    GameObject Boom;
    [HideInInspector] public Vector3 DeathDir = Vector3.zero;
    [HideInInspector] public float DeathVel;
    Vector3 DeathSpin;
    int DeathType;
    float DeathLength;
    GameObject Trail;
    Transform DecoRoot;

    [HideInInspector] public Quaternion oldRot;
    [HideInInspector] public Vector3 rotDelta;
    Quaternion LagDir;
    Pose lastTrans;

    [HideInInspector] public float throttle;
    [HideInInspector] public float flareIntensity = 1f;

    public bool Cloak = false;
    public bool isCloaked = false;
    public bool isCloaking = false;
    public float cloakedAmount = 0f;

    [HideInInspector] public Vector3 lastPos = Vector3.zero;
    [HideInInspector] public Vector3 currentPos;
    public Vector3 velocity;

    private ArmorStatus armor;
    private ShieldStatus shield;

    #region PROPERTIES
    public ShipSettingsAsset Settings => settings;

    public string DisplayName => settings.DisplayName;
    public TEAM AITeam => settings.AITeam;
    public IReadOnlyArmorStatus Armor => armor;
    public IReadOnlyShieldStatus Shield => shield;

    public float ShieldFrontNormalized => shield.Front / settings.Shield.Front;
    public float ShieldBackNormalized => shield.Back / settings.Shield.Back;
    #endregion

    private void Awake() {
        Assert.IsNotNull(settings);
	}

	void Start()
    {
        armor = new ArmorStatus(settings.Armor);
        shield = new ShieldStatus(settings.Shield);

        //assign a random ID
        SetId();
        if (settings.Class == CLASS.FIGHTER)
        { 
            shipRadius = GetComponent<SphereCollider>().radius;
            //grab the display part of the billboard, for futher modification
            GetBillboardMat();
        }
        else
        {
            shipRadius = GetComponent<MeshCollider>().bounds.size.magnitude/6;
        }

        //Organize the scene
        gameObject.transform.SetParent(GameObject.FindWithTag("GamePlayObjs").transform);

        //Make sure everyone knows we're here
        // TODO: this is extremely inefficient.
        GameObjTracker.RegisterAllShips();
        GameObjTracker.RegisterTeams();

        //grab the sub-object engine flares to control them
        engineFlares = GetComponentsInChildren<EngineFlare>();
        //Atomic Batteries to power
        capacitorLevel = settings.CapacitorSize;
        cloakCapacitorLevel = settings.CloakPower;
        //Turbines to speed
        //check fuel Light
        _Fuel = settings.MaxFuel;
        //Power Weapons
        InitGuns();

        _CoreStrength = (settings.Armor.Sum + (settings.Shield.Sum) / 2f)/3f; //Generalized fomula for the unarmored mechanical core of the ship
        CoreMax = _CoreStrength;

        //Init SFX
        InitEngineSFX();
        InitAfterburnSFX();
        if (settings.HasCloak)
        {
            InitCloakSFX();
        }
    }

    // TODO: these settings shouldn't be hardcoded. AudioSources should be part
    // of the prefabs, rather than instantiated at runtime.
    #region SFX INIT
    private void InitEngineSFX()
    {
        EngineSFX = gameObject.AddComponent<AudioSource>();
        EngineSFX.clip = EngineSound;
        EngineSFX.playOnAwake = true;
        EngineSFX.loop = true;
        EngineSFX.volume = MinMaxThrottleVolume.x;
        EngineSFX.spatialBlend = 1f;
        EngineSFX.dopplerLevel = 2f;
        EngineSFX.maxDistance = 80f;
        EngineSFX.minDistance = 10f;
        EngineSFX.rolloffMode = AudioRolloffMode.Linear;
        EngineSFX.Play();
    }

    private void InitCloakSFX()
    {
        CloakSFX = gameObject.AddComponent<AudioSource>();
        CloakSFX.playOnAwake = false;
        CloakSFX.loop = false;
        CloakSFX.volume = .25f;
        CloakSFX.spatialBlend = 1f;
        CloakSFX.dopplerLevel = 1f;
        CloakSFX.pitch = 1f;
        CloakSFX.maxDistance = 120f;
        CloakSFX.minDistance = 25f;
        CloakSFX.rolloffMode = AudioRolloffMode.Linear;
    }

    private void InitAfterburnSFX()
    {
        AfterburnSFX = gameObject.AddComponent<AudioSource>();
        AfterburnSFX.clip = AfterburnSound;
        AfterburnSFX.playOnAwake = true;
        AfterburnSFX.loop = true;
        AfterburnSFX.volume = 0f;
        AfterburnSFX.spatialBlend = 1f;
        AfterburnSFX.dopplerLevel = 2f;
        AfterburnSFX.pitch = AfterburnPitch;
        AfterburnSFX.maxDistance = 120f;
        AfterburnSFX.minDistance = 1f;
        AfterburnSFX.rolloffMode = AudioRolloffMode.Linear;
        AfterburnSFX.Play();
    }
    #endregion

    void DoSFX() 
    {
        EngineSFX.volume = Mathf.Lerp(MinMaxThrottleVolume.x, MinMaxThrottleVolume.y, throttle);
        EngineSFX.pitch = Mathf.Lerp(MinMaxThrottlePitch.x, MinMaxThrottlePitch.y, throttle);
        if(isAfterburning)
        {
            AfterburnBlend = 1f;
        }
        else 
        {
            AfterburnBlend = 0f;
        }
        AfterburnSFX.volume = Mathf.Lerp(AfterburnSFX.volume, AfterburnBlend* AfterburnVolume, AfterburnSmoothness);


    }
    public void SetId()
    {
        ShipID = Random.Range(-32000, 32000);
        while (ShipID == 0)
        {
            ShipID = Random.Range(-32000, 32000);
        }
    }

    public void GetBillboardMat() 
    {
        billboardMat = Billboard.GetComponent<Renderer>().material;   
    }

    void TargetManage()
    {
        if (currentTarget != null)
        {
            if (currentTarget.isLocked)
            {
                currentLocked = true;
            }
            else
            {
                currentLocked = false;
            }
        }
    }

    int countFireIndex = 0;
    int lastFireIndex = 0;
    //Find our Guns, Figure out what they are, sequence them and put them in a list! 
    void InitGuns()
    {
        ProjectileWeapon[] tempProjWeapon;
        tempProjWeapon = GetComponentsInChildren<ProjectileWeapon>();        

        foreach (ProjectileWeapon projWeapon in tempProjWeapon)
        {
            //ignore any turret mounted weapons
            if (!projWeapon.turretMounted)
                projWeapons.Add(projWeapon);

            //Init gun index
            if (projWeapon.index == 0)
            {
                projWeapon.index = countFireIndex;
                countFireIndex++;
            }
        }

    }
    //Do Velocity calculation
    void DoVelocity()
    {
        currentPos = transform.position;
        velocity = (currentPos - lastPos) /Time.deltaTime;
        lastPos = transform.position;
    }
    //Fire Guns! 
    public void FireGuns(bool fire)
    {
        //force the guns to disable if we're still cloaked! 
        if (cloakedAmount > .1f)
        {
            fire = false;
        }
        //loop through the guns
        foreach (ProjectileWeapon projWeapon in projWeapons)
        {
            if (capacitorLevel < projWeapon.powerDrain * (countFireIndex + 1 ))
            {
                if (recover >= .99f && projWeapon.index != lastFireIndex) // Can the ship fire? Is this gun *not* the last to fire? Are we Cloaked? 
                {
                    projWeapon.fire = fire;
                    //increment through guns
                    
                    // if(logDebug){print("aactually setting state to " + fire);}
                    //are we firing?
                    isFiring = fire; //Make sure our broadcast flag is set! 
                }
                if (recover >= .99f && projWeapon.index == lastFireIndex) // Can the ship fire? Is this gun the last to fire? 
                {
                    projWeapon.fire = false;
                }
                if (recover < .75f) //wait for recharge or return of control! 
                {
                    projWeapon.fire = false;
                    isFiring = false;
                }
            }
            else if (recover >= .99f)
            {
                projWeapon.fire = fire;
                isFiring = fire;
            }

            if (fire == false)
            {
                projWeapon.fire = fire;
                isFiring = fire;
            }

            if (projWeapon.hasFired)
            {
                lastFireIndex = projWeapon.index;
            }
        }
    }
    //Handle our Fuel Levels
    void DoFuel()
    {
        var normalizedThrottle = Mathf.Clamp01(speed / settings.TopSpeed);
        if (_Fuel > 0) //WE've got fuel, let's go! 
        {
            if (!isAfterburning)
            {//Do normal fuel drain based on throttle
                _Fuel -= normalizedThrottle * Time.deltaTime * 4f;
            }
            else// Now we're burning fuel to GO VERY FAST
            {
                _Fuel -= settings.FuelBurnRate * Time.deltaTime * 5f;
            }
        }
        else //Fuck, basically just a max coasting speed. Good fucking luck, cowboy
        {
            bingoFuel = true;
            speed = Mathf.Min(targetSpeed, settings.TopSpeed * .666f);
            isAfterburning = false;
        }
    }
    //Handle Internal Damage
    void InternalDamage(bool doComponentDamage)
    {
        //Show that internal damage has taken place! 
        hitInternal = doComponentDamage;
        //print("Internal Damage is " + doComponentDamage);
        Instantiate(InternalDamageVFX, transform.position, Quaternion.identity, transform);
        if (hitInternal) //only do damage if intentional! 
        {
            if (lastHit == HitLoc.F) // damage the components that got hit from the front, randomly
            {
                componentDamage.ComUnit = +Mathf.Clamp01(Random.Range(-3f, .5f));
                componentDamage.CompSys = +Mathf.Clamp01(Random.Range(-3f, .5f));
                componentDamage.Track = +Mathf.Clamp01(Random.Range(-3f, .5f));
                componentDamage.EjectSys = +Mathf.Clamp01(Random.Range(-3f, .5f));
            }
            if (lastHit == HitLoc.B) // damage the components that got hit from the back, randomly
            {
                componentDamage.IonDrive = +Mathf.Clamp01(Random.Range(-3f, .5f));
                componentDamage.PowerPlant = +Mathf.Clamp01(Random.Range(-3f, .5f));
                componentDamage.Jets = +Mathf.Clamp01(Random.Range(-3f, .5f));
                componentDamage.CompSys = +Mathf.Clamp01(Random.Range(-3f, .5f));
            }
            if (lastHit == HitLoc.U || lastHit == HitLoc.D) // damage the components that got hit from the top/bottom
            {
                componentDamage.CompSys = +Mathf.Clamp01(Random.Range(-3f, .5f));
                componentDamage.AccelAbs = +Mathf.Clamp01(Random.Range(-3f, .5f));
                componentDamage.RepairSys = +Mathf.Clamp01(Random.Range(-3f, .5f));
                componentDamage.ShieldGen = +Mathf.Clamp01(Random.Range(-3f, .5f));
                componentDamage.EjectSys = +Mathf.Clamp01(Random.Range(-3f, .5f));
            }

            if (lastHit == HitLoc.R || lastHit == HitLoc.L) // damage the components that got hit from the top/bottom
            {
                componentDamage.AccelAbs = +Mathf.Clamp01(Random.Range(-3f, .5f));
                componentDamage.Jets = +Mathf.Clamp01(Random.Range(-3f, .5f));
            }
        }
    }
    //Handle Armor Damage VFX
    void ArmorDamage(Vector3 pos)
    {
        //Show that armor damage has taken place! 
        GameObject Debris = Instantiate(DamageVFX, pos, Quaternion.identity, DecoRoot);
        Debris.GetComponent<DampInitVelocity>().initDir = DeathDir;
        Debris.GetComponent<DampInitVelocity>().initVel = DeathVel;
    }
    //Gently avoid obstacles!
    public void AvoidObstacles(float urgency, float avoidRadius)
    {
        //Check for all coliders within a reasonable distance
        Collider[] hitColliders = Physics.OverlapSphere(gameObject.transform.position, avoidRadius, AutoAvoids);
        int i = 0;
        while (i < hitColliders.Length)
        {
            if (hitColliders[i] != gameObject.GetComponent<SphereCollider>()) //Ignore it if we're.. LOOKING AT OURSELVES
            {

                //Find the direction to the obstacle
                Vector3 avoidDir = hitColliders[i].transform.position - gameObject.transform.position;
                //Inverse linear falloff to push out more strongly the closer they are
                float magPush = (1 - (avoidDir.magnitude / avoidRadius)) + .5f;
                //Gradually autofalloff to 0 intensity
                //magPush = Mathf.SmoothStep(magPush,0,.5f);
                //HEAVE AWAY, BOIS
                transform.position -= avoidDir * urgency * magPush * Time.deltaTime;

                if (Vector3.Dot(transform.up, avoidDir) > 0)
                {
                    pitch -= magPush / 8f;
                }
                else
                {
                    pitch += magPush / 8f;
                }

                //if(magPush > 0)
                //print(name +" is pushing away from "+ hitColliders[i].name +"with an intensity of " + magPush);

            }
            i++;
        }

    }
    //Violently Collide!
    public void DoBounce(float recoverRate, float bounceRadius)
    {
        Collider[] bounceColliders = Physics.OverlapSphere(gameObject.transform.position, bounceRadius/10, CollidesWith);
        int ib = 0;
        while (ib < bounceColliders.Length)
        {
            if (bounceColliders[ib] != gameObject.GetComponent<SphereCollider>()) //Ignore it if we're.. LOOKING AT OURSELVES
            {
                //print("Bounce detected between "+ name +" and "+ bounceColliders[ib].name);
                ShipSettings hitShip = bounceColliders[ib].gameObject.GetComponent<ShipSettings>();
                if (recover >= 1f)// && hitShip != null)
                {
                    //Find the direction to the collision
                    Vector3 colDir = bounceColliders[ib].transform.position - gameObject.transform.position;
                    //equally bounce each ship, damage is made from the rest of the momentum
                    BouncePush = (speed + hitShip.speed) / 2f;
                    var weightDamageThem = ((speed + hitShip.speed) / hitShip.speed) / 4f;
                    var weightSpin = shipRadius / hitShip.shipRadius; 
                    var weightDamage = ((speed + hitShip.speed) / speed) / 4f;
                    DoDamage(bounceColliders[ib].transform.position, (speed + hitShip.speed) * .01f * weightDamage, hitShip.ShipID);
                    hitShip.DoDamage(transform.position, (speed + hitShip.speed) * .01f * weightDamageThem, ShipID);
                    //print("RAM Detected:" + name +" has rammed " + hitShip.name + " at relative speeds of " + speed +" and " + hitShip.speed+
                    //" and will be damaged " + (speed+hitShip.speed)*.05f*weightDamage + "to " + (speed+hitShip.speed)*.05f*weightDamageThem);
                    InternalDamage(false);
                    hitShip.recover = 0f;
                    hitShip.BouncePush = BouncePush;
                    hitShip.BounceDir = BounceDir;
                    BounceDir = -BounceDir;
                    BounceSpin = new Vector3(Random.Range(-3f, 3f), Random.Range(-3f, 3f), Random.Range(-3f, 3f));
                    hitShip.BounceSpin = new Vector3(Random.Range(-3f, 3f), Random.Range(-3f, 3f), Random.Range(-3f, 3f));
                    recover =  Random.Range(weightSpin * .25f, weightSpin * .5f);
                }
            }

            ib++;
        }
        if (recover < 1)
        {
            var byaw_ = Mathf.Clamp(BounceSpin.y, -1f, 1f);
            var bpitch_ = Mathf.Clamp(BounceSpin.x, -1f, 1f);
            var broll_ = Mathf.Clamp(BounceSpin.z, -1f, 1f);
            float turnRateFactor = settings.TurnRate * 5f * Time.deltaTime * (1 - recover);
            byaw_ *= turnRateFactor;
            bpitch_ *= turnRateFactor;
            broll_ *= turnRateFactor;

            pitch *= recover;
            yaw *= recover;
            roll *= recover;
            speed *= recover;
            if (recover < .025f)
            {
                InternalDamage(false);
            }
            transform.localRotation *= Quaternion.AngleAxis(broll_, Vector3.forward) * Quaternion.AngleAxis(byaw_, Vector3.up) * Quaternion.AngleAxis(bpitch_, invertYAxis ? Vector3.right : Vector3.left);
            transform.position += BounceDir * BouncePush * Time.deltaTime * (1 - recover);
            //speed = Random.Range(-topSpeed,topSpeed/2);
            recover += recoverRate * Time.deltaTime;
        }
    }
    //Handle Damage
    public int[] DoDamage(Vector3 hitLoc, float damage, int hitID) //returns true for a sheild hit, false for a hull hit, and the firing ship's ID. 
    {
        int[] hitTracker = new int[2];
        hitTracker[1] = ShipID;
        lastHitID = hitID;
        //reset Last hitID periodically
        if (GameObjTracker.frames % 30 == 0)
        {
            lastHitID = 0;
        }

            //Where'd the hit come from, to the center of the ship?
            Vector3 damageAngle = hitLoc - transform.position;
        //Check font/back hit of the impact, apply that to the shields
        if (Vector3.Angle(transform.forward, damageAngle) < 90) //Hit from the front
        {
            lastHit = HitLoc.F;
            //print("hit from the front! Angle of" + Vector3.Angle(transform.forward, damageAngle));
            if (shield.Front > damage)//if shields can take the hit, let them
            {
                shield.Front -= damage;
                hitTracker[0] = 1;
                return hitTracker;
            }
            else //oh no! the armor needs to take the hit, minus whatever damage the shield can absorb.
            {
                damage -= shield.Front;
                shield.Front = 0;
                
                //check front/left/right armor quadrants, apply damage
                if (Vector3.Angle(transform.forward, damageAngle) <= 45) // front armor hit!
                {
                    if (armor.Front > damage) //can the armor take the hit? 
                    {
                        armor.Front -= damage;
                        ArmorDamage(transform.position + damageAngle / 2);
                    }
                    else  //armor takes what it can, passes the rest onto internal damage;
                    {
                        damage -= armor.Front;
                        armor.Front = 0;
                        _CoreStrength -= damage;
                        InternalDamage(true);
                    }
                }
                else if (Vector3.Angle(-transform.right, damageAngle) <= 45) // left armor hit!)
                {
                    lastHit = HitLoc.L;
                    if (armor.Left > damage) //can the armor take the hit? 
                    {
                        armor.Left -= damage;
                        ArmorDamage(transform.position + damageAngle / 2);
                    }
                    else  //armor takes what it can, passes the rest onto internal damage;
                    {
                        damage -= armor.Left;
                        armor.Left = 0;
                        _CoreStrength -= damage;
                        InternalDamage(true);
                    }
                }
                else if (Vector3.Angle(transform.right, damageAngle) <= 45) // right armor hit!)
                {
                    lastHit = HitLoc.R;
                    if (armor.Right > damage) //can the armor take the hit? 
                    {
                        armor.Right -= damage;
                        ArmorDamage(transform.position + damageAngle / 2);
                    }
                    else  //armor takes what it can, passes the rest onto internal damage;
                    {
                        damage -= armor.Right;
                        armor.Right = 0;
                        _CoreStrength -= damage;
                        InternalDamage(true);
                    }
                }
                else //HUH, no armor seems to have been hit. That's a dirty lie, so let's make them all suffer, plus a liiitle bit of core damage for fibbing.
                {
                    if (Vector3.Angle(transform.up, damageAngle) <= 45)
                    { lastHit = HitLoc.U; }
                    if (Vector3.Angle(-transform.up, damageAngle) <= 45)
                    { lastHit = HitLoc.D; }
                    float scaledDamage = damage / 3f;
                    armor.Front -= scaledDamage;
                    armor.Left -= scaledDamage;
                    armor.Right -= scaledDamage;
                    InternalDamage(true);
                    _CoreStrength -= damage / 8;
                }
                hitTracker[0] = 0;
                return hitTracker;
            }
        }
        else //Hit from the back
        {
            lastHit = HitLoc.B;
            hitInAss = true;
            //print("hit from the back!");
            if (shield.Back > damage)//if shields can take the hit, let them
            {
                shield.Back -= damage;
                hitTracker[0] = 1;
                return hitTracker;
                //print("Shields damaged for "+ damage);
            }
            else //oh no! the armor needs to take the hit, minus whatever damage the shield can absorb.
            {
                // TODO: REFACTOR USING Side enum! ******************************

                damage -= shield.Back;
                shield.Back = 0;
                //print("damage is now "+ damage);
                //check front/left/right armor quadrants, apply damage
                if (Vector3.Angle(-transform.forward, damageAngle) <= 45) // back armor hit!
                {
                    if (armor.Back > damage) //can the armor take the hit? 
                    {
                        armor.Back -= damage;
                        ArmorDamage(transform.position + damageAngle / 2);
                    }
                    else  //armor takes what it can, passes the rest onto internal damage;
                    {
                        damage -= armor.Back;
                        armor.Back = 0;
                        _CoreStrength -= damage;
                        InternalDamage(true);
                    }
                }
                else if (Vector3.Angle(-transform.right, damageAngle) <= 45) // left armor hit!)
                {
                    lastHit = HitLoc.L;
                    if (armor.Left > damage) //can the armor take the hit? 
                    {
                        armor.Left -= damage;
                        ArmorDamage(transform.position + damageAngle / 2);
                    }
                    else  //armor takes what it can, passes the rest onto internal damage;
                    {
                        damage -= armor.Left;
                        armor.Left = 0;
                        _CoreStrength -= damage;
                        InternalDamage(true);
                    }
                }
                else if (Vector3.Angle(transform.right, damageAngle) <= 45) // right armor hit!)
                {
                    lastHit = HitLoc.R;
                    if (armor.Right > damage) //can the armor take the hit? 
                    {
                        armor.Right -= damage;
                        ArmorDamage(transform.position + damageAngle / 2);
                    }
                    else  //armor takes what it can, passes the rest onto internal damage;
                    {
                        damage -= armor.Right;
                        armor.Right = 0;
                        _CoreStrength -= damage;
                        InternalDamage(true);
                    }
                }
                else //HUH, no armor seems to have been hit. That's a dirty lie, so let's make them all suffer, plus a liiitle bit of core damage for fibbing.
                {
                    if (Vector3.Angle(transform.up, damageAngle) <= 45)
                    { lastHit = HitLoc.U; }
                    if (Vector3.Angle(-transform.up, damageAngle) <= 45)
                    { lastHit = HitLoc.D; }
                    float scaledDamage = damage / 3f;
                    armor.Back -= scaledDamage;
                    armor.Left -= scaledDamage;
                    armor.Right -= scaledDamage;
                    InternalDamage(true);

                    _CoreStrength -= damage / 4f;
                }
                hitTracker[0] = 0;
                return hitTracker;
            }
        }
    }

    bool playerUIDisconnected = false;
    DampInitVelocity lagMove;
    //Handle disconnecting the player camera when the ship dies, if it has one! TODO: add delay for the death animation
    void DoPlayerUIDisconnect() 
    { 
        //San check
        if (isPlayer && playerUI != null && !playerUIDisconnected)
        {
            //disconnect the UI from the ship
            playerUI.transform.parent = DecoRoot;
            //make sure we're uncloaked
            Cloak = false;
            //get the cockpit view control, set it to chase cam mode! 
            CockpitViewSwitcher cockpit = playerUI.GetComponentInChildren<CockpitViewSwitcher>();
            cockpit.ChaseSwitch = false;
            cockpit.ChaseCam = true;
            cockpit.deathCam = true;
            //add a damp initial velocity to the chase cam, give it the velocity!
            if (!lagMove)
            {
                lagMove = playerUI.gameObject.AddComponent<DampInitVelocity>();
                lagMove.initDir = DeathDir;
                lagMove.initVel = DeathVel;
            }
            //tell the game manager we've done this
            GameObjTracker.oldUI = playerUI.gameObject;
            playerUIDisconnected = true;
        }

    }

    //Handle Overall Ship Health
    void DoHealth()
    {
        const float RECHARGE_RATE_FACTOR = 1 / 20f;

        //Constantly recharge the shields till full
        if (shield.Front < settings.Shield.Front)
            shield.Front += settings.ShieldRechargeRate * Time.deltaTime * RECHARGE_RATE_FACTOR;
        if (shield.Back < settings.Shield.Back)
            shield.Back += settings.ShieldRechargeRate * Time.deltaTime * RECHARGE_RATE_FACTOR;
        //Should do component damage here when the corestrength is low. Ignore for now
        if (_CoreStrength < CoreMax * .666f)
        {
            if (!Trail)
            {
                foreach (EngineFlare flare in engineFlares)
                {
                    Trail = Instantiate(DamageTrails, flare.gameObject.transform.position + flare.gameObject.transform.forward * 4, Quaternion.identity, flare.gameObject.transform);
                }
            }
        }
        if (_CoreStrength > 0)
        {
            DeathDir = transform.forward;
            DeathVel = speed;
        }

        if (!DecoRoot)
            DecoRoot = GameObject.Find("Deco_Objs").transform;

        if (_CoreStrength <= 0) //We dead, son
        {   isDead = true;
            //make sure we're uncloaked
            Cloak = false;
            cloakedAmount = 0f;
            //Let's set up how we're going to die!
            if (DeathSpin == Vector3.zero) //this happens once, let's take advantage!
            {
                DeathDir = transform.forward;
                DeathVel = speed;
                DeathSpin = new Vector3(Random.Range(-3f, 3f), Random.Range(-3f, 3f), Random.Range(-3f, 3f));
                DeathType = Random.Range(0, 2); //we've got three current deaths - immediate, short spin, and death tumble!
                DeathLength = Random.Range(2f, 4f);
            }
            if (DeathType == 0) //Die Immediately.
            {
                if (!Boom)
                {
                    Boom = Instantiate(DeathVFX[Random.Range(0, DeathVFX.Length - 1)], transform.position, Quaternion.identity, DecoRoot);
                    Boom.GetComponent<DampInitVelocity>().initDir = DeathDir;
                    Boom.GetComponent<DampInitVelocity>().initVel = DeathVel;

                }
                DoPlayerUIDisconnect();
                Destroy(gameObject, .25f);
                transform.position += DeathDir * DeathVel * Time.deltaTime;
            }

            if (DeathType == 1)//Short burst of explosions, then Die
            {
                float dyaw_ = Mathf.Clamp(DeathSpin.y, -1f, 1f);
                float dpitch_ = Mathf.Clamp(DeathSpin.x, -1f, 1f);
                float droll_ = Mathf.Clamp(DeathSpin.z, -1f, 1f);
                dyaw_ *= settings.TurnRate * 2f * Time.deltaTime;
                dpitch_ *= settings.TurnRate * 2f * Time.deltaTime;
                droll_ *= settings.TurnRate * 3f * Time.deltaTime;
                transform.localRotation *= Quaternion.AngleAxis(droll_, Vector3.forward) * Quaternion.AngleAxis(dyaw_, Vector3.up) * Quaternion.AngleAxis(dpitch_, invertYAxis ? Vector3.right : Vector3.left);
                transform.position += DeathDir * DeathVel * Time.deltaTime;
                speed = 0f;
                DeathLength -= Time.deltaTime * 5.5f;
                if (DeathLength > .5f)
                {
                    GameObject DeathTrail = Instantiate(DeathTrailVFX, transform.position, Quaternion.identity, DecoRoot);
                    DeathTrail.GetComponent<DampInitVelocity>().initDir = DeathDir;
                    DeathTrail.GetComponent<DampInitVelocity>().initVel = DeathVel / 8;
                }
                if (DeathLength <= 0 && !Boom)
                {
                    Boom = Instantiate(DeathVFX[Random.Range(0, DeathVFX.Length - 1)], transform.position, Quaternion.identity, DecoRoot);
                    Boom.GetComponent<DampInitVelocity>().initDir = DeathDir;
                    Boom.GetComponent<DampInitVelocity>().initVel = DeathVel;
                    DoPlayerUIDisconnect();
                    Destroy(gameObject, .25f);
                }
            }

            if (DeathType == 2)
            {
                float dyaw_ = Mathf.Clamp(DeathSpin.y, -1f, 1f);
                float dpitch_ = Mathf.Clamp(DeathSpin.x, -1f, 1f);
                float droll_ = Mathf.Clamp(DeathSpin.z, -1f, 1f);
                dyaw_ *= settings.TurnRate * 2f * Time.deltaTime;
                dpitch_ *= settings.TurnRate * 2f * Time.deltaTime;
                droll_ *= settings.TurnRate * 3f * Time.deltaTime;
                transform.localRotation *= Quaternion.AngleAxis(droll_, Vector3.forward) * Quaternion.AngleAxis(dyaw_, Vector3.up) * Quaternion.AngleAxis(dpitch_, invertYAxis ? Vector3.right : Vector3.left);
                transform.position += DeathDir * DeathVel * Time.deltaTime;
                DeathLength -= Time.deltaTime;
                if (DeathLength > .25f)
                {
                    GameObject DeathTrail = Instantiate(DeathTrailVFX, transform.position, Quaternion.identity, DecoRoot);
                    DeathTrail.GetComponent<DampInitVelocity>().initDir = DeathDir;
                    DeathTrail.GetComponent<DampInitVelocity>().initVel = DeathVel / 8;
                }
                if (DeathLength <= 0 && !Boom)
                {
                    Boom = Instantiate(DeathVFX[Random.Range(0, DeathVFX.Length - 1)], transform.position, Quaternion.identity, DecoRoot);
                    Boom.GetComponent<DampInitVelocity>().initDir = DeathDir;
                    Boom.GetComponent<DampInitVelocity>().initVel = DeathVel;
                    DoPlayerUIDisconnect();
                    Destroy(gameObject, .25f);
                }
            }
        }
    }
    //Handle Power Management
    void Power()
    {
        if (capacitorLevel < settings.CapacitorSize) //Charge Them Guns
        {
            //Only charge if we're not cloaked! 
            if (!isCloaked)
            {
                capacitorLevel += settings.RechargeRate * Time.deltaTime;
            }
        }
    }
    //Helpful Utilities
    public static float GetSignedAngle(Quaternion A, Quaternion B, Vector3 axis)
    {
        float angle = 0f;
        Vector3 angleAxis = Vector3.zero;
        (A * Quaternion.Inverse(B)).ToAngleAxis(out angle, out angleAxis);
        if (Vector3.Angle(axis, angleAxis) > 90f)
        {
            angle = -angle;
        }
        return Mathf.DeltaAngle(0f, angle);
    }
    void DeltaRot()
    {
        Quaternion qLocal = Quaternion.Inverse(transform.rotation) * lastTrans.rotation;

        float newPitchDelta = (qLocal * Vector3.forward).y / Time.deltaTime;
        float newYawDelta = (qLocal * Vector3.right).z / Time.deltaTime;
        float newRollDelta = (qLocal * Vector3.up).x / Time.deltaTime;

        Vector3 rotDeltaRough = new Vector3(-newPitchDelta, -newYawDelta, newRollDelta);

        rotDelta = Vector3.Lerp(rotDelta, rotDeltaRough, settings.DeltaSmooth);

        lastTrans.position = transform.position;
        lastTrans.rotation = transform.rotation;
    }
    void Steer() //Autopilot!
    {
        var yaw_ = Mathf.Clamp(yaw, -1f, 1f);
        var pitch_ = Mathf.Clamp(pitch, -1f, 1f);
        var roll_ = Mathf.Clamp(roll, -1f, 1f);
        yaw_ *= settings.TurnRate * Time.deltaTime;
        pitch_ *= settings.TurnRate * Time.deltaTime;
        roll_ *= settings.TurnRate * Time.deltaTime;
        transform.localRotation *= Quaternion.AngleAxis(roll_, Vector3.forward) * Quaternion.AngleAxis(yaw_, Vector3.up) * Quaternion.AngleAxis(pitch_, invertYAxis ? Vector3.right : Vector3.left);

        DeltaRot();
    }
    //Handle our Speed and Acceleration
    void DoThrottle()
    {
        var targetSpeed_ = Mathf.Clamp(targetSpeed, 0f, settings.BurnSpeed);

        if (speed < targetSpeed_)
        // accelerating
        {
            speed = Mathf.Lerp(speed, targetSpeed_, settings.Acceleration * Time.deltaTime);
        }
        else if (speed > targetSpeed_)
        // decelerating
        {
            speed = Mathf.Lerp(speed, targetSpeed_, settings.Deceleration * Time.deltaTime);
        }

        LagDir = Quaternion.Slerp(LagDir, transform.rotation, .15f * (settings.Lag + (settings.BurnSpeed / speed) * settings.Lag));

        transform.position += LagDir * Vector3.forward * speed * Time.deltaTime;

        //set Afterburning flag
        if (targetSpeed > settings.TopSpeed + .1f)
        { isAfterburning = true; }
        else
        { isAfterburning = false; }
        // also set the visible flare throttles
        foreach (EngineFlare flare in engineFlares)
        {
            flare.FlareThrottle = (speed / (settings.TopSpeed))*flareIntensity;
        }
        throttle = speed / settings.TopSpeed;
    }

    //Handle our cloaking device, if we have one!
    public void DoCloak()
    {
        if (!settings.HasCloak) return;

        //attempt to handle an edge case of not *completely* cloaked and getting stuck.
        if (isCloaking && cloakedAmount >= .99f)
        {
            cloakedAmount = 1f;
            isCloaked = true;
            isCloaking = false;
        }
        //Force disable the cloak if we don't have enough power to engage it - but only if we're not already cloaked! 
        if (!isCloaked && Cloak && cloakCapacitorLevel <= settings.CloakPower * .1f)
        {
            Cloak = false;
        }
        //Start Cloaking
        if (Cloak && !isCloaked)
        {
            if (cloakedAmount <= 1.1f)
                cloakedAmount += Time.deltaTime / settings.TimeToCloak;
            if (cloakedAmount >= 1.05f)
            {
                cloakedAmount = 1f;
                isCloaked = true;
                GameObjTracker.radarRefreshNeeded = true;
                GameObjTracker.bracketRefreshNeeded = true;
            }
            if (cloakedAmount <= 0.05)
            {
                CloakSFX.PlayOneShot(CloakOnSound);
            }
        }
        //Uncloak
        if (!Cloak && isCloaked)
        {
            cloakedAmount -= Time.deltaTime / settings.TimeToCloak;

            if (cloakedAmount <= 0f)
            {
                cloakedAmount = 0f;
                isCloaked = false;
                GameObjTracker.radarRefreshNeeded = true;
                GameObjTracker.bracketRefreshNeeded = true;
            }
            if (cloakedAmount >= .95)
            {
                CloakSFX.PlayOneShot(CloakOffSound);
            }
        }
        //Handle midway state and broadcast it
        if (cloakedAmount > .01f && cloakedAmount < .99f)
        {
            isCloaking = true;
        }
        else
        {
            isCloaking = false;
        }


        //if the guns are charged, recharge the Cloak, if it's not in use 
        if (capacitorLevel >= settings.CapacitorSize && cloakCapacitorLevel < settings.CloakPower && !isCloaked && cloakedAmount < .1f)
        {
            cloakCapacitorLevel += settings.RechargeRate * Time.deltaTime;
        }
        //if the cloak is on, drain the cloak capacitors, then the gun capacitors
        if (isCloaked)
        {
            if (cloakCapacitorLevel > 0)
            {
                cloakCapacitorLevel -= settings.CloakDrain * Time.deltaTime;
            }
            if (cloakCapacitorLevel <= 0 && capacitorLevel > 0)
            {
                capacitorLevel -= settings.CloakDrain * Time.deltaTime;
            }
            //if we've run out of power, force an uncloak! 
            if (capacitorLevel <= 0 && cloakCapacitorLevel <= 0)
            {
                Cloak = false;
            }
        }

        //control the visual effect of the cloak
        if (Cloak && !isCloaking && !isCloaked)
        {
            GetBillboardMat();
        }
        //handle the Billboard material animation
        billboardMat.SetFloat("_CloakAmount", cloakedAmount);
        //handle dimming the engines! 
        flareIntensity = (1 - cloakedAmount * 1.1f);
    }

    // late update to give human or AI player scripts a chance to set values first
    void LateUpdate()
    {
        if (!isDead)
        {
            Steer();
            DoThrottle();
            Power();
        }
        DoHealth();
        DoFuel();
        if (settings.Class == CLASS.FIGHTER)
        {
            AvoidObstacles(1f, shipRadius * 4f);
        }
        else
        {
            AvoidObstacles(.25f, shipRadius * 4f);
        }
        if (settings.HasCloak) DoCloak();
        TargetManage();
        DoVelocity();
        //Collision Detecting, but make sure the full collision is only being used if the ship is afterburning, simple manuvers won't do it as much.
        if (isAfterburning)
        {
            DoBounce(.5f, shipRadius / 64f);
        }
        else
        {
            DoBounce(.75f, shipRadius / 64f);
        }
        DoSFX();
    }

#if UNITY_EDITOR
    /*
    [UnityEditor.CustomEditor(typeof(ShipSettings))]
    public class ShipSettingsEditor : UnityEditor.Editor {
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();

            if (GUILayout.Button("Create settings asset")) {
                var instance = (ShipSettings)target;

                var asset = ScriptableObject.CreateInstance<ShipSettingsAsset>();
                asset.aiTeam = instance.AITeam;
                asset.@class = instance.Class;
                asset.weight = instance.Weight;
                asset.displayName = instance.DisplayName;

                asset.settings.TurnRate = instance.settings.TurnRate;
                asset.maxFuel = instance.maxFuel;
                asset.fuelBurnRate = instance.fuelBurnRate;
                asset.topSpeed = instance.topSpeed;
                asset.burnSpeed = instance.burnSpeed;
                asset.acceleration = instance.acceleration;
                asset.deceleration = instance.deceleration;
                asset.lag = instance.lag;

                asset.deltaSmooth = instance.deltaSmooth;

                asset.capacitorSize = instance.capacitorSize;
                asset.weaponRechargeRate = instance.rechargeRate;

                asset.armor = new ArmorSettings(instance.armor.Front, instance.armor.Back, instance.armor.Left, instance.armor.Right);
                asset.shield = new ShieldSettings(instance.shield.Front, instance.shield.Back);
                asset.shieldRechargeRate = instance.shieldRechargeRate;
                asset.hasCloak = instance.hasCloak;
                asset.timeToCloak = instance.timeToCloak;
                asset.cloakPower = instance.cloakPower;
                asset.cloakDrain = instance.cloakDrain;

                var name = instance.DisplayName + ".asset";
                string path = System.IO.Path.Join("Assets", "WingCommander/Settings/Ships", name);
                AssetDatabase.CreateAsset(asset, path);
			}
        }
    }
    */
#endif
}