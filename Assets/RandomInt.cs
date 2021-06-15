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

    int? lastGenInt;
    int? NumberGen (int min, int max, int skip)
    {
        if(GameObjTracker.frames % skip == 0)
        {
            lastGenInt = Random.Range(min,max);
            return lastGenInt;
        }
        else{
            if(lastGenInt == null)
            {
            lastGenInt = Random.Range(min,max);
            return lastGenInt;
            }
            return lastGenInt;
        }
    }
    // Update is called once per frame
    void Update()
    {
       RandInt.text = NumberGen(minAmt,maxAmt,frameskip).Value.ToString("D"+leadingDigits); 
    }
}
