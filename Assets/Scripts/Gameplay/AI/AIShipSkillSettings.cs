using OneManEscapePlan.Common;
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
    [SerializeField, Min(0)] protected float evadeLength = .5f;
    [SerializeField, Min(0)] protected float evadeAmount = 1.5f;
    [SerializeField, Min(0)] protected float forceFireAngle = 17.5f;
    [SerializeField, Min(0)] protected float forceFireDistanceDivisor = 4.5f;
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
    public float EvadeLength => evadeLength;
    public float EvadeAmount => evadeAmount;
    public float ForceFireAngle => forceFireAngle;
    public float ForceFireDistance => engageDistance / forceFireDistanceDivisor;
    public AITurretSkillSettings TurretSkillSettings => turretSkillSettings;
}
