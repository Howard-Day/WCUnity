using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Required when Using UI elements.
public class SetSpeed : MonoBehaviour
{
    public Text Speed;
    public string preText;
    public bool randompreText = false;
    public bool inBase8 = false;

    public int minAmt;
    public int maxAmt;
    public int frameskip;

    ShipSettings shipMain;
    // Start is called before the first frame update
    void Start()
    {
        shipMain = (ShipSettings)gameObject.GetComponentInParent<ShipSettings>();
    }
    int? lastGenInt;
    int? NumberGen(int min, int max, int skip)
    {
        if (GameObjTracker.frames % skip == 0)
        {
            lastGenInt = Random.Range(min, max);

            return lastGenInt;
        }
        else
        {
            if (lastGenInt == null)
            {
                lastGenInt = Random.Range(min, max);
                if (inBase8)
                {
                    lastGenInt = Int32ToString(lastGenInt.Value, 8);
                }
                return lastGenInt;
            }
            return lastGenInt;
        }
    }
    public static int Int32ToString(int value, int toBase)
    {
        int result = 0;
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
        float setSpeed = shipMain.targetSpeed;
        if (setSpeed >= shipMain.topSpeed)
            setSpeed = shipMain.topSpeed;
        if (shipMain.isAfterburning)
            setSpeed = shipMain.burnSpeed;

        int speedDisp = Mathf.FloorToInt(setSpeed * 10);
        if (!inBase8)
        {
            if (randompreText)
            {
                Speed.text = NumberGen(minAmt, maxAmt, frameskip).Value.ToString("D2") + speedDisp.ToString();
            }
            else
            {
                Speed.text = preText + speedDisp.ToString();
            }
        }
        else 
        {            
            if (randompreText)
            {
                Speed.text = NumberGen(minAmt, maxAmt, frameskip).Value.ToString("D2") + Int32ToString(speedDisp,8);
            }
            else
            {
                Speed.text = preText + Int32ToString(speedDisp, 8);
            }
        }
    }
}
