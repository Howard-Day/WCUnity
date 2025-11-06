using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

public partial class AIPlayer {
    void Reposition()
    {
        //Early Bail if no target
        if (AITarget == null)
        {
            GoToDefaultState();
        }
        //Basic State setup
        else
        {
            float angleToTarget = AngleTo(AITarget.transform.position);
            Vector3 dirToTarget = AITarget.transform.position - transform.position;
            float distToTarget = Vector3.Distance(AITarget.transform.position, transform.position);

            //No shootie
            weaponsSystem.StopFiring();

            randPos = Vector3.zero;
            if (randPos.magnitude == 0)
            {
                randPos = transform.position + Random.onUnitSphere * skillSettings.EngageDistance;
            }
            if (distToTarget < 80f && distToTarget > 40f)
            {
                ship.Engines.TargetSpeed = ship.Settings.BurnSpeed;
            }
            else
            {
                ship.Engines.TargetSpeed = ship.Settings.TopSpeed;
            }
            SteerTo(randPos);
            if (Vector3.Distance(transform.position, randPos) < 20f || distToTarget > 100f)
            {
                randPos = Vector3.zero;
                ActiveAIState = AIState.ATTACK;
            }
        }
    }
}
