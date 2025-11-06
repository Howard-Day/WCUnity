using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// See also <seealso cref="TEAMExtensionMethods"/>
/// </summary>
[Flags]
public enum TEAM { KILRATHI = 1, NEUTRAL = 2, ENV = 4, CONFED = 8, PIRATE = 16 };
public enum CLASS { FIGHTER, FRIGATE, CAPITAL, STARBASE };
public enum WEIGHT { LIGHT, MEDIUM, HEAVY, BOMBER };

[CreateAssetMenu(menuName = "Wing Commander/Ship Settings")]
public class ShipSettingsAsset : ScriptableObject
{
    [Header("Choose Team, Name, and filters")]
    [SerializeField] private TEAM aiTeam = TEAM.CONFED; // TODO: Does this belong here?
    [SerializeField] private CLASS @class = CLASS.FIGHTER;
    [SerializeField] private WEIGHT weight = WEIGHT.MEDIUM;
    [SerializeField] private string displayName;

    [Header("Movement Settings")]
    [Min(1)]
    [SerializeField] private float turnRate = 50f;
    [Min(0)]
    [SerializeField] private float maxFuel = 2500f;
    [Min(0)]
    [SerializeField] private float fuelBurnRate = 2f;
    [Min(0)]
    [SerializeField] private float topSpeed = 20f;
    [Min(0)]
    [SerializeField] private float burnSpeed = 50f;
    [Min(0)]
    [SerializeField] private float acceleration = 1.5f;
    [Min(0)]
    [SerializeField] private float deceleration = 1f;
    [Tooltip("TODO: How is this setting used?")]
    [SerializeField] private float lag = 1f;

    [Header("Rotation Delta")]
    [SerializeField] private float deltaSmooth = .2f;

    [Header("Weapon Settings")]
    [Min(1)]
    [SerializeField] private float capacitorSize = 50f;
    [FormerlySerializedAs("weaponRechargeRate"), Min(.01f)]
    [SerializeField] private float rechargeRate = 1f;

    [Header("Health Settings")]
    [SerializeField] private ArmorStatus armor;
    [SerializeField] private ShieldStatus shield;
    [Min(0)]
    [SerializeField] private float shieldRechargeRate = 1;
    [Header("Special Abilities")]
    [SerializeField] private bool hasCloak = false;
    [Min(0)]
    [SerializeField] private float timeToCloak = 2f;
    [Min(0)]
    [SerializeField] private float cloakPower = 20f;
    [Min(0)]
    [SerializeField] private float cloakDrain = 1f;

    [Header("Other settings")]
    [Min(0)]
    [SerializeField] private float radarRange = 9999;
    [SerializeField] private bool canJoinFormations = true;

    public TEAM AITeam => aiTeam;
    public CLASS Class => @class;
    public WEIGHT Weight => weight;
    public string DisplayName => displayName;
    public float TurnRate => turnRate;
    public float MaxFuel => maxFuel;
    public float FuelBurnRate => fuelBurnRate;
    public float TopSpeed => topSpeed;
    public float BurnSpeed => burnSpeed;
    public float Acceleration => acceleration;
    public float Deceleration => deceleration;
    public float Lag => lag;
    public float DeltaSmooth => deltaSmooth;
    public float CapacitorSize => capacitorSize;
    public float RechargeRate => rechargeRate;
    public IReadOnlyArmorStatus Armor => armor;
    public IReadOnlyShieldStatus Shield => shield;
    public float ShieldRechargeRate => shieldRechargeRate;
    public bool HasCloak => hasCloak;
    public float TimeToCloak => timeToCloak;
    public float CloakPower => cloakPower;
    public float CloakDrain => cloakDrain;
    public float RadarRange => radarRange;
    public bool CanJoinFormations => canJoinFormations;
}

public static class TEAMExtensionMethods
{
    public static TEAM GetHostileTeamsMask(this TEAM team)
    {
        if (team == TEAM.KILRATHI)
        {
            return TEAM.CONFED | TEAM.PIRATE;
        }
        else if (team == TEAM.CONFED)
        {
            return TEAM.KILRATHI | TEAM.PIRATE;
        }
        else if (Enum.IsDefined(typeof(TEAM), team))
        {
            return TEAM.PIRATE;
        } 
        else
        {
            throw new System.ArgumentException($"Invalid team {(int)team}");
        }
    }
}