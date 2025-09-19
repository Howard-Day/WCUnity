using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

abstract public class Unit : MonoBehaviour
{
    #region FIELDS
    #endregion

    #region PROPERTIES
    abstract public string DisplayName { get; }
    abstract public TEAM Team { get; }
    #endregion
}