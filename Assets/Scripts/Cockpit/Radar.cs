using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Required when Using UI elements.

public class Radar : MonoBehaviour
{

    ShipSettings shipMain;
    public Vector2 nearFarClip;
    public float radarMapXScale = .33f, radarMapYScale = .33f;
    public Color friendlyNear;
    public Color friendlyFar;

    public Color enemyNear;
    public Color enemyFar;

    public Color neutralNear;
    public Color neutralFar;

    public Color envNear;
    public Color envFar;

    public Color navigation;


    public Sprite[] fighterBlips;
    public Sprite[] capitalBlips;
    public Sprite[] EnvBlips;

    public Toggle HitFore;
    public Toggle HitRight;
    public Toggle HitLeft;
    public Toggle HitUp;
    public Toggle HitDown;
    public Toggle HitBack;

    
    // Start is called before the first frame update
    void Start()
    {   //Find our Ship Root! 
        shipMain = (ShipSettings)gameObject.GetComponentInParent<ShipSettings>();
    }
    public void RegisterBlips()
    {

    }
    void DoHitFlash() //Show incoming fire on the radar! 
    {
        if(GameObjTracker.frames % 120 == 0) // Every 2 sec (approx) reset the hit history
        {
            shipMain.lastHit = ShipSettings.HitLoc.NULL;
        }
        if(GameObjTracker.frames % 60 == 0) //every sec (approx) reset the hit flashes to off
        {
            HitFore.isOn = false;
            HitRight.isOn = false;
            HitLeft.isOn = false;
            HitUp.isOn = false;
            HitDown.isOn = false;
            HitBack.isOn = false;            
        }
        //Flash the appropriate Radar section for incoming fire!
        if(shipMain.lastHit == ShipSettings.HitLoc.F)
        {HitFore.isOn = true;
        }       
        if(shipMain.lastHit == ShipSettings.HitLoc.R)
        {HitRight.isOn = true;
        }       
        if(shipMain.lastHit == ShipSettings.HitLoc.L)
        {HitLeft.isOn = true;
        }       
        if(shipMain.lastHit == ShipSettings.HitLoc.U)
        {HitUp.isOn = true;
        }       
        if(shipMain.lastHit == ShipSettings.HitLoc.D)
        {HitDown.isOn = true;
        }       
        if(shipMain.lastHit == ShipSettings.HitLoc.B)
        {HitBack.isOn = true;
        }       
    }
    void DoBlips(ShipSettings.TEAM dispTeam, Color Near, Color Far, Vector2 clipDist) //Generic per-Team blips
    {
        int i = 0;
        while(i<GameObjTracker.Ships.Count-1)
        {
            ShipSettings ship = (ShipSettings)GameObjTracker.Ships[i];
            //check team, and self!
            if(ship != shipMain && ship.AITeam == dispTeam)
            {
                Vector3 blipLoc = ship.transform.position;
                Vector3 blipAngle = shipMain.transform.position-blipLoc;
                float blipDist = blipAngle.magnitude;
                blipAngle.Normalize();
                float x = Vector3.Dot(blipAngle, shipMain.transform.right);
		        float y = Vector3.Dot(blipAngle, shipMain.transform.up);
                //x = x/2+(Vector3.Dot(blipAngle, -shipMain.transform.forward)+1)/2;
                //y = y/2+(Vector3.Dot(blipAngle, -shipMain.transform.forward)+1)/2;

                float normalizedDist = Mathf.Clamp01((clipDist.x+blipDist)/clipDist.y);
                GameObject blip = new GameObject();
                Image blipSprite = (Image)blip.AddComponent<Image>();
                if(ship.shipRadius >= 40)//Big contact! 
                { //use the last 3 sprites as normalized distance falloffs
                    blipSprite.sprite = capitalBlips[Mathf.CeilToInt(normalizedDist*3)];
                }
                else//Fighter contact! 
                { //use the last 3 sprites as normalized distance falloffs
                    blipSprite.sprite = fighterBlips[Mathf.CeilToInt(normalizedDist*3)];
                }
                blipSprite.color = Color.Lerp(Near,Far,normalizedDist);
                blipSprite.transform.position = gameObject.transform.position;
                blipSprite.transform.parent = gameObject.transform;
                blipSprite.transform.localEulerAngles = Vector3.zero;
                blipSprite.transform.Translate(x*radarMapXScale,y*radarMapYScale, -.0035f);
                blip.transform.localScale = Vector3.one*.001f;
                blip.name = "blip";
            }
            i++;
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        DoHitFlash();
        if(shipMain.AITeam == ShipSettings.TEAM.CONFED)
        {
            DoBlips(ShipSettings.TEAM.KILRATHI,enemyNear,enemyFar,nearFarClip);
            DoBlips(ShipSettings.TEAM.CONFED,friendlyNear,friendlyFar,nearFarClip);
            DoBlips(ShipSettings.TEAM.PIRATE,enemyNear,enemyFar,nearFarClip);      
            DoBlips(ShipSettings.TEAM.NEUTRAL,neutralNear,neutralFar,nearFarClip);      
        }
        if(shipMain.AITeam == ShipSettings.TEAM.KILRATHI)
        {
            DoBlips(ShipSettings.TEAM.CONFED,enemyNear,enemyFar,nearFarClip);
            DoBlips(ShipSettings.TEAM.KILRATHI,friendlyNear,friendlyFar,nearFarClip);
            DoBlips(ShipSettings.TEAM.PIRATE,enemyNear,enemyFar,nearFarClip);      
            DoBlips(ShipSettings.TEAM.NEUTRAL,neutralNear,neutralFar,nearFarClip);      
        }
    }
}
