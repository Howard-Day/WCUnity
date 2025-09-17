using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public enum TEAM { CONFED, KILRATHI, NEUTRAL, PIRATE, ENV };
public enum CLASS { FIGHTER, FRIGATE, CAPITAL, STARBASE };
public enum WEIGHT { LIGHT, MEDIUM, HEAVY, BOMBER };

public class ShipSettingsAsset : ScriptableObject
{
	[Header("Choose Team, Name, and filters")]
	[SerializeField] private TEAM aiTeam = TEAM.CONFED; // TODO: Does this belong here?
    [SerializeField] private CLASS @class = CLASS.FIGHTER;
	[SerializeField] private WEIGHT weight = WEIGHT.MEDIUM;
	[SerializeField] private string displayName;

	[Header("Movement Settings")]
	[SerializeField] private float turnRate = 50f;
	[SerializeField] private float maxFuel = 2500f;
	[SerializeField] private float fuelBurnRate = 2f;
	[SerializeField] private float topSpeed = 20f;
	[SerializeField] private float burnSpeed = 50f;
	[SerializeField] private float acceleration = 1.5f;
	[SerializeField] private float deceleration = 1f;
	[Tooltip("TODO: How is this setting used?")]
	[SerializeField] private float lag = 1f;

	[Header("Rotation Delta")]
	[SerializeField] private float deltaSmooth = .2f;

	[Header("Weapon Settings")]
	[SerializeField] private float capacitorSize = 50f;
    [FormerlySerializedAs("weaponRechargeRate")]
	[SerializeField] private float rechargeRate = 1f;

	[Header("Health Settings")]
	[SerializeField] private ArmorStatus armor;
	[SerializeField] private ShieldStatus shield;
	[SerializeField] private float shieldRechargeRate = 1;
	[Header("Special Abilities")]
	[SerializeField] private bool hasCloak = false;
	[SerializeField] private float timeToCloak = 2f;
	[SerializeField] private float cloakPower = 20f;
	[SerializeField] private float cloakDrain = 1f;

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
}