using UnityEngine;

public class SelectedVehicleSpawner : MonoBehaviour
{
    public enum RespawnSetupMode
    {
        Freeroam,
        Race,
        AutoDetect
    }

    public enum SpawnHeightReferenceMode
    {
        SkidPlateCollider,
        NonTriggerColliders,
        GeneratedWheelRenderers,
        LowestOfGeneratedWheelsAndColliders
    }

    [Header("Vehicle Database")]
    public VehicleDatabase vehicleDatabase;

    [Header("Garage Roster Fallback")]
    public VehicleData[] vehicleRoster;

    [Header("Spawn")]
    public Transform spawnPoint;
    public bool spawnOnStart = true;

    [Header("Spawn Pivot Height Alignment")]
    public bool alignVehicleLowestPointToSpawnPivot = true;
    public SpawnHeightReferenceMode spawnHeightReferenceMode = SpawnHeightReferenceMode.SkidPlateCollider;
    public float spawnPivotClearance = 0.03f;

    [Header("Skid Plate Reference")]
    public string skidPlateColliderName = "Bottom_SkidPlate_AntiStuck";
    public bool fallbackToColliderIfSkidPlateMissing = true;

    [Header("Generated Wheel Names")]
    public string frontLeftGeneratedWheelName = "Generated_FL_Wheel";
    public string frontRightGeneratedWheelName = "Generated_FR_Wheel";
    public string rearLeftGeneratedWheelName = "Generated_RL_Wheel";
    public string rearRightGeneratedWheelName = "Generated_RR_Wheel";

    [Header("Lowest Point Options")]
    public bool ignoreTriggerCollidersForSpawnHeight = true;
    public bool ignoreDisabledCollidersForSpawnHeight = true;
    public bool ignoreParticleRenderersForSpawnHeight = true;

    [Header("Fallback")]
    public bool useFirstVehicleIfNoSave = true;

    [Header("Camera Follow")]
    public bool assignCameraFollowTarget = true;
    public CameraFollow cameraFollow;

    [Header("Race Scene Registration")]
    public bool registerWithRaceManager = true;
    public RaceManager raceManager;

    [Header("Player Respawn Setup")]
    public bool configurePlayerRespawn = true;
    public RespawnSetupMode respawnSetupMode = RespawnSetupMode.AutoDetect;
    public Transform freeroamStartPoint;
    public Transform raceInitialCheckpointPoint;
    public bool tagSpawnedVehicleAsPlayer = true;

    [Header("Race Return Handling")]
    [Tooltip("If true, race return ignores PlayerSpawnPoint and lets FreeroamReturnManager place the spawned vehicle at the mission marker.")]
    public bool letFreeroamReturnManagerPositionRaceReturns = true;

    [Tooltip("Optional temporary spawn point used only while returning from race. If blank, this spawner object's transform is used.")]
    public Transform raceReturnTemporarySpawnPoint;

    [Header("Debug")]
    public bool logSpawnedVehicle = true;
    public bool logRespawnSetup = true;
    public bool logSpawnAlignment = true;

    private GameObject spawnedVehicle;

    private void Start()
    {
        if (spawnOnStart)
            SpawnEquippedVehicle();
    }

    public GameObject SpawnEquippedVehicle()
    {
        ClearSpawnedVehicle();

        VehicleData equippedVehicle = FindEquippedVehicle();

        if (equippedVehicle == null)
        {
            Debug.LogWarning("SelectedVehicleSpawner: No equipped vehicle found.");
            return null;
        }

        if (equippedVehicle.gameplayPrefab == null)
        {
            Debug.LogWarning("SelectedVehicleSpawner: Equipped vehicle has no gameplay prefab: " + equippedVehicle.displayName);
            return null;
        }

        bool isReturningFromRace =
            letFreeroamReturnManagerPositionRaceReturns &&
            RaceLaunchData.ReturningFromRace;

        Transform finalSpawnPoint = GetFinalSpawnPoint(isReturningFromRace);

        spawnedVehicle = Instantiate(
            equippedVehicle.gameplayPrefab,
            finalSpawnPoint.position,
            finalSpawnPoint.rotation
        );

        if (tagSpawnedVehicleAsPlayer)
            spawnedVehicle.tag = "Player";

        SyncSpawnedVehicleProfile(spawnedVehicle, equippedVehicle);
        EnableGameplayComponents(spawnedVehicle);

        // Normal freeroam spawn uses PlayerSpawnPoint alignment.
        // Race return does NOT, because FreeroamReturnManager places the car at the mission marker.
        if (!isReturningFromRace && alignVehicleLowestPointToSpawnPivot)
            AlignVehicleLowestPointToSpawnPivot(spawnedVehicle, finalSpawnPoint);

        // Normal freeroam spawn configures respawn here.
        // Race return does NOT, because FreeroamReturnManager sets respawn after placing the car.
        if (!isReturningFromRace && configurePlayerRespawn)
            TryConfigurePlayerRespawn(spawnedVehicle);

        if (assignCameraFollowTarget)
            TryAssignCameraFollowTarget(spawnedVehicle.transform, equippedVehicle);

        if (registerWithRaceManager)
            TryRegisterWithRaceManager(spawnedVehicle);

        if (logSpawnedVehicle)
        {
            Debug.Log(
                "SelectedVehicleSpawner: Spawned equipped vehicle: " +
                equippedVehicle.displayName +
                " | ReturningFromRace=" +
                isReturningFromRace +
                " | SpawnPointUsed=" +
                finalSpawnPoint.name
            );
        }

        return spawnedVehicle;
    }

    private Transform GetFinalSpawnPoint(bool isReturningFromRace)
    {
        if (isReturningFromRace)
        {
            if (raceReturnTemporarySpawnPoint != null)
                return raceReturnTemporarySpawnPoint;

            return transform;
        }

        if (spawnPoint != null)
            return spawnPoint;

        return transform;
    }

    private void SyncSpawnedVehicleProfile(GameObject vehicle, VehicleData vehicleData)
    {
        if (vehicle == null || vehicleData == null)
            return;

        CarProfile profile = vehicle.GetComponent<CarProfile>();

        if (profile == null)
            profile = vehicle.GetComponentInChildren<CarProfile>(true);

        if (profile == null)
            return;

        profile.carID = vehicleData.vehicleId;
        profile.displayName = vehicleData.displayName;
        profile.vehicleType = vehicleData.vehicleType;
    }

    private void AlignVehicleLowestPointToSpawnPivot(GameObject vehicle, Transform finalSpawnPoint)
    {
        if (vehicle == null || finalSpawnPoint == null)
            return;

        if (!TryGetSpawnReferenceY(vehicle.transform, out float referenceY))
        {
            Debug.LogWarning("SelectedVehicleSpawner: Could not calculate vehicle lowest/reference point for spawn alignment.");
            return;
        }

        float targetY = finalSpawnPoint.position.y + spawnPivotClearance;
        float deltaY = targetY - referenceY;

        vehicle.transform.position += Vector3.up * deltaY;
        ResetVehicleVelocity(vehicle);

        if (logSpawnAlignment)
        {
            Debug.Log(
                "SelectedVehicleSpawner: Aligned vehicle lowest point to spawn pivot. " +
                "Mode=" + spawnHeightReferenceMode +
                ", SpawnPivotY=" + finalSpawnPoint.position.y.ToString("F3") +
                ", Clearance=" + spawnPivotClearance.ToString("F3") +
                ", ReferenceY=" + referenceY.ToString("F3") +
                ", DeltaY=" + deltaY.ToString("F3") +
                ", FinalPosition=" + vehicle.transform.position
            );
        }
    }

    private bool TryGetSpawnReferenceY(Transform vehicleRoot, out float referenceY)
    {
        referenceY = 0f;

        switch (spawnHeightReferenceMode)
        {
            case SpawnHeightReferenceMode.SkidPlateCollider:
                if (TryGetSkidPlateColliderBottomY(vehicleRoot, out referenceY))
                    return true;

                Debug.LogWarning("SelectedVehicleSpawner: Skid plate not found: " + skidPlateColliderName);

                if (fallbackToColliderIfSkidPlateMissing)
                {
                    Debug.LogWarning("SelectedVehicleSpawner: Falling back to lowest non-trigger collider.");
                    return TryGetLowestColliderY(vehicleRoot, out referenceY);
                }

                return false;

            case SpawnHeightReferenceMode.NonTriggerColliders:
                return TryGetLowestColliderY(vehicleRoot, out referenceY);

            case SpawnHeightReferenceMode.GeneratedWheelRenderers:
                return TryGetLowestGeneratedWheelRendererY(vehicleRoot, out referenceY);

            case SpawnHeightReferenceMode.LowestOfGeneratedWheelsAndColliders:
                return TryGetLowestOfWheelsAndColliders(vehicleRoot, out referenceY);

            default:
                return TryGetLowestColliderY(vehicleRoot, out referenceY);
        }
    }

    private bool TryGetSkidPlateColliderBottomY(Transform vehicleRoot, out float skidPlateBottomY)
    {
        skidPlateBottomY = 0f;

        if (vehicleRoot == null || string.IsNullOrWhiteSpace(skidPlateColliderName))
            return false;

        Transform skidPlate = FindDeepChild(vehicleRoot, skidPlateColliderName);

        if (skidPlate == null)
            return false;

        Collider skidCollider = skidPlate.GetComponent<Collider>();

        if (skidCollider == null)
            skidCollider = skidPlate.GetComponentInChildren<Collider>(true);

        if (skidCollider == null)
            return false;

        if (ignoreDisabledCollidersForSpawnHeight && !skidCollider.enabled)
            return false;

        if (ignoreTriggerCollidersForSpawnHeight && skidCollider.isTrigger)
            return false;

        skidPlateBottomY = skidCollider.bounds.min.y;
        return true;
    }

    private bool TryGetLowestOfWheelsAndColliders(Transform vehicleRoot, out float lowestY)
    {
        lowestY = 0f;

        bool hasWheelLowest = TryGetLowestGeneratedWheelRendererY(vehicleRoot, out float wheelLowestY);
        bool hasColliderLowest = TryGetLowestColliderY(vehicleRoot, out float colliderLowestY);

        if (hasWheelLowest && hasColliderLowest)
        {
            lowestY = Mathf.Min(wheelLowestY, colliderLowestY);
            return true;
        }

        if (hasWheelLowest)
        {
            lowestY = wheelLowestY;
            return true;
        }

        if (hasColliderLowest)
        {
            lowestY = colliderLowestY;
            return true;
        }

        return false;
    }

    private bool TryGetLowestGeneratedWheelRendererY(Transform vehicleRoot, out float lowestY)
    {
        lowestY = 0f;

        bool found = false;
        float minY = float.MaxValue;

        TryIncludeWheelRendererLowestY(vehicleRoot, frontLeftGeneratedWheelName, ref found, ref minY);
        TryIncludeWheelRendererLowestY(vehicleRoot, frontRightGeneratedWheelName, ref found, ref minY);
        TryIncludeWheelRendererLowestY(vehicleRoot, rearLeftGeneratedWheelName, ref found, ref minY);
        TryIncludeWheelRendererLowestY(vehicleRoot, rearRightGeneratedWheelName, ref found, ref minY);

        if (!found)
        {
            Transform[] allChildren = vehicleRoot.GetComponentsInChildren<Transform>(true);

            for (int i = 0; i < allChildren.Length; i++)
            {
                Transform child = allChildren[i];

                if (child == null)
                    continue;

                if (!child.name.StartsWith("Generated_") || !child.name.Contains("_Wheel"))
                    continue;

                TryIncludeRendererLowestY(child, ref found, ref minY);
            }
        }

        if (!found)
            return false;

        lowestY = minY;
        return true;
    }

    private void TryIncludeWheelRendererLowestY(Transform vehicleRoot, string wheelName, ref bool found, ref float minY)
    {
        if (vehicleRoot == null || string.IsNullOrWhiteSpace(wheelName))
            return;

        Transform wheel = FindDeepChild(vehicleRoot, wheelName);

        if (wheel == null)
            return;

        TryIncludeRendererLowestY(wheel, ref found, ref minY);
    }

    private void TryIncludeRendererLowestY(Transform root, ref bool found, ref float minY)
    {
        if (root == null)
            return;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

        if (renderers == null || renderers.Length == 0)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null)
                continue;

            if (ignoreParticleRenderersForSpawnHeight && renderer is ParticleSystemRenderer)
                continue;

            float rendererMinY = renderer.bounds.min.y;

            if (rendererMinY < minY)
            {
                minY = rendererMinY;
                found = true;
            }
        }
    }

    private bool TryGetLowestColliderY(Transform vehicleRoot, out float lowestY)
    {
        lowestY = 0f;

        if (vehicleRoot == null)
            return false;

        Collider[] colliders = vehicleRoot.GetComponentsInChildren<Collider>(true);

        if (colliders == null || colliders.Length == 0)
            return false;

        bool found = false;
        float minY = float.MaxValue;

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider col = colliders[i];

            if (col == null)
                continue;

            if (ignoreDisabledCollidersForSpawnHeight && !col.enabled)
                continue;

            if (ignoreTriggerCollidersForSpawnHeight && col.isTrigger)
                continue;

            float colliderMinY = col.bounds.min.y;

            if (colliderMinY < minY)
            {
                minY = colliderMinY;
                found = true;
            }
        }

        if (!found)
            return false;

        lowestY = minY;
        return true;
    }

    private VehicleData FindEquippedVehicle()
    {
        VehicleData[] roster = GetActiveRoster();

        if (roster == null || roster.Length == 0)
        {
            Debug.LogWarning("SelectedVehicleSpawner: Vehicle roster is empty.");
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

    private void EnableGameplayComponents(GameObject vehicle)
    {
        if (vehicle == null)
            return;

        Rigidbody[] rigidbodies = vehicle.GetComponentsInChildren<Rigidbody>(true);

        for (int i = 0; i < rigidbodies.Length; i++)
        {
            Rigidbody rb = rigidbodies[i];

            if (rb == null)
                continue;

            rb.isKinematic = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        CarController[] controllers = vehicle.GetComponentsInChildren<CarController>(true);

        for (int i = 0; i < controllers.Length; i++)
        {
            controllers[i].enabled = true;
            controllers[i].SetCanDrive(true);
            controllers[i].ResetVehicleVelocity();
        }

        CarHopInput[] hopInputs = vehicle.GetComponentsInChildren<CarHopInput>(true);

        for (int i = 0; i < hopInputs.Length; i++)
            hopInputs[i].enabled = true;
    }

    private void ResetVehicleVelocity(GameObject vehicle)
    {
        if (vehicle == null)
            return;

        Rigidbody[] rigidbodies = vehicle.GetComponentsInChildren<Rigidbody>(true);

        for (int i = 0; i < rigidbodies.Length; i++)
        {
            Rigidbody rb = rigidbodies[i];

            if (rb == null)
                continue;

            if (rb.isKinematic)
                continue;

            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        CarController[] controllers = vehicle.GetComponentsInChildren<CarController>(true);

        for (int i = 0; i < controllers.Length; i++)
            controllers[i].ResetVehicleVelocity();
    }

    private void TryConfigurePlayerRespawn(GameObject vehicle)
    {
        if (vehicle == null)
            return;

        PlayerRespawn respawn = vehicle.GetComponent<PlayerRespawn>();

        if (respawn == null)
            respawn = vehicle.GetComponentInChildren<PlayerRespawn>(true);

        if (respawn == null)
        {
            Debug.LogWarning("SelectedVehicleSpawner: Spawned vehicle has no PlayerRespawn component.");
            return;
        }

        RespawnSetupMode finalMode = GetFinalRespawnSetupMode();

        if (finalMode == RespawnSetupMode.Race)
        {
            Transform checkpointPoint = raceInitialCheckpointPoint != null ? raceInitialCheckpointPoint : spawnPoint;

            if (checkpointPoint != null)
                respawn.ConfigureForRace(checkpointPoint);
            else
                respawn.ConfigureForRace(vehicle.transform);

            if (logRespawnSetup)
                Debug.Log("SelectedVehicleSpawner: Configured PlayerRespawn for Race.");
        }
        else
        {
            Transform startPoint = freeroamStartPoint != null ? freeroamStartPoint : spawnPoint;

            if (startPoint != null)
                respawn.ConfigureForFreeroam(startPoint);
            else
                respawn.ConfigureForFreeroam(vehicle.transform);

            if (logRespawnSetup)
                Debug.Log("SelectedVehicleSpawner: Configured PlayerRespawn for Freeroam.");
        }
    }

    private RespawnSetupMode GetFinalRespawnSetupMode()
    {
        if (respawnSetupMode != RespawnSetupMode.AutoDetect)
            return respawnSetupMode;

        RaceManager foundRaceManager = raceManager;

        if (foundRaceManager == null)
            foundRaceManager = RaceManager.Instance;

        if (foundRaceManager == null)
            foundRaceManager = FindObjectOfType<RaceManager>();

        if (foundRaceManager != null)
            return RespawnSetupMode.Race;

        return RespawnSetupMode.Freeroam;
    }

    private void TryAssignCameraFollowTarget(Transform target, VehicleData vehicleData)
    {
        if (target == null)
            return;

        if (cameraFollow == null)
            cameraFollow = FindObjectOfType<CameraFollow>();

        if (cameraFollow == null)
        {
            Debug.LogWarning("SelectedVehicleSpawner: No CameraFollow found in scene.");
            return;
        }

        cameraFollow.SetTarget(target);
        cameraFollow.ApplyVehicleData(vehicleData);
    }

    private void TryRegisterWithRaceManager(GameObject vehicle)
    {
        if (vehicle == null)
            return;

        if (raceManager == null)
            raceManager = RaceManager.Instance;

        if (raceManager == null)
            raceManager = FindObjectOfType<RaceManager>();

        if (raceManager == null)
            return;

        CarController controller = vehicle.GetComponent<CarController>();

        if (controller == null)
            controller = vehicle.GetComponentInChildren<CarController>(true);

        if (controller == null)
        {
            Debug.LogWarning("SelectedVehicleSpawner: Spawned vehicle has no CarController to register with RaceManager.");
            return;
        }

        raceManager.SetPlayerCar(controller);
        Debug.Log("SelectedVehicleSpawner: Registered spawned player car with RaceManager.");
    }

    private void ClearSpawnedVehicle()
    {
        if (spawnedVehicle != null)
        {
            Destroy(spawnedVehicle);
            spawnedVehicle = null;
        }
    }

    private Transform FindDeepChild(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
            return null;

        if (root.name == childName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            Transform result = FindDeepChild(child, childName);

            if (result != null)
                return result;
        }

        return null;
    }

    public GameObject GetSpawnedVehicle()
    {
        return spawnedVehicle;
    }
}