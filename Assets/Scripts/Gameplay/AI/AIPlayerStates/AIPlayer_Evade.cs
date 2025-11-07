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
    double evadeEndTime = 0;
    double jukeEndTime = 0;

    public void StartEvading(bool extendIfAlreadyEvading) // Punch it, Chewie! 
    {
        if (ActiveAIState == AIState.EVADE)
        {
            if (extendIfAlreadyEvading) ResetEvadeTimer();
            return;
        }
        ActiveAIState = AIState.EVADE;

        weaponsSystem.StopFiring();

        ship.Engines.TargetSpeed = ship.Settings.BurnSpeed;
        //Does the ship have a cloaking device? If so, engage it!
        if (ship.Settings.HasCloak)
        {
            ship.Cloak = true;
        }
        jukeEndTime = 0;

        ResetEvadeTimer();
    }

    private void ResetEvadeTimer()
    {
        evadeEndTime = Time.timeAsDouble + skillSettings.EvadeDurationRange.GetRandom();
    }

    void Evade()
    {
        if (Time.timeAsDouble >= jukeEndTime) // every few second jerk around wildly! 
        {
            EvadeSteer = GetRandomVector3(2f * skillSettings.EvadeAmount);
            jukeEndTime = Time.timeAsDouble + skillSettings.EvadeJukeDurationRange.GetRandom();
        }

        ship.pitch = Mathf.SmoothDamp(ship.pitch, EvadeSteer.x, ref evadePitchVelocity, SMOOTH_DAMP_TIME);
        ship.yaw = Mathf.SmoothDamp(ship.yaw, EvadeSteer.y, ref evadeYawVelocity, SMOOTH_DAMP_TIME);
        ship.roll = Mathf.SmoothDamp(ship.roll, EvadeSteer.z, ref evadeRollVelocity, SMOOTH_DAMP_TIME);

        if (Time.timeAsDouble >= evadeEndTime)
        {
            EvadeSteer = Vector3.zero;
            evadeEndTime = 0;
            if (AITarget == null)
            {
                GoToDefaultState();
            }
            else
            {
                ActiveAIState = AIState.ATTACK;
            }
        }
    }

    private Vector3 GetRandomVector3(float range)
    {
        return new Vector3(Random.Range(-range, range), Random.Range(-range, range), Random.Range(-range, range));
    }
}
