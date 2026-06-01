using UnityEngine;

[CreateAssetMenu(menuName = "Tokyo Nights/Race Modes/Race Mode Definition")]
public class RaceModeDefinition : ScriptableObject
{
    [Header("Race Info")]
    public string raceID = "road_pointtopoint";
    public string raceDisplayName = "Tokyo Road Race";

    [TextArea(2, 5)]
    public string description;

    [Header("Scene")]
    public string raceSceneName = "RaceScene";

    [Header("Return")]
    public string returnSceneName = "MainMenuScene";

    [Tooltip("Only needed when returning to freeroam. Main menu race modes can leave this blank.")]
    public string returnMarkerID = "";

    [Header("Track Variant")]
    public RaceLoadRequest.TrackVariant trackVariant = RaceLoadRequest.TrackVariant.Road;

    [Header("Vehicle Requirement")]
    public VehicleType requiredVehicleType = VehicleType.Road;

    [Header("AI")]
    [Tooltip("Set to -1 to use the AI count from the RaceDefinition inside RaceScene.")]
    public int overrideAICount = -1;

    [Header("Unlock")]
    public bool unlocked = true;

    public bool CanUseVehicle(VehicleData vehicle)
    {
        if (!unlocked)
            return false;

        if (vehicle == null)
            return false;

        if (requiredVehicleType == VehicleType.Any)
            return true;

        return vehicle.vehicleType == requiredVehicleType;
    }
}