using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomSlider : MonoBehaviour
{
    public float Fill = 1f;

    Material SliderMat;
    // Start is called before the first frame update
    void Start()
    {
        SliderMat = gameObject.GetComponent<MeshRenderer>().material;
    }

    // Update is called once per frame
    void Update()
    {
        SliderMat.SetFloat("_FillAmount", Fill);
    }
}
