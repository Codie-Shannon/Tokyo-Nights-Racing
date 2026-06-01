using System.Collections.Generic;
using UnityEngine;

public enum TrafficPrefabSelectionMode
{
    WeightedRandom,
    Random,
    Cycle
}

public class TrafficSpawner : MonoBehaviour
{
    [Header("Traffic Network")]
    public Transform trafficNetworkRoot;
    public bool autoFindSpawnNodes = true;
    public bool autoFindTrafficCars = true;

    [Header("Manual Spawn Nodes")]
    public List<TrafficNode> spawnNodes = new List<TrafficNode>();

    [Header("Initial Population Nodes")]
    [Tooltip("If true, the first scene load can use SpawnOnly + Normal nodes to populate the city.")]
    public bool useNormalNodesForInitialSpawn = true;

    [Tooltip("If true, Intersection nodes can also be used only for initial population. Usually keep this OFF.")]
    public bool useIntersectionNodesForInitialSpawn = false;

    [Tooltip("Initial spawn candidates found from the network. Used only during Spawn Initial Traffic.")]
    public List<TrafficNode> initialSpawnNodes = new List<TrafficNode>();

    [Tooltip("Normal/Intersection initial spawn nodes must have at least one valid next node.")]
    public bool requireInitialSpawnNodeHasNextNode = true;

    [Tooltip("Do not use initial spawn nodes too close to CityExitDespawn nodes.")]
    public bool rejectInitialSpawnTooCloseToCityExit = true;

    [Tooltip("Minimum distance from an initial spawn node to a CityExitDespawn node.")]
    public float minimumInitialSpawnDistanceFromCityExit = 12f;

    [Header("Traffic Cars")]
    public List<TrafficCarAI> trafficCars = new List<TrafficCarAI>();

    [Header("Traffic Vehicle Database")]
    public TrafficVehicleDatabase trafficVehicleDatabase;
    public TrafficPrefabSelectionMode prefabSelectionMode = TrafficPrefabSelectionMode.WeightedRandom;

    [Tooltip("Legacy fallback prefab. Used only if Traffic Vehicle Database is missing/empty.")]
    public TrafficCarAI trafficCarPrefab;

    [Header("Spawn Settings")]
    public int maxActiveCars = 100;
    public bool instantiateMissingCars = false;
    public Transform trafficCarParent;
    public float respawnDelayMin = 2f;
    public float respawnDelayMax = 6f;
    public float checkInterval = 1f;

    [Header("Traffic Density Setting")]
    [Tooltip("If true, maxActiveCars is loaded from PlayerPrefs key Settings_TrafficDensity.")]
    public bool useSavedTrafficDensity = true;

    [Tooltip("PlayerPrefs key written by SettingsMenuManager.")]
    public string trafficDensityPlayerPrefsKey = "Settings_TrafficDensity";

    [Tooltip("Default traffic density if no saved setting exists.")]
    public int defaultTrafficDensity = 100;

    [Tooltip("Lowest allowed traffic density.")]
    public int minimumTrafficDensity = 50;

    [Tooltip("Highest allowed traffic density.")]
    public int maximumTrafficDensity = 200;

    [Header("Spawn Positioning")]
    public bool spawnAtSpawnNode = true;
    public bool rotateTowardFirstTargetNode = true;
    public float spawnHeightOffset = 0.8f;
    public bool snapSpawnToGround = true;
    public float groundSnapRayStartHeight = 20f;
    public float groundSnapRayDistance = 60f;
    public LayerMask groundSnapLayers = ~0;

    [Header("Spawn Target Safety")]
    public bool rejectCityExitAsFirstTarget = true;
    public bool rejectNoTrafficAsFirstTarget = true;

    [Tooltip("First target after spawn must be at least this far from the spawn node.")]
    public float minimumSpawnToTargetDistance = 6f;

    [Tooltip("Used only for initial population from Normal/Intersection nodes. Allows shorter target spacing if needed.")]
    public float minimumInitialSpawnToTargetDistance = 3f;

    [Header("Player Avoidance")]
    public Transform player;
    public float minDistanceFromPlayer = 25f;

    [Header("Spawn Blocking")]
    public bool checkSpawnBlocked = true;
    public LayerMask spawnBlockLayers = ~0;
    public float spawnBlockRadius = 3f;

    [Header("First Node After Spawn")]
    public bool useSpawnNodeNextNodeAsCurrent = true;
    public bool randomizeSpawnExitNode = true;

    [Header("Startup")]
    public bool spawnOnStart = true;
    public bool disableCarsBeforeInitialSpawn = true;

    [Header("Debug")]
    public bool logSpawnerActions = false;
    public bool drawGizmos = true;

    private readonly Dictionary<TrafficCarAI, float> respawnTimers = new Dictionary<TrafficCarAI, float>();
    private float checkTimer;

    private void Start()
    {
        ApplySavedTrafficDensity();

        if (autoFindSpawnNodes)
        {
            FindSpawnNodes();
            FindInitialSpawnNodes();
        }

        if (autoFindTrafficCars)
            FindTrafficCars();

        if (instantiateMissingCars)
            CreateMissingCars();

        if (disableCarsBeforeInitialSpawn)
            DisableAllManagedCars();

        if (spawnOnStart)
            SpawnInitialTraffic();
    }

    private void Update()
    {
        checkTimer += Time.deltaTime;

        if (checkTimer < checkInterval)
            return;

        checkTimer = 0f;
        UpdateRespawns();
    }

    public void ApplySavedTrafficDensity()
    {
        if (!useSavedTrafficDensity)
            return;

        int savedDensity = PlayerPrefs.GetInt(trafficDensityPlayerPrefsKey, defaultTrafficDensity);
        savedDensity = ClampTrafficDensity(savedDensity);

        maxActiveCars = savedDensity;

        if (logSpawnerActions)
            Debug.Log("TrafficSpawner applied saved traffic density: " + maxActiveCars);
    }

    public void SetTrafficDensityRuntime(int density, bool respawnNow)
    {
        density = ClampTrafficDensity(density);

        maxActiveCars = density;
        PlayerPrefs.SetInt(trafficDensityPlayerPrefsKey, density);
        PlayerPrefs.Save();

        TrimActiveCarsToLimit();

        if (respawnNow)
            FillTrafficToLimit();

        if (logSpawnerActions)
            Debug.Log("TrafficSpawner runtime traffic density set to: " + maxActiveCars);
    }

    private int ClampTrafficDensity(int density)
    {
        if (density <= 50)
            return 50;

        if (density <= 100)
            return 100;

        if (density <= 150)
            return 150;

        return 200;
    }

    private void TrimActiveCarsToLimit()
    {
        int activeCount = CountActiveCars();

        if (activeCount <= maxActiveCars)
            return;

        for (int i = trafficCars.Count - 1; i >= 0; i--)
        {
            TrafficCarAI car = trafficCars[i];

            if (car == null)
                continue;

            if (!car.gameObject.activeInHierarchy)
                continue;

            car.gameObject.SetActive(false);
            activeCount--;

            if (activeCount <= maxActiveCars)
                break;
        }
    }

    private void FillTrafficToLimit()
    {
        int activeCount = CountActiveCars();

        if (activeCount >= maxActiveCars)
            return;

        foreach (TrafficCarAI car in trafficCars)
        {
            if (car == null)
                continue;

            if (car.gameObject.activeInHierarchy)
                continue;

            bool didSpawn = TrySpawnCar(car, false);

            if (didSpawn)
                activeCount++;

            if (activeCount >= maxActiveCars)
                break;
        }
    }

    [ContextMenu("Find Spawn Nodes")]
    public void FindSpawnNodes()
    {
        spawnNodes.Clear();

        TrafficNode[] nodes = GetAllTrafficNodes();

        foreach (TrafficNode node in nodes)
        {
            if (node == null)
                continue;

            if (node.nodeType == TrafficNodeType.SpawnOnly)
                spawnNodes.Add(node);
        }

        if (logSpawnerActions)
            Debug.Log("TrafficSpawner found runtime spawn nodes: " + spawnNodes.Count);
    }

    [ContextMenu("Find Initial Spawn Nodes")]
    public void FindInitialSpawnNodes()
    {
        initialSpawnNodes.Clear();

        TrafficNode[] nodes = GetAllTrafficNodes();

        foreach (TrafficNode node in nodes)
        {
            if (node == null)
                continue;

            if (!IsValidInitialSpawnCandidateType(node))
                continue;

            if (requireInitialSpawnNodeHasNextNode && !HasAnyValidNextNode(node, true))
                continue;

            if (rejectInitialSpawnTooCloseToCityExit && IsTooCloseToCityExit(node))
                continue;

            initialSpawnNodes.Add(node);
        }

        if (logSpawnerActions)
            Debug.Log("TrafficSpawner found initial population nodes: " + initialSpawnNodes.Count);
    }

    private TrafficNode[] GetAllTrafficNodes()
    {
        if (trafficNetworkRoot != null)
            return trafficNetworkRoot.GetComponentsInChildren<TrafficNode>(true);

        return FindObjectsOfType<TrafficNode>(true);
    }

    private bool IsValidInitialSpawnCandidateType(TrafficNode node)
    {
        if (node == null)
            return false;

        if (node.nodeType == TrafficNodeType.SpawnOnly)
            return true;

        if (useNormalNodesForInitialSpawn && node.nodeType == TrafficNodeType.Normal)
            return true;

        if (useIntersectionNodesForInitialSpawn && node.nodeType == TrafficNodeType.Intersection)
            return true;

        return false;
    }

    private bool HasAnyValidNextNode(TrafficNode node, bool initialSpawn)
    {
        if (node == null || node.nextNodes == null || node.nextNodes.Count == 0)
            return false;

        foreach (TrafficNode nextNode in node.nextNodes)
        {
            if (IsValidFirstTarget(node, nextNode, initialSpawn))
                return true;
        }

        return false;
    }

    private bool IsTooCloseToCityExit(TrafficNode node)
    {
        if (node == null)
            return true;

        TrafficNode[] allNodes = GetAllTrafficNodes();

        foreach (TrafficNode otherNode in allNodes)
        {
            if (otherNode == null)
                continue;

            if (otherNode.nodeType != TrafficNodeType.CityExitDespawn && !otherNode.canBeDespawnPoint)
                continue;

            float distance = Vector3.Distance(node.transform.position, otherNode.transform.position);

            if (distance < minimumInitialSpawnDistanceFromCityExit)
                return true;
        }

        return false;
    }

    [ContextMenu("Find Traffic Cars")]
    public void FindTrafficCars()
    {
        trafficCars.Clear();

        TrafficCarAI[] cars = FindObjectsOfType<TrafficCarAI>(true);

        foreach (TrafficCarAI car in cars)
        {
            if (car == null)
                continue;

            if (!trafficCars.Contains(car))
                trafficCars.Add(car);
        }

        if (logSpawnerActions)
            Debug.Log("TrafficSpawner found traffic cars: " + trafficCars.Count);
    }

    [ContextMenu("Create Missing Cars")]
    public void CreateMissingCars()
    {
        ApplySavedTrafficDensity();

        while (trafficCars.Count < maxActiveCars)
        {
            TrafficCarAI prefabToSpawn = GetTrafficPrefabForNewCar();

            if (prefabToSpawn == null)
            {
                Debug.LogWarning(
                    "TrafficSpawner cannot create missing cars. " +
                    "Assign Traffic Vehicle Database with usable prefabs, or assign legacy Traffic Car Prefab fallback."
                );

                return;
            }

            TrafficCarAI newCar = Instantiate(prefabToSpawn);

            if (trafficCarParent != null)
                newCar.transform.SetParent(trafficCarParent, true);

            newCar.name = "TrafficCar_Auto_" + trafficCars.Count.ToString("00") + "_" + prefabToSpawn.name;
            newCar.gameObject.SetActive(false);

            if (!trafficCars.Contains(newCar))
                trafficCars.Add(newCar);

            if (logSpawnerActions)
                Debug.Log("TrafficSpawner created pooled traffic car: " + newCar.name);
        }
    }

    private TrafficCarAI GetTrafficPrefabForNewCar()
    {
        if (trafficVehicleDatabase != null && trafficVehicleDatabase.HasUsableVehicles())
        {
            switch (prefabSelectionMode)
            {
                case TrafficPrefabSelectionMode.Random:
                    return trafficVehicleDatabase.GetRandomPrefab();

                case TrafficPrefabSelectionMode.Cycle:
                    return trafficVehicleDatabase.GetCyclePrefab();

                case TrafficPrefabSelectionMode.WeightedRandom:
                default:
                    return trafficVehicleDatabase.GetWeightedRandomPrefab();
            }
        }

        return trafficCarPrefab;
    }

    [ContextMenu("Disable All Managed Cars")]
    public void DisableAllManagedCars()
    {
        foreach (TrafficCarAI car in trafficCars)
        {
            if (car == null)
                continue;

            car.gameObject.SetActive(false);
        }

        respawnTimers.Clear();
    }

    [ContextMenu("Spawn Initial Traffic")]
    public void SpawnInitialTraffic()
    {
        ApplySavedTrafficDensity();

        if (initialSpawnNodes.Count == 0)
            FindInitialSpawnNodes();

        if (initialSpawnNodes.Count == 0)
        {
            Debug.LogWarning("TrafficSpawner has no initial spawn nodes. Add SpawnOnly/Normal TrafficNodes or click Find Initial Spawn Nodes.");
            return;
        }

        int spawned = 0;

        foreach (TrafficCarAI car in trafficCars)
        {
            if (car == null)
                continue;

            if (spawned >= maxActiveCars)
            {
                car.gameObject.SetActive(false);
                continue;
            }

            bool didSpawn = TrySpawnCar(car, true);

            if (didSpawn)
                spawned++;
        }

        if (logSpawnerActions)
            Debug.Log("TrafficSpawner initial spawned cars: " + spawned + " / " + maxActiveCars);
    }

    private void UpdateRespawns()
    {
        int activeCount = CountActiveCars();

        foreach (TrafficCarAI car in trafficCars)
        {
            if (car == null)
                continue;

            if (car.gameObject.activeInHierarchy)
            {
                if (respawnTimers.ContainsKey(car))
                    respawnTimers.Remove(car);

                continue;
            }

            if (activeCount >= maxActiveCars)
                continue;

            if (!respawnTimers.ContainsKey(car))
                respawnTimers[car] = Random.Range(respawnDelayMin, respawnDelayMax);

            respawnTimers[car] -= checkInterval;

            if (respawnTimers[car] <= 0f)
            {
                bool didSpawn = TrySpawnCar(car, false);

                if (didSpawn)
                {
                    respawnTimers.Remove(car);
                    activeCount++;
                }
                else
                {
                    respawnTimers[car] = Random.Range(respawnDelayMin, respawnDelayMax);
                }
            }
        }
    }

    private bool TrySpawnCar(TrafficCarAI car, bool initialSpawn)
    {
        if (car == null)
            return false;

        TrafficNode spawnNode = GetValidSpawnNode(initialSpawn);

        if (spawnNode == null)
        {
            if (logSpawnerActions)
            {
                Debug.LogWarning(initialSpawn
                    ? "TrafficSpawner could not find a valid initial spawn node."
                    : "TrafficSpawner could not find a valid runtime SpawnOnly node.");
            }

            return false;
        }

        TrafficNode firstTargetNode = GetInitialCurrentNode(spawnNode, initialSpawn);

        if (firstTargetNode == null)
        {
            Debug.LogWarning(
                "Spawn node has no valid first target node: " +
                spawnNode.name +
                "\nMake sure it points to a valid node that is not too close and not a CityExitDespawn."
            );

            return false;
        }

        Vector3 spawnPosition = GetSpawnPosition(spawnNode);
        Quaternion spawnRotation = GetSpawnRotation(spawnNode, firstTargetNode);

        car.gameObject.SetActive(true);

        Rigidbody rb = car.GetComponent<Rigidbody>();

        if (rb != null)
        {
            if (!rb.isKinematic)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            rb.WakeUp();
        }

        car.transform.position = spawnPosition;
        car.transform.rotation = spawnRotation;
        car.currentNode = firstTargetNode;

        car.SetSpawnedState(firstTargetNode);

        car.transform.position = spawnPosition;
        car.transform.rotation = spawnRotation;
        car.currentNode = firstTargetNode;

        if (rb != null)
        {
            rb.position = spawnPosition;
            rb.rotation = spawnRotation;

            if (!rb.isKinematic)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            rb.WakeUp();
        }

        if (logSpawnerActions)
        {
            Debug.Log(
                "TrafficSpawner spawned " +
                car.name +
                (initialSpawn ? " [INITIAL]" : " [RUNTIME]") +
                "\nSpawn Node: " +
                spawnNode.name +
                "\nSpawn Node Type: " +
                spawnNode.nodeType +
                "\nSpawn Node Pos: " +
                spawnNode.transform.position +
                "\nSpawn Pos Used: " +
                spawnPosition +
                "\nTarget Node: " +
                firstTargetNode.name +
                "\nTarget Type: " +
                firstTargetNode.nodeType +
                "\nTarget Pos: " +
                firstTargetNode.transform.position +
                "\nSpawn To Target Distance: " +
                Vector3.Distance(spawnNode.transform.position, firstTargetNode.transform.position).ToString("F2")
            );
        }

        return true;
    }

    private Vector3 GetSpawnPosition(TrafficNode spawnNode)
    {
        Vector3 basePosition = spawnNode.transform.position;

        if (!spawnAtSpawnNode)
            basePosition = spawnNode.transform.position;

        if (!snapSpawnToGround)
            return basePosition + Vector3.up * spawnHeightOffset;

        Vector3 rayStart = basePosition + Vector3.up * groundSnapRayStartHeight;

        if (Physics.Raycast(
                rayStart,
                Vector3.down,
                out RaycastHit hit,
                groundSnapRayDistance,
                groundSnapLayers,
                QueryTriggerInteraction.Ignore))
        {
            return hit.point + Vector3.up * spawnHeightOffset;
        }

        return basePosition + Vector3.up * spawnHeightOffset;
    }

    private Quaternion GetSpawnRotation(TrafficNode spawnNode, TrafficNode targetNode)
    {
        if (!rotateTowardFirstTargetNode || targetNode == null)
            return spawnNode.transform.rotation;

        Vector3 direction = targetNode.transform.position - spawnNode.transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return spawnNode.transform.rotation;

        return Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private TrafficNode GetValidSpawnNode(bool initialSpawn)
    {
        List<TrafficNode> sourceList = initialSpawn ? initialSpawnNodes : spawnNodes;

        if (sourceList == null || sourceList.Count == 0)
            return null;

        int attempts = Mathf.Max(5, sourceList.Count * 2);

        for (int i = 0; i < attempts; i++)
        {
            TrafficNode candidate = sourceList[Random.Range(0, sourceList.Count)];

            if (candidate == null)
                continue;

            if (!initialSpawn && candidate.nodeType != TrafficNodeType.SpawnOnly)
                continue;

            if (initialSpawn && !IsValidInitialSpawnCandidateType(candidate))
                continue;

            if (!IsSpawnNodeSafe(candidate))
                continue;

            TrafficNode initialTarget = GetInitialCurrentNode(candidate, initialSpawn);

            if (initialTarget == null)
                continue;

            return candidate;
        }

        return null;
    }

    private bool IsSpawnNodeSafe(TrafficNode spawnNode)
    {
        if (spawnNode == null)
            return false;

        if (player != null)
        {
            float distanceToPlayer = Vector3.Distance(player.position, spawnNode.transform.position);

            if (distanceToPlayer < minDistanceFromPlayer)
                return false;
        }

        if (checkSpawnBlocked)
        {
            Vector3 checkPosition = GetSpawnPosition(spawnNode);

            Collider[] hits = Physics.OverlapSphere(
                checkPosition,
                spawnBlockRadius,
                spawnBlockLayers,
                QueryTriggerInteraction.Ignore
            );

            foreach (Collider hit in hits)
            {
                if (hit == null)
                    continue;

                TrafficCarAI trafficCar = hit.GetComponentInParent<TrafficCarAI>();

                if (trafficCar != null)
                    return false;

                if (player != null && hit.transform.IsChildOf(player))
                    return false;

                if (hit.transform == player)
                    return false;
            }
        }

        return true;
    }

    private TrafficNode GetInitialCurrentNode(TrafficNode spawnNode, bool initialSpawn)
    {
        if (spawnNode == null)
            return null;

        if (!useSpawnNodeNextNodeAsCurrent)
        {
            if (IsValidFirstTarget(spawnNode, spawnNode, initialSpawn))
                return spawnNode;

            return null;
        }

        if (spawnNode.nextNodes == null || spawnNode.nextNodes.Count == 0)
            return null;

        List<TrafficNode> validNextNodes = new List<TrafficNode>();

        foreach (TrafficNode nextNode in spawnNode.nextNodes)
        {
            if (!IsValidFirstTarget(spawnNode, nextNode, initialSpawn))
                continue;

            validNextNodes.Add(nextNode);
        }

        if (validNextNodes.Count == 0)
            return null;

        if (randomizeSpawnExitNode)
            return validNextNodes[Random.Range(0, validNextNodes.Count)];

        return validNextNodes[0];
    }

    private bool IsValidFirstTarget(TrafficNode spawnNode, TrafficNode targetNode, bool initialSpawn)
    {
        if (spawnNode == null || targetNode == null)
            return false;

        if (rejectNoTrafficAsFirstTarget && targetNode.nodeType == TrafficNodeType.NoTraffic)
            return false;

        if (rejectCityExitAsFirstTarget &&
            (targetNode.nodeType == TrafficNodeType.CityExitDespawn || targetNode.canBeDespawnPoint))
            return false;

        float distance = Vector3.Distance(spawnNode.transform.position, targetNode.transform.position);
        float minDistance = initialSpawn ? minimumInitialSpawnToTargetDistance : minimumSpawnToTargetDistance;

        if (distance < minDistance)
            return false;

        return true;
    }

    private int CountActiveCars()
    {
        int count = 0;

        foreach (TrafficCarAI car in trafficCars)
        {
            if (car == null)
                continue;

            if (car.gameObject.activeInHierarchy)
                count++;
        }

        return count;
    }

    private void OnValidate()
    {
        maxActiveCars = Mathf.Max(0, maxActiveCars);

        defaultTrafficDensity = ClampTrafficDensity(defaultTrafficDensity);
        minimumTrafficDensity = Mathf.Max(0, minimumTrafficDensity);
        maximumTrafficDensity = Mathf.Max(minimumTrafficDensity, maximumTrafficDensity);

        respawnDelayMin = Mathf.Max(0f, respawnDelayMin);
        respawnDelayMax = Mathf.Max(respawnDelayMin, respawnDelayMax);

        checkInterval = Mathf.Max(0.1f, checkInterval);

        spawnHeightOffset = Mathf.Max(0f, spawnHeightOffset);
        groundSnapRayStartHeight = Mathf.Max(0f, groundSnapRayStartHeight);
        groundSnapRayDistance = Mathf.Max(0.1f, groundSnapRayDistance);

        minimumSpawnToTargetDistance = Mathf.Max(0f, minimumSpawnToTargetDistance);
        minimumInitialSpawnToTargetDistance = Mathf.Max(0f, minimumInitialSpawnToTargetDistance);
        minimumInitialSpawnDistanceFromCityExit = Mathf.Max(0f, minimumInitialSpawnDistanceFromCityExit);

        minDistanceFromPlayer = Mathf.Max(0f, minDistanceFromPlayer);
        spawnBlockRadius = Mathf.Max(0.1f, spawnBlockRadius);
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos)
            return;

        if (spawnNodes != null)
        {
            Gizmos.color = Color.green;

            foreach (TrafficNode spawnNode in spawnNodes)
            {
                if (spawnNode == null)
                    continue;

                Gizmos.DrawWireSphere(spawnNode.transform.position, spawnBlockRadius);
            }
        }

        if (initialSpawnNodes != null)
        {
            Gizmos.color = Color.cyan;

            foreach (TrafficNode initialNode in initialSpawnNodes)
            {
                if (initialNode == null)
                    continue;

                Gizmos.DrawWireCube(initialNode.transform.position + Vector3.up * 0.4f, Vector3.one * 1.2f);
            }
        }
    }
}