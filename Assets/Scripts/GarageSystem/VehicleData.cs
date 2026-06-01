using UnityEngine;

[CreateAssetMenu(fileName = "NewVehicleData", menuName = "Garage System/Vehicle Data")]
public class VehicleData : ScriptableObject
{
    [Header("Identity")]
    public string vehicleId = "vehicle_id";
    public string displayName = "New Vehicle";

    [TextArea(2, 4)]
    public string description = "Vehicle description.";

    [Header("Vehicle Type")]
    public VehicleType vehicleType = VehicleType.Road;

    [Header("Prefabs")]
    public GameObject gameplayPrefab;
    public GameObject aiPrefab;
    public GameObject previewPrefab;

    [Header("Preview Settings")]
    public Vector3 previewPositionOffset = Vector3.zero;
    public Vector3 previewRotationEuler = Vector3.zero;
    public Vector3 previewScale = Vector3.one;

    [Header("Availability")]
    public bool unlockedByDefault = true;

    [Header("Camera Override")]
    [Tooltip("Optional direct override. Usually leave this OFF and use VehicleCameraSettingsDatabase instead.")]
    public bool overrideCameraSettings = false;

    public VehicleCameraSettings cameraSettings = new VehicleCameraSettings();
}
