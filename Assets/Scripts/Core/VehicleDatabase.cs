using UnityEngine;

[CreateAssetMenu(fileName = "VehicleDatabase", menuName = "Tokyo Nights/Vehicle Database")]
public class VehicleDatabase : ScriptableObject
{
    [Header("Vehicles")]
    public VehicleData[] vehicles;

    public VehicleData[] GetVehicles()
    {
        return vehicles;
    }

    public bool HasVehicles()
    {
        return vehicles != null && vehicles.Length > 0;
    }

    public VehicleData GetFirstVehicle()
    {
        if (!HasVehicles())
            return null;

        return vehicles[0];
    }

    public VehicleData FindById(string vehicleId)
    {
        if (string.IsNullOrWhiteSpace(vehicleId))
            return null;

        if (vehicles == null)
            return null;

        for (int i = 0; i < vehicles.Length; i++)
        {
            VehicleData vehicle = vehicles[i];

            if (vehicle == null)
                continue;

            if (vehicle.vehicleId == vehicleId)
                return vehicle;
        }

        return null;
    }

    public bool Contains(VehicleData vehicleData)
    {
        if (vehicleData == null)
            return false;

        if (vehicles == null)
            return false;

        for (int i = 0; i < vehicles.Length; i++)
        {
            VehicleData vehicle = vehicles[i];

            if (vehicle == null)
                continue;

            if (vehicle == vehicleData)
                return true;

            if (vehicle.vehicleId == vehicleData.vehicleId)
                return true;
        }

        return false;
    }
}