using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(menuName = "Wing Commander/AI Turret Skill Settings")]
public class AITurretSkillSettings : AISkillSettings
{
    [SerializeField, Min(0)] private float engageDistance = 150f;
    [SerializeField, Min(0)] private float leadAmount = 1f;
    [SerializeField, Min(0)] private float rotationSpeed = 1f;
    [SerializeField, Min(0)] private int scanNewTargetFreq;
    [SerializeField, Min(0)] private float aimAccuracyAngle = 15f;
    [SerializeField] private bool alwaysTargetPrimary;

    public float EngageDistance => engageDistance;
    public float LeadAmount => leadAmount;
    public float RotationSpeed => rotationSpeed;
    public float AimAccuracyAngle => aimAccuracyAngle;
    public int ScanNewTargetFreq => scanNewTargetFreq;
    public bool AlwaysTargetPrimary => alwaysTargetPrimary;
}