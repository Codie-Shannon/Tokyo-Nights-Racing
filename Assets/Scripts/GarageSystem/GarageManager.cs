using TMPro;
using UnityEngine;

public class GarageManager : MonoBehaviour
{
    [Header("Vehicle Database")]
    public VehicleDatabase vehicleDatabase;

    [Header("Garage Roster Fallback")]
    public VehicleData[] vehicleRoster;

    public int selectedVehicleIndex;

    [Header("Preview")]
    public Transform previewSpawnPoint;
    public bool rotatePreviewCar = true;
    public float previewRotationSpeed = 18f;

    [Header("Relative Stat Display Range")]
    [Range(0f, 10f)]
    public float minimumVisibleStat = 4f;

    [Range(0f, 10f)]
    public float maximumVisibleStat = 10f;

    [Header("UI - Vehicle Info")]
    public TMP_Text vehicleNameText;
    public TMP_Text vehicleDescriptionText;
    public TMP_Text vehicleCounterText;
    public TMP_Text equippedStatusText;

    [Header("UI - Stats")]
    public GarageVehicleStatsDisplay statsDisplay;

    [Header("Garage Navigation")]
    public GarageBackButton garageBackButton;

    [Tooltip("If true, pressing the equip key will equip the selected vehicle and leave the garage.")]
    public bool equipKeyAlsoGoesBack = true;

    [Header("Input")]
    public KeyCode previousKey = KeyCode.LeftArrow;
    public KeyCode nextKey = KeyCode.RightArrow;
    public KeyCode equipKey = KeyCode.Return;

    [Header("Status Text")]
    public string equippedText = "EQUIPPED";
    public string availableText = "AVAILABLE";
    public string lockedText = "LOCKED";

    [Header("Debug")]
    public bool logSelectedVehicle = true;

    private GameObject currentPreviewInstance;
    private string equippedVehicleId = "";

    private void Start()
    {
        LoadEquippedVehicle();
        SelectEquippedVehicleIfPossible();
        ClampSelectedIndex();
        RefreshGarage();
    }

    private void Update()
    {
        if (Input.GetKeyDown(previousKey))
        {
            PreviousVehicle();
        }

        if (Input.GetKeyDown(nextKey))
        {
            NextVehicle();
        }

        if (Input.GetKeyDown(equipKey))
        {
            if (equipKeyAlsoGoesBack)
            {
                EquipSelectedVehicleAndBack();
            }
            else
            {
                EquipSelectedVehicle();
            }
        }

        RotatePreview();
    }

    public void PreviousVehicle()
    {
        if (!HasVehicles())
            return;

        VehicleData[] roster = GetActiveRoster();

        selectedVehicleIndex--;

        if (selectedVehicleIndex < 0)
            selectedVehicleIndex = roster.Length - 1;

        RefreshGarage();
    }

    public void NextVehicle()
    {
        if (!HasVehicles())
            return;

        VehicleData[] roster = GetActiveRoster();

        selectedVehicleIndex++;

        if (selectedVehicleIndex >= roster.Length)
            selectedVehicleIndex = 0;

        RefreshGarage();
    }

    public void EquipSelectedVehicle()
    {
        VehicleData selectedVehicle = GetSelectedVehicle();

        if (selectedVehicle == null)
        {
            Debug.LogWarning("GarageManager: No selected vehicle to equip.");
            return;
        }

        if (!selectedVehicle.unlockedByDefault)
        {
            Debug.LogWarning($"GarageManager: Vehicle is locked and cannot be equipped: {selectedVehicle.displayName}");
            return;
        }

        equippedVehicleId = selectedVehicle.vehicleId;
        GarageSaveSystem.SaveEquippedVehicle(equippedVehicleId);

        RefreshGarage();

        Debug.Log($"GarageManager: Equipped vehicle: {selectedVehicle.displayName} ({selectedVehicle.vehicleId})");
    }

    public void EquipSelectedVehicleAndBack()
    {
        VehicleData selectedVehicle = GetSelectedVehicle();

        if (selectedVehicle == null)
        {
            Debug.LogWarning("GarageManager: No selected vehicle to equip.");
            return;
        }

        if (!selectedVehicle.unlockedByDefault)
        {
            Debug.LogWarning($"GarageManager: Vehicle is locked and cannot be equipped: {selectedVehicle.displayName}");
            return;
        }

        EquipSelectedVehicle();

        if (garageBackButton == null)
        {
            garageBackButton = FindFirstObjectByType<GarageBackButton>();
        }

        if (garageBackButton == null)
        {
            Debug.LogWarning("GarageManager: Vehicle equipped, but no GarageBackButton was assigned/found, so garage cannot go back.");
            return;
        }

        garageBackButton.Back();
    }

    public void RefreshGarage()
    {
        if (!HasVehicles())
        {
            ClearGarage();
            return;
        }

        ClampSelectedIndex();

        VehicleData selectedVehicle = GetSelectedVehicle();

        if (selectedVehicle == null)
        {
            ClearGarage();
            return;
        }

        SpawnPreview(selectedVehicle);
        UpdateVehicleInfo(selectedVehicle);
        UpdateStats(selectedVehicle);

        if (logSelectedVehicle)
        {
            Debug.Log($"Garage selected vehicle: {selectedVehicle.displayName}");
        }
    }

    private void LoadEquippedVehicle()
    {
        string fallbackId = "";

        VehicleData[] roster = GetActiveRoster();

        if (roster != null && roster.Length > 0 && roster[0] != null)
        {
            fallbackId = roster[0].vehicleId;
        }

        equippedVehicleId = GarageSaveSystem.LoadEquippedVehicleId(fallbackId);

        if (string.IsNullOrWhiteSpace(equippedVehicleId))
        {
            equippedVehicleId = fallbackId;
        }
    }

    private void SelectEquippedVehicleIfPossible()
    {
        if (!HasVehicles())
        {
            selectedVehicleIndex = 0;
            return;
        }

        VehicleData[] roster = GetActiveRoster();

        for (int i = 0; i < roster.Length; i++)
        {
            if (roster[i] == null)
                continue;

            if (roster[i].vehicleId == equippedVehicleId)
            {
                selectedVehicleIndex = i;
                return;
            }
        }

        selectedVehicleIndex = 0;
    }

    private void SpawnPreview(VehicleData vehicleData)
    {
        ClearPreview();

        if (vehicleData == null)
            return;

        GameObject prefabToSpawn = vehicleData.previewPrefab != null
            ? vehicleData.previewPrefab
            : vehicleData.gameplayPrefab;

        if (prefabToSpawn == null)
        {
            Debug.LogWarning($"GarageManager: No preview or gameplay prefab assigned for {vehicleData.displayName}.");
            return;
        }

        Vector3 spawnPosition = previewSpawnPoint != null
            ? previewSpawnPoint.position
            : Vector3.zero;

        Quaternion spawnRotation = previewSpawnPoint != null
            ? previewSpawnPoint.rotation
            : Quaternion.identity;

        currentPreviewInstance = Instantiate(prefabToSpawn, spawnPosition, spawnRotation);

        currentPreviewInstance.transform.position += vehicleData.previewPositionOffset;
        currentPreviewInstance.transform.rotation *= Quaternion.Euler(vehicleData.previewRotationEuler);
        currentPreviewInstance.transform.localScale = vehicleData.previewScale;

        DisablePreviewGameplayComponents(currentPreviewInstance);
    }

    private void DisablePreviewGameplayComponents(GameObject previewObject)
    {
        if (previewObject == null)
            return;

        CarController[] controllers = previewObject.GetComponentsInChildren<CarController>(true);
        for (int i = 0; i < controllers.Length; i++)
        {
            controllers[i].SetCanDrive(false);
            controllers[i].enabled = false;
        }

        CarHopInput[] hopInputs = previewObject.GetComponentsInChildren<CarHopInput>(true);
        for (int i = 0; i < hopInputs.Length; i++)
        {
            hopInputs[i].enabled = false;
        }

        Rigidbody[] rigidbodies = previewObject.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < rigidbodies.Length; i++)
        {
            Rigidbody rb = rigidbodies[i];

            if (rb == null)
                continue;

            // Clear movement before making it kinematic.
            if (!rb.isKinematic)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            rb.isKinematic = true;
        }
    }

    private void ClearPreview()
    {
        if (currentPreviewInstance != null)
        {
            Destroy(currentPreviewInstance);
            currentPreviewInstance = null;
        }
    }

    private void RotatePreview()
    {
        if (!rotatePreviewCar)
            return;

        if (currentPreviewInstance == null)
            return;

        currentPreviewInstance.transform.Rotate(
            Vector3.up,
            previewRotationSpeed * Time.deltaTime,
            Space.World
        );
    }

    private void UpdateVehicleInfo(VehicleData vehicleData)
    {
        VehicleData[] roster = GetActiveRoster();

        if (vehicleNameText != null)
            vehicleNameText.text = vehicleData.displayName;

        if (vehicleDescriptionText != null)
            vehicleDescriptionText.text = vehicleData.description;

        if (vehicleCounterText != null)
            vehicleCounterText.text = $"{selectedVehicleIndex + 1} / {roster.Length}";

        if (equippedStatusText != null)
            equippedStatusText.text = GetStatusText(vehicleData);
    }

    private string GetStatusText(VehicleData vehicleData)
    {
        if (vehicleData == null)
            return "";

        if (!vehicleData.unlockedByDefault)
            return lockedText;

        if (vehicleData.vehicleId == equippedVehicleId)
            return equippedText;

        return availableText;
    }

    private void UpdateStats(VehicleData selectedVehicle)
    {
        if (statsDisplay == null)
            return;

        VehicleData[] roster = GetActiveRoster();

        GarageVehicleStats relativeStats =
            GarageVehicleStatCalculator.CalculateRelativeStats(
                selectedVehicle,
                roster,
                minimumVisibleStat,
                maximumVisibleStat
            );

        statsDisplay.DisplayStats(relativeStats);
    }

    private void ClearGarage()
    {
        ClearPreview();

        if (vehicleNameText != null)
            vehicleNameText.text = "No Vehicle";

        if (vehicleDescriptionText != null)
            vehicleDescriptionText.text = "";

        if (vehicleCounterText != null)
            vehicleCounterText.text = "0 / 0";

        if (equippedStatusText != null)
            equippedStatusText.text = "";

        if (statsDisplay != null)
            statsDisplay.Clear();
    }

    private VehicleData GetSelectedVehicle()
    {
        if (!HasVehicles())
            return null;

        VehicleData[] roster = GetActiveRoster();

        ClampSelectedIndex();

        return roster[selectedVehicleIndex];
    }

    private VehicleData[] GetActiveRoster()
    {
        if (vehicleDatabase != null && vehicleDatabase.HasVehicles())
            return vehicleDatabase.GetVehicles();

        return vehicleRoster;
    }

    private bool HasVehicles()
    {
        VehicleData[] roster = GetActiveRoster();
        return roster != null && roster.Length > 0;
    }

    private void ClampSelectedIndex()
    {
        if (!HasVehicles())
        {
            selectedVehicleIndex = 0;
            return;
        }

        VehicleData[] roster = GetActiveRoster();

        selectedVehicleIndex = Mathf.Clamp(selectedVehicleIndex, 0, roster.Length - 1);
    }

    private void OnValidate()
    {
        maximumVisibleStat = Mathf.Max(maximumVisibleStat, minimumVisibleStat);
        ClampSelectedIndex();
    }
}