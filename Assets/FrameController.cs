using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrameController : MonoBehaviour
{
    public bool FrameActive = false;
    Animation anim;

    bool isActive = false;
    bool isDeactive = true;
    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animation>();
        anim.PlayQueued("FramesNull");
    }

    // Update is called once per frame
    void Update()
    {
        if(FrameActive && !isActive)
        {            
            anim.PlayQueued("FramesIn");
            anim.wrapMode = WrapMode.Clamp;
            isActive = true;
            isDeactive = false;
        }
        if (!FrameActive && !isDeactive)
        {
            anim.PlayQueued("FramesOut");
            anim.wrapMode = WrapMode.Clamp;
            isDeactive = true;
            isActive = false;
        }

    }
}
