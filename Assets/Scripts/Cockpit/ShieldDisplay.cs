using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Required when Using UI elements.
public class ShieldDisplay : MonoBehaviour
{
    public bool inBase8 = false;
    public float minShield = 0f;
    public float maxShield = 1f;
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
        if (GameObjTracker.frames % frameLength == 0)
        {
            if (thing.isOn)
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

    public static string Int32ToString(int value, int toBase)
    {
        string result = string.Empty;
        do
        {
            result = "0123456789ABCDEF"[value % toBase] + result;
            value /= toBase;
        }
        while (value > 0);

        return result;
    }

    // Update is called once per frame
    void Update()
    {

        var shield = shipMain.Shield;
        FrontShield.normalizedValue = (shield.Front / shipMain.Settings.Shield.Front) * (maxShield - minShield) + minShield;
        RearShield.normalizedValue = shield.Back / shipMain.Settings.Shield.Back * (maxShield - minShield) + minShield;

        if (!inBase8)
        {
            ForeAmt.text = Mathf.FloorToInt(shield.Front * 10).ToString();
            RearAmt.text = Mathf.FloorToInt(shield.Back * 10).ToString();
        }
        else 
        {
            ForeAmt.text = Int32ToString(Mathf.FloorToInt(shield.Front * 10),8);
            RearAmt.text = Int32ToString(Mathf.FloorToInt(shield.Back * 10),8);
        }

        ForeArmor.normalizedValue = shipMain.Armor.Front / shipMain.Settings.Armor.Front;
        BackArmor.normalizedValue = shipMain.Armor.Back / shipMain.Settings.Armor.Back;
        LeftArmor.normalizedValue = shipMain.Armor.Left / shipMain.Settings.Armor.Left;
        RightArmor.normalizedValue = shipMain.Armor.Right / shipMain.Settings.Armor.Right;

        if (shield.Front < shipMain.Settings.Shield.Front)
        {
            Blink(ForeLight, 25);
        }
        else { ForeLight.isOn = false; }
        if (shield.Back < shipMain.Settings.Shield.Back)
        {
            Blink(RearLight, 25);
        }
        else { RearLight.isOn = false; }

        if (shipMain._CoreStrength < shipMain.CoreMax)
        {
            Blink(CoreLight, 10);
            if (FrontShield.normalizedValue < .5f || RearShield.normalizedValue < .5f)
            {
                Blink(EjectLight, 40);
            }
            else
            {
                EjectLight.isOn = false;
            }
        }
        else { CoreLight.isOn = false; }


    }
}
