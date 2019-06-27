using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Required when Using UI elements.

public class Lock : MonoBehaviour
{
    ShipSettings shipMain;
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
    /// <summary>
    /// Start is called on the frame when a script is enabled just before
    /// any of the Update methods is called the first time.
    /// </summary>
    void Start()
    {
        shipMain = (ShipSettings)gameObject.GetComponentInParent<ShipSettings>();    
    }

    // Update is called once per frame
    void Update()
    {
        if(shipMain.isLocked){
            Blink(locked, 15);
        }
        else
        {
            locked.isOn = false;
        }
    }
}
