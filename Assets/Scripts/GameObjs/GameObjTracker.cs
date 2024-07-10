using System.Collections.Generic;
using UnityEngine;

public class GameObjTracker : MonoBehaviour
{
    public float speedMultiplier = 1f;
    public int MaxShipsPerSideToSpawn = 1;
    static public List<ShipSettings> Ships;
    static public List<ShipSettings> ConfedShips;
    static public List<ShipSettings> KilrathiShips;
    static public List<ShipSettings> NeutralShips;
    static public List<ShipSettings> PirateShips;
    static public List<ShipSettings> Environmental;
    static public bool radarRefreshNeeded = false;
    static public bool bracketRefreshNeeded = false;
    public GameObject[] KilrathiSpawn;
    public GameObject[] ConfedSpawn;
    public GameObject[] PirateSpawn;
    public bool randomPlayerSpawn = false;
    [HideInInspector] public int playerSpawnIndex = 0;
    public GameObject[] PlayerSpawn;
    public static int confedKills = 0;
    public static int kilrathiKills = 0;
    public static int playerKills = 0;
    public static int friendlyKills = 0;
    public static bool playerNeedsRespawn = false;
    public static GameObject oldUI;
    // Start is called before the first frame update
    [HideInInspector] public static int frames = 0;
    static public GameObject Tracker;
    static bool hasSetRandomLook = false;
    static Vector3 averageLoc = Vector3.zero;

    void Start()
    {
        Tracker = gameObject;
        RegisterAllShips();
        RegisterTeams();
    }

    public static void RegisterAllShips()
    {
        Ships = new List<ShipSettings>();
        foreach (Transform child in GameObject.FindGameObjectWithTag("GamePlayObjs").transform)
        {
            ShipSettings ship = child.GetComponent<ShipSettings>();
            if (ship != null && !ship.isDead)
            {
                Ships.Add(ship);
            }
        }
        //print("GameObj Tracker: "+ Ships.Count + " ships found!");
    }


    public static void CheckDestroyedEnemies()
    {
        foreach (ShipSettings ship in Ships)
        {
            if (ship.isDead)
            {
                radarRefreshNeeded = true;
                bracketRefreshNeeded = true;
            }
        }

    }
    public static ShipSettings GetShipByID(int checkID)
    {
        ShipSettings result = null;

        foreach (ShipSettings tarShip in Ships)
        {
            if (tarShip.ShipID == checkID)
            {
                result = tarShip;
            }
        }
        return result;
    }

    public static Vector3 GetAverageShipLocInRange(Vector3 refLoc, float range, int ourID)
    {
        //reset Average location
        averageLoc = Vector3.zero;
        int foundShipCount = 0;
        //loop through all ships
        foreach (ShipSettings tarShip in Ships)
        {
            //San check and if it's within range, add the location to the list, and count how many we've found, and it's not us
            if (tarShip != null && Vector3.Distance(tarShip.transform.position, refLoc) < range && tarShip.ShipID != ourID)
            {
                averageLoc += tarShip.transform.position;
                foundShipCount++;
            }
        }
        //if we didn't find any ships, look towards a random point, but only if we haven't found one already!
        if (foundShipCount == 0 && hasSetRandomLook == false)
        {
            averageLoc = refLoc + Random.onUnitSphere * range;
            hasSetRandomLook = true;
        }
        //otherwise average the point of interest
        if (foundShipCount != 0)
        {
            averageLoc = averageLoc / foundShipCount;
            hasSetRandomLook = false;
        }
        return averageLoc;        
    }

    public static void RegisterTeams()
    {
        if (Ships.Count == 0)
            return;

        ConfedShips = new List<ShipSettings>();
        KilrathiShips = new List<ShipSettings>();
        NeutralShips = new List<ShipSettings>();
        PirateShips = new List<ShipSettings>();
        Environmental = new List<ShipSettings>();

        foreach (ShipSettings ship in Ships)
        {
            if (ship.AITeam == ShipSettings.TEAM.CONFED)
                ConfedShips.Add(ship);
            if (ship.AITeam == ShipSettings.TEAM.KILRATHI)
                KilrathiShips.Add(ship);
            if (ship.AITeam == ShipSettings.TEAM.NEUTRAL)
                NeutralShips.Add(ship);
            if (ship.AITeam == ShipSettings.TEAM.PIRATE)
                PirateShips.Add(ship);
            if (ship.AITeam == ShipSettings.TEAM.ENV)
                Environmental.Add(ship);
        }
        //print("GameObj Tracker: Found "+ ConfedShips.Count + " Confed Ships, "+ KilrathiShips.Count + " Kilrathi Ships, "+ NeutralShips.Count + " Neutral Ships, and "+ PirateShips.Count + " Pirate Ships!");
        radarRefreshNeeded = true;
        bracketRefreshNeeded = true;
        //print("Radar Refresh is: "+ radarRefreshNeeded);
    }

    void SpawnExtraShips()
    {
        if (KilrathiShips.Count < MaxShipsPerSideToSpawn && frames % 2400 == 0)
        {
            int spawnIndex = Random.Range(0, KilrathiSpawn.Length);
            GameObject ship = Instantiate(KilrathiSpawn[spawnIndex], Random.onUnitSphere * 1200f, Quaternion.identity);
            ship.name = KilrathiSpawn[spawnIndex].name;
            radarRefreshNeeded = true;
            bracketRefreshNeeded = true;
        }
        if (ConfedShips.Count < MaxShipsPerSideToSpawn && frames % 2400 == 0)
        {
            int spawnIndex = Random.Range(0, ConfedSpawn.Length);
            GameObject ship = Instantiate(ConfedSpawn[spawnIndex], Random.onUnitSphere * 1200f, Quaternion.identity);
            ship.name = ConfedSpawn[spawnIndex].name;
            radarRefreshNeeded = true;
            bracketRefreshNeeded = true;
        }
        if (PirateShips.Count < MaxShipsPerSideToSpawn && frames % 2400 == 0)
        {
            int spawnIndex = Random.Range(0, PirateSpawn.Length);
            GameObject ship = Instantiate(PirateSpawn[spawnIndex], Random.onUnitSphere * 1200f, Quaternion.identity);
            ship.name = PirateSpawn[spawnIndex].name;
            radarRefreshNeeded = true;
            bracketRefreshNeeded = true;
        }
        if (playerNeedsRespawn && frames % 2400 == 0)
        {
            Destroy(oldUI);
            int RandomIndex = Mathf.RoundToInt(Random.Range(0, PlayerSpawn.Length));
            if (!randomPlayerSpawn)
            {
                RandomIndex = playerSpawnIndex;
            }
            GameObject ship = Instantiate(PlayerSpawn[RandomIndex], Random.onUnitSphere * 200f, Quaternion.identity);
            ship.name = PlayerSpawn[RandomIndex].name;
            radarRefreshNeeded = true;
            bracketRefreshNeeded = true;
            playerNeedsRespawn = false;
        }
    }
    // Update is called once per frame
    void Update()
    {
        frames++;
        CheckDestroyedEnemies();
        SpawnExtraShips();
    }
}
