using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Required when Using UI elements.
public class RandomSliders : MonoBehaviour
{
    public Image[] Sliders;
    public int frameSkip;
    public float speed = .2f;
    float[] SlidersMax;
    float[] SlidersTarget;

    // Start is called before the first frame update
    int intCount = 0;
    void Start()
    {
        SlidersMax = new float[Sliders.Length];
        SlidersTarget = new float[Sliders.Length];
        foreach (Image s in Sliders)
        {
            SlidersMax[intCount] = s.fillAmount;
            SlidersTarget[intCount] = Random.Range(0, s.fillAmount);
            intCount++;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (GameObjTracker.frames % frameSkip == 0)
        {
            int chooseRand = Random.Range(0, Sliders.Length);
            SlidersTarget[chooseRand] = Random.Range(0, SlidersMax[chooseRand]);
        }
        for (int i = 0; i < Sliders.Length; i++)
        {
            Sliders[i].fillAmount = Mathf.SmoothStep(Sliders[i].fillAmount, SlidersTarget[i], speed);
        }
    }
}
