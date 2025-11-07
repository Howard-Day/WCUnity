using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

public class HUDRoot : MonoBehaviour
{
    public Radar srcRadar;
    public Camera hudCamera;
    public Sprite defaultBracket;
    public Sprite targetBracket;
    public Sprite lockedBracket;

    public Material bracketMat;

    public Reticle reticle;

    [SerializeField] private Vector2 nearFarClip;
    [SerializeField] private float angleClip;

    private List<BracketController> hudBrackets;
    private GameObject RootHUD;
    private ShipSettings shipMain;
    private ObjectPool<BracketController> bracketsPool;

    private void Awake()
    {
        bracketsPool = new WCObjectPool<BracketController>(CreateBracket);
    }

    // Start is called before the first frame update
    void Start()
    {
        //Find our Ship Root! 
        shipMain = gameObject.GetComponentInParent<ShipSettings>();
        RootHUD = new GameObject();
        RootHUD.name = "HUDRoot";
        RootHUD.transform.parent = gameObject.transform;
        RootHUD.transform.localPosition = Vector3.zero;
        RootHUD.transform.localScale = Vector3.one;

        RegisterBrackets();
    }

    private void OnDestroy()
    {
        bracketsPool.Dispose();
    }

    public void RegisterBrackets()
    {
        if (hudBrackets == null) hudBrackets = new List<BracketController>(20);

        for (int i = 0; i < hudBrackets.Count; i++)
        {
            BracketController bracket = hudBrackets[i];
            bracketsPool.Release(bracket);
        }
        hudBrackets.Clear();

        if (shipMain.Team == TEAM.CONFED)
        {
            MakeBrackets(GameObjTracker.Instance.KilrathiShips, srcRadar.enemyNear);
            MakeBrackets(GameObjTracker.Instance.ConfedShips, srcRadar.friendlyNear);
        }
        if (shipMain.Team == TEAM.KILRATHI)
        {
            MakeBrackets(GameObjTracker.Instance.ConfedShips, srcRadar.enemyNear);
            MakeBrackets(GameObjTracker.Instance.KilrathiShips, srcRadar.friendlyNear);
        }
        if (shipMain.Team == TEAM.PIRATE)
        {
            MakeBrackets(GameObjTracker.Instance.ConfedShips, srcRadar.enemyNear);
            MakeBrackets(GameObjTracker.Instance.KilrathiShips, srcRadar.enemyNear);
        }
        if (shipMain.Team == TEAM.NEUTRAL)
        {
            MakeBrackets(GameObjTracker.Instance.ConfedShips, srcRadar.neutralNear);
            MakeBrackets(GameObjTracker.Instance.KilrathiShips, srcRadar.neutralNear);
        }
        MakeBrackets(GameObjTracker.Instance.PirateShips, srcRadar.enemyNear);
        MakeBrackets(GameObjTracker.Instance.NeutralShips, srcRadar.neutralNear);
        MakeBrackets(GameObjTracker.Instance.EnvironmentalShips, srcRadar.envNear);
        GameObjTracker.Instance.BracketRefreshNeeded = false;
        //print("Radar Refresh is: "+ GameObjTracker.radarRefreshNeeded);

    }

    BracketController CreateBracket()
    {
        GameObject bracketObj = new GameObject();
        BracketController bracket = bracketObj.AddComponent<BracketController>();
        //RectTransform rect = bracketObj.AddComponent<RectTransform>() as RectTransform;
        bracketObj.name = "bracket";
        bracketObj.layer = 8;
        bracketObj.transform.parent = RootHUD.transform;
        bracketObj.transform.localPosition = Vector3.zero;
        bracketObj.transform.localScale = Vector3.one;
        bracket.hudCamera = hudCamera;
        bracket.clipDist = nearFarClip;
        bracket.clipAngle = angleClip;
        bracket.HUDRoot = this;
        bracket.shipMain = shipMain;
        return bracket;
    }

    void MakeBrackets(IReadOnlyList<ShipSettings> Ships, Color Color)
    {
        if (Ships != null && Ships.Count > 0 )
        {
            foreach (ShipSettings ship in Ships) //Go through a list of ships, add them 
            {
                if (ship != shipMain && !ship.IsDead && !ship.IsCloaked ) //But only if we're not looking at ourselves! Or they're not dead or cloaked. :P
                {
                    BracketController bracket = bracketsPool.Get();
                    bracket.ship = ship;
                    bracket.Color = Color;
                    //bracket.Init();
                    hudBrackets.Add(bracket);
                }
            }
        }

    }
    // Update is called once per frame
    void Update()
    {
        if (GameObjTracker.Instance.BracketRefreshNeeded)
        {
            RegisterBrackets();
        }
    }

#if UNITY_EDITOR
    private void OnGUI()
    {
        if (shipMain != null)
        {
            GUILayout.Label($"Target: {shipMain.CurrentTarget}");
            var aiPlayer = shipMain.GetComponent<AIPlayer>();
            if (aiPlayer != null)
            {
                GUILayout.Label($"Skill level: {aiPlayer.SkillSettings.SkillLevel}");
                GUILayout.Label($"AI State: {aiPlayer.ActiveAIState}");
                GUILayout.Label($"Rolling: {aiPlayer.IsRolling}");
                GUILayout.Label($"Will overshoot destination: {aiPlayer.WillOvershootDestination}");
            }
        }
    }
#endif
}
