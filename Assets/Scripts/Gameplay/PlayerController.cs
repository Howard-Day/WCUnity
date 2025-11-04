using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Min(.01f)]
    [SerializeField] float mouseSteerDecayTime = 1f;
    [SerializeField] float mouseSensitivity = 100f;

    ShipSettings ship;
    CockpitViewSwitcher viewSwitcher;
    WeaponsSystem weaponsSystem;

    bool lastSteerInputWasMouse = false;
    float pitchInput;
    float yawInput;
    float rollInput;
    float throttleInput;
    bool afterburn;

    float mousePitchDecayVelocity;
    float mouseYawDecayVelocity;

    void Awake()
    {
        ship = GetComponentInParent<ShipSettings>();
        weaponsSystem = ship.GetComponent<WeaponsSystem>();
        Assert.IsNotNull(weaponsSystem);

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
            viewSwitcher.ChaseSwitch = false;
            viewSwitcher.RandomSwitch = false;
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
    public void OnPitch(InputAction.CallbackContext context)
    {
        OnAxisInput(context, ref pitchInput);
    }

    /// <summary>
    /// Steer with a joystick or analog stick
    /// </summary>
    /// <param name="context"></param>
    public void OnYaw(InputAction.CallbackContext context)
    {
        OnAxisInput(context, ref yawInput);
    }

    /// <summary>
    /// Steer with a joystick or analog stick
    /// </summary>
    /// <param name="context"></param>
    public void OnRoll(InputAction.CallbackContext context)
    {
        OnAxisInput(context, ref rollInput);
    }

    private void OnAxisInput(in InputAction.CallbackContext context, ref float valueRef)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            valueRef = context.ReadValue<float>();
            lastSteerInputWasMouse = false;
        }
        else if (context.phase == InputActionPhase.Canceled)
        {
            valueRef = 0;
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
            throttleInput = context.ReadValue<float>();
        } else if (context.phase == InputActionPhase.Canceled)
        {
            throttleInput = 0;
        }
    }

    public void OnPressAfterburn(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            afterburn = true;
        }
        else if (context.phase == InputActionPhase.Canceled)
        {
            afterburn = false;
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

    public void OnPressTargetNearestHostile(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            ship.CurrentTarget = ship.TargetingSystem.FindNearestTarget(ship.Team.GetHostileTeamsMask());
        }
    }

    public void OnPressTargetForward(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            ship.CurrentTarget = ship.TargetingSystem.FindTargetForward();
        }
    }

    public void OnPressFire(InputAction.CallbackContext context)
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
        if (value)
        {
            weaponsSystem.FireGuns();
        } else
        {
            weaponsSystem.StopFiring();
        }
    }

    void Steer()
    {
        // If we are steering with a mouse, our rate of turn decays over time.
        if (lastSteerInputWasMouse)
        {
            ship.pitch = Mathf.SmoothDamp(ship.pitch, 0, ref mousePitchDecayVelocity, mouseSteerDecayTime);
            ship.yaw = Mathf.SmoothDamp(ship.yaw, 0, ref mouseYawDecayVelocity, mouseSteerDecayTime);
        } else
        {
            Vector2 pitchRoll = new Vector2(rollInput, pitchInput);
            // WARNING: This is only valid if pitch and roll are on the same stick.
            pitchRoll = ConvertCircleToSquare(pitchRoll);

            ship.pitch = -pitchRoll.y;
            ship.yaw = yawInput;
            ship.roll = -pitchRoll.x;

            if (afterburn)
            {
                ship.Engines.TargetSpeed = ship.Settings.BurnSpeed;
            }
            else
            {
                float throttle = ship.Engines.Throttle;
                throttle += throttleInput * 2f / Time.deltaTime;
                ship.Engines.SetNormalizedTargetSpeed(Mathf.Clamp01(throttle));
            }
        }
    }

    /// <summary>
    /// Convert analog input from a circular range of motion into a square range
    /// of motion.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    /// <remarks>
    /// from https://discussions.unity.com/t/making-a-square-vector2-fit-a-circle-vector2/635349/4
    /// Analog sticks often have a circular range of motion, which means we don't
    /// get the full range of values in the corners (e.g. if we push the stick into
    /// the top-right, we'll get a value of (0.7,0.7) instead of (1,1). This
    /// function converts that circular range into a square range, so the corner
    /// actually reads as (1,1).
    /// </remarks>
    Vector2 ConvertCircleToSquare(Vector2 input)
    {
        const float COS_45 = 0.70710678f;

        if (input.sqrMagnitude == 0) // Or < EPSILON, Or < inner circle threshold. Your choice.
        {
            return Vector2.zero;
        }

        Vector2 normal = input.normalized;
        float x, y;

        if (normal.x != 0 && normal.y >= -COS_45 && normal.y <= COS_45)
        {
            x = normal.x >= 0 ? input.x / normal.x : -input.x / normal.x;
        }
        else
        {
            x = input.x / Mathf.Abs(normal.y);
        }

        if (normal.y != 0 && normal.x >= -COS_45 && normal.x <= COS_45)
        {
            y = normal.y >= 0 ? input.y / normal.y : -input.y / normal.y;
        }
        else
        {
            y = input.y / Mathf.Abs(normal.x);
        }

        x = Mathf.Clamp(x, -1f, 1f);
        y = Mathf.Clamp(y, -1f, 1f);

        return new Vector2(x, y);
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(PlayerController))]
    private class PlayerControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var instance = (PlayerController)target;

            if (Application.isPlaying)
            {
                GUILayout.Space(10);
                UnityEditor.EditorGUILayout.LabelField("DEBUG", UnityEditor.EditorStyles.boldLabel);
                LabelField("Pitch", instance.pitchInput.ToString("F2"));
                LabelField("Yaw", instance.yawInput.ToString("F2"));
                LabelField("Roll", instance.rollInput.ToString("F2"));
                LabelField("Throttle", instance.throttleInput.ToString("F2"));

                Vector2 pitchRoll = new Vector2(instance.rollInput, instance.pitchInput);
                Vector2 squarePitchRoll = instance.ConvertCircleToSquare(pitchRoll);
                LabelField("Pitch/Roll (Square)", squarePitchRoll.ToString("F2"));
            }
        }

        private void LabelField(string label, string value)
        {
            UnityEditor.EditorGUILayout.LabelField(label, value);
        }

        public override bool RequiresConstantRepaint()
        {
            return Application.isPlaying || base.RequiresConstantRepaint();
        }
    }
#endif
}