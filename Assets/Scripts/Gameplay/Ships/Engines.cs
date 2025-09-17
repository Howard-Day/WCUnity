using OneManEscapePlan.Common;
using UnityEngine;
using UnityEngine.Assertions;

public class Engines : ShipSystem
{
    #region FIELDS
    [SerializeField] public GameObject damageTrails;

    [Header("Audio")]
    [SerializeField, NonNull] private AudioSource engineSFX;
    [SerializeField, NonNull] private AudioSource afterburnSFX;
    [SerializeField] private Vector2 minMaxThrottleVolume = Vector2.one;
    [SerializeField] private Vector2 minMaxThrottlePitch = Vector2.one;
    [SerializeField] private float afterburnPitch = 1f;
    [SerializeField] private float afterburnVolume = .25f;
    [SerializeField] private float afterburnSmoothness = .25f;

    private EngineFlare[] engineFlares;
    private GameObject Trail;

    private float fuel;
    private bool bingoFuel = false;
    private float throttle;
    private float flareIntensity = 1f;
    private float speed;
    private float targetSpeed;
    private bool isAfterburning;
    private Quaternion lagDir;
    private float afterburnBlend = 0f;
    #endregion

    #region PROPERTIES
    public float Fuel => fuel;
    public float MaxFuel => Settings.MaxFuel;
    public float Speed => speed;
    public float TargetSpeed { get => targetSpeed; set => targetSpeed = value; }
    public float Throttle { get => throttle; set => throttle = value; }
    public bool IsAfterburning { get => isAfterburning; set => isAfterburning = value; }
    public Vector2 MinMaxThrottleVolume { get => minMaxThrottleVolume; set => minMaxThrottlePitch = value; }
    public Vector2 MinMaxThrottlePitch { get => minMaxThrottlePitch; set => minMaxThrottlePitch = value; }
    public float AfterburnPitch { get => afterburnPitch; set => afterburnPitch = value; }
    public float AfterburnVolume { get => afterburnVolume; set => afterburnVolume = value; }
    public float AfterburnSmoothness { get => afterburnSmoothness; set => afterburnSmoothness = value; }
    #endregion

    override protected void Awake()
    {
        base.Awake();
        Assert.IsNotNull(engineSFX);
        Assert.IsNotNull(afterburnSFX);

        fuel = Settings.MaxFuel;
    }

    private void Start()
    {
        //grab the sub-object engine flares to control them
        engineFlares = GetComponentsInChildren<EngineFlare>();

        engineSFX.volume = minMaxThrottleVolume.x;
    }

    void LateUpdate()
    {
        if (!ship.IsDead)
        {
            DoThrottle();
        }
        DoHealth();
        DoFuel();
        //Collision Detecting, but make sure the full collision is only being used if the ship is afterburning, simple manuvers won't do it as much.
        if (isAfterburning)
        {
            ship.DoBounce(.5f, ship.shipRadius / 64f);
        }
        else
        {
            ship.DoBounce(.75f, ship.shipRadius / 64f);
        }
        DoSFX();

        if (ship.isCloaked)
        {
            flareIntensity = (1 - ship.cloakedAmount * 1.1f);
        }
    }

    void DoThrottle()
    {
        var targetSpeed_ = Mathf.Clamp(targetSpeed, 0f, Settings.BurnSpeed);

        if (speed < targetSpeed_)
        // accelerating
        {
            speed = Mathf.Lerp(speed, targetSpeed_, Settings.Acceleration * Time.deltaTime);
        }
        else if (speed > targetSpeed_)
        // decelerating
        {
            speed = Mathf.Lerp(speed, targetSpeed_, Settings.Deceleration * Time.deltaTime);
        }

        lagDir = Quaternion.Slerp(lagDir, transform.rotation, .15f * (Settings.Lag + (Settings.BurnSpeed / speed) * Settings.Lag));

        transform.position += lagDir * Vector3.forward * speed * Time.deltaTime;

        //set Afterburning flag
        if (targetSpeed > Settings.TopSpeed + .1f)
        { isAfterburning = true; }
        else
        { isAfterburning = false; }
        // also set the visible flare throttles
        foreach (EngineFlare flare in engineFlares)
        {
            flare.FlareThrottle = (speed / (Settings.TopSpeed)) * flareIntensity;
        }
        throttle = speed / Settings.TopSpeed;
    }

    void DoHealth()
    {
        if (ship._CoreStrength < ship.CoreMax * .666f)
        {
            if (!Trail)
            {
                foreach (EngineFlare flare in engineFlares)
                {
                    Trail = Instantiate(damageTrails, flare.gameObject.transform.position + flare.gameObject.transform.forward * 4, Quaternion.identity, flare.gameObject.transform);
                }
            }
        }
    }

    //Handle our Fuel Levels
    void DoFuel()
    {
        var normalizedThrottle = Mathf.Clamp01(speed / Settings.TopSpeed);
        if (fuel > 0) //WE've got fuel, let's go! 
        {
            if (!isAfterburning)
            {//Do normal fuel drain based on throttle
                fuel -= normalizedThrottle * Time.deltaTime * 4f;
            }
            else// Now we're burning fuel to GO VERY FAST
            {
                fuel -= Settings.FuelBurnRate * Time.deltaTime * 5f;
            }
        }
        else //Fuck, basically just a max coasting speed. Good fucking luck, cowboy
        {
            bingoFuel = true;
            speed = Mathf.Min(targetSpeed, Settings.TopSpeed * .666f);
            isAfterburning = false;
        }
    }

    void DoSFX()
    {
        engineSFX.volume = Mathf.Lerp(minMaxThrottleVolume.x, minMaxThrottleVolume.y, throttle);
        engineSFX.pitch = Mathf.Lerp(minMaxThrottlePitch.x, minMaxThrottlePitch.y, throttle);
        if (isAfterburning)
        {
            afterburnBlend = 1f;
        }
        else
        {
            afterburnBlend = 0f;
        }
        afterburnSFX.volume = Mathf.Lerp(afterburnSFX.volume, afterburnBlend * afterburnVolume, afterburnSmoothness);
    }
}
