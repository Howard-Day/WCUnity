using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

public partial class AIPlayer {
    void Patrol()
    {
        //stop firing, if we are
        weaponsSystem.StopFiring();
        //Check to see if we've got any patrol points assigned already! 
        if (PatrolPoints.Count == 0)
        {
            // TODO: better logic for patrol points
            //Oh no! We need some to patrol, let's generate some, lessay 4
            int pp = 0;
            int numPoints = 4;
            while (pp <= numPoints)
            {
                PatrolPoints.Add(Random.onUnitSphere * Random.Range(320f, 500f));
                pp++;
            }
        }
        float ppDist = Vector3.Distance(transform.position, PatrolPoints[nextPatrolPoint]);
        if (ppDist > 10)
        {
            SteerTo(PatrolPoints[nextPatrolPoint]);
            ship.Engines.TargetSpeed = ship.Settings.TopSpeed * .75f; //Cruise speed! No rush, juuust loooking for baddies. 
        }
        else
        {
            print(gameObject.name + " Reached patrol point " + nextPatrolPoint + "; going to the next!");
            nextPatrolPoint++;
        }
        if (nextPatrolPoint > PatrolPoints.Count - 1) //Cycle the patrol point list
        {
            print(gameObject.name + " is Loooping patrol points!");
            nextPatrolPoint = 0;
        }

        // Target the last ship that attacked us, if any.
        if (AITarget != null && ship.lastHitID != 0)
        {
            AITarget = FindShipByID(ship.lastHitID, ship.Team);
        }

        // Distance check currently disabled; otherwise, distances are probably too low right now.
        if (AITarget != null /*&& DistanceTo(AITarget.gameObject) <= skillSettings.EngageDistance * 1.5f*/)
        { // If we're withing the engage envelope, let's go check it out! 
            ActiveAIState = AIState.CHASE;
        }
    }
}
