using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RTTFilterControl : MonoBehaviour
{
    [ExecuteInEditMode]

    RenderTexture RTT;
    CRTEffect CRTCheck;

    private void Start()
    {
        RTT = (RenderTexture)gameObject.GetComponentInChildren<MeshRenderer>().material.mainTexture;
        CRTCheck = GetComponent<CRTEffect>();
    }
    // Update is called once per frame
    void Update()
    {
        if (CRTCheck.isActiveAndEnabled)
        {
            if (RTT.filterMode == FilterMode.Point)
            {
                RTT.filterMode = FilterMode.Bilinear;
            }
        }
        if (!CRTCheck.isActiveAndEnabled)
        {
            if (RTT.filterMode == FilterMode.Bilinear)
            {
                RTT.filterMode = FilterMode.Point;
            }
        }

    }
}
