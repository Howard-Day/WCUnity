using OneManEscapePlan.Common;
using UnityEngine;
using UnityEngine.Assertions;

public class ShipSystem : MonoBehaviour
{
    [SerializeField] public ShipSettings ship;

    public ShipSettings Ship => ship;

    protected ShipSettingsAsset Settings => ship.Settings;

    virtual protected void Awake()
    {
        if (ship == null) ship = GetComponentInParent<ShipSettings>();
        Assert.IsNotNull(ship);
    }
}
