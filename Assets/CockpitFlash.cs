using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CockpitFlash : MonoBehaviour
{
    public int index;
    public Color brightColor;
    public Color darkColor;
    public float fadeTime = .25f;

    ProjectileWeapon gun;
    ShipSettings ship;
    public float brightness;
    Texture baseArt;
    Material flashMat;
    // Start is called before the first frame update
    void Start()
    {
        //init the fade and store references 
        brightness = 0f;
        //get the main sprite on the parent
        baseArt = GetComponentInParent<SpriteRenderer>().sprite.texture;
        flashMat = GetComponent<SpriteRenderer>().materials[0]; 
        flashMat.SetTexture("_BaseTex", baseArt);
        flashMat.SetColor("_BrightColor", brightColor);
        flashMat.SetColor("_DarkColor", darkColor);
        flashMat.SetFloat("_Brightness", brightness);
    }
    int frameDelay = 0;

    // Update is called once per frame
    void Update()
    {
        //Run checks to make sure we've gotten the objects needed
        if (ship == null)
        {
            ship = GetComponentInParent<ShipSettings>();
        }
        if (gun == null && ship.projWeapons.Count > 0 )
        {            
            gun = ship.projWeapons[index];
        }
        if (!gun)
            Debug.Log("failed to find a gun the index of " + index);

        //Flash the gun if we've fired!
        if(gun != null)
        {
            if (gun.hasFired)
            {
                brightness = 1f;
                //reset the check flag so we only fire once! 
                if (frameDelay > 2)
                {
                    gun.hasFired = false;
                    frameDelay = 0;
                }
                frameDelay++;
                
            }
        }
        //fade the flash off based on the fadeTime
        if (brightness > 0)
        {
            brightness -= Time.deltaTime / fadeTime;
        }
        //assign the brightness to the shader
        flashMat.SetFloat("_Brightness", brightness);
    }
}
