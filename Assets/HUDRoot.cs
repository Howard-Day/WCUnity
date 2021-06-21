using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HUDRoot : MonoBehaviour
{
    public Radar srcRadar;
    public Camera hudCamera;
    public Sprite defaultBracket;
    public Sprite targetBracket;
    public Sprite lockedBracket;

    public Material bracketMat;


    ShipSettings shipMain;
    public Vector2 nearFarClip;
    public float angleClip;
    [HideInInspector]
    public List<BracketController> HUDBrackets;
    GameObject RootHUD;



    // Start is called before the first frame update
    void Start()
    {
        //Find our Ship Root! 
        HUDBrackets = new List<BracketController>();
        shipMain = (ShipSettings)gameObject.GetComponentInParent<ShipSettings>();
        RootHUD = new GameObject();
        RootHUD.name = "HUDRoot";
        RootHUD.transform.parent = gameObject.transform;
        RootHUD.transform.localPosition = Vector3.zero;
        RootHUD.transform.localScale = Vector3.one;
        RegisterBrackets();
    }



    //radarRefreshNeeded
    public void RegisterBrackets()
    {
        foreach (BracketController bracket in HUDBrackets)
        {
            Destroy(bracket.gameObject);
        }
        HUDBrackets = new List<BracketController>();

        if (shipMain.AITeam == ShipSettings.TEAM.CONFED)
        {
            MakeBrackets(GameObjTracker.KilrathiShips, srcRadar.enemyNear);
            MakeBrackets(GameObjTracker.ConfedShips, srcRadar.friendlyNear);
        }
        if (shipMain.AITeam == ShipSettings.TEAM.KILRATHI)
        {
            MakeBrackets(GameObjTracker.ConfedShips, srcRadar.enemyNear);
            MakeBrackets(GameObjTracker.KilrathiShips, srcRadar.friendlyNear);
        }
        if (shipMain.AITeam == ShipSettings.TEAM.PIRATE)
        {
            MakeBrackets(GameObjTracker.ConfedShips, srcRadar.enemyNear);
            MakeBrackets(GameObjTracker.KilrathiShips, srcRadar.enemyNear);
        }
        if (shipMain.AITeam == ShipSettings.TEAM.NEUTRAL)
        {
            MakeBrackets(GameObjTracker.ConfedShips, srcRadar.neutralNear);
            MakeBrackets(GameObjTracker.KilrathiShips, srcRadar.neutralNear);
        }
        MakeBrackets(GameObjTracker.PirateShips, srcRadar.enemyNear);
        MakeBrackets(GameObjTracker.NeutralShips, srcRadar.neutralNear);
        MakeBrackets(GameObjTracker.Environmental, srcRadar.envNear);
        GameObjTracker.bracketRefreshNeeded = false;
        //print("Radar Refresh is: "+ GameObjTracker.radarRefreshNeeded);

    }

    void MakeBrackets(List<ShipSettings> Ships, Color Color)
    {
        foreach (ShipSettings ship in Ships) //Go through a list of ships, add them 
        {
            if (ship != shipMain) //But only if we're not looking at ourselves! 
            {
                GameObject bracketObj = new GameObject();
                BracketController bracket = bracketObj.AddComponent<BracketController>() as BracketController;
                //RectTransform rect = bracketObj.AddComponent<RectTransform>() as RectTransform;
                bracketObj.name = "bracket";
                bracketObj.layer = 8;
                bracketObj.transform.parent = RootHUD.transform;
                bracketObj.transform.localPosition = Vector3.zero;
                bracketObj.transform.localScale = Vector3.one;
                bracket.hudCamera = hudCamera;
                bracket.ship = ship;
                bracket.clipDist = nearFarClip;
                bracket.clipAngle = angleClip;
                bracket.Color = Color;
                bracket.HUDRoot = gameObject.GetComponent<HUDRoot>();
                bracket.shipMain = shipMain;
                HUDBrackets.Add(bracket);
            }
        }

    }



    // Update is called once per frame
    void Update()
    {
        if (GameObjTracker.bracketRefreshNeeded == true)
        {
            RegisterBrackets();
        }
    }
}
