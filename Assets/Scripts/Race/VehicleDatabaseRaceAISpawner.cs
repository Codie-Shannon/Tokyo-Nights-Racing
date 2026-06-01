using System.Collections.Generic;
using UnityEngine;

public class VehicleDatabaseRaceAISpawner : MonoBehaviour
{
    public enum AISelectionMode
    {
        Random,
        Cycle
    }

    [Header("Vehicle Database")]
    public VehicleDatabase vehicleDatabase;

    [Header("Grid Start Manager")]
    public GridStartManager gridStartManager;

    [Header("AI Selection")]
    public AISelectionMode selectionMode = AISelectionMode.Random;

    [Tooltip("If true, AI vehicles are filtered by RaceDefinition.requiredVehicleType.")]
    public bool filterAIByRaceRequiredVehicleType = true;

    [Tooltip("If true and not enough matching AI vehicles exist, the same valid vehicles can be reused.")]
    public bool allowRepeats = true;

    [Tooltip("If true, vehicles without an AI prefab are ignored.")]
    public bool requireAIPrefab = true;

    [Header("Fallback")]
    [Tooltip("If no matching AI vehicles are found, allow any vehicle with an AI prefab as a fallback.")]
    public bool allowAnyVehicleFallbackIfNoMatches = false;

    [Header("Debug")]
    public bool logDebugMessages = true;

    private int cycleIndex = 0;

    public void PrepareAIForRace(RaceDefinition raceDefinition)
    {
        if (raceDefinition == null)
        {
            LogWarning("Cannot prepare AI because RaceDefinition is null.");
            return;
        }

        if (gridStartManager == null)
            gridStartManager = raceDefinition.gridStartManager;

        if (gridStartManager == null)
            gridStartManager = FindFirstObjectByType<GridStartManager>();

        if (gridStartManager == null)
        {
            LogWarning("Cannot prepare AI because no GridStartManager was found.");
            return;
        }

        if (vehicleDatabase == null)
        {
            LogWarning("Cannot prepare AI because VehicleDatabase is not assigned.");
            gridStartManager.ClearPendingAIVehicles();
            return;
        }

        if (!vehicleDatabase.HasVehicles())
        {
            LogWarning("Cannot prepare AI because VehicleDatabase has no vehicles.");
            gridStartManager.ClearPendingAIVehicles();
            return;
        }

        int aiCount = GetFinalAICount(raceDefinition);

        if (aiCount <= 0)
        {
            gridStartManager.ClearPendingAIVehicles();
            Log("Race requested 0 AI vehicles. Cleared pending AI.");
            return;
        }

        List<VehicleData> candidates = GetCandidateVehicles(raceDefinition);

        if (candidates.Count == 0 && allowAnyVehicleFallbackIfNoMatches)
        {
            candidates = GetAnyAICapableVehicles();

            LogWarning(
                "No matching AI vehicles found for race type " +
                raceDefinition.requiredVehicleType +
                ". Using fallback any-type AI vehicles. Count=" +
                candidates.Count
            );
        }

        if (candidates.Count == 0)
        {
            gridStartManager.ClearPendingAIVehicles();

            LogWarning(
                "No valid AI vehicles found for race '" +
                raceDefinition.raceDisplayName +
                "' | RequiredType=" +
                raceDefinition.requiredVehicleType +
                ". GridStartManager may use fallback AI prefab if assigned."
            );

            return;
        }

        List<VehicleData> selectedAI = SelectAIVehicles(candidates, aiCount);

        gridStartManager.SetPendingAIVehiclesForNextGrid(selectedAI);

        LogPreparedAI(raceDefinition, selectedAI);
    }

    private int GetFinalAICount(RaceDefinition raceDefinition)
    {
        int aiCount = raceDefinition.aiCount;

        if (RaceLaunchData.HasRaceLaunchData && RaceLaunchData.UseOverrideAICount)
            aiCount = RaceLaunchData.OverrideAICount;

        return Mathf.Max(0, aiCount);
    }

    private List<VehicleData> GetCandidateVehicles(RaceDefinition raceDefinition)
    {
        List<VehicleData> candidates = new List<VehicleData>();

        VehicleData[] vehicles = vehicleDatabase.GetVehicles();

        if (vehicles == null)
            return candidates;

        for (int i = 0; i < vehicles.Length; i++)
        {
            VehicleData vehicle = vehicles[i];

            if (vehicle == null)
                continue;

            if (requireAIPrefab && vehicle.aiPrefab == null)
                continue;

            if (filterAIByRaceRequiredVehicleType)
            {
                if (!VehicleMatchesRequiredType(vehicle, raceDefinition.requiredVehicleType))
                    continue;
            }

            candidates.Add(vehicle);
        }

        return candidates;
    }

    private List<VehicleData> GetAnyAICapableVehicles()
    {
        List<VehicleData> candidates = new List<VehicleData>();

        VehicleData[] vehicles = vehicleDatabase.GetVehicles();

        if (vehicles == null)
            return candidates;

        for (int i = 0; i < vehicles.Length; i++)
        {
            VehicleData vehicle = vehicles[i];

            if (vehicle == null)
                continue;

            if (requireAIPrefab && vehicle.aiPrefab == null)
                continue;

            candidates.Add(vehicle);
        }

        return candidates;
    }

    private bool VehicleMatchesRequiredType(VehicleData vehicle, VehicleType requiredType)
    {
        if (vehicle == null)
            return false;

        if (requiredType == VehicleType.Any)
            return true;

        return vehicle.vehicleType == requiredType;
    }

    private List<VehicleData> SelectAIVehicles(List<VehicleData> candidates, int aiCount)
    {
        List<VehicleData> selected = new List<VehicleData>();

        if (candidates == null || candidates.Count == 0 || aiCount <= 0)
            return selected;

        List<VehicleData> pool = new List<VehicleData>(candidates);

        for (int i = 0; i < aiCount; i++)
        {
            if (pool.Count == 0)
            {
                if (!allowRepeats)
                    break;

                pool = new List<VehicleData>(candidates);
            }

            VehicleData chosen = null;

            switch (selectionMode)
            {
                case AISelectionMode.Cycle:
                    chosen = SelectCycle(pool);
                    break;

                case AISelectionMode.Random:
                default:
                    chosen = SelectRandom(pool);
                    break;
            }

            if (chosen == null)
                continue;

            selected.Add(chosen);

            if (!allowRepeats)
                pool.Remove(chosen);
        }

        return selected;
    }

    private VehicleData SelectRandom(List<VehicleData> pool)
    {
        if (pool == null || pool.Count == 0)
            return null;

        int index = Random.Range(0, pool.Count);
        VehicleData chosen = pool[index];

        if (allowRepeats)
            return chosen;

        pool.RemoveAt(index);
        return chosen;
    }

    private VehicleData SelectCycle(List<VehicleData> pool)
    {
        if (pool == null || pool.Count == 0)
            return null;

        if (cycleIndex < 0)
            cycleIndex = 0;

        int index = cycleIndex % pool.Count;
        VehicleData chosen = pool[index];

        cycleIndex++;

        if (!allowRepeats)
            pool.Remove(chosen);

        return chosen;
    }

    private void LogPreparedAI(RaceDefinition raceDefinition, List<VehicleData> selectedAI)
    {
        if (!logDebugMessages)
            return;

        string message =
            "[VehicleDatabaseRaceAISpawner] Prepared AI for race: " +
            raceDefinition.raceDisplayName +
            " | Race ID: " +
            raceDefinition.raceID +
            " | Required Type: " +
            raceDefinition.requiredVehicleType +
            " | Count: " +
            selectedAI.Count +
            "\n";

        for (int i = 0; i < selectedAI.Count; i++)
        {
            VehicleData vehicle = selectedAI[i];

            if (vehicle == null)
                continue;

            message +=
                (i + 1) +
                ". " +
                vehicle.displayName +
                " | Type=" +
                vehicle.vehicleType +
                " | AI Prefab=" +
                (vehicle.aiPrefab != null ? vehicle.aiPrefab.name : "None") +
                "\n";
        }

        Debug.Log(message);
    }

    private void Log(string message)
    {
        if (logDebugMessages)
            Debug.Log("[VehicleDatabaseRaceAISpawner] " + message);
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning("[VehicleDatabaseRaceAISpawner] " + message);
    }
}