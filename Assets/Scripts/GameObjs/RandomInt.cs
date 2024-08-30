using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Required when Using UI elements.
public class RandomInt : MonoBehaviour
{
    public Text RandInt;
    public string leadingDigits = "6";
    public int minAmt;
    public int maxAmt;
    public int frameskip;

    int lastGenInt;
    int NumberGen(int min, int max, int skip)
    {
        if (GameObjTracker.frames % skip == 0)
        {
            lastGenInt = Random.Range(min, max);
            lastGenInt = Int32ToBaseInt(lastGenInt, 8);
            return lastGenInt;
        }
        else
        {
            if (lastGenInt == null)
            {
                lastGenInt = Random.Range(min, max);
                lastGenInt = Int32ToBaseInt(lastGenInt, 8);
                return lastGenInt;
            }
            return lastGenInt;
        }
    }
    public static int Int32ToBaseInt(int value, int toBase)
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
        int RandomInt = NumberGen(minAmt, maxAmt, frameskip);
        if (RandomInt > maxAmt)
        {
           // RandomInt = Int32ToBaseInt(maxAmt/8, 8);
        }
        RandInt.text = RandomInt.ToString("D" + leadingDigits);//.ToString("D" + leadingDigits);
    }
}
