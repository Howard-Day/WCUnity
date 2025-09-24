using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI; // Required when Using UI elements.

public class Radar : MonoBehaviour
{
    #region FIELDS
    ShipSettings shipMain;
    [SerializeField] private Vector2 nearFarClip;
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

    [SerializeField] private Toggle HitFore;
    [SerializeField] private Toggle HitRight;
    [SerializeField] private Toggle HitLeft;
    [SerializeField] private Toggle HitUp;
    [SerializeField] private Toggle HitDown;
    [SerializeField] private Toggle HitBack;

    private List<BlipController> radarBlips;
    private GameObject blipRoot;
    private ObjectPool<BlipController> blipPool;

    #endregion
    private void Awake()
    {
        blipPool = new WCObjectPool<BlipController>(CreateBlip);
    }

    // Start is called before the first frame update
    void Start()
    {   
        //Find our Ship Root! 
        shipMain = (ShipSettings)gameObject.GetComponentInParent<ShipSettings>();
        blipRoot = new GameObject();
        blipRoot.name = "BlipRoot";
        blipRoot.transform.parent = gameObject.transform;
        blipRoot.transform.localPosition = Vector3.zero;
        blipRoot.transform.localScale = Vector3.one;
        RegisterBlips();
        shipMain.lastHit = ShipSettings.HitLoc.NULL;

        HitFore.isOn = false;
        HitRight.isOn = false;
        HitLeft.isOn = false;
        HitUp.isOn = false;
        HitDown.isOn = false;
        HitBack.isOn = false;
    }

    private void OnDestroy()
    {
        blipPool.Dispose();
    }

    public void RegisterBlips()
    {
        if (radarBlips == null) radarBlips = new List<BlipController>(20);

        for (int i = 0; i < radarBlips.Count; i++)
        {
            BlipController blip = radarBlips[i];
            blipPool.Release(blip);
        }
        radarBlips.Clear();
        
        if (shipMain.Team == TEAM.CONFED)
        {
            MakeBlips(GameObjTracker.Instance.KilrathiShips, enemyNear, enemyFar);
            MakeBlips(GameObjTracker.Instance.ConfedShips, friendlyNear, friendlyFar);
        }
        if (shipMain.Team == TEAM.KILRATHI)
        {
            MakeBlips(GameObjTracker.Instance.ConfedShips, enemyNear, enemyFar);
            MakeBlips(GameObjTracker.Instance.KilrathiShips, friendlyNear, friendlyFar);
        }
        if (shipMain.Team == TEAM.PIRATE)
        {
            MakeBlips(GameObjTracker.Instance.ConfedShips, enemyNear, enemyFar);
            MakeBlips(GameObjTracker.Instance.KilrathiShips, enemyNear, enemyFar);
        }
        if (shipMain.Team == TEAM.NEUTRAL)
        {
            MakeBlips(GameObjTracker.Instance.ConfedShips, neutralNear, neutralFar);
            MakeBlips(GameObjTracker.Instance.KilrathiShips, neutralNear, neutralFar);
        }
        MakeBlips(GameObjTracker.Instance.PirateShips, enemyNear, enemyFar);
        MakeBlips(GameObjTracker.Instance.NeutralShips, neutralNear, neutralFar);
        MakeBlips(GameObjTracker.Instance.EnvironmentalShips, envNear, envFar);
        GameObjTracker.Instance.RadarRefreshNeeded = false;
        //print("Radar Refresh is: "+ GameObjTracker.radarRefreshNeeded);
    }

    void MakeBlips(IReadOnlyList<ShipSettings> Ships, Color Near, Color Far)
    {        
        if (Ships != null && Ships.Count > 0)
        {
            foreach (ShipSettings ship in Ships) //Go through a list of ships, add them 
            {
                if (ship != shipMain && !ship.IsDead) //But only if we're not looking at ourselves! Or they're not dead. :P
                {
                    BlipController blip = blipPool.Get();
                    blip.ship = ship;
                    blip.Near = Near;
                    blip.Far = Far;
                    radarBlips.Add(blip);
                }
            }
        }
    }

    BlipController CreateBlip()
    {
        GameObject blipObj = new GameObject();
        BlipController blip = blipObj.AddComponent<BlipController>() as BlipController;
        blipObj.name = "blip";
        blipObj.transform.parent = blipRoot.transform;
        blipObj.transform.localPosition = Vector3.zero;
        blipObj.transform.localScale = Vector3.one;
        blip.clipDist = nearFarClip;
        blip.radarRoot = this;
        blip.shipMain = shipMain;
        return blip;
    }

    void DoHitFlash() //Show incoming fire on the radar! 
    {
        if (GameObjTracker.Instance.CurrentFrame % 120 == 0 || Camera.main == null) // Every 2 sec (approx) reset the hit history, or if the cockpit has been destroyed. 
        {
            shipMain.lastHit = ShipSettings.HitLoc.NULL;
        }
        if (GameObjTracker.Instance.CurrentFrame % 30 == 0 || Camera.main == null) //every sec (approx) reset the hit flashes to off
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
        if (GameObjTracker.Instance.RadarRefreshNeeded)
        {
            RegisterBlips();
        }

    }
}
