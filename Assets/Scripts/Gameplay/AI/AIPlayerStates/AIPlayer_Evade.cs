using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

public partial class AIPlayer {
    void Evade()
    {
        if (evadeTimer == 0) //We're starting to evade
        {//Punch it, Chewie! 
            weaponsSystem.StopFiring();

            ship.Engines.TargetSpeed = ship.Settings.BurnSpeed;
            if (EvadeSteer == Vector3.zero)// have we chosen where to steer? 
            {
                EvadeSteer = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));
            }
            //Does the ship have a cloaking device? If so, engage it!
            if (ship.Settings.HasCloak)
            {
                ship.Cloak = true;
            }
        }
        if (GameObjTracker.Instance.CurrentFrame % Random.Range(60, 120) == 0) // every few second jerk around wildly! 
        {
            EvadeSteer = new Vector3(Random.Range(-2f, 2f), Random.Range(-2f, 2f), Random.Range(-2f, 2f));
        }
        ship.pitch = Mathf.Lerp(ship.pitch, EvadeSteer.x * skillSettings.EvadeAmount, .001f);
        ship.yaw = Mathf.Lerp(ship.yaw, EvadeSteer.y * skillSettings.EvadeAmount, .001f);
        ship.roll = Mathf.Lerp(ship.roll, EvadeSteer.z * skillSettings.EvadeAmount, .001f);
        //count down the time to return to normal combat. If we have a cloaking device, increase the wait time to be extra sneaky! 
        if (!ship.Settings.HasCloak)
        {
            evadeTimer += Time.deltaTime;
        }
        else
        {
            evadeTimer += Time.deltaTime / 4f;
        }
        if (evadeTimer >= skillSettings.EvadeLength)
        {
            EvadeSteer = Vector3.zero;
            ActiveAIState = AIState.ATTACK;
        }
        if (AITarget == null)
        {
            GoToDefaultState();
        }
    }
}
