using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TrafficVehicleDatabase", menuName = "Tokyo Nights/Traffic/Traffic Vehicle Database")]
public class TrafficVehicleDatabase : ScriptableObject
{
    [Header("Traffic Vehicles")]
    public List<TrafficVehicleData> vehicles = new List<TrafficVehicleData>();

    private int cycleIndex;

    public bool HasUsableVehicles()
    {
        if (vehicles == null || vehicles.Count == 0)
            return false;

        for (int i = 0; i < vehicles.Count; i++)
        {
            if (vehicles[i] != null && vehicles[i].IsUsable())
                return true;
        }

        return false;
    }

    public TrafficCarAI GetRandomPrefab()
    {
        if (!HasUsableVehicles())
            return null;

        List<TrafficVehicleData> usableVehicles = GetUsableVehicles();

        if (usableVehicles.Count == 0)
            return null;

        int randomIndex = Random.Range(0, usableVehicles.Count);
        return usableVehicles[randomIndex].trafficPrefab;
    }

    public TrafficCarAI GetWeightedRandomPrefab()
    {
        if (!HasUsableVehicles())
            return null;

        List<TrafficVehicleData> usableVehicles = GetUsableVehicles();

        if (usableVehicles.Count == 0)
            return null;

        float totalWeight = 0f;

        for (int i = 0; i < usableVehicles.Count; i++)
        {
            totalWeight += Mathf.Max(0f, usableVehicles[i].spawnWeight);
        }

        if (totalWeight <= 0f)
            return GetRandomPrefab();

        float roll = Random.Range(0f, totalWeight);
        float runningTotal = 0f;

        for (int i = 0; i < usableVehicles.Count; i++)
        {
            runningTotal += Mathf.Max(0f, usableVehicles[i].spawnWeight);

            if (roll <= runningTotal)
                return usableVehicles[i].trafficPrefab;
        }

        return usableVehicles[usableVehicles.Count - 1].trafficPrefab;
    }

    public TrafficCarAI GetCyclePrefab()
    {
        if (!HasUsableVehicles())
            return null;

        List<TrafficVehicleData> usableVehicles = GetUsableVehicles();

        if (usableVehicles.Count == 0)
            return null;

        if (cycleIndex >= usableVehicles.Count)
            cycleIndex = 0;

        TrafficCarAI prefab = usableVehicles[cycleIndex].trafficPrefab;

        cycleIndex++;

        if (cycleIndex >= usableVehicles.Count)
            cycleIndex = 0;

        return prefab;
    }

    public List<TrafficVehicleData> GetUsableVehicles()
    {
        List<TrafficVehicleData> usableVehicles = new List<TrafficVehicleData>();

        if (vehicles == null)
            return usableVehicles;

        for (int i = 0; i < vehicles.Count; i++)
        {
            TrafficVehicleData data = vehicles[i];

            if (data == null)
                continue;

            if (data.IsUsable())
                usableVehicles.Add(data);
        }

        return usableVehicles;
    }

    public bool ContainsPrefab(TrafficCarAI prefab)
    {
        if (prefab == null || vehicles == null)
            return false;

        for (int i = 0; i < vehicles.Count; i++)
        {
            TrafficVehicleData data = vehicles[i];

            if (data == null)
                continue;

            if (data.trafficPrefab == prefab)
                return true;
        }

        return false;
    }

    public void AddPrefabIfMissing(TrafficCarAI prefab)
    {
        if (prefab == null)
            return;

        if (ContainsPrefab(prefab))
            return;

        TrafficVehicleData data = new TrafficVehicleData
        {
            displayName = prefab.name,
            trafficPrefab = prefab,
            enabled = true,
            spawnWeight = 1f
        };

        vehicles.Add(data);
    }

    public void ResetCycle()
    {
        cycleIndex = 0;
    }
}