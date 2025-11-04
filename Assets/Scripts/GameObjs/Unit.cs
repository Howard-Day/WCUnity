using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

abstract public class Unit : MonoBehaviour, ITargetable
{
    #region FIELDS
    #endregion

    #region PROPERTIES
    public abstract string DisplayName { get; }
    public abstract TEAM Team { get; }
    public abstract bool IsCloaked { get; }
    public abstract float Radius { get; }
    public abstract Vector3 Velocity { get; }
    public abstract Sprite VDUImage { get; }
    #endregion
}