using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] public float speed = 100f;
    [SerializeField] public float damage = 1f;
    [SerializeField] public float range = 1000f;
    [SerializeField] public GameObject hitShield;
    [SerializeField] public GameObject hitHull;
    [SerializeField] public LayerMask shootMask;

    [HideInInspector] public int ProjID;
    float distanceTraveled = 0f;


    /// <summary>
    /// Start is called on the frame when a script is enabled just before
    /// any of the Update methods is called the first time.
    /// </summary>
    void Start()
    {
        gameObject.transform.SetParent(GameObject.FindWithTag("Projectiles").transform);

    }
    RaycastHit Hit;

    void DoCollision()
    {

        if (Physics.Linecast(transform.position, transform.position + (transform.forward * speed * Time.deltaTime), out Hit, shootMask, QueryTriggerInteraction.Collide))
        {
            //print("Hit Detected at "+ Hit.point + Hit.collider);
            ShipSettings shipHit = Hit.transform.gameObject.GetComponent<ShipSettings>();
            if (shipHit.ShipID != ProjID)
            {
                if (shipHit.DoDamage(Hit.point, damage, ProjID)[0] == 1)
                {
                    Instantiate(hitShield, Hit.point, Quaternion.identity, shipHit.transform); ///Shield hits follow the ship that's hit
                }
                else
                {
                    Instantiate(hitHull, Hit.point, Quaternion.identity, gameObject.transform.parent); //Hull Hits do not
                }
                Destroy(gameObject);
            }
            else
            {
            }

        }
    }

    void FixedUpdate()
    {
        DoCollision();
        transform.position += transform.forward * speed * Time.deltaTime;

        distanceTraveled += speed * Time.deltaTime;
        if (distanceTraveled > range)
        {
            Destroy(gameObject);
        }

    }
}
