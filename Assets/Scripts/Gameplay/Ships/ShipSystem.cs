using OneManEscapePlan.Common;
using UnityEngine;
using UnityEngine.Assertions;

public class ShipSystem : MonoBehaviour
{
    [SerializeField] protected ShipSettings ship;

    public ShipSettings Ship => ship;

    protected ShipSettingsAsset Settings => ship.Settings;

    virtual protected void Awake()
    {
        if (ship == null) ship = GetComponentInParent<ShipSettings>();
        Assert.IsNotNull(ship);
    }

    private void OnValidate()
    {
        AutoConfig();
    }

#if UNITY_EDITOR
    virtual protected void AutoConfig()
    {
        if (ship == null)
        {
            ship = GetComponent<ShipSettings>();
            if (ship == null) ship = GetComponentInParent<ShipSettings>();
            if (ship != null) UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif

    [UnityEditor.CustomEditor(typeof(ShipSystem), editorForChildClasses: true, isFallback = true)]
    protected class ShipSystemEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        }
    }
}