using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class MissionMarkerInteract : MonoBehaviour
{
    public static readonly List<MissionMarkerInteract> AllMarkers = new List<MissionMarkerInteract>();

    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.E;
    public string playerTag = "Player";

    [Header("Legacy UI")]
    [Tooltip("Optional old prompt UI. You can leave this blank if using MissionMarkerUIController.")]
    public GameObject promptUI;

    [Header("Mission Marker UI Panel")]
    public bool useMissionMarkerUIPanel = true;

    [Tooltip("If true, the old promptUI will still be shown alongside the new mission marker UI.")]
    public bool alsoShowLegacyPromptUI = false;

    [Header("Race Scene Launch")]
    public string raceID = "paved_normal";
    public string raceDisplayName = "Tokyo Street Race";
    public string raceSceneName = "RaceScene";

    [Header("Track Variant")]
    public RaceLoadRequest.TrackVariant trackVariant = RaceLoadRequest.TrackVariant.Road;

    [Header("Vehicle Requirement")]
    [Tooltip("If ON, the marker checks the currently equipped VehicleData.vehicleType before allowing the race to start.")]
    public bool requireAllowedVehicleType = true;

    [Tooltip("Allowed vehicle types for this race marker. If empty and Require Allowed Vehicle Type is ON, the marker will use Track Variant fallback rules.")]
    public VehicleType[] allowedVehicleTypes;

    [Tooltip("If ON and Allowed Vehicle Types is empty, the marker chooses allowed vehicle types from the selected Track Variant.")]
    public bool useTrackVariantFallbackRules = true;

    [Header("Vehicle Database")]
    [Tooltip("Preferred source of vehicle data.")]
    public VehicleDatabase vehicleDatabase;

    [Tooltip("Fallback roster if Vehicle Database is not assigned.")]
    public VehicleData[] vehicleRoster;

    [Tooltip("If no saved/equipped vehicle is found, use the first vehicle in the database/roster.")]
    public bool useFirstVehicleIfNoSave = true;

    [Header("Return To Freeroam")]
    public string returnSceneName = "MainScene - Tokyo";
    public string returnMarkerID = "paved_normal_marker";

    [Header("Optional Overrides")]
    public int selectedVehicleIndex = 0;

    [Tooltip("Set to -1 to use the AI count from the RaceDefinition inside the RaceScene.")]
    public int overrideAICount = -1;

    [Header("Objects To Hide")]
    public GameObject markerVisualRoot;
    public Collider triggerCollider;

    [Header("Debug")]
    public bool logVehicleRequirementChecks = true;

    private bool playerInRange = false;
    private bool markerEnabled = true;
    private bool raceStartRequested = false;

    private void OnEnable()
    {
        if (!AllMarkers.Contains(this))
            AllMarkers.Add(this);
    }

    private void OnDisable()
    {
        AllMarkers.Remove(this);
    }

    private void Start()
    {
        HideLegacyPrompt();

        if (triggerCollider == null)
            triggerCollider = GetComponent<Collider>();
    }

    private void Update()
    {
        if (!markerEnabled)
            return;

        if (!playerInRange)
            return;

        if (raceStartRequested)
            return;

        if (Input.GetKeyDown(interactKey))
            StartAssignedRace();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!markerEnabled)
            return;

        if (!other.CompareTag(playerTag))
            return;

        playerInRange = true;

        ShowLegacyPrompt();
        RefreshMissionMarkerUI();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        playerInRange = false;

        HideLegacyPrompt();
        HideMissionMarkerUI();
    }

    public void StartAssignedRace()
    {
        if (!ValidateRaceSetup())
        {
            RefreshMissionMarkerUI();
            return;
        }

        if (!CanCurrentVehicleEnterRace(out VehicleData equippedVehicle, out string blockReason))
        {
            Debug.LogWarning("[MissionMarkerInteract] Race blocked: " + blockReason);
            RefreshMissionMarkerUI();
            return;
        }

        raceStartRequested = true;

        HideLegacyPrompt();
        HideMissionMarkerUI();

        RaceLoadRequest.SelectedTrackVariant = trackVariant;

        RaceLaunchData.SetRaceLaunchData(
            raceID,
            raceDisplayName,
            raceSceneName,
            returnSceneName,
            returnMarkerID,
            selectedVehicleIndex,
            overrideAICount
        );

        if (LoadingScreenController.Instance != null)
            LoadingScreenController.Instance.ShowImmediate("Loading Race...");

        string vehicleName = equippedVehicle != null ? equippedVehicle.displayName : "Unknown Vehicle";
        string vehicleType = equippedVehicle != null ? FormatVehicleType(equippedVehicle.vehicleType) : "Unknown Type";

        Debug.Log(
            "[MissionMarkerInteract] Loading shared race scene: " +
            raceSceneName +
            " | Race ID: " +
            raceID +
            " | Variant: " +
            trackVariant +
            " | Vehicle: " +
            vehicleName +
            " | Vehicle Type: " +
            vehicleType +
            " | Return Marker ID: " +
            returnMarkerID
        );

        SceneManager.LoadScene(raceSceneName);
    }

    private bool ValidateRaceSetup()
    {
        if (string.IsNullOrWhiteSpace(raceID))
        {
            Debug.LogWarning("MissionMarkerInteract has no Race ID assigned.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(raceSceneName))
        {
            Debug.LogWarning("MissionMarkerInteract has no Race Scene Name assigned.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(returnSceneName))
        {
            Debug.LogWarning("MissionMarkerInteract has no Return Scene Name assigned.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(returnMarkerID))
        {
            Debug.LogWarning("MissionMarkerInteract has no Return Marker ID assigned.");
            return false;
        }

        return true;
    }

    private void RefreshMissionMarkerUI()
    {
        if (!useMissionMarkerUIPanel)
            return;

        if (MissionMarkerUIController.Instance == null)
            return;

        VehicleData equippedVehicle = FindEquippedVehicle();

        string displayRaceName = string.IsNullOrWhiteSpace(raceDisplayName)
            ? "UNKNOWN RACE"
            : raceDisplayName;

        string raceTypeLabel = GetRaceTypeDisplayName();
        string requiredVehicleLabel = GetRequiredVehicleDisplayName();

        string currentVehicleLabel = equippedVehicle != null
            ? FormatVehicleType(equippedVehicle.vehicleType)
            : "Unknown Vehicle Type";

        bool canStart = CanCurrentVehicleEnterRace(out _, out _);

        MissionMarkerUIController.Instance.ShowMission(
            displayRaceName,
            raceTypeLabel,
            requiredVehicleLabel,
            currentVehicleLabel,
            canStart
        );
    }

    private void HideMissionMarkerUI()
    {
        if (!useMissionMarkerUIPanel)
            return;

        if (MissionMarkerUIController.Instance == null)
            return;

        MissionMarkerUIController.Instance.Hide();
    }

    private void ShowLegacyPrompt()
    {
        if (promptUI == null)
            return;

        if (!alsoShowLegacyPromptUI && useMissionMarkerUIPanel)
            return;

        promptUI.SetActive(true);
    }

    private void HideLegacyPrompt()
    {
        if (promptUI != null)
            promptUI.SetActive(false);
    }

    private bool CanCurrentVehicleEnterRace(out VehicleData equippedVehicle, out string blockReason)
    {
        equippedVehicle = FindEquippedVehicle();
        blockReason = "";

        if (!requireAllowedVehicleType)
        {
            blockReason = "Vehicle requirement disabled.";
            return true;
        }

        if (equippedVehicle == null)
        {
            blockReason = "No equipped vehicle found.";
            return false;
        }

        VehicleType equippedType = equippedVehicle.vehicleType;
        bool allowed = IsVehicleTypeAllowed(equippedType);

        if (logVehicleRequirementChecks)
        {
            Debug.Log(
                "[MissionMarkerInteract] Vehicle requirement check. " +
                "Race=" + raceDisplayName +
                ", TrackVariant=" + trackVariant +
                ", EquippedVehicle=" + equippedVehicle.displayName +
                ", EquippedType=" + equippedType +
                ", Allowed=" + allowed
            );
        }

        if (!allowed)
        {
            blockReason =
                "Vehicle '" +
                equippedVehicle.displayName +
                "' is type '" +
                equippedType +
                "' and is not allowed for race '" +
                raceDisplayName +
                "'.";

            return false;
        }

        return true;
    }

    private bool IsVehicleTypeAllowed(VehicleType vehicleType)
    {
        if (!requireAllowedVehicleType)
            return true;

        if (allowedVehicleTypes != null && allowedVehicleTypes.Length > 0)
        {
            for (int i = 0; i < allowedVehicleTypes.Length; i++)
            {
                if (allowedVehicleTypes[i] == VehicleType.Any)
                    return true;

                if (allowedVehicleTypes[i] == vehicleType)
                    return true;
            }

            return false;
        }

        if (!useTrackVariantFallbackRules)
            return true;

        return IsVehicleTypeAllowedByTrackVariant(vehicleType);
    }

    private bool IsVehicleTypeAllowedByTrackVariant(VehicleType vehicleType)
    {
        switch (trackVariant)
        {
            case RaceLoadRequest.TrackVariant.Road:
                return vehicleType == VehicleType.Road;

            case RaceLoadRequest.TrackVariant.OffRoad:
                return vehicleType == VehicleType.OffRoad ||
                       vehicleType == VehicleType.AllTerrain;

            case RaceLoadRequest.TrackVariant.AllTerrain:
                return vehicleType == VehicleType.AllTerrain ||
                       vehicleType == VehicleType.OffRoad ||
                       vehicleType == VehicleType.MonsterTruck;

            case RaceLoadRequest.TrackVariant.MonsterTruck:
                return vehicleType == VehicleType.MonsterTruck;

            default:
                return true;
        }
    }

    private VehicleData FindEquippedVehicle()
    {
        VehicleData[] roster = GetActiveRoster();

        if (roster == null || roster.Length == 0)
        {
            Debug.LogWarning("[MissionMarkerInteract] Vehicle roster is empty.");
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

    private VehicleData[] GetActiveRoster()
    {
        if (vehicleDatabase != null && vehicleDatabase.HasVehicles())
            return vehicleDatabase.GetVehicles();

        return vehicleRoster;
    }

    private string GetRaceTypeDisplayName()
    {
        switch (trackVariant)
        {
            case RaceLoadRequest.TrackVariant.Road:
                return "Road Race";

            case RaceLoadRequest.TrackVariant.OffRoad:
                return "Off-Road Race";

            case RaceLoadRequest.TrackVariant.AllTerrain:
                return "All-Terrain Race";

            case RaceLoadRequest.TrackVariant.MonsterTruck:
                return "Monster Truck Race";

            default:
                return "Race";
        }
    }

    private string GetRequiredVehicleDisplayName()
    {
        if (!requireAllowedVehicleType)
            return "Any Vehicle";

        if (allowedVehicleTypes != null && allowedVehicleTypes.Length > 0)
        {
            if (allowedVehicleTypes.Length == 1)
                return FormatVehicleType(allowedVehicleTypes[0]);

            string label = "";

            for (int i = 0; i < allowedVehicleTypes.Length; i++)
            {
                if (i > 0)
                    label += " / ";

                label += FormatVehicleType(allowedVehicleTypes[i]);
            }

            return label;
        }

        if (!useTrackVariantFallbackRules)
            return "Any Vehicle";

        switch (trackVariant)
        {
            case RaceLoadRequest.TrackVariant.Road:
                return "Road Vehicle";

            case RaceLoadRequest.TrackVariant.OffRoad:
                return "Off-Road / All-Terrain";

            case RaceLoadRequest.TrackVariant.AllTerrain:
                return "All-Terrain / Off-Road / Monster Truck";

            case RaceLoadRequest.TrackVariant.MonsterTruck:
                return "Monster Truck";

            default:
                return "Any Vehicle";
        }
    }

    private string FormatVehicleType(VehicleType vehicleType)
    {
        switch (vehicleType)
        {
            case VehicleType.Any:
                return "Any Vehicle";

            case VehicleType.Road:
                return "Road Vehicle";

            case VehicleType.OffRoad:
                return "Off-Road Vehicle";

            case VehicleType.AllTerrain:
                return "All-Terrain Vehicle";

            case VehicleType.MonsterTruck:
                return "Monster Truck";

            default:
                return vehicleType.ToString();
        }
    }

    public void SetMarkerActive(bool active)
    {
        markerEnabled = active;
        playerInRange = false;
        raceStartRequested = false;

        HideLegacyPrompt();
        HideMissionMarkerUI();

        if (markerVisualRoot != null)
            markerVisualRoot.SetActive(active);

        if (triggerCollider != null)
            triggerCollider.enabled = active;
    }

    public static void SetAllMarkersActive(bool active)
    {
        for (int i = 0; i < AllMarkers.Count; i++)
        {
            if (AllMarkers[i] != null)
                AllMarkers[i].SetMarkerActive(active);
        }
    }
}