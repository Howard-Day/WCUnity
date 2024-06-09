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

    public Material blipMat;


    public Sprite[] fighterBlips;
    public Sprite[] capitalBlips;
    public Sprite[] EnvBlips;

    public Toggle HitFore;
    public Toggle HitRight;
    public Toggle HitLeft;
    public Toggle HitUp;
    public Toggle HitDown;
    public Toggle HitBack;
    [HideInInspector]
    public List<BlipController> RadarBlips;
    GameObject BlipRoot;

    // Start is called before the first frame update
    void Start()
    {   //Find our Ship Root! 
        RadarBlips = new List<BlipController>();
        shipMain = (ShipSettings)gameObject.GetComponentInParent<ShipSettings>();
        BlipRoot = new GameObject();
        BlipRoot.name = "BlipRoot";
        BlipRoot.transform.parent = gameObject.transform;
        BlipRoot.transform.localPosition = Vector3.zero;
        BlipRoot.transform.localScale = Vector3.one;
        RegisterBlips();

        HitFore.isOn = false;
        HitRight.isOn = false;
        HitLeft.isOn = false;
        HitUp.isOn = false;
        HitDown.isOn = false;
        HitBack.isOn = false;
    }

    //radarRefreshNeeded
    public void RegisterBlips()
    {
        foreach (BlipController blip in RadarBlips)
        {
            Destroy(blip.gameObject);
        }
        RadarBlips = new List<BlipController>();

        if (shipMain.AITeam == ShipSettings.TEAM.CONFED)
        {
            MakeBlips(GameObjTracker.KilrathiShips, enemyNear, enemyFar);
            MakeBlips(GameObjTracker.ConfedShips, friendlyNear, friendlyFar);
        }
        if (shipMain.AITeam == ShipSettings.TEAM.KILRATHI)
        {
            MakeBlips(GameObjTracker.ConfedShips, enemyNear, enemyFar);
            MakeBlips(GameObjTracker.KilrathiShips, friendlyNear, friendlyFar);
        }
        if (shipMain.AITeam == ShipSettings.TEAM.PIRATE)
        {
            MakeBlips(GameObjTracker.ConfedShips, enemyNear, enemyFar);
            MakeBlips(GameObjTracker.KilrathiShips, enemyNear, enemyFar);
        }
        if (shipMain.AITeam == ShipSettings.TEAM.NEUTRAL)
        {
            MakeBlips(GameObjTracker.ConfedShips, neutralNear, neutralFar);
            MakeBlips(GameObjTracker.KilrathiShips, neutralNear, neutralFar);
        }
        MakeBlips(GameObjTracker.PirateShips, enemyNear, enemyFar);
        MakeBlips(GameObjTracker.NeutralShips, neutralNear, neutralFar);
        MakeBlips(GameObjTracker.Environmental, envNear, envFar);
        GameObjTracker.radarRefreshNeeded = false;
        //print("Radar Refresh is: "+ GameObjTracker.radarRefreshNeeded);

    }

    void MakeBlips(List<ShipSettings> Ships, Color Near, Color Far)
    {
        foreach (ShipSettings ship in Ships) //Go through a list of ships, add them 
        {
            if (ship != shipMain) //But only if we're not looking at ourselves! 
            {
                GameObject blipObj = new GameObject();
                BlipController blip = blipObj.AddComponent<BlipController>() as BlipController;
                blipObj.name = "blip";
                blipObj.transform.parent = BlipRoot.transform;
                blipObj.transform.localPosition = Vector3.zero;
                blipObj.transform.localScale = Vector3.one;
                blip.ship = ship;
                blip.clipDist = nearFarClip;
                blip.Near = Near;
                blip.Far = Far;
                blip.radarRoot = gameObject.GetComponent<Radar>();
                blip.shipMain = shipMain;
                RadarBlips.Add(blip);
            }
        }

    }

    void DoHitFlash() //Show incoming fire on the radar! 
    {
        if (GameObjTracker.frames % 120 == 0 || Camera.main == null) // Every 2 sec (approx) reset the hit history, or if the cockpit has been destroyed. 
        {
            shipMain.lastHit = ShipSettings.HitLoc.NULL;
        }
        if (GameObjTracker.frames % 30 == 0 || Camera.main == null) //every sec (approx) reset the hit flashes to off
        {
            HitFore.isOn = false;
            HitRight.isOn = false;
            HitLeft.isOn = false;
            HitUp.isOn = false;
            HitDown.isOn = false;
            HitBack.isOn = false;
        }
        //Flash the appropriate Radar section for incoming fire!
        if (shipMain.lastHit == ShipSettings.HitLoc.F)
        {
            HitFore.isOn = true;
        }
        if (shipMain.lastHit == ShipSettings.HitLoc.R)
        {
            HitRight.isOn = true;
        }
        if (shipMain.lastHit == ShipSettings.HitLoc.L)
        {
            HitLeft.isOn = true;
        }
        if (shipMain.lastHit == ShipSettings.HitLoc.U)
        {
            HitUp.isOn = true;
        }
        if (shipMain.lastHit == ShipSettings.HitLoc.D)
        {
            HitDown.isOn = true;
        }
        if (shipMain.lastHit == ShipSettings.HitLoc.B)
        {
            HitBack.isOn = true;
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        DoHitFlash();
        if (GameObjTracker.radarRefreshNeeded == true)
        {
            RegisterBlips();
        }

    }
}
