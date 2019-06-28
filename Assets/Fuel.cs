using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Required when Using UI elements.

public class Fuel : MonoBehaviour
{
    ShipSettings shipMain;
    public Toggle lowFuelWarning;
    public Slider FuelLevel;

    // Start is called before the first frame update

    void Blink(Toggle thing, int frameLength)
    {
        if(GameObjTracker.frames % frameLength == 0)
        {
        if(thing.isOn)
            thing.isOn = false;
        else
            thing.isOn = true;                
        }
    }

    void Start()
    {
        shipMain = (ShipSettings)gameObject.GetComponentInParent<ShipSettings>();
    }

    // Update is called once per frame
    void Update()
    {
        FuelLevel.normalizedValue = shipMain._Fuel/shipMain.maxFuel;
        if(FuelLevel.normalizedValue < .2f)
        {
            Blink(lowFuelWarning, 25);
        }
    }
}
