﻿using System.Collections.Generic;
using UnityEngine;

public class GameObjTracker : MonoBehaviour
{
    public float speedMultiplier = 1f;
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
    public GameObject PlayerSpawn;
    // Start is called before the first frame update
    [HideInInspector] public static int frames;
    static public GameObject Tracker;

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
            if(ship != null)
            {
                Ships.Add(ship);
            }
        }
        //print("GameObj Tracker: "+ Ships.Count + " ships found!");
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
        if(KilrathiShips.Count < 3)
        {
            int spawnIndex = Random.Range(0,KilrathiSpawn.Length);
            GameObject ship = Instantiate(KilrathiSpawn[spawnIndex], Random.onUnitSphere*1200f,Quaternion.identity);
            ship.name = KilrathiSpawn[spawnIndex].name;
        }        
        if(ConfedShips.Count < 3)
        {
            int spawnIndex = Random.Range(0,ConfedSpawn.Length);
            GameObject ship = Instantiate(ConfedSpawn[Random.Range(0,1)], Random.onUnitSphere*1200f,Quaternion.identity);
            ship.name = ConfedSpawn[spawnIndex].name;
        }
        if(Camera.main == null)
        {
            GameObject ship = Instantiate(PlayerSpawn, Random.onUnitSphere*200f,Quaternion.identity);
            ship.name = PlayerSpawn.name;
        }
    }
    // Update is called once per frame
    void Update()
    {
        frames ++;
        SpawnExtraShips();
    
    }
}
