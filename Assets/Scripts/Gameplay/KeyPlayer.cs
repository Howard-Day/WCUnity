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
    public bool afterBurn = false;

    ShipSettings ship;
    ProjectileWeapon[] laserCannons;
    // Start is called before the first frame update
    void Start()
    {
        ship = GetComponent<ShipSettings>();
        laserCannons = GetComponentsInChildren<ProjectileWeapon>();
    }
    void FireGuns(bool fire)
    {
        foreach (ProjectileWeapon laserCannon in laserCannons)
        {
            laserCannon.fire = fire;
        }
    }
    void DoBurn(bool isAfterburning)
    {
        if (isAfterburning)
        {
            ship.isAfterburning = isAfterburning;
            ship.targetSpeed = ship.burnSpeed;
        }
        if (!isAfterburning)
        {
            ship.isAfterburning = isAfterburning;
            //ship.targetSpeed = ship.burnSpeed;
        }
    }

    // Update is called once per frame
    void Update()
    {
        ship.pitch = _pitch;
        ship.yaw = _yaw;
        ship.roll = _roll;
        ship.targetSpeed = Mathf.Lerp(0, ship.topSpeed, _targetthrottle);
        DoBurn(afterBurn);
        FireGuns(fireGuns);
    }
}
