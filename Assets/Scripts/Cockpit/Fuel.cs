using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Required when Using UI elements.

public class Fuel : MonoBehaviour
{
    Engines engines;
    public Toggle lowFuelWarning;
    public Slider FuelLevel;
    public CustomSlider FuelLevelCustom;

    // Start is called before the first frame update

    void Blink(Toggle thing, int frameLength)
    {
        if (GameObjTracker.Instance.CurrentFrame % frameLength == 0)
        {
            if (thing.isOn)
                thing.isOn = false;
            else
                thing.isOn = true;
        }
    }

    void Start()
    {
        engines = gameObject.GetComponentInParent<Engines>();
    }

    // Update is called once per frame
    void Update()
    {
        float sliderVal = engines.Fuel / engines.MaxFuel;


        if (FuelLevelCustom != null)
        {
            FuelLevelCustom.Fill = sliderVal;
        }
        else 
        {
            FuelLevel.normalizedValue = sliderVal;
        }
        if (sliderVal < .2f)
        {
            Blink(lowFuelWarning, 25);
        }
    }
}
