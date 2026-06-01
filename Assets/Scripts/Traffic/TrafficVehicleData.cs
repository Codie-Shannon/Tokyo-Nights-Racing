using UnityEngine;

[System.Serializable]
public class TrafficVehicleData
{
    [Header("Identity")]
    public string displayName = "Traffic Car";

    [Header("Prefab")]
    public TrafficCarAI trafficPrefab;

    [Header("Spawn")]
    public bool enabled = true;

    [Min(0f)]
    public float spawnWeight = 1f;

    public bool IsUsable()
    {
        return enabled && trafficPrefab != null && spawnWeight > 0f;
    }
}