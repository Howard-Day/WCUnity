using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

public partial class AIPlayer
{
    void Wingman()
    {
        if (ship.Flight == null) //Look for a wingleader in this state 
        {
            TryJoinFlight();
        }
        if (ship.Flight == null || ship.Flight.Leader == ship) // No wingleaders? Individual patrol mode!
        {
            ActiveAIState = AIState.PATROL;
        }
        else // We are a wingman in a formation
        {
            var leader = ship.Flight.Leader;

            //See how far away and what direction we need to go
            var localFormationPose = ship.Flight.GetSlotPose(ship);
            var leadDist = Vector3.Distance(localFormationPose.position, transform.position);
            var dirToPos = localFormationPose.position - transform.position;

            Debug.DrawLine(gameObject.transform.position, localFormationPose.position, Color.green, .10f);
            if (verboseLogging)
            {
                OMEPLogger.Log(this, $"leadDist: {leadDist}, AITarget: {AITarget}, " +
                    $"Target dist: {Vector3.Distance(AITarget.transform.position, transform.position)}," +
                    $" Engage dist: {skillSettings.EngageDistance}");
            }

            if (leadDist != 0)
            {
                if (leadDist > 120)//If we're a ways off, aim right at the formation point and afterburn into position.
                {
                    ship.Engines.TargetSpeed = ship.Settings.BurnSpeed;
                    SteerTo(localFormationPose.position);
                }
                if (leadDist <= 120 && leadDist > 20) //If we're a moderate distance away, set speed to the lead ship +25%, aim at the formation position.
                {
                    ship.Engines.TargetSpeed = leader.Engines.Speed + ship.Settings.TopSpeed / 4;
                    SteerTo(localFormationPose.position);
                }
                if (leadDist <= 20) //If we're close, Match speed, and aim at a point parallel to the direction of the lead ship
                {
                    ship.Engines.TargetSpeed = leader.Engines.Speed;
                    SteerTo(localFormationPose.position + leader.transform.forward * ship.Radius * 4f);
                    //A gentle push, like the avoidance system, to nudge us into place
                    float formPush = (dirToPos.magnitude / 10) * .5f;
                    transform.position += dirToPos * formPush * Time.deltaTime;
                }
                if (leadDist <= 30) // attempt to match roll once we get close-ish
                {
                    // TODO: this is hacky and affected by framerate. use ship controls to rotate instead of lerping.
                    transform.rotation = Quaternion.Lerp(transform.rotation, localFormationPose.rotation, .005f);
                    //QuaternionUtil.SmoothDamp(transform.rotation,WingmanTo.transform.rotation, ref refForm, .15f);
                    ship.roll = leader.roll;
                }
            }

            if (AITarget != null)
            {
                var distanceToTarget = Vector3.Distance(transform.position, AITarget.transform.position);
                // If our target is pretty far away, switch to our leader's target.
                if (distanceToTarget > skillSettings.MaxDistanceFromFlightLeader)
                {
                    if (leader.CurrentTarget != null)
                    {
                        AITarget = leader.CurrentTarget;
                    }
                }
                else if (distanceToTarget < skillSettings.EngageDistance) // Hold formation until we're very close
                {
                    // If we're not too far from flight leader, engage target if close enough to target.
                    // Note that we scale down the max distance from flight leader here, so that we don't
                    // have to turn around and head back towards leader immediately after engaging.
                    if (leadDist <= skillSettings.MaxDistanceFromFlightLeader * .5f)
                    {
                        ActiveAIState = AIState.REPOSITION;
                    }
                }
            }
        }
    }
}
