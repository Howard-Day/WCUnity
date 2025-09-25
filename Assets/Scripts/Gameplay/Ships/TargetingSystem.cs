using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Note: currently used by players but not by AI
public class TargetingSystem : ShipSystem
{
    private SearchResults searchResults = new SearchResults();

    /// <summary>
    /// Find the closest target matching the given team flags and within the range
    /// of our radar, in any direction.
    /// </summary>
    /// <param name="teamFlags">Flags indicating the teams that are valid targets</param>
    /// <returns></returns>
    public ShipSettings FindNearestTarget(TEAM teamFlags)
    {
        searchResults.bestMatch = null;
        searchResults.bestThreshold = ship.Settings.RadarRange;

        Search(teamFlags, SearchNearest, searchResults);

        return searchResults.bestMatch;
    }

    /// <summary>
    /// Find the ship that is closest to directly in front of us (in terms of angle)
    /// and within the range of our radar.
    /// </summary>
    /// <param name="maxAngle">Max angle between dead ahead and the target, in degrees.</param>
    /// <param name="teamFlags">(optional) Flags indicating the teams that are valid targets</param>
    /// <returns></returns>
    public ShipSettings FindTargetForward(float maxAngle = 30, TEAM? teamFlags = null)
    {
        Assert.IsTrue(maxAngle > 0);

        searchResults.bestMatch = null;
        searchResults.bestThreshold = maxAngle;

        if (teamFlags.HasValue)
        {
            Search(teamFlags.Value, SearchForward, searchResults);
        } else
        {
            SearchForward(GameObjTracker.Instance.AllShips, searchResults);
        }

        return searchResults.bestMatch;
    }

    private void Search(TEAM teamFlags, UnityAction<IReadOnlyList<ShipSettings>, SearchResults> searchFunction, SearchResults results)
    {
        if (teamFlags.HasFlag(TEAM.CONFED))
        {
            searchFunction(GameObjTracker.Instance.ConfedShips, results);
        }
        if (teamFlags.HasFlag(TEAM.KILRATHI))
        {
            searchFunction(GameObjTracker.Instance.KilrathiShips, results);
        }
        if (teamFlags.HasFlag(TEAM.NEUTRAL))
        {
            searchFunction(GameObjTracker.Instance.NeutralShips, results);
        }
        if (teamFlags.HasFlag(TEAM.PIRATE))
        {
            searchFunction(GameObjTracker.Instance.NeutralShips, results);
        }
        if (teamFlags.HasFlag(TEAM.ENV))
        {
            searchFunction(GameObjTracker.Instance.EnvironmentalShips, results);
        }
    }

    private void SearchNearest(IReadOnlyList<ShipSettings> ships, SearchResults results)
    {
        foreach (ShipSettings otherShip in ships)
        {
            if (otherShip != null && !otherShip.isCloaked && otherShip != this.ship) {
                float shipDist = Vector3.Distance(this.ship.transform.position, otherShip.transform.position);
                if (shipDist < results.bestThreshold)
                {
                    results.bestThreshold = shipDist;
                    results.bestMatch = otherShip;
                }
            }
        }
    }

    private void SearchForward(IReadOnlyList<ShipSettings> ships, SearchResults results)
    {
        foreach (ShipSettings otherShip in ships)
        {
            if (otherShip != null && !otherShip.isCloaked && otherShip != this.ship)
            {
                float shipDist = Vector3.Distance(this.ship.transform.position, otherShip.transform.position);
                if (shipDist < this.ship.Settings.RadarRange)
                {
                    Vector3 direction = otherShip.transform.position - ship.transform.position;
                    float angle = Vector3.Angle(ship.transform.forward, direction);
                    if (angle < results.bestThreshold)
                    {
                        results.bestMatch = otherShip;
                        results.bestThreshold = angle;
                    }
                }
            }
        }
    }

    private class SearchResults
    {
        public ShipSettings bestMatch;
        public float bestThreshold;
    }
}
