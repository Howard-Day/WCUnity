using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BlipController : MonoBehaviour
{
    public Radar radarRoot;
    public ShipSettings ship;
    public ShipSettings shipMain;
    public Color Near;
    public Color Far;
    public Vector2 clipDist;
    

    // Update is called once per frame
    void Update()
    {
        if(!ship)
        {
            radarRoot.RegisterBlips();
            Destroy(this.gameObject);
            return;
        }
        Vector3 blipLoc = ship.transform.position;
        Vector3 blipAngle = shipMain.transform.position-blipLoc;
        float blipDist = blipAngle.magnitude;
        blipAngle.Normalize();
        float x = Vector3.Dot(blipAngle, shipMain.transform.right);
        float y = Vector3.Dot(blipAngle, shipMain.transform.up);
        //x = x/2+(Vector3.Dot(blipAngle, -shipMain.transform.forward)+1)/2;
        //y = y/2+(Vector3.Dot(blipAngle, -shipMain.transform.forward)+1)/2;

        float normalizedDist = Mathf.Clamp01((clipDist.x+blipDist)/clipDist.y);
        
        Image blipSprite = (Image)gameObject.AddComponent<Image>();
        if(ship.shipRadius >= 40)//Big contact! 
        { //use the last 3 sprites as normalized distance falloffs
            blipSprite.sprite = radarRoot.capitalBlips[Mathf.CeilToInt(normalizedDist*3)];
        }
        else//Fighter contact! 
        { //use the last 3 sprites as normalized distance falloffs
            blipSprite.sprite = radarRoot.fighterBlips[Mathf.CeilToInt(normalizedDist*3)];
        }
        blipSprite.color = Color.Lerp(Near,Far,normalizedDist);
        blipSprite.transform.position = gameObject.transform.position;
        blipSprite.transform.parent = gameObject.transform;
        blipSprite.transform.localEulerAngles = Vector3.zero;
        blipSprite.transform.Translate(x*radarRoot.radarMapXScale,y*radarRoot.radarMapYScale, -.0035f);
        transform.localScale = Vector3.one*.001f;
        name = "blip";
    }
}
