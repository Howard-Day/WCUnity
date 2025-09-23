using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class AISkillSettings : ScriptableObject
{
    [SerializeField] private AILevel skillLevel;

    public AILevel SkillLevel => skillLevel;
}