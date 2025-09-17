using NUnit.Framework;
using OneManEscapePlan.Common;
using UnityEngine;

[RequireComponent(typeof(ShipSettings))]
public class ShipSystem : MonoBehaviour
{
    [SerializeField, NonNull] public ShipSettings ship;

    public ShipSettings Ship => ship;

    protected ShipSettingsAsset Settings => ship.Settings;

    virtual protected void Awake()
    {
        Assert.IsNotNull(ship);
    }
}
