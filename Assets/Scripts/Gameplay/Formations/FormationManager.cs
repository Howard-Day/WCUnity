using OneManEscapePlan.Common;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class FormationManager : MonoBehaviour
{
    [SerializeField, NonNull] private List<FormationTemplate> formationTemplates;
    [SerializeField] private bool verboseLogging;

    private List<Formation> confedFormations = new List<Formation>(10);
    private List<Formation> kilrathiFormations = new List<Formation>(10);
    private List<Formation> pirateFormations = new List<Formation>(10);
    private List<Formation> neutralFormations = new List<Formation>(10);

    void Awake()
    {
        Assert.IsFalse(formationTemplates.Count == 0);
    }

    public void OnShipAdded(ShipSettings ship)
    {
        Assert.IsNotNull(ship);
        // TODO: AI should only have a percent chance to join a formation within a reasonable distance
        if (ship.Settings.CanJoinFormations) TryJoinFormation(ship, float.MaxValue, true, true);
    }

    /// <summary>
    /// Try to join a formation with the given criteria.
    /// </summary>
    /// <param name="ship"></param>
    /// <param name="maxDistance">Only join a formation if within this distance (measured from the formation's current leader)</param>
    /// <param name="canCreateNewFormation">Whether we can create a new formation if no valid formation is available</param>
    /// <param name="snapToPosition">Whether to teleport the ship to its default position in the formation. Should only be <c>true</c> if the ship just spawned.</param>
    /// <returns></returns>
    public Formation TryJoinFormation(ShipSettings ship, float maxDistance, bool canCreateNewFormation, bool snapToPosition)
    {
        Assert.IsNotNull(ship);
        Assert.IsTrue(ship.Settings.CanJoinFormations);
        var formations = GetFormations(ship.Team);
        if (formations == null)
        {
            OMEPLogger.Log(this, $"Formations are not available for team {ship.Team}");
            return null;
        }

        return TryJoinFormation(ship, formations, maxDistance, canCreateNewFormation, snapToPosition);
    }

    private List<Formation> GetFormations(TEAM team)
    {
        if (team == TEAM.CONFED)
        {
            return confedFormations;
        }
        else if (team == TEAM.KILRATHI)
        {
            return kilrathiFormations;
        }
        else if (team == TEAM.PIRATE)
        {
            return pirateFormations;
        }
        else if (team == TEAM.NEUTRAL)
        {
            return neutralFormations;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Try to join a formation with the given criteria.
    /// </summary>
    /// <param name="ship"></param>
    /// <param name="currentFormations">All current formations for the ship's team</param>
    /// <param name="maxDistance">Only join a formation if within this distance (measured from the formation's current leader)</param>
    /// <param name="canCreateNewFormation">Whether we can create a new formation if no valid formation is available</param>
    /// <param name="snapToPosition">Whether to teleport the ship to its default position in the formation. Should only be <c>true</c> if the ship just spawned.</param>
    /// <returns></returns>
    private Formation TryJoinFormation(ShipSettings ship, List<Formation> currentFormations, float maxDistance, bool canCreateNewFormation, bool snapToPosition)
    {
        if (verboseLogging) OMEPLogger.Log(this, $"{ship} {maxDistance} {canCreateNewFormation} {snapToPosition}");
        Assert.IsNotNull(ship);
        Assert.IsNotNull(currentFormations);

        foreach (var formation in currentFormations)
        {
            var leader = formation.Leader;

            // We can join a formation if it has no leader or if it is within the search distance
            if (leader == null || Vector3.Distance(ship.transform.position, leader.transform.position) < maxDistance)
            {
                if (TryJoinFormation(ship, formation, snapToPosition))
                {
                    return formation;
                }
            }
        }

        if (canCreateNewFormation)
        {
            return CreateFormationFor(ship);
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Try to add the given ship to the given formation.
    /// </summary>
    /// <param name="ship"></param>
    /// <param name="formation"></param>
    /// <param name="snapToPosition"></param>
    /// <returns><c>true</c> if joined successfully, <c>false</c> if the formation was full.</returns>
    private bool TryJoinFormation(ShipSettings ship, Formation formation, bool snapToPosition)
    {
        if (verboseLogging) OMEPLogger.Log(this, $"{ship} {formation} {snapToPosition}");
        Assert.IsNotNull(ship);
        Assert.IsNotNull(formation);

        int slotIndex = formation.TryAddShip(ship);
        if (slotIndex >= 0)
        {
            if (verboseLogging) OMEPLogger.Log(this, "Joined formation!");
            ship.Formation = formation;
            if (snapToPosition)
            {
                var pose = formation.GetSlotPose(slotIndex);
                ship.transform.position = pose.position;
                ship.transform.rotation = pose.rotation;
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// Create a new formation on the given ship's team, and add the ship
    /// to it as leader.
    /// </summary>
    /// <param name="ship"></param>
    /// <returns></returns>
    private Formation CreateFormationFor(ShipSettings ship)
    {
        if (verboseLogging) OMEPLogger.Log(this, ship);
        Assert.IsNotNull(ship);
        var template = formationTemplates.GetRandom();
        var formation = new Formation(ship.Team, template);
        formation.VerboseLogging = verboseLogging;
        formation.SetShipAt(0, ship);
        ship.Formation = formation;

        var teamFormations = GetFormations(ship.Team);
        teamFormations.Add(formation);

        return formation;
    }
}
