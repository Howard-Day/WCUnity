using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
public class WeaponsSystem : ShipSystem
{
    #region FIELDS
    [SerializeField] public List<ProjectileWeapon> projWeapons;

    private Capacitor capacitor;

    private int lastFireIndex;
    private bool isFiring;
    #endregion

    #region PROPERTIES
    public Capacitor Capacitor
    {
        get => capacitor;
        set
        {
            Assert.IsNotNull(value);
            capacitor = value;
            for (int i = 0; i < projWeapons.Count; i++)
            {
                ProjectileWeapon weapon = projWeapons[i];
                weapon.index = i;
                weapon.Capacitor = value;
            }
        }
    }
    public bool IsFiring => isFiring;
    #endregion

    public void FireGuns()
    {
        //OMEPLogger.Log(this, null);

        // TODO: this doesn't belong here.
        if (ship.cloakedAmount > .1f)
        {
            StopFiring();
            return;
        }

        foreach (ProjectileWeapon projWeapon in projWeapons)
        {
            if (capacitor.CurrentCharge < projWeapon.powerDrain * (projWeapons.Count + 1))
            {
                if (ship.recover >= .99f && projWeapon.index != lastFireIndex) // Can the ship fire? Is this gun *not* the last to fire? Are we Cloaked? 
                {
                    projWeapon.fire = true;
                    //increment through guns

                    // if(logDebug){print("aactually setting state to " + fire);}
                    //are we firing?
                    isFiring = true; //Make sure our broadcast flag is set! 
                }
                if (ship.recover >= .99f && projWeapon.index == lastFireIndex) // Can the ship fire? Is this gun the last to fire? 
                {
                    projWeapon.fire = false;
                }
                if (ship.recover < .75f) //wait for recharge or return of control! 
                {
                    projWeapon.fire = false;
                    isFiring = false;
                }
            }
            else if (ship.recover >= .99f)
            {
                projWeapon.fire = true;
                isFiring = true;
            }

            if (projWeapon.hasFired)
            {
                lastFireIndex = projWeapon.index;
            }
        }
    }

    public void StopFiring()
    {
        foreach (var projWeapon in projWeapons)
        {
            projWeapon.fire = false;
        }
        isFiring = false;
    }

    /// <summary>
    /// Call this function to update the weapons system if new
    /// weapons are added at runtime.  Otherwise, weapons should
    /// be set from the Inspector.
    /// </summary>
    public void FindWeapons()
    {
        Assert.IsNotNull(ship);

        if (projWeapons == null)
        {
            projWeapons = new List<ProjectileWeapon>(GetComponentsInChildren<ProjectileWeapon>());
        }
        else
        {
            projWeapons.Clear();
            projWeapons.AddRange(GetComponentsInChildren<ProjectileWeapon>());
        }

        // If we are not a turret, remove from the list any weapons that belong to turrets.
        // This is a bit hacky.
        if (ship.transform == this.transform) // We are not a turret
        {
            for (int i = 0; i < projWeapons.Count; i++)
            {
                ProjectileWeapon weapon = projWeapons[i];
                if (weapon.turretMounted)
                {
                    projWeapons.RemoveAt(i);
                    i--;
                }
            }
        }
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(WeaponsSystem))]
    private class WeaponsSystemEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var instance = target as WeaponsSystem;

            if (!Application.isPlaying)
            {
                if (GUILayout.Button("Auto-find weapons"))
                {
                    UnityEditor.Undo.RecordObject(instance, "Auto-find weapons");
                    instance.ship = instance.GetComponent<ShipSettings>();
                    if (instance.ship == null) instance.ship = instance.GetComponentInParent<ShipSettings>();
                    instance.FindWeapons();
                    UnityEditor.EditorUtility.SetDirty(instance);
                }
            }
        }
    }
#endif
}
