using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Required when Using UI elements.

public class Lock : MonoBehaviour
{
    public ShipSettings mainShip;
    public Toggle locked;
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


    // Update is called once per frame
    void Update()
    {
        if(mainShip.isLocked){
            Blink(locked, 15);
        }
        else
        {
            locked.isOn = false;
        }
    }
}
