using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Formation
{
    #region FIELDS
    private TEAM team;
    private FormationTemplate template;
    private List<ShipSettings> ships;
    private int leaderIndex = 0;
    #endregion

    #region CONSTRUCTORS
    public Formation(TEAM team, FormationTemplate template)
    {
        this.team = team;
        this.ships = new List<ShipSettings>(template.MaxShips);
        this.Template = template;
    }

    public Formation(TEAM team, FormationTemplate template, List<ShipSettings> ships)
    {
        this.team = team;
        this.ships = ships ?? throw new ArgumentNullException(nameof(ships));
        this.Template = template;
    }
    #endregion

    #region PROPERTIES
    public TEAM Team => team;

    public FormationTemplate Template { 
        get =>  template; 
        set
        {
            if (value == null) throw new ArgumentException(nameof(value));
            template = value;

            if (ships.Count > template.MaxShips)
            {
                OMEPLogger.Log(this, $"Trimming formation from {ships.Count} to {template.MaxShips}");
                while (ships.Count > template.MaxShips)
                {
                    ships.RemoveAt(ships.Count - 1);
                }
            } else
            {
                while (ships.Count < template.MaxShips)
                {
                    ships.Add(null);
                }
            }
        }
    }

    public int LeaderIndex
    {
        get => leaderIndex;
        set
        {
            if (value < 0 || value >= ships.Count) throw new System.ArgumentOutOfRangeException(nameof(value), $"{value} is out of range 0 - {ships.Count - 1}");
            leaderIndex = value;
        }
    }

    public bool IsFull => !ships.Contains(null);
    #endregion

    public void SetShipAt(ShipSettings ship, int index)
    {
        if (index < 0 || index >= ships.Count) throw new System.ArgumentOutOfRangeException(nameof(index), $"{index} is out of range 0 - {ships.Count - 1}");
        if (ship != null && ship.Team != team) throw new System.InvalidOperationException($"Cannot add {ship} on team {ship.Team} to formation for team {team}");

        ships[index] = ship;

        if (ship == null && leaderIndex == index)
        {
            FindNewLeader();
        }
    }

    private void FindNewLeader()
    {
        for (int i = 0; i < ships.Count; i++)
        {
            if (ships[i] != null)
            {
                leaderIndex = i;
                break;
            }
        }
    }
}