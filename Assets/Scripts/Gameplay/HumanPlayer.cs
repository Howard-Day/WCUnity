using UnityEngine;

[RequireComponent(typeof(ShipSettings))]
public class HumanPlayer : MonoBehaviour
{
    [SerializeField] float speedSelectionSpeed = 10f;


    ShipSettings ship;
    ProjectileWeapon[] laserCannons;


    void Start()
    {
        ship = GetComponent<ShipSettings>();
        laserCannons = GetComponentsInChildren<ProjectileWeapon>();
    }

    void Update()
    {
        Steer();
        Throttle();
        FireGuns();
    }

    void Steer()
    {
        ship.yaw = Mathf.Clamp((Input.mousePosition.x / Screen.width) * 2f - 1f, -1f, 1f);
        ship.pitch = Mathf.Clamp((Input.mousePosition.y / Screen.height) * 2f - 1f, -1f, 1f); ;
    }

    void Throttle()
    {
        var fullStop = Input.GetKey(KeyCode.Backspace);
        var fullSpeed = Input.GetKey(KeyCode.Backslash);
        var afterBurn = Input.GetKey(KeyCode.Tab);
        var afterBurnOff = Input.GetKeyUp(KeyCode.Tab);
        var accelerate = Input.GetKey(KeyCode.Equals) || Input.GetKey(KeyCode.KeypadPlus); // KeyCode.Equals is the plus key without modifier
        var decelerate = Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus);

        var engines = ship.Engines;

        if (afterBurnOff)
        {
            engines.TargetSpeed = ship.Settings.TopSpeed / 2;
        }

        if (afterBurn)
        {
            engines.TargetSpeed = ship.Settings.BurnSpeed;
        }
        else
        {

            if (fullSpeed)
            {
                engines.TargetSpeed = ship.Settings.TopSpeed;
            }
            if (fullStop)
            {
                engines.TargetSpeed = 0f;
            }
            else
            {
                if (accelerate && !decelerate && engines.TargetSpeed < ship.Settings.TopSpeed)
                {
                    engines.TargetSpeed += speedSelectionSpeed * Time.deltaTime;
                }
                else if (decelerate && !accelerate && engines.TargetSpeed > 0f)
                {
                    engines.TargetSpeed -= speedSelectionSpeed * Time.deltaTime;
                }
            }
        }

    }

    void FireGuns()
    {
        var fire = Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0);
        foreach (ProjectileWeapon laserCannon in laserCannons)
        {
            laserCannon.fire = fire;
            if (fire)
            {
                ship.isFiring = true;
            }
            else
            {
                ship.isFiring = false;
            }
        }
    }
}
