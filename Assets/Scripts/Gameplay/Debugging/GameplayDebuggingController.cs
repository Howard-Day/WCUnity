using UnityEngine;
using UnityEngine.InputSystem;

public class GameplayDebuggingController : MonoBehaviour
{
    public InputAction possessCurrentShipAction = new InputAction(binding: "<Keyboard>/enter");

    public PlayerController playerControllerPrefab;

    private PlayerController playerController;

    private void Start()
    {
        possessCurrentShipAction.performed += PossessCurrentShipAction_performed;
    }

    private void PossessCurrentShipAction_performed(InputAction.CallbackContext obj)
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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnEnable()
    {
        possessCurrentShipAction.Enable();
    }

    private void OnDisable()
    {
        possessCurrentShipAction.Disable();
    }
}
