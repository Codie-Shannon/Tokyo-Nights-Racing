using UnityEngine;

public class RaceDefinition : MonoBehaviour
{
    [Header("Identity")]
    public string raceID = "desert_circuit";
    public string raceDisplayName = "Desert Circuit";

    [Header("Race Type")]
    public RaceType raceType = RaceType.Circuit;

    [Header("Vehicle Rules")]
    public VehicleType requiredVehicleType = VehicleType.Any;
    public VehicleUsageRule vehicleUsageRule = VehicleUsageRule.UseCurrentCar;

    [Header("Start Setup")]
    public bool useGridStart = true;
    public Transform startPoint;
    public GridStartManager gridStartManager;

    [Header("Race Layout")]
    public Transform finishPoint;
    public bool useFinishTrigger = false;
    public GameObject checkpointGroup;
    public int laps = 1;

    [Header("Optional Race Setup")]
    public Transform waypointParent;
    [Min(0)] public int aiCount = 3;

    [Header("Optional UI")]
    [TextArea]
    public string description;

    public bool MatchesRaceID(string id)
    {
        return !string.IsNullOrWhiteSpace(raceID) && raceID == id;
    }
}