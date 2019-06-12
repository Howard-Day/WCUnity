using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Required when Using UI elements.

public class PowerLevelDisplay : MonoBehaviour
{
    public Ship mainShip;
    Slider PowerLevel;
 /// <summary>
/// Start is called on the frame when a script is enabled just before
/// any of the Update methods is called the first time.
/// </summary>
void Start()
{
    PowerLevel = gameObject.GetComponent<Slider>();    
}
    // Update is called once per frame
    void Update()
    {
        PowerLevel.value = Mathf.Clamp01(mainShip.capacitorLevel/mainShip.capacitorSize);
    }
}
