using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

public partial class AIPlayer
{
    void Chase()
    {
        if (AITarget != null)
        {
            var distanceFromFlightLeader = ship.DistanceFromFlightLeader;
            if (distanceFromFlightLeader.HasValue && distanceFromFlightLeader > skillSettings.MaxDistanceFromFlightLeader)
            {
                GoToDefaultState();
                return;
            }

            float angleToTarget = AngleTo(AITarget.transform.position);
            if (randApproach.magnitude == 0)
            {
                randApproach = Random.onUnitSphere * AITarget.Radius * .5f;
            }

            SteerTo(AITarget.transform.position);// + (randApproach * (Vector3.Distance(AITarget.transform.position, transform.position) / engageDist)));

            ship.Engines.TargetSpeed = ship.Settings.TopSpeed;

            if (Vector3.Distance(AITarget.transform.position, transform.position) > skillSettings.EngageDistance)
            {
                ship.Engines.TargetSpeed = ship.Settings.BurnSpeed;
            }
            if (Vector3.Distance(AITarget.transform.position, transform.position) <= skillSettings.EngageDistance)
            {
                randApproach = Vector3.zero;
                ActiveAIState = AIState.ATTACK;
            }
            //Does the ship have a cloaking device? If so, engage it!
            if (ship.Settings.HasCloak)
            {
                ship.Cloak = true;
            }

        }
        else // No target.
        {
            GoToDefaultState();
        }
        //print(gameObject.name + " Is engaging! Throttle set to " + ship.Engines.TargetSpeed);
    }
}
