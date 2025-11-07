using OneManEscapePlan.Common;
using OneManEscapePlan.Common.Scripts.DataStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(menuName = "Wing Commander/AI Ship Skill Settings")]
public class AIShipSkillSettings : AISkillSettings 
{
    [SerializeField, Min(0)] protected float avoidAngle = 15f;
    [SerializeField, Min(0)] protected float avoidDistance = 50f;
    [SerializeField, Min(0)] protected float avoidSpeed = .15f;
    [SerializeField, Min(0)] protected float avoidTime = 2.5f;
    [SerializeField, Min(0)] protected float turnSpeed = 0.05f;
    [SerializeField, Min(0)] protected float leadAmount = 1.25f;
    [SerializeField, Min(0)] protected float engageDistance = 175;
    [SerializeField, Min(0)] protected float aimAccuracy = 4f;
    [SerializeField, Min(0)] protected float aimUpdate = 10;
    [Tooltip("In seconds")]
    [SerializeField] protected FloatRangeValue evadeDurationRange = new FloatRangeValue(.75f, 1.5f);
    [SerializeField, Range(90f, 180f)] protected float evadeIfTargetBehindUsThreshold = 140;
    [SerializeField, Min(0)] protected float evadeAmount = 1.5f;
    [Tooltip("Normalized percentage of evade duration, between 0 and 1")]
    [SerializeField] protected FloatRangeValue evadeJukeDurationRange = new FloatRangeValue(.25f, .75f);
    [SerializeField, Min(0)] protected float forceFireAngle = 17.5f;
    [SerializeField, Min(0)] protected float forceFireDistanceDivisor = 4.5f;
    [SerializeField, Min(0)] protected float shieldLowThreshold = .5f;
    [SerializeField, Min(0)] protected float inFrontAngleThreshold = 30f;
    [Tooltip("In meters. If we're further than this distance, we'll disengage combat and return to formation.")]
    [SerializeField, Min(0)] protected float maxDistanceFromFlightLeader = 1000f;
    [SerializeField, NonNull] protected AITurretSkillSettings turretSkillSettings;

    public float AvoidAngle => avoidAngle;
    public float AvoidDistance => avoidDistance;
    public float AvoidSpeed => avoidSpeed;
    public float AvoidTime => avoidTime;
    public float TurnSpeed => turnSpeed;
    public float LeadAmount => leadAmount;
    public float EngageDistance => engageDistance;
    public float AimAccuracy => aimAccuracy;
    public float AimUpdate => aimUpdate;
    public float EvadeIfTargetBehindUsThreshold => evadeIfTargetBehindUsThreshold;
    public FloatRangeValue EvadeDurationRange => evadeDurationRange;
    public float EvadeAmount => evadeAmount;
    public FloatRangeValue EvadeJukeDurationRange => evadeJukeDurationRange;
    public float ForceFireAngle => forceFireAngle;
    public float ForceFireDistance => engageDistance / forceFireDistanceDivisor;
    public float ShieldLowThreshold => shieldLowThreshold;
    public float InFrontAngleThreshold => inFrontAngleThreshold;
    public float MaxDistanceFromFlightLeader => maxDistanceFromFlightLeader;
    public AITurretSkillSettings TurretSkillSettings => turretSkillSettings;
}
