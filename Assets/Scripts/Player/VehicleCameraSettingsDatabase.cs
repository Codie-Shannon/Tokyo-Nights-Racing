using UnityEngine;

[System.Serializable]
public class VehicleDataCameraVariant
{
    public VehicleData vehicleData;
    public VehicleCameraSettings settings = new VehicleCameraSettings();
}

[System.Serializable]
public class VehicleTypeCameraVariant
{
    public VehicleType vehicleType = VehicleType.Road;
    public VehicleCameraSettings settings = new VehicleCameraSettings();
}

[CreateAssetMenu(fileName = "VehicleCameraSettingsDatabase", menuName = "Garage System/Vehicle Camera Settings Database")]
public class VehicleCameraSettingsDatabase : ScriptableObject
{
    [Header("Fallback")]
    public VehicleCameraSettings defaultSettings = new VehicleCameraSettings();

    [Header("Specific VehicleData Variants")]
    [Tooltip("Highest priority. Use this when one exact VehicleData asset needs its own camera settings.")]
    public VehicleDataCameraVariant[] vehicleDataVariants;

    [Header("Vehicle Type Variants")]
    [Tooltip("Fallback by broad vehicle type. Example: Road, OffRoad, AllTerrain, MonsterTruck.")]
    public VehicleTypeCameraVariant[] vehicleTypeVariants;

    public VehicleCameraSettings GetSettingsForVehicle(VehicleData vehicleData, VehicleType fallbackVehicleType, out string source)
    {
        if (vehicleData != null && vehicleDataVariants != null)
        {
            for (int i = 0; i < vehicleDataVariants.Length; i++)
            {
                VehicleDataCameraVariant variant = vehicleDataVariants[i];

                if (variant == null || variant.vehicleData == null || variant.settings == null)
                    continue;

                if (variant.vehicleData == vehicleData)
                {
                    source = "Camera Database VehicleData Variant";
                    return variant.settings;
                }
            }
        }

        if (vehicleTypeVariants != null)
        {
            for (int i = 0; i < vehicleTypeVariants.Length; i++)
            {
                VehicleTypeCameraVariant variant = vehicleTypeVariants[i];

                if (variant == null || variant.settings == null)
                    continue;

                if (variant.vehicleType == fallbackVehicleType)
                {
                    source = "Camera Database VehicleType Variant";
                    return variant.settings;
                }
            }
        }

        source = "Camera Database Default";
        return defaultSettings;
    }

    public VehicleCameraSettings GetSettingsForVehicleType(VehicleType vehicleType)
    {
        string source;
        return GetSettingsForVehicle(null, vehicleType, out source);
    }

    [ContextMenu("Create Default Vehicle Type Variants")]
    public void CreateDefaultVehicleTypeVariants()
    {
        defaultSettings = new VehicleCameraSettings
        {
            offset = new Vector3(0f, 1.55f, -4.35f),
            reverseOffset = new Vector3(0f, 1.55f, 4.35f),
            lookAtOffset = new Vector3(0f, 0.9f, 0f),
            followSpeed = 4.5f,
            rotationSpeed = 4.2f,
            lookAheadDistance = 3.2f,
            reverseLookBehindDistance = 4.2f,
            turnShiftAmount = 0.12f,
            cameraRollAmount = 0.4f,
            speedPullbackAmount = 0.45f,
            baseFOV = 72f,
            maxFOV = 80f
        };

        vehicleTypeVariants = new VehicleTypeCameraVariant[]
        {
            new VehicleTypeCameraVariant
            {
                vehicleType = VehicleType.Road,
                settings = new VehicleCameraSettings
                {
                    offset = new Vector3(0f, 1.55f, -4.35f),
                    reverseOffset = new Vector3(0f, 1.55f, 4.35f),
                    lookAtOffset = new Vector3(0f, 0.9f, 0f),
                    followSpeed = 4.5f,
                    rotationSpeed = 4.2f,
                    lookAheadDistance = 3.2f,
                    reverseLookBehindDistance = 4.2f,
                    turnShiftAmount = 0.12f,
                    cameraRollAmount = 0.4f,
                    speedPullbackAmount = 0.45f,
                    baseFOV = 72f,
                    maxFOV = 80f
                }
            },
            new VehicleTypeCameraVariant
            {
                vehicleType = VehicleType.OffRoad,
                settings = new VehicleCameraSettings
                {
                    offset = new Vector3(0f, 2.05f, -5.4f),
                    reverseOffset = new Vector3(0f, 1.95f, 5.2f),
                    lookAtOffset = new Vector3(0f, 1.25f, 0f),
                    followSpeed = 4.0f,
                    rotationSpeed = 3.8f,
                    lookAheadDistance = 3.8f,
                    reverseLookBehindDistance = 5.0f,
                    turnShiftAmount = 0.10f,
                    cameraRollAmount = 0.3f,
                    speedPullbackAmount = 0.35f,
                    baseFOV = 74f,
                    maxFOV = 81f
                }
            },
            new VehicleTypeCameraVariant
            {
                vehicleType = VehicleType.AllTerrain,
                settings = new VehicleCameraSettings
                {
                    offset = new Vector3(0f, 2.35f, -6.1f),
                    reverseOffset = new Vector3(0f, 2.2f, 5.8f),
                    lookAtOffset = new Vector3(0f, 1.45f, 0f),
                    followSpeed = 3.8f,
                    rotationSpeed = 3.6f,
                    lookAheadDistance = 4.1f,
                    reverseLookBehindDistance = 5.5f,
                    turnShiftAmount = 0.09f,
                    cameraRollAmount = 0.25f,
                    speedPullbackAmount = 0.30f,
                    baseFOV = 75f,
                    maxFOV = 82f
                }
            },
            new VehicleTypeCameraVariant
            {
                vehicleType = VehicleType.MonsterTruck,
                settings = new VehicleCameraSettings
                {
                    offset = new Vector3(0f, 2.8f, -7.2f),
                    reverseOffset = new Vector3(0f, 2.5f, 6.5f),
                    lookAtOffset = new Vector3(0f, 1.8f, 0f),
                    followSpeed = 3.5f,
                    rotationSpeed = 3.4f,
                    lookAheadDistance = 4.5f,
                    reverseLookBehindDistance = 6.0f,
                    turnShiftAmount = 0.08f,
                    cameraRollAmount = 0.2f,
                    speedPullbackAmount = 0.25f,
                    baseFOV = 76f,
                    maxFOV = 82f
                }
            }
        };
    }
}
