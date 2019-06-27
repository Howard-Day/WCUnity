using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RTTDepthSet : MonoBehaviour
{
    public RenderTexture RTT;
    // Start is called before the first frame update
    void Start()
    {
        RTT.depth = 32;
    }

}
