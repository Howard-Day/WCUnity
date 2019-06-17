using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameObjTracker : MonoBehaviour
{
    public float speedMultiplier = 1f;
    public List<ShipSettings> Ships;
    public List<ShipSettings> ConfedShips;
    public List<ShipSettings> KilrathiShips;
    public List<ShipSettings> NeutralShips;
    public List<ShipSettings> PirateShips;
    // Start is called before the first frame update
    [HideInInspector] public int frames;
    void Start()
    {
        RegisterAllShips();
        RegisterTeams();
    }
    public void RegisterAllShips()
    {
        Ships = new List<ShipSettings>(); 
        foreach (Transform child in transform)
        {
            ShipSettings ship = child.GetComponent<ShipSettings>();
            if(ship != null)
            {
                Ships.Add(ship);
            }
        }
        print("GameObj Tracker: "+ Ships.Count + " ships found!");
    }
    public void RegisterTeams()
    {
        if (Ships.Count == 0)
            return;

        ConfedShips = new List<ShipSettings>();
        KilrathiShips = new List<ShipSettings>();
        NeutralShips = new List<ShipSettings>();
        PirateShips = new List<ShipSettings>();
        
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
        }
        print("GameObj Tracker: Found "+ ConfedShips.Count + " Confed Ships, "+ KilrathiShips.Count + " Kilrathi Ships, "+ NeutralShips.Count + " Neutral Ships, and "+ PirateShips.Count + " Pirate Ships!");

    }

    // Update is called once per frame
    void Update()
    {
        frames ++;
    }
}
