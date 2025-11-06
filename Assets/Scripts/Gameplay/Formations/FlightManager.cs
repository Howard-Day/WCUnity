using OneManEscapePlan.Common;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class FlightManager : MonoBehaviour
{
    private static FlightManager _instance;
    public static FlightManager Instance
    {
        get
        {
            if (_instance == null) _instance = GameObject.FindAnyObjectByType<FlightManager>();
            return _instance;
        }
    }

    [SerializeField, NonNull] private List<FormationTemplate> formationTemplates;
    [SerializeField] private bool verboseLogging;

    private List<Flight> confedFlights = new List<Flight>(10);
    private List<Flight> kilrathiFlights = new List<Flight>(10);
    private List<Flight> pirateFlights = new List<Flight>(10);
    private List<Flight> neutralFlights = new List<Flight>(10);

    void Awake()
    {
        Assert.IsFalse(formationTemplates.Count == 0);
        Assert.IsTrue(_instance == null || _instance == this);
        _instance = this;
    }

    public void OnShipAdded(ShipSettings ship)
    {
        Assert.IsNotNull(ship);
        // TODO: AI should only have a percent chance to join a Flight within a reasonable distance
        if (ship.Settings.CanJoinFormations) TryJoinFlight(ship, float.MaxValue, true, true);
    }

    /// <summary>
    /// Try to join a Flight with the given criteria.
    /// </summary>
    /// <param name="ship"></param>
    /// <param name="maxDistance">Only join a Flight if within this distance (measured from the Flight's current leader)</param>
    /// <param name="canCreateNewFlight">Whether we can create a new Flight if no valid formation is available</param>
    /// <param name="snapToPosition">Whether to teleport the ship to its default position in the Flight. Should only be <c>true</c> if the ship just spawned.</param>
    /// <returns></returns>
    public Flight TryJoinFlight(ShipSettings ship, float maxDistance, bool canCreateNewFlight, bool snapToPosition)
    {
        Assert.IsNotNull(ship);
        Assert.IsTrue(ship.Settings.CanJoinFormations);
        var flights = GetFlights(ship.Team);
        if (flights == null)
        {
            OMEPLogger.Log(this, $"Flights are not available for team {ship.Team}");
            return null;
        }

        return TryJoinFlight(ship, flights, maxDistance, canCreateNewFlight, snapToPosition);
    }

    /// <summary>
    /// Try to join the closest Flight with the given criteria.
    /// </summary>
    /// <param name="ship"></param>
    /// <param name="maxDistance">Only join a Flight if within this distance (measured from the Flight's current leader)</param>
    /// <param name="canCreateNewFlight">Whether we can create a new Flight if no valid Flight is available</param>
    /// <returns></returns>
    public Flight TryJoinClosestFlight(ShipSettings ship, float maxDistance, bool canCreateNewFlight)
    {
        Assert.IsNotNull(ship);
        Assert.IsTrue(ship.Settings.CanJoinFormations);
        var flights = GetFlights(ship.Team);
        if (flights == null)
        {
            OMEPLogger.Log(this, $"Flights are not available for team {ship.Team}");
            return null;
        }

        return TryJoinClosestFlight(ship, flights, maxDistance, canCreateNewFlight);
    }

    private List<Flight> GetFlights(TEAM team)
    {
        if (team == TEAM.CONFED)
        {
            return confedFlights;
        }
        else if (team == TEAM.KILRATHI)
        {
            return kilrathiFlights;
        }
        else if (team == TEAM.PIRATE)
        {
            return pirateFlights;
        }
        else if (team == TEAM.NEUTRAL)
        {
            return neutralFlights;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Try to join a Flight with the given criteria.
    /// </summary>
    /// <param name="ship"></param>
    /// <param name="currentFlights">All current Flight for the ship's team</param>
    /// <param name="maxDistance">Only join a Flight if within this distance (measured from the Flight's current leader)</param>
    /// <param name="canCreateNewFlight">Whether we can create a new Flight if no valid Flight is available</param>
    /// <param name="snapToPosition">Whether to teleport the ship to its default position in the Flight. Should only be <c>true</c> if the ship just spawned.</param>
    /// <returns></returns>
    private Flight TryJoinFlight(ShipSettings ship, List<Flight> currentFlights, float maxDistance, bool canCreateNewFlight, bool snapToPosition)
    {
        if (verboseLogging) OMEPLogger.Log(this, $"{ship} {maxDistance} {canCreateNewFlight} {snapToPosition}");
        Assert.IsNotNull(ship);
        Assert.IsNotNull(currentFlights);

        foreach (var flight in currentFlights)
        {
            var leader = flight.Leader;

            // We can join a Flight if it has no leader or if it is within the search distance
            if (leader == null || Vector3.Distance(ship.transform.position, leader.transform.position) < maxDistance)
            {
                if (TryJoinFlight(ship, flight, snapToPosition))
                {
                    return flight;
                }
            }
        }

        if (canCreateNewFlight)
        {
            return CreateFlightFor(ship);
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Try to join the closest Flight with the given criteria.
    /// </summary>
    /// <param name="ship"></param>
    /// <param name="currentFlights">All current Flights for the ship's team</param>
    /// <param name="maxDistance">Only join a Flight if within this distance (measured from the Flight's current leader)</param>
    /// <param name="canCreateNewFlight">Whether we can create a new Flight if no valid Flight is available</param>
    /// <returns></returns>
    private Flight TryJoinClosestFlight(ShipSettings ship, List<Flight> currentFlights, float maxDistance, bool canCreateNewFlight)
    {
        if (verboseLogging) OMEPLogger.Log(this, $"{ship} {maxDistance} {canCreateNewFlight}");
        Assert.IsNotNull(ship);
        Assert.IsNotNull(currentFlights);

        float bestDistance = maxDistance;
        Flight bestResult = null;

        foreach (var flight in currentFlights)
        {
            var leader = flight.Leader;

            float distance = Vector3.Distance(ship.transform.position, leader.transform.position);
            if (distance < bestDistance && !flight.IsFull)
            {
                bestDistance = distance;
                bestResult = flight;
            }
        }

        if (bestResult != null && TryJoinFlight(ship, bestResult, false))
        {
            return bestResult;
        }
        if (canCreateNewFlight)
        {
            return CreateFlightFor(ship);
        }

        return null;
    }

    /// <summary>
    /// Try to add the given ship to the given Flight.
    /// </summary>
    /// <param name="ship"></param>
    /// <param name="flight"></param>
    /// <param name="snapToPosition"></param>
    /// <returns><c>true</c> if joined successfully, <c>false</c> if the Flight was full.</returns>
    private bool TryJoinFlight(ShipSettings ship, Flight flight, bool snapToPosition)
    {
        if (verboseLogging) OMEPLogger.Log(this, $"{ship} {flight} {snapToPosition}");
        Assert.IsNotNull(ship);
        Assert.IsNotNull(flight);

        int slotIndex = flight.TryAddShip(ship);
        if (slotIndex >= 0)
        {
            if (verboseLogging) OMEPLogger.Log(this, "Joined Flight!");
            ship.Flight = flight;
            if (snapToPosition)
            {
                var pose = flight.GetSlotPose(slotIndex);
                ship.transform.position = pose.position;
                ship.transform.rotation = pose.rotation;
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// Create a new Flight on the given ship's team, and add the ship
    /// to it as leader.
    /// </summary>
    /// <param name="ship"></param>
    /// <returns></returns>
    private Flight CreateFlightFor(ShipSettings ship)
    {
        if (verboseLogging) OMEPLogger.Log(this, ship);
        Assert.IsNotNull(ship);
        var template = formationTemplates.GetRandom();
        var flight = new Flight(ship.Team, template);
        flight.VerboseLogging = verboseLogging;
        flight.SetShipAt(0, ship);
        ship.Flight = flight;

        var teamFlights = GetFlights(ship.Team);
        teamFlights.Add(flight);

        return flight;
    }
}
