using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Required when Using UI elements.
public class ShieldDisplay : MonoBehaviour
{
    ShipSettings shipMain;
    public Slider FrontShield;
    public Slider RearShield;
    public Slider ForeArmor;
    public Slider BackArmor;
    public Slider LeftArmor;
    public Slider RightArmor;

    public Toggle ForeLight;
    public Toggle RearLight;
    public Toggle CoreLight;
     public Toggle EjectLight;

    public Text ForeAmt;
    public Text RearAmt; 

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
        
        FrontShield.normalizedValue = shipMain.Shield.x/shipMain._ShieldMax.x;
        ForeAmt.text = Mathf.FloorToInt(shipMain.Shield.x*10).ToString();
        
        RearShield.normalizedValue = shipMain.Shield.y/shipMain._ShieldMax.y;
        RearAmt.text = Mathf.FloorToInt(shipMain.Shield.y*10).ToString();

        ForeArmor.normalizedValue = shipMain.Armor.x/shipMain._ArmorMax.x;
        BackArmor.normalizedValue = shipMain.Armor.y/shipMain._ArmorMax.y;               
        LeftArmor.normalizedValue = shipMain.Armor.z/shipMain._ArmorMax.z;
        RightArmor.normalizedValue = shipMain.Armor.w/shipMain._ArmorMax.w;

        if(shipMain.Shield.x < shipMain._ShieldMax.x)
        {
            Blink(ForeLight, 25);
        }
        else{ ForeLight.isOn = false;}
        if(shipMain.Shield.y < shipMain._ShieldMax.y)
        {
            Blink(RearLight, 25);
        }
        else{ RearLight.isOn = false;}
        
        if(shipMain._CoreStrength < shipMain.Core)
        {
            Blink(CoreLight, 10);
            if(FrontShield.normalizedValue < .5f || RearShield.normalizedValue < .5f)
            {
                Blink(EjectLight, 40);
            }
            else
            { 
                EjectLight.isOn = false;
            }
        }
        else{ CoreLight.isOn = false;}
        

    }
}
