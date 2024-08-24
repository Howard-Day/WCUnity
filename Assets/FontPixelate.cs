using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FontPixelate : MonoBehaviour
{
    [ExecuteInEditMode]
    
    // Start is called before the first frame update
    void OnEnable()
    {
        Text text = GetComponent<Text>();
        text.font.material.mainTexture.filterMode = FilterMode.Point;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
