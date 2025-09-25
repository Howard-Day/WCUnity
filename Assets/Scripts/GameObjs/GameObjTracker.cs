using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class GameObjTracker : MonoBehaviour
{
    #region STATIC
    private static GameObjTracker instance;
    public static GameObjTracker Instance
    {
        get
        {
            if (instance == null) instance = GameObject.FindAnyObjectByType<GameObjTracker>();
            return instance;
        }
    }
    public static bool HasInstance => instance != null;
    #endregion

    #region FIELDS
    [SerializeField] private int maxShipsPerSideToSpawn = 3;
    [SerializeField] private GameObject[] KilrathiSpawn;
    [SerializeField] private GameObject[] ConfedSpawn;
    [SerializeField] private GameObject[] PirateSpawn;
    [SerializeField] private GameObject[] PlayerSpawn;

    [SerializeField] private bool randomPlayerSpawn = false;

    [SerializeField] private ShipEvent shipAddedEvent = new ShipEvent();
    [SerializeField] private ShipEvent shipRemovedEvent = new ShipEvent();

    private int playerSpawnIndex = 0;
    
    private int confedKills = 0;
    private int kilrathiKills = 0;
    private int playerKills = 0;
    private int friendlyKills = 0;
    private bool playerNeedsRespawn = false;
    private GameObject oldUI;
    private int currentFrame = 0;
    private bool hasSetRandomLook = false;

    private List<ShipSettings> ships;
    private List<ShipSettings> confedShips;
    private List<ShipSettings> kilrathiShips;
    private List<ShipSettings> neutralShips;
    private List<ShipSettings> pirateShips;
    private List<ShipSettings> environmentalShips;
    private bool radarRefreshNeeded = false;
    private bool bracketRefreshNeeded = false;
    #endregion

    #region PROPERTIES
    public GameObject OldUI { get => oldUI; set => oldUI = value; }
    public bool RadarRefreshNeeded { get => radarRefreshNeeded; set => radarRefreshNeeded = value; }
    public bool BracketRefreshNeeded { get => bracketRefreshNeeded; set => bracketRefreshNeeded = value; }
    public bool PlayerNeedsRespawn { get => playerNeedsRespawn; set => playerNeedsRespawn = value; }
    public IReadOnlyList<ShipSettings> AllShips => ships;
    public IReadOnlyList<ShipSettings> ConfedShips => confedShips;
    public IReadOnlyList<ShipSettings> KilrathiShips => kilrathiShips;
    public IReadOnlyList<ShipSettings> NeutralShips => neutralShips;
    public IReadOnlyList<ShipSettings> PirateShips => pirateShips;
    public IReadOnlyList<ShipSettings> EnvironmentalShips => environmentalShips;
    public int CurrentFrame => currentFrame;
    public ShipEvent ShipAddedEvent => shipAddedEvent;
    public ShipEvent ShipRemovedEvent => shipRemovedEvent;
    #endregion

    private void Awake()
    {
        Assert.IsTrue(instance == null || instance == this);
        instance = this;

        ships = new List<ShipSettings>(40);
        const int TEAM_STARTING_CAPACITY = 20;
        confedShips = new List<ShipSettings>(TEAM_STARTING_CAPACITY);
        kilrathiShips = new List<ShipSettings>(TEAM_STARTING_CAPACITY);
        neutralShips = new List<ShipSettings>(TEAM_STARTING_CAPACITY);
        pirateShips = new List<ShipSettings>(TEAM_STARTING_CAPACITY);
        environmentalShips = new List<ShipSettings>(TEAM_STARTING_CAPACITY);
    }

    void Start()
    {
        Application.targetFrameRate = 60;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public IReadOnlyList<ShipSettings> GetTeamShips(TEAM team)
    {
        if (team == TEAM.CONFED) return confedShips;
        if (team == TEAM.KILRATHI) return kilrathiShips;
        if (team == TEAM.NEUTRAL) return neutralShips;
        if (team == TEAM.PIRATE) return pirateShips;
        if (team == TEAM.ENV) return environmentalShips;
        throw new System.ArgumentException($"Unrecognized team {team}");
    }

    public void SetRefreshNeeded()
    {
        radarRefreshNeeded = true;
        bracketRefreshNeeded = true;
    }

    public void AddShip(ShipSettings ship)
    {
        Assert.IsNotNull(ship);
        Assert.IsFalse(ships.Contains(ship));

        ships.Add(ship);
        if (ship.Team == TEAM.CONFED) confedShips.Add(ship);
        else if (ship.Team == TEAM.KILRATHI) kilrathiShips.Add(ship);
        else if (ship.Team == TEAM.PIRATE) pirateShips.Add(ship);
        else if (ship.Team == TEAM.NEUTRAL) neutralShips.Add(ship);
        else if (ship.Team == TEAM.ENV) environmentalShips.Add(ship);
        SetRefreshNeeded();

        shipAddedEvent.Invoke(ship);
    }

    public void RemoveShip(ShipSettings ship)
    {
        Assert.IsNotNull(ship);

        ships.Remove(ship);
        if (ship.Team == TEAM.CONFED) confedShips.Remove(ship);
        else if (ship.Team == TEAM.KILRATHI) kilrathiShips.Remove(ship);
        else if (ship.Team == TEAM.PIRATE) pirateShips.Remove(ship);
        else if (ship.Team == TEAM.NEUTRAL) neutralShips.Remove(ship);
        else if (ship.Team == TEAM.ENV) environmentalShips.Remove(ship);
        SetRefreshNeeded();

        shipRemovedEvent.Invoke(ship);
    }

    public ShipSettings GetShipByID(int checkID)
    {
        ShipSettings result = null;

        foreach (ShipSettings ship in ships)
        {
            if (ship.ShipID == checkID)
            {
                result = ship;
            }
        }
        return result;
    }

    public Vector3 GetAverageShipLocInRange(Vector3 refLoc, float range, int ourID)
    {
        var averageLoc = Vector3.zero;
        int foundShipCount = 0;
        //loop through all ships
        foreach (ShipSettings tarShip in ships)
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

    void SpawnExtraShips()
    {
        if (KilrathiSpawn.Length > 0 &&  kilrathiShips.Count < maxShipsPerSideToSpawn && currentFrame % 240 == 0)
        {
            int spawnIndex = Random.Range(0, KilrathiSpawn.Length);
            GameObject ship = Instantiate(KilrathiSpawn[spawnIndex], Random.onUnitSphere * 1200f, Quaternion.identity);
            ship.name = KilrathiSpawn[spawnIndex].name;
            radarRefreshNeeded = true;
            bracketRefreshNeeded = true;
        }
        if (ConfedSpawn.Length > 0 &&  confedShips.Count < maxShipsPerSideToSpawn && currentFrame % 240 == 0)
        {
            int spawnIndex = Random.Range(0, ConfedSpawn.Length);
            GameObject ship = Instantiate(ConfedSpawn[spawnIndex], Random.onUnitSphere * 1200f, Quaternion.identity);
            ship.name = ConfedSpawn[spawnIndex].name;
            radarRefreshNeeded = true;
            bracketRefreshNeeded = true;
        }
        if (PirateSpawn.Length > 0 && pirateShips.Count < maxShipsPerSideToSpawn && currentFrame % 240 == 0)
        {
            int spawnIndex = Random.Range(0, PirateSpawn.Length);
            GameObject ship = Instantiate(PirateSpawn[spawnIndex], Random.onUnitSphere * 1200f, Quaternion.identity);
            ship.name = PirateSpawn[spawnIndex].name;
            radarRefreshNeeded = true;
            bracketRefreshNeeded = true;
        }

        if (playerNeedsRespawn)
        {
            if (oldUI != null)
            {
                Destroy(oldUI);
            }
            if (PlayerSpawn.Length > 0)
            {
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
    }
    // Update is called once per frame
    void Update()
    {
        currentFrame++;
        SpawnExtraShips();
        if (playerNeedsRespawn)
        {
            print("Trying to respawn Player!");
        }

    }
}
