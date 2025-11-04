using OneManEscapePlan.Common.Scripts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public interface ITargetable : IComponent
{
    string name { get; } // implemented by MonoBehaviour.name
    TEAM Team { get; }
    bool IsCloaked { get; }
    float Radius { get; }
    Vector3 Velocity { get; }
    Sprite VDUImage { get; }
    string DisplayName { get; }
}

public interface IHaveEngines
{
    Engines Engines { get; }
}

public interface IHaveShields
{
    IReadOnlyShieldStatus Shield { get; }
}

public interface IHaveArmor
{
    IReadOnlyArmorStatus Armor { get; }
}

public interface IHaveHealth
{
    float CurrentHealth { get; }
    float MaxHealth { get; }
    float NormalizedHealth { get; }
}