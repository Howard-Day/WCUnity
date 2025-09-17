using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Min(.01f)]
    [SerializeField] float mouseSteerDecayTime = 1f;
    [SerializeField] float mouseSensitivity = 100f;

    ShipSettings ship;
    CockpitViewSwitcher viewSwitcher;
    ProjectileWeapon[] laserCannons;

    bool lastSteerInputWasMouse = false;

    float mousePitchDecayVelocity;
    float mouseYawDecayVelocity;

    void Awake()
    {
        ship = GetComponentInParent<ShipSettings>();
        laserCannons = ship.GetComponentsInChildren<ProjectileWeapon>();

        // Remove AI.
        var aiPlayer = ship.GetComponent<AIPlayer>();
        if (aiPlayer != null)
        {
            Destroy(aiPlayer);
        }

        // Disable automatic view switching.
        viewSwitcher = ship.GetComponentInChildren<CockpitViewSwitcher>();
        if (viewSwitcher != null)
        {
            viewSwitcher.enabled = false;
        }

        // Reset ship inputs
        ship.pitch = 0;
        ship.yaw = 0;
        ship.roll = 0;
        SetFiring(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        Steer();
    }

    /// <summary>
    /// Steer with a joystick or analog stick
    /// </summary>
    /// <param name="context"></param>
    public void OnSteer(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            var input = context.ReadValue<Vector2>();
            ship.yaw = input.x;
            ship.pitch = input.y;
            lastSteerInputWasMouse = false;
        }
    }

    public void OnMouseSteer(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            var delta = context.ReadValue<Vector2>();
            float sensitivity = mouseSensitivity * Time.deltaTime / 60f;
            float pitch = Mathf.Clamp(delta.y * sensitivity, -1f, 1f);
            float yaw = Mathf.Clamp(delta.x * sensitivity, -1f, 1f);
            ship.pitch = pitch;
            ship.yaw = yaw;
            lastSteerInputWasMouse = true;
        }
    }

    public void OnThrottle(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            var input = context.ReadValue<float>();
            input = input / 2f + 1f; //Change range from (-1,1) to (0,1)
            ship.Engines.SetNormalizedTargetSpeed(input);
        }
    }

    public void OnPressToggleView(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            if (viewSwitcher != null)
            {
                if (viewSwitcher.activeView == CockpitViewSwitcher.View.Chase) {
                    viewSwitcher.activeView = CockpitViewSwitcher.View.Main;
                } else
                {
                    viewSwitcher.activeView = CockpitViewSwitcher.View.Chase;
                }
            }
        }
    }

    public void OnPressFireButton(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            SetFiring(true);
        } else if (context.phase == InputActionPhase.Canceled)
        {
            SetFiring(false);
        }
    }

    private void SetFiring(bool value)
    {
        foreach (ProjectileWeapon laserCannon in laserCannons)
        {
            laserCannon.fire = value;
        }
        ship.isFiring = value;
    }

    void Steer()
    {
        // If we are steering with a mouse, our rate of turn decays over time.
        if (lastSteerInputWasMouse)
        {
            ship.pitch = Mathf.SmoothDamp(ship.pitch, 0, ref mousePitchDecayVelocity, mouseSteerDecayTime);
            ship.yaw = Mathf.SmoothDamp(ship.yaw, 0, ref mouseYawDecayVelocity, mouseSteerDecayTime);
        }
    }
}