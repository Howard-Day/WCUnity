using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

/// <summary>
/// A Flight consists of one or more ships that fly together.
/// Flights will fly in formation using a 
/// <see cref="FormationTemplate"/>, but may break formation
/// during combat. A Flight has one ship designated as the 
/// Leader. Ships can be added to or removed from the flight
/// at any time.
/// 
/// While flying in formation, other ships in the flight
/// attempt to position themselves relative to the leader
/// using the formation layout defined in the template. 
/// </summary>
public class Flight : IEnumerable<ShipSettings>
{
    private static int nextID = 0;

    #region FIELDS
    private int id;
    private TEAM team;
    private FormationTemplate template;
    private List<ShipSettings> ships;
    private int leaderIndex = 0;
    private Color color;
    private float scale = 1f;
    #endregion

    #region CONSTRUCTORS
    private void Init(TEAM team, FormationTemplate template, bool verboseLogging = false)
    {
        VerboseLogging = verboseLogging;
        this.team = team;
        color = UnityEngine.Random.ColorHSV(0, 1, .75f, 1, 1, 1);

        id = nextID;
        if (nextID == int.MaxValue) nextID = 0;
        else nextID++;

        this.Template = template;
    }

    public Flight(TEAM team, FormationTemplate template, bool verboseLogging = false)
    {
        this.ships = new List<ShipSettings>(template.MaxShips);
        Init(team, template, verboseLogging);
    }

    public Flight(TEAM team, FormationTemplate template, List<ShipSettings> ships, bool verboseLogging = false)
    {
        this.ships = ships ?? throw new ArgumentNullException(nameof(ships));
        Init(team, template, verboseLogging);
    }
    #endregion

    #region PROPERTIES
    public bool VerboseLogging { get; set; }

    public int ID => id;

    public TEAM Team => team;
    /// <summary>
    /// Currently only used for visualization in the Editor.
    /// </summary>
    public Color Color { get => color; set => color = value; }

    /// <summary>
    /// Multiplier used to scale the spacing of the template.
    /// </summary>
    public float Scale { 
        get => scale; 
        set
        {
            Assert.IsFalse(value <= 0);
            scale = value;
        }
    }

    public FormationTemplate Template
    {
        get => template;
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
            }
            else
            {
                while (ships.Count < template.MaxShips)
                {
                    ships.Add(null);
                }
            }
        }
    }

    public ShipSettings Leader
    {
        get
        {
            if (leaderIndex < 0) return null;
            return ships[leaderIndex];
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

    public int CurrentShipCount
    {
        get
        {
            int count = 0;
            foreach (ShipSettings ship in ships)
            {
                if (ship != null) count++;
            }
            return count;
        }
    }

    public int MaxShipCount => template.MaxShips;

    public bool IsFull => !ships.Contains(null);
    public bool IsEmpty => CurrentShipCount == 0;
    #endregion

    public void SetShipAt(int index, ShipSettings ship)
    {
        if (VerboseLogging) OMEPLogger.Log(this, $"{index} {ship}");
        if (index < 0 || index >= ships.Count) throw new System.ArgumentOutOfRangeException(nameof(index), $"{index} is out of range 0 - {ships.Count - 1}");
        if (ship != null && ship.Team != team) throw new System.InvalidOperationException($"Cannot add {ship} on team {ship.Team} to formation for team {team}");

        ships[index] = ship;

        if (leaderIndex < 0 || (ship == null && leaderIndex == index))
        {
            FindNewLeader();
        }
    }

    public ShipSettings GetShipAt(int index)
    {
        if (index < 0 || index >= ships.Count) throw new System.ArgumentOutOfRangeException(nameof(index), $"{index} is out of range 0 - {ships.Count - 1}");
        return ships[index];
    }

    /// <summary>
    /// Try adding the given ship to the first empty slot.
    /// </summary>
    /// <param name="ship"></param>
    /// <returns>The index of the slot that the ship was added to,
    /// or <c>-1</c> if no slots were available.</returns>
    public int TryAddShip(ShipSettings ship)
    {
        Assert.IsNotNull(ship);
        for (int i = 0; i < ships.Count; i++)
        {
            if (ships[i] == null)
            {
                SetShipAt(i, ship);
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Remove the given ship from the Flight, if present.
    /// </summary>
    /// <param name="ship"></param>
    /// <returns><c>true</c> if removed, <c>false</c> if not found in
    /// the Flight.</returns>
    public bool RemoveShip(ShipSettings ship)
    {
        Assert.IsNotNull(ship);
        for (int i = 0; i < ships.Count; i++)
        {
            if (ships[i] == ship)
            {
                SetShipAt(i, null);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Get the world position and rotation of the slot with the
    /// given index, determined relative to the pose of the leader
    /// using the formation template.
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public Pose GetSlotPose(int index)
    {
        if (index < 0 || index >= ships.Count) throw new System.ArgumentOutOfRangeException(nameof(index), $"{index} is out of range 0 - {ships.Count - 1}");
        if (leaderIndex < 0) throw new InvalidOperationException("Can't get slot pose for formation with no leader");

        var leader = ships[leaderIndex];
        if (index == leaderIndex)
        {
            return new Pose(leader.transform.position, leader.transform.rotation);
        }

        var localOffset = template.GetLocalOffset(leaderIndex, index) * scale;
        Vector3 worldPosition = leader.transform.TransformPoint(localOffset);
        var worldRotation = leader.transform.rotation;
        return new Pose(worldPosition, worldRotation);
    }

    public Pose GetSlotPose(ShipSettings ship)
    {
        for (int i = 0; i < ships.Count; i++)
        {
            if (ships[i] == ship)
            {
                return GetSlotPose(i);
            }
        }
        throw new System.ArgumentException($"{ship} is not a member of formation {this}");
    }

    private void FindNewLeader()
    {
        // TODO: pick leader by skill level?
        for (int i = 0; i < ships.Count; i++)
        {
            if (ships[i] != null)
            {
                leaderIndex = i;
                return;
            }
        }

        leaderIndex = -1;
    }

    public override string ToString()
    {
        return $"{team} {template.DisplayName} formation";
    }

    public FlightShipsEnumerator GetEnumerator()
    {
        return new FlightShipsEnumerator(this);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator<ShipSettings> IEnumerable<ShipSettings>.GetEnumerator()
    {
        return GetEnumerator();
    }

    public struct FlightShipsEnumerator : IEnumerator<ShipSettings>
    {
        private Flight formation;
        private int currentIndex;

        public FlightShipsEnumerator(Flight formation)
        {
            this.formation = formation;
            this.currentIndex = -1;
        }

        public ShipSettings Current {
            get
            {
                if (currentIndex < 0 || currentIndex >= formation.MaxShipCount)
                {
                    return null;
                }
                return formation.GetShipAt(currentIndex);
            }
        }

        object IEnumerator.Current => Current;

        public void Dispose()
        {
            formation = null;
        }

        public bool MoveNext()
        {
            currentIndex++;
            return currentIndex < formation.MaxShipCount;
        }

        public void Reset()
        {
            currentIndex = 0;
        }
    }
}