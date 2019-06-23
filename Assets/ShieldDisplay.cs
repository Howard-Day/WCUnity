using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Required when Using UI elements.
public class ShieldDisplay : MonoBehaviour
{
    public ShipSettings mainShip;
    public Slider FrontShield;
    public Slider RearShield;
    public Slider ForeArmor;
    public Slider BackArmor;
    public Slider LeftArmor;
    public Slider RightArmor;

    public Toggle ForeLight;
    public Toggle RearLight;
    public Toggle CoreLight;

    public Text ForeAmt;
    public Text RearAmt; 

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
