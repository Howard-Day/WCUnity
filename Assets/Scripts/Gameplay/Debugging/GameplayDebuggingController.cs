using UnityEngine;
using UnityEngine.InputSystem;

public class GameplayDebuggingController : MonoBehaviour
{
    public InputAction possessCurrentShipAction = new InputAction(binding: "<Keyboard>/enter");
    public InputAction freezeAllAction = new InputAction(binding: "<Keyboard>/end");
    public InputAction killAllAction = new InputAction(binding: "<Keyboard>/k");

    public PlayerController playerControllerPrefab;

    private PlayerController playerController;

    private void Start()
    {
        possessCurrentShipAction.performed += PossessCurrentShipAction_performed;
        freezeAllAction.performed += FreezeAllAction_performed;
        killAllAction.performed += KillAllAction_performed;
    }

    void OnEnable()
    {
        possessCurrentShipAction.Enable();
        freezeAllAction.Enable();
        killAllAction.Enable();
    }

    private void OnDisable()
    {
        possessCurrentShipAction.Disable();
        freezeAllAction.Disable();
        killAllAction.Disable();
    }

    private void PossessCurrentShipAction_performed(InputAction.CallbackContext obj)
    {
        if (obj.phase == InputActionPhase.Performed)
        {
            if (playerController == null)
            {
                var camera = Camera.main;
                var ship = camera.GetComponentInParent<ShipSettings>();
                if (ship != null)
                {
                    playerController = Instantiate(playerControllerPrefab, ship.transform);
                }
            }
        }
    }

    private void FreezeAllAction_performed(InputAction.CallbackContext obj)
    {
        if (obj.phase == InputActionPhase.Performed)
        {
            OMEPLogger.Log(this, null);
            var ais = GameObject.FindObjectsByType<AIPlayer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (AIPlayer ai in ais)
            {
                ai.GetComponentInChildren<Engines>().enabled = false;
                var ship = ai.GetComponentInChildren<ShipSettings>();
                if (ship != null)
                {
                    ship.pitch = 0;
                    ship.yaw = 0;
                    ship.roll = 0;
                }
                ai.enabled = false;
            }

            var turretAIs = GameObject.FindObjectsByType<AITurret>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var ai in turretAIs)
            {
                ai.enabled = false;
            }
        }
        freezeAllAction.Enable();
    }

    private void KillAllAction_performed(InputAction.CallbackContext obj)
    {
        foreach (ShipSettings ship in GameObjTracker.Instance.AllShips)
        {
            ship._CoreStrength = 0f;
        }
        freezeAllAction.Disable();
    }
}