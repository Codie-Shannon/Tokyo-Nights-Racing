using UnityEngine;

public static class GarageSaveSystem
{
    private const string EquippedVehicleIdKey = "Garage_EquippedVehicleId";

    public static void SaveEquippedVehicle(string vehicleId)
    {
        if (string.IsNullOrWhiteSpace(vehicleId))
        {
            Debug.LogWarning("GarageSaveSystem: Tried to save an empty vehicle ID.");
            return;
        }

        PlayerPrefs.SetString(EquippedVehicleIdKey, vehicleId);
        PlayerPrefs.Save();

        Debug.Log($"GarageSaveSystem: Saved equipped vehicle ID: {vehicleId}");
    }

    public static string LoadEquippedVehicleId(string fallbackVehicleId = "")
    {
        return PlayerPrefs.GetString(EquippedVehicleIdKey, fallbackVehicleId);
    }

    public static bool HasEquippedVehicle()
    {
        return PlayerPrefs.HasKey(EquippedVehicleIdKey);
    }

    public static void ClearEquippedVehicle()
    {
        PlayerPrefs.DeleteKey(EquippedVehicleIdKey);
        PlayerPrefs.Save();

        Debug.Log("GarageSaveSystem: Cleared equipped vehicle.");
    }
}