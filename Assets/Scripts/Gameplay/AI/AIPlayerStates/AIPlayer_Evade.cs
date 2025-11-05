using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

public partial class AIPlayer {

    private const float SMOOTH_DAMP_TIME = .333f; // TODO: don't hardcode

    float evadePitchVelocity;
    float evadeYawVelocity;
    float evadeRollVelocity;

    void Evade()
    {
        if (evadeTimer == 0) //We're starting to evade
        {//Punch it, Chewie! 
            weaponsSystem.StopFiring();

            ship.Engines.TargetSpeed = ship.Settings.BurnSpeed;
            if (EvadeSteer == Vector3.zero)// have we chosen where to steer? 
            {
                EvadeSteer = GetRandomVector3(1f);
            }
            //Does the ship have a cloaking device? If so, engage it!
            if (ship.Settings.HasCloak)
            {
                ship.Cloak = true;
            }
        }
        if (GameObjTracker.Instance.CurrentFrame % Random.Range(60, 120) == 0) // every few second jerk around wildly! 
        {
            EvadeSteer = GetRandomVector3(2f);
        }
        // TODO: this is framerate-dependent; should use SmoothDamp or linear changes
        ship.pitch = Mathf.SmoothDamp(ship.pitch, EvadeSteer.x * skillSettings.EvadeAmount, ref evadePitchVelocity, SMOOTH_DAMP_TIME);
        ship.yaw = Mathf.SmoothDamp(ship.yaw, EvadeSteer.y * skillSettings.EvadeAmount, ref evadeYawVelocity, SMOOTH_DAMP_TIME);
        ship.roll = Mathf.SmoothDamp(ship.roll, EvadeSteer.z * skillSettings.EvadeAmount, ref evadeRollVelocity, SMOOTH_DAMP_TIME);

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

    private Vector3 GetRandomVector3(float range)
    {
        return new Vector3(Random.Range(-range, range), Random.Range(-range, range), Random.Range(-range, range));
    }
}
