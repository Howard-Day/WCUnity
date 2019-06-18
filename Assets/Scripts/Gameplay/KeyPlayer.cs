using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyPlayer : MonoBehaviour
{
    public float _pitch = 0f;
    public float _yaw = 0f;
    public float _roll = 0f;
    public float _targetthrottle = 0;
    public bool fireGuns = false;
    
    ShipSettings ship;
    LaserCannon[] laserCannons;
    // Start is called before the first frame update
    void Start()
    {
        ship = GetComponent<ShipSettings>();
          laserCannons = GetComponentsInChildren<LaserCannon>();
    }
    void FireGuns(bool fire)
    {
    foreach (LaserCannon laserCannon in laserCannons)
        {
        laserCannon.fire = fire;
        }
    }

    // Update is called once per frame
    void Update()
    {
        ship.pitch = _pitch;
        ship.yaw = _yaw;
        ship.roll = _roll;
        ship.targetSpeed = Mathf.Lerp(0,ship.topSpeed,_targetthrottle);
        FireGuns(fireGuns);
    }
}
