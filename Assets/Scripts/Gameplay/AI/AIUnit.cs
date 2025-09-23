using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public enum AIState { PATROL, BREAK, SEARCH, WINGMAN, ENGAGE, HUNT, EVADE, PROTECT, REPOSITION, FLEE, DEATH, VICTORY };
public enum AILevel { CHUMP, NOVICE, DEFAULT, SKILLED, ACE, MASTER };

abstract public class AIUnit : MonoBehaviour
{
    [Header("Debug Options")]
    public bool logDebug = false;

    protected WeaponsSystem weaponsSystem;
    protected Transform AITarget;

    protected float averageGunSpeed = 0f;
    protected float cooldownWait;
    protected bool cooldownWaiting = false;

    abstract protected Capacitor MainCapacitor { get; }

    //Utility to Get a ship by ID
    public ShipSettings FindShipByID(int id, TEAM team)
    {

        ShipSettings foundShip = GameObjTracker.Instance.GetShipByID(id);
        if (foundShip != null && foundShip.Team != team)
        {
            return foundShip;
        }
        else
        {
            return null;
        }
    }

    //Utility to do a Simple Distance Calc
    public float DistanceTo(GameObject obj)
    {
        float dist = Vector3.Distance(obj.transform.position, transform.position);
        return dist;
    }

    //Handy thing -since the cockpits can have offset pitches to line up the reticles, we need to adust our forward angle if it's a player ship.
    protected float AngleTo(Vector3 target)
    {
        if (target == null)
            return 0f;
        Vector3 tempForward;
        tempForward = transform.forward;
        return Vector3.Angle(tempForward, target - transform.position);
    }

    //Custom Angle-to-Target test
    protected float CustomAngleTo(Vector3 testVec, Vector3 target)
    {
        if (target == null)
            return 0f;
        return Vector3.Angle(testVec, target - transform.position);
    }

    //Handle Gun Cooldown wait
    protected void DoGunCooldown(float waitTime, float minCapacitorLevel)
    {
        float normalizedCapacitorLevel = MainCapacitor.CurrentChargeNormalized;
        // if the capacitors are low, add wait time
        if (normalizedCapacitorLevel < .1f && !cooldownWaiting)
        {
            cooldownWait += Time.deltaTime * 10;
        }
        //if the wait time has triggered, go into cooldown mode
        if (cooldownWait > waitTime && !cooldownWaiting)
        {
            cooldownWaiting = true;
        }
        //cooldown mode, disable firing till the capacitors are to a minimum level
        if (cooldownWaiting)
        {
            weaponsSystem.StopFiring();
            if (normalizedCapacitorLevel >= minCapacitorLevel)
            {
                cooldownWait = 0;
                cooldownWaiting = false;
            }
        }
    }

    //Find our average attached gun speed for leading targets
    protected void DoGunSpeed()
    {
        //loop through our guns, if they're initialized, and we haven't figured this out yet
        if (averageGunSpeed == 0 || averageGunSpeed == float.NaN)
        {
            float tempGunSpeed = 0f;
            //loop through our guns, and add all their speeds together
            if (logDebug) { print("the number of found weapons is " + weaponsSystem.projWeapons.Count); }
            foreach (ProjectileWeapon gun in weaponsSystem.projWeapons)
            {
                tempGunSpeed += gun.speed;
            }
            //return the cumulative gunspeeds by the number of guns, set the value so this only runs once.
            // NOTE: AITurret originally multiplied this value by leadAmount, but that doesn't seem right
            // since leadAmount is also used in PredictV3Pos()
            averageGunSpeed = tempGunSpeed / weaponsSystem.projWeapons.Count;
        }
    }
}
