using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Required when Using UI elements.

public class PowerLevelDisplay : MonoBehaviour
{
    ShipSettings shipMain;
    Slider PowerLevel;
    /// <summary>
    /// Start is called on the frame when a script is enabled just before
    /// any of the Update methods is called the first time.
    /// </summary>
    void Start()
    {
        shipMain = (ShipSettings)gameObject.GetComponentInParent<ShipSettings>();
        PowerLevel = gameObject.GetComponent<Slider>();
    }
    // Update is called once per frame
    void Update()
    {
        PowerLevel.value = Mathf.Clamp01(shipMain.capacitorLevel / shipMain.capacitorSize);
    }
}
