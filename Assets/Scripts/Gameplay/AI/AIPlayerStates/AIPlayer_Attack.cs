using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

public partial class AIPlayer
{
    void Attack()
    {
        //Early bail, if no target
        if (AITarget == null)
        {
            GoToDefaultState();
        }
        else
        {
            var distanceFromFlightLeader = ship.DistanceFromFlightLeader;
            if (distanceFromFlightLeader.HasValue && distanceFromFlightLeader > skillSettings.MaxDistanceFromFlightLeader)
            {
                GoToDefaultState();
                return;
            }

            //Does the ship have a cloaking device? If so, disengage it!
            if (ship.Settings.HasCloak)
            {
                if (ship.IsCloaked)
                {
                    ship.Cloak = false;
                }
            }
            //Track the target according to our ability
            randApproach = Vector3.zero;

            if (randApproach.magnitude == 0)
            {
                randApproach = Random.onUnitSphere * AITarget.Radius * skillSettings.AimAccuracy;
            }

            float angleToTarget = AngleTo(AITarget.transform.position);
            float distToTarget = Vector3.Distance(AITarget.transform.position, transform.position);

            // Closest Target is in front of us
            if (angleToTarget < 140)
            {
                // If we're too far away to match speed to the target, get closer
                if (distToTarget > followDist)
                {
                    ship.Engines.TargetSpeed = ship.Settings.TopSpeed;
                }
                else // Match the target's speed
                {
                    if (AITarget is IHaveEngines ihe)
                    {
                        ship.Engines.TargetSpeed = Mathf.Max(Mathf.Min(ihe.Engines.TargetSpeed, ship.Settings.TopSpeed), ship.Settings.TopSpeed / 4);
                    }
                    else
                    {
                        ship.Engines.TargetSpeed = AITarget.Velocity.magnitude;
                    }
                }
                // Try and turn toward the target! 
                if (distToTarget > skillSettings.EngageDistance)
                {
                    if (angleToTarget < 60)
                    {
                        ship.Engines.TargetSpeed = ship.Settings.BurnSpeed;
                    }
                    else
                    {
                        ship.Engines.TargetSpeed = ship.Settings.TopSpeed;
                    }
                }
            }
            // OH NOES, WE BEIN HUNTED SON
            else
            {
                evadeTimer = 0;
                ActiveAIState = AIState.EVADE;
            }

            // Get the target's velocity, adding a miss possibility
            Vector3 shootAt = DoRandomOffset(skillSettings.AimAccuracy, skillSettings.AimUpdate);
            currentTargetPos = AITarget.transform.position;// + shootAt;
            Vector3 targetVelocity = AITarget.Velocity;

            // Predict where we need to shoot in order to hit our target! 
            Vector3 aimPoint = PredictV3Pos(ship.transform.position, averageGunSpeed, currentTargetPos, targetVelocity * 2f);
            //Debug.DrawLine(transform.position,shootAt+AITarget.transform.position,Color.red,.01f);
            //Debug.DrawLine(transform.position,transform.position+transform.forward*25,Color.yellow,.01f);

            // Steer to the predicted aiming location! (Only if we're not trying to avoid something)
            if (!isAvoiding)
            {
                SteerTo(aimPoint);
            }

            float angleToShoot = AngleTo(aimPoint);

            var aiTargetShip = AITarget as ShipSettings;

            // If we're within range and aim, start firing
            bool shouldFire =
                (angleToShoot < skillSettings.AimAccuracy * 2f && distToTarget <= skillSettings.EngageDistance * 2) ||
                (angleToShoot < skillSettings.AimAccuracy * 4f && distToTarget < skillSettings.EngageDistance / 8f); // Very close range, less accuracy needed

            if (shouldFire)
            {
                if (verboseLogging) { print("attempting to fire"); }
                aiTargetShip.isBeingShot = true;
                weaponsSystem.FireGuns();
            }
            else
            {
                aiTargetShip.isBeingShot = false;
                weaponsSystem.StopFiring();
            }

            // We've gotten too far away, go back into engage mode
            if (distToTarget > skillSettings.EngageDistance * 1.5f)
            {
                ActiveAIState = AIState.CHASE;
            }
            // Oh no, we've crashed, reposition!
            if (ship.recover < 1)
            {
                ActiveAIState = AIState.REPOSITION;
            }
        }
    }
}
