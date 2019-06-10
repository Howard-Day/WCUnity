using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EngineFlare : MonoBehaviour
{
    public Texture2D IdleThrottle;
    public Texture2D ThirdThrottle;
    public Texture2D CruiseThrottle;
    public Texture2D FullThrottle;
    public Texture2D Afterburn;
    public Vector2 FlareWidths;
    public float FlareThrottle;

    Material FlareMat; 
    bool TextureSwap = false; 

    // Start is called before the first frame update
    void Start()
    {
        FlareMat = (Material)gameObject.GetComponent<MeshRenderer>().sharedMaterial;
    }

    void SwapTexture(Texture2D to, Material mat)
    {
        if (!TextureSwap)
            return;
        mat.mainTexture = to;
        TextureSwap = false;
     
    }

    // Update is called once per frame
    void Update()
    {
        FlareMat.SetFloat("_LineWidth", Mathf.Lerp(FlareWidths.x,FlareWidths.y,FlareThrottle ) );
        if(FlareThrottle <= .1f && TextureSwap == false)
        {
            TextureSwap = true;
            SwapTexture(IdleThrottle,FlareMat);
        }
        
        if(FlareThrottle <= .35f && FlareThrottle > .1f  && TextureSwap == false)
        {
            TextureSwap = true;
            SwapTexture(ThirdThrottle,FlareMat);
        }

        if(FlareThrottle <= .75f && FlareThrottle > .35f && TextureSwap == false)
        {
            TextureSwap = true;
            SwapTexture(CruiseThrottle,FlareMat);
        }
        
        if(FlareThrottle <= 1.0f && FlareThrottle > .75f && TextureSwap == false)
        {
            TextureSwap = true;
            SwapTexture(FullThrottle,FlareMat);
        }

        if(FlareThrottle > 1.0f && TextureSwap == false)
        {
            TextureSwap = true;
            SwapTexture(Afterburn,FlareMat);
        }

    }
}
