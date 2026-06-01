using UnityEngine;
using System.Collections.Generic;

public class GridStartManager : MonoBehaviour
{
    [Header("References")]
    public Transform playerCar;
    public GameObject aiCarPrefab;
    public Transform waypointParent;
    public Transform checkpointParent;
    public Transform gridParent;

    [Header("Spawn Counts")]
    [Min(0)] public int aiCount = 3;

    [Header("Grid Layout")]
    [Min(1)] public int columns = 2;
    [Min(1)] public int rows = 2;
    public float sideSpacing = 3.5f;
    public float rowSpacing = 5.5f;
    public float verticalOffset = 0.5f;

    [Header("Behavior")]
    public bool generateGridOnStart = false;
    public bool clearOldGridBeforeGenerating = true;
    public bool spawnAIOnStart = false;
    public bool randomizePositions = true;
    public bool autoCalculateRows = true;

    [Header("Database AI")]
    [Tooltip("If ON, pending VehicleData AI selections are cleared after SetupRaceGrid uses them.")]
    public bool clearPendingAIVehiclesAfterSetup = true;

    [Header("Debug")]
    public bool drawGizmos = true;
    public Color gizmoColor = Color.yellow;
    public bool logDebugMessages = true;

    private readonly List<Transform> gridPositions = new List<Transform>();
    private readonly List<GameObject> spawnedAICars = new List<GameObject>();

    private List<VehicleData> pendingAIVehiclesForNextGrid;

    void Start()
    {
        if (generateGridOnStart)
            GenerateGridPositions();

        if (spawnAIOnStart)
            SetupRaceGrid();
    }

    public void SetPlayerCar(Transform player)
    {
        playerCar = player;
    }

    public void ConfigureForRace(Transform waypointRoot, Transform checkpointRoot, int aiToSpawn)
    {
        waypointParent = waypointRoot;
        checkpointParent = checkpointRoot;
        aiCount = aiToSpawn;
    }

    public void SetPendingAIVehiclesForNextGrid(List<VehicleData> aiVehicles)
    {
        if (aiVehicles == null)
        {
            pendingAIVehiclesForNextGrid = null;
            return;
        }

        pendingAIVehiclesForNextGrid = new List<VehicleData>(aiVehicles);
        aiCount = pendingAIVehiclesForNextGrid.Count;

        Log("Pending AI vehicles set for next grid. Count=" + pendingAIVehiclesForNextGrid.Count);
    }

    public void ClearPendingAIVehicles()
    {
        pendingAIVehiclesForNextGrid = null;
    }

    public void SetupRaceGrid()
    {
        List<VehicleData> vehiclesToUse = pendingAIVehiclesForNextGrid;

        if (vehiclesToUse != null)
        {
            SetupRaceGridInternal(vehiclesToUse, null);

            if (clearPendingAIVehiclesAfterSetup)
                pendingAIVehiclesForNextGrid = null;

            return;
        }

        SetupRaceGridInternal(null, aiCarPrefab);
    }

    public void SetupRaceGridWithAIVehicles(List<VehicleData> aiVehicles)
    {
        SetupRaceGridInternal(aiVehicles, null);
    }

    private void SetupRaceGridInternal(List<VehicleData> aiVehicles, GameObject fallbackAIPrefab)
    {
        bool usingVehicleDataList = aiVehicles != null;

        int finalAICount = usingVehicleDataList ? aiVehicles.Count : aiCount;
        aiCount = finalAICount;

        if (autoCalculateRows)
        {
            int totalNeededSlots = finalAICount + (playerCar != null ? 1 : 0);
            rows = Mathf.CeilToInt((float)Mathf.Max(1, totalNeededSlots) / columns);
        }

        GenerateGridPositions();

        int requiredSlots = finalAICount + (playerCar != null ? 1 : 0);

        if (gridPositions.Count < requiredSlots)
        {
            Debug.LogError($"Not enough grid positions. Need {requiredSlots}, but only have {gridPositions.Count}.");
            return;
        }

        ClearSpawnedAI();

        List<Transform> availableSpots = new List<Transform>(gridPositions);

        if (randomizePositions)
            ShuffleList(availableSpots);

        int spotIndex = 0;

        // Place player
        if (playerCar != null)
        {
            PlaceTransformAtSpot(playerCar, availableSpots[spotIndex]);

            RacerProgress playerProgress = playerCar.GetComponent<RacerProgress>();
            if (playerProgress != null)
            {
                AssignCheckpointProgressData(playerProgress);
                playerProgress.SetStartingGridPosition(spotIndex);
            }

            PlayerRespawn playerRespawn = playerCar.GetComponent<PlayerRespawn>();
            if (playerRespawn != null)
            {
                playerRespawn.SetRespawnPoint(playerCar.position, playerCar.rotation);
            }

            spotIndex++;
        }

        // Spawn AI
        for (int i = 0; i < finalAICount; i++)
        {
            VehicleData vehicleData = usingVehicleDataList && i < aiVehicles.Count ? aiVehicles[i] : null;

            GameObject prefabToSpawn = vehicleData != null && vehicleData.aiPrefab != null
                ? vehicleData.aiPrefab
                : fallbackAIPrefab;

            if (prefabToSpawn == null)
            {
                Debug.LogWarning($"AI slot {i + 1} has no AI prefab. Skipping.");
                continue;
            }

            GameObject aiInstance = Instantiate(prefabToSpawn);

            aiInstance.name = vehicleData != null
                ? $"{vehicleData.displayName}_AI_{i + 1}"
                : $"AICar_{i + 1}";

            spawnedAICars.Add(aiInstance);

            PlaceTransformAtSpot(aiInstance.transform, availableSpots[spotIndex]);

            SyncCarProfile(aiInstance, vehicleData);
            SetupAIController(aiInstance);
            SetupRacerProgress(aiInstance, spotIndex);

            Log("Spawned grid AI: " + aiInstance.name + " from " + prefabToSpawn.name);

            spotIndex++;
        }
    }

    public void GenerateGridPositions()
    {
        if (autoCalculateRows)
        {
            int totalNeededSlots = aiCount + (playerCar != null ? 1 : 0);
            rows = Mathf.CeilToInt((float)Mathf.Max(1, totalNeededSlots) / columns);
        }

        if (gridParent == null)
        {
            GameObject parent = new GameObject("GeneratedStartGrid");
            parent.transform.SetParent(transform);
            parent.transform.localPosition = Vector3.zero;
            parent.transform.localRotation = Quaternion.identity;
            gridParent = parent.transform;
        }

        if (clearOldGridBeforeGenerating)
        {
            ClearGridChildren();
        }

        gridPositions.Clear();

        int totalSlots = rows * columns;
        int created = 0;

        for (int row = 0; row < rows; row++)
        {
            int carsInThisRow = Mathf.Min(columns, totalSlots - created);
            float rowWidth = (carsInThisRow - 1) * sideSpacing;
            float startX = -rowWidth * 0.5f;

            for (int col = 0; col < carsInThisRow; col++)
            {
                Vector3 localPos = new Vector3(
                    startX + col * sideSpacing,
                    verticalOffset,
                    -row * rowSpacing
                );

                GameObject slot = new GameObject($"Grid_{created}");
                slot.transform.SetParent(gridParent);
                slot.transform.localPosition = localPos;
                slot.transform.localRotation = Quaternion.identity;

                gridPositions.Add(slot.transform);
                created++;
            }
        }
    }

    private void SyncCarProfile(GameObject aiInstance, VehicleData vehicleData)
    {
        if (aiInstance == null || vehicleData == null)
            return;

        CarProfile profile = aiInstance.GetComponent<CarProfile>();

        if (profile == null)
            profile = aiInstance.GetComponentInChildren<CarProfile>(true);

        if (profile == null)
            return;

        profile.carID = vehicleData.vehicleId;
        profile.displayName = vehicleData.displayName;
        profile.vehicleType = vehicleData.vehicleType;
    }

    private void SetupAIController(GameObject aiInstance)
    {
        if (aiInstance == null)
            return;

        AICarController aiController = aiInstance.GetComponent<AICarController>();

        if (aiController == null)
            aiController = aiInstance.GetComponentInChildren<AICarController>(true);

        if (aiController == null)
            return;

        aiController.canDrive = false;
        aiController.ResetRaceStartTimer();

        if (waypointParent != null)
        {
            aiController.SetWaypointParent(waypointParent);
        }

        aiController.ResetToClosestWaypoint();
    }

    private void SetupRacerProgress(GameObject aiInstance, int spotIndex)
    {
        if (aiInstance == null)
            return;

        RacerProgress aiProgress = aiInstance.GetComponent<RacerProgress>();

        if (aiProgress == null)
            aiProgress = aiInstance.GetComponentInChildren<RacerProgress>(true);

        if (aiProgress == null)
            return;

        AssignCheckpointProgressData(aiProgress);
        aiProgress.SetStartingGridPosition(spotIndex);
    }

    void AssignCheckpointProgressData(RacerProgress progress)
    {
        if (progress == null || checkpointParent == null) return;

        progress.SetCheckpointParent(checkpointParent);
        progress.ResetProgress();
    }

    void PlaceTransformAtSpot(Transform obj, Transform spot)
    {
        Rigidbody rb = obj.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = false;
        }

        obj.position = spot.position;
        obj.rotation = spot.rotation;
    }

    void ClearGridChildren()
    {
        if (gridParent == null) return;

        for (int i = gridParent.childCount - 1; i >= 0; i--)
        {
            Destroy(gridParent.GetChild(i).gameObject);
        }
    }

    public void ClearSpawnedAI()
    {
        for (int i = 0; i < spawnedAICars.Count; i++)
        {
            if (spawnedAICars[i] != null)
            {
                Destroy(spawnedAICars[i]);
            }
        }

        spawnedAICars.Clear();
    }

    void ShuffleList(List<Transform> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);
            Transform temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        Gizmos.color = gizmoColor;

        Transform parentToUse = gridParent != null ? gridParent : transform;

        int totalNeededSlots = aiCount + (playerCar != null ? 1 : 0);
        int rowsToDraw = autoCalculateRows
            ? Mathf.CeilToInt((float)Mathf.Max(1, totalNeededSlots) / columns)
            : rows;

        int totalSlots = rowsToDraw * columns;
        int created = 0;

        for (int row = 0; row < rowsToDraw; row++)
        {
            int carsInThisRow = Mathf.Min(columns, totalSlots - created);
            float rowWidth = (carsInThisRow - 1) * sideSpacing;
            float startX = -rowWidth * 0.5f;

            for (int col = 0; col < carsInThisRow; col++)
            {
                Vector3 localPos = new Vector3(
                    startX + col * sideSpacing,
                    verticalOffset,
                    -row * rowSpacing
                );

                Vector3 worldPos = parentToUse.TransformPoint(localPos);

                Gizmos.DrawWireCube(worldPos, new Vector3(1.5f, 0.5f, 3f));
                Gizmos.DrawLine(worldPos, worldPos + parentToUse.forward * 2f);

                created++;
            }
        }
    }

    public List<GameObject> GetSpawnedAICars()
    {
        return spawnedAICars;
    }

    private void Log(string message)
    {
        if (logDebugMessages)
            Debug.Log("[GridStartManager] " + message);
    }
}
