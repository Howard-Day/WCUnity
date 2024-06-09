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
    public Vector2 FlareLengths;
    public float FlareThrottle;

    Material FlareMat;
    bool TextureSwap = false;
    VolumetricLines.VolumetricLineBehavior VolLine;

    // Start is called before the first frame update
    void Start()
    {
        FlareMat = (Material)gameObject.GetComponent<MeshRenderer>().sharedMaterial;
        VolLine = (VolumetricLines.VolumetricLineBehavior)gameObject.GetComponent<VolumetricLines.VolumetricLineBehavior>();
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
        FlareMat.SetFloat("_LineWidth", Mathf.Lerp(FlareWidths.x, FlareWidths.y, FlareThrottle) * Random.Range(.9f, 1.1f));
        if (FlareThrottle <= .2f && TextureSwap == false)
        {
            TextureSwap = true;
            SwapTexture(IdleThrottle, FlareMat);
        }

        if (FlareThrottle <= .45f && FlareThrottle > .1f && TextureSwap == false)
        {
            TextureSwap = true;
            SwapTexture(ThirdThrottle, FlareMat);
        }

        if (FlareThrottle <= .75f && FlareThrottle > .35f && TextureSwap == false)
        {
            TextureSwap = true;
            SwapTexture(CruiseThrottle, FlareMat);
        }

        if (FlareThrottle <= 1.1f && FlareThrottle > .75f && TextureSwap == false)
        {
            TextureSwap = true;
            SwapTexture(FullThrottle, FlareMat);
        }

        if (FlareThrottle > 1.1f && TextureSwap == false)
        {
            TextureSwap = true;
            SwapTexture(Afterburn, FlareMat);
            FlareMat.SetFloat("_LineWidth", FlareWidths.y * Random.Range(1.25f, 1.5f));
        }
        VolLine.StartPos = new Vector3(0, 0, Mathf.Lerp(FlareLengths.y, FlareLengths.x, FlareThrottle));

    }
}
