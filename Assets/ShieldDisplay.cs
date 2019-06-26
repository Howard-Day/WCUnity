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

    // Update is called once per frame
    void Update()
    {
        FrontShield.normalizedValue = mainShip.Shield.x/mainShip._ShieldMax.x;
        ForeAmt.text = Mathf.FloorToInt(mainShip.Shield.x*10).ToString();
        
        RearShield.normalizedValue = mainShip.Shield.y/mainShip._ShieldMax.y;
        RearAmt.text = Mathf.FloorToInt(mainShip.Shield.y*10).ToString();

        ForeArmor.normalizedValue = mainShip.Armor.x/mainShip._ArmorMax.x;
        BackArmor.normalizedValue = mainShip.Armor.y/mainShip._ArmorMax.y;               
        LeftArmor.normalizedValue = mainShip.Armor.z/mainShip._ArmorMax.z;
        RightArmor.normalizedValue = mainShip.Armor.w/mainShip._ArmorMax.w;

        if(mainShip.Shield.x < mainShip._ShieldMax.x)
        {
            Blink(ForeLight, 25);
        }
        else{ ForeLight.isOn = false;}
        if(mainShip.Shield.y < mainShip._ShieldMax.y)
        {
            Blink(RearLight, 25);
        }
        else{ RearLight.isOn = false;}
        
        if(mainShip.Core < mainShip._CoreStrength)
        {
            Blink(CoreLight, 10);
            Blink(EjectLight, 40);
        }
        else{ CoreLight.isOn = false; EjectLight.isOn = false;}
        

    }
}
