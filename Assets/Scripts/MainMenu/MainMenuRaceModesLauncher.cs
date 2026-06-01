using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuRaceModesLauncher : MonoBehaviour
{
    [Header("Race Mode Database")]
    public RaceModeDatabase raceModeDatabase;

    [Header("Vehicle Database")]
    public VehicleDatabase vehicleDatabase;

    [Header("Fallback")]
    public bool useFirstVehicleIfNoSave = true;

    [Header("Loading")]
    public string loadingMessage = "Loading Race...";

    [Header("Debug")]
    public bool logDebugMessages = true;

    public void LaunchRandomRaceForEquippedVehicle()
    {
        VehicleData equippedVehicle = FindEquippedVehicle();

        if (equippedVehicle == null)
        {
            Debug.LogWarning("[MainMenuRaceModesLauncher] No equipped vehicle found.");
            return;
        }

        if (raceModeDatabase == null)
        {
            Debug.LogWarning("[MainMenuRaceModesLauncher] No RaceModeDatabase assigned.");
            return;
        }

        RaceModeDefinition selectedRace = raceModeDatabase.GetRandomRaceMode(equippedVehicle);

        if (selectedRace == null)
        {
            Debug.LogWarning(
                "[MainMenuRaceModesLauncher] No valid race mode found for vehicle: " +
                equippedVehicle.displayName +
                " / Type: " +
                equippedVehicle.vehicleType
            );

            return;
        }

        LaunchRace(selectedRace, equippedVehicle);
    }

    private void LaunchRace(RaceModeDefinition raceMode, VehicleData equippedVehicle)
    {
        if (raceMode == null)
            return;

        RaceLoadRequest.SelectedTrackVariant = raceMode.trackVariant;

        int selectedVehicleIndex = GetVehicleIndex(equippedVehicle);

        RaceLaunchData.SetRaceLaunchData(
            raceMode.raceID,
            raceMode.raceDisplayName,
            raceMode.raceSceneName,
            raceMode.returnSceneName,
            raceMode.returnMarkerID,
            selectedVehicleIndex,
            raceMode.overrideAICount
        );

        if (LoadingScreenController.Instance != null)
            LoadingScreenController.Instance.ShowImmediate(loadingMessage);

        if (logDebugMessages)
        {
            Debug.Log(
                "[MainMenuRaceModesLauncher] Launching race mode: " +
                raceMode.raceDisplayName +
                " | Race ID: " +
                raceMode.raceID +
                " | Vehicle: " +
                equippedVehicle.displayName +
                " | Vehicle Type: " +
                equippedVehicle.vehicleType +
                " | Track Variant: " +
                raceMode.trackVariant +
                " | Return Scene: " +
                raceMode.returnSceneName +
                " | Return Marker: " +
                raceMode.returnMarkerID
            );
        }

        SceneManager.LoadScene(raceMode.raceSceneName);
    }

    private VehicleData FindEquippedVehicle()
    {
        VehicleData[] roster = GetActiveRoster();

        if (roster == null || roster.Length == 0)
        {
            Debug.LogWarning("[MainMenuRaceModesLauncher] Vehicle roster is empty.");
            return null;
        }

        string fallbackId = "";

        if (useFirstVehicleIfNoSave && roster[0] != null)
            fallbackId = roster[0].vehicleId;

        string equippedVehicleId = GarageSaveSystem.LoadEquippedVehicleId(fallbackId);

        for (int i = 0; i < roster.Length; i++)
        {
            VehicleData vehicle = roster[i];

            if (vehicle == null)
                continue;

            if (vehicle.vehicleId == equippedVehicleId)
                return vehicle;
        }

        if (useFirstVehicleIfNoSave)
            return roster[0];

        return null;
    }

    private int GetVehicleIndex(VehicleData vehicle)
    {
        if (vehicle == null)
            return 0;

        VehicleData[] roster = GetActiveRoster();

        if (roster == null || roster.Length == 0)
            return 0;

        for (int i = 0; i < roster.Length; i++)
        {
            if (roster[i] == vehicle)
                return i;

            if (roster[i] != null && roster[i].vehicleId == vehicle.vehicleId)
                return i;
        }

        return 0;
    }

    private VehicleData[] GetActiveRoster()
    {
        if (vehicleDatabase != null && vehicleDatabase.HasVehicles())
            return vehicleDatabase.GetVehicles();

        return null;
    }
}