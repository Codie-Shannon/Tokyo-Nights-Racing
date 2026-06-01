using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TrafficCarAI : MonoBehaviour
{
    [Header("Route")]
    public TrafficNode currentNode;

    [Tooltip("If true, the car will pick the next node randomly from the current node's nextNodes list.")]
    public bool chooseRandomNextNode = true;

    [Header("Speed")]
    public bool randomizeSpeedOnStart = true;
    public float minSpeedKph = 18f;
    public float maxSpeedKph = 30f;
    public float chosenSpeedKph = 25f;

    [Header("Driving")]
    public float accelerationLerp = 3f;
    public float brakeLerp = 10f;

    [Tooltip("Default steering strength. Nodes can override this.")]
    public float steeringStrength = 5f;

    public float turnSlowdownAngle = 45f;
    public float turnSpeedMultiplier = 0.45f;

    [Header("Waypoint Reaching")]
    public float defaultReachDistance = 4f;

    [Header("Grounding")]
    public float groundRayDistance = 8f;
    public LayerMask groundLayers = ~0;

    [Header("Obstacle Detection")]
    public bool useObstacleAvoidance = true;

    [Tooltip("Layers the traffic car should stop for. Use Player/Car/TrafficCar. Do NOT include Ground/Road.")]
    public LayerMask obstacleLayers = ~0;

    public bool autoCalculateObstacleDistances = true;
    public float obstacleRayDistance = 3f;
    public float obstacleBrakeDistance = 2.5f;
    public float emergencyStopDistance = 1.25f;
    public float obstacleRayHeight = 0.8f;
    public float obstacleSphereRadius = 0.9f;

    [Header("Auto Obstacle Distance Settings")]
    public float minAutoRayDistance = 2.5f;
    public float maxAutoRayDistance = 5f;
    public float minAutoBrakeDistance = 2f;
    public float maxAutoBrakeDistance = 4f;
    public float minAutoEmergencyStopDistance = 1f;
    public float maxAutoEmergencyStopDistance = 2f;
    public float autoDistanceMinSpeedKph = 15f;
    public float autoDistanceMaxSpeedKph = 45f;

    [Header("Intersection Zone Behaviour")]
    [Tooltip("If true, AI will stop at nodes that require a clear intersection zone.")]
    public bool useIntersectionZones = true;

    [Tooltip("How hard the car brakes when waiting for an intersection zone.")]
    public float intersectionWaitBrakeLerp = 14f;

    [Tooltip("If true, logs when the car waits for or reserves an intersection.")]
    public bool logIntersectionWaiting = false;

    [Header("City Exit / Despawn")]
    public bool despawnAtCityExit = true;
    public bool disableObjectOnDespawn = true;
    public float despawnDelay = 0f;

    [Tooltip("Prevents cars from instantly despawning right after spawn.")]
    public float minimumAliveTimeBeforeDespawn = 4f;

    [Header("Stuck Recovery")]
    public bool useStuckRecovery = false;
    public float stuckRecoveryStartupDelay = 5f;
    public float stuckSpeedThresholdKph = 1f;
    public float stuckTimeBeforeReverse = 4f;
    public float reverseTime = 0.8f;
    public float reverseSpeedKph = 4f;

    [Header("Stability")]
    public bool dampenBodyRoll = true;
    public float angularDampingX = 0.85f;
    public float angularDampingZ = 0.85f;

    [Header("Debug")]
    public bool drawDebug = true;
    public bool logChosenSpeedOnStart = false;
    public bool logCityExitDespawn = true;

    public bool IsWaitingForIntersection => isWaitingForIntersection;
    public bool HasReservedIntersection => reservedIntersectionZone != null;
    public bool IsDespawning => isDespawning;
    public bool IsReversing => isReversing;
    public bool HasObstacleAhead => hasObstacleAhead;
    public float LastObstacleDistance => lastObstacleDistance;
    public float CurrentSpeedKph => GetSpeedKph();
    public TrafficIntersectionZone ReservedIntersectionZone => reservedIntersectionZone;

    private Rigidbody rb;
    private TrafficNode previousNode;

    private TrafficIntersectionZone reservedIntersectionZone;

    private float runtimeObstacleRayDistance;
    private float runtimeObstacleBrakeDistance;
    private float runtimeEmergencyStopDistance;

    private float stuckTimer;
    private float reverseTimer;
    private float timeSinceSpawn;
    private float despawnTimer;

    private bool isReversing;
    private bool isDespawning;
    private bool isWaitingForIntersection;

    private bool hasObstacleAhead;
    private float lastObstacleDistance;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        PickSpeed();
        CalculateObstacleDistances();
    }

    private void FixedUpdate()
    {
        timeSinceSpawn += Time.fixedDeltaTime;

        UpdateReservedIntersectionZone();

        hasObstacleAhead = false;
        lastObstacleDistance = runtimeObstacleRayDistance;

        if (isDespawning)
        {
            HandleDespawning();
            return;
        }

        if (currentNode == null)
        {
            ApplyBrake();
            KeepCarStable();
            return;
        }

        float currentSpeedKph = GetSpeedKph();

        if (useStuckRecovery)
        {
            UpdateStuckRecovery(currentSpeedKph);
        }

        if (isReversing)
        {
            ReverseAway();
            KeepCarStable();
            return;
        }

        Vector3 targetPosition = currentNode.transform.position;
        Vector3 toTarget = targetPosition - transform.position;
        toTarget.y = 0f;

        float distanceToNode = toTarget.magnitude;
        float reachDistance = Mathf.Max(defaultReachDistance, currentNode.reachDistance);

        if (distanceToNode <= reachDistance)
        {
            HandleReachedNode();
            KeepCarStable();
            return;
        }

        isWaitingForIntersection = false;

        Vector3 targetDirection = toTarget.normalized;

        float signedAngle = Vector3.SignedAngle(transform.forward, targetDirection, Vector3.up);
        float absAngle = Mathf.Abs(signedAngle);

        SteerTowards(signedAngle);

        bool obstacleAhead = false;
        float obstacleDistance = runtimeObstacleRayDistance;

        if (useObstacleAvoidance)
        {
            obstacleAhead = IsObstacleAhead(out obstacleDistance);
        }

        hasObstacleAhead = obstacleAhead;
        lastObstacleDistance = obstacleDistance;

        float targetSpeedKph = chosenSpeedKph;

        if (currentNode != null)
        {
            targetSpeedKph = Mathf.Min(targetSpeedKph, currentNode.recommendedSpeedKph);
        }

        if (absAngle > turnSlowdownAngle)
        {
            targetSpeedKph *= turnSpeedMultiplier;
        }

        if (obstacleAhead)
        {
            float obstacleSlowdown = Mathf.InverseLerp(0f, runtimeObstacleBrakeDistance, obstacleDistance);
            targetSpeedKph *= Mathf.Clamp01(obstacleSlowdown);

            if (obstacleDistance <= runtimeEmergencyStopDistance)
            {
                EmergencyStop();
                KeepCarStable();
                return;
            }
        }

        if (currentSpeedKph < targetSpeedKph && !obstacleAhead)
        {
            Accelerate(targetSpeedKph);
        }
        else
        {
            ApplyBrake();
        }

        KeepCarStable();
    }

    private void HandleReachedNode()
    {
        if (currentNode == null)
        {
            ApplyBrake();
            return;
        }

        if (currentNode.IsCityExit() && despawnAtCityExit)
        {
            if (timeSinceSpawn < minimumAliveTimeBeforeDespawn)
            {
                TrafficNode nextNode = currentNode.GetNextNode();

                if (nextNode != null && !nextNode.IsCityExit())
                {
                    previousNode = currentNode;
                    currentNode = nextNode;
                }
                else
                {
                    ApplyBrake();
                }

                return;
            }

            BeginDespawn();
            return;
        }

        if (currentNode.nodeType == TrafficNodeType.NoTraffic)
        {
            ApplyBrake();
            return;
        }

        if (useIntersectionZones && currentNode.requireClearZoneBeforeAdvancing)
        {
            if (!TryEnterIntersectionFromCurrentNode())
            {
                isWaitingForIntersection = true;
                ApplyIntersectionWaitBrake();

                if (logIntersectionWaiting)
                {
                    Debug.Log(gameObject.name + " waiting for intersection zone at node: " + currentNode.name);
                }

                return;
            }
        }

        AdvanceToNextNode();
    }

    private bool TryEnterIntersectionFromCurrentNode()
    {
        if (currentNode == null)
        {
            return false;
        }

        TrafficIntersectionZone zone = currentNode.clearZone;

        if (zone == null)
        {
            return true;
        }

        bool reserved = zone.TryReserve(this);

        if (reserved)
        {
            reservedIntersectionZone = zone;

            if (logIntersectionWaiting)
            {
                Debug.Log(gameObject.name + " reserved intersection zone: " + zone.name);
            }
        }

        return reserved;
    }

    private void UpdateReservedIntersectionZone()
    {
        if (reservedIntersectionZone == null)
        {
            return;
        }

        reservedIntersectionZone.UpdateOwnerState(this);

        if (!reservedIntersectionZone.IsReservedBy(this))
        {
            reservedIntersectionZone = null;
        }
    }

    private void ReleaseReservedIntersectionZone()
    {
        if (reservedIntersectionZone == null)
        {
            return;
        }

        reservedIntersectionZone.ReleaseIfOwner(this);
        reservedIntersectionZone = null;
    }

    private void ApplyIntersectionWaitBrake()
    {
        Vector3 currentVelocity = rb.velocity;
        Vector3 flatVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);

        flatVelocity = Vector3.Lerp(
            flatVelocity,
            Vector3.zero,
            intersectionWaitBrakeLerp * Time.fixedDeltaTime
        );

        rb.velocity = new Vector3(flatVelocity.x, currentVelocity.y, flatVelocity.z);
    }

    private void BeginDespawn()
    {
        ReleaseReservedIntersectionZone();

        isDespawning = true;
        despawnTimer = despawnDelay;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        if (logCityExitDespawn)
        {
            Debug.Log(gameObject.name + " reached city exit node and will despawn: " + currentNode.name);
        }

        if (despawnDelay <= 0f)
        {
            CompleteDespawn();
        }
    }

    private void HandleDespawning()
    {
        ApplyBrake();

        despawnTimer -= Time.fixedDeltaTime;

        if (despawnTimer <= 0f)
        {
            CompleteDespawn();
        }
    }

    private void CompleteDespawn()
    {
        ReleaseReservedIntersectionZone();

        if (disableObjectOnDespawn)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        ReleaseReservedIntersectionZone();
    }

    public void RespawnAtNode(TrafficNode spawnNode)
    {
        if (spawnNode == null)
        {
            return;
        }

        ReleaseReservedIntersectionZone();

        currentNode = spawnNode;
        previousNode = null;

        transform.position = spawnNode.transform.position;
        transform.rotation = spawnNode.transform.rotation;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        timeSinceSpawn = 0f;
        stuckTimer = 0f;
        reverseTimer = 0f;
        despawnTimer = 0f;

        isReversing = false;
        isDespawning = false;
        isWaitingForIntersection = false;
        hasObstacleAhead = false;
        lastObstacleDistance = 0f;

        PickSpeed();
        CalculateObstacleDistances();

        gameObject.SetActive(true);
    }

    public void SetSpawnedState(TrafficNode targetNode)
    {
        ReleaseReservedIntersectionZone();

        currentNode = targetNode;
        previousNode = null;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        timeSinceSpawn = 0f;
        stuckTimer = 0f;
        reverseTimer = 0f;
        despawnTimer = 0f;

        isReversing = false;
        isDespawning = false;
        isWaitingForIntersection = false;
        hasObstacleAhead = false;
        lastObstacleDistance = 0f;

        PickSpeed();
        CalculateObstacleDistances();
    }

    public void ForceDespawn()
    {
        ReleaseReservedIntersectionZone();

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        isReversing = false;
        isDespawning = false;
        isWaitingForIntersection = false;
        hasObstacleAhead = false;

        gameObject.SetActive(false);
    }

    private void AdvanceToNextNode()
    {
        TrafficNode nextNode = currentNode.GetNextNode();

        if (nextNode == null)
        {
            ApplyBrake();
            return;
        }

        previousNode = currentNode;
        currentNode = nextNode;
    }

    private void PickSpeed()
    {
        minSpeedKph = Mathf.Max(1f, minSpeedKph);
        maxSpeedKph = Mathf.Max(minSpeedKph, maxSpeedKph);

        if (randomizeSpeedOnStart)
        {
            chosenSpeedKph = Random.Range(minSpeedKph, maxSpeedKph);
        }
        else
        {
            chosenSpeedKph = Mathf.Clamp(chosenSpeedKph, minSpeedKph, maxSpeedKph);
        }

        if (logChosenSpeedOnStart)
        {
            Debug.Log(gameObject.name + " chosen traffic speed: " + chosenSpeedKph.ToString("F1") + " kph");
        }
    }

    private void CalculateObstacleDistances()
    {
        if (!autoCalculateObstacleDistances)
        {
            runtimeObstacleRayDistance = Mathf.Max(0.1f, obstacleRayDistance);
            runtimeObstacleBrakeDistance = Mathf.Max(0.1f, obstacleBrakeDistance);
            runtimeEmergencyStopDistance = Mathf.Max(0.05f, emergencyStopDistance);
            return;
        }

        float t = Mathf.InverseLerp(autoDistanceMinSpeedKph, autoDistanceMaxSpeedKph, chosenSpeedKph);

        runtimeObstacleRayDistance = Mathf.Lerp(minAutoRayDistance, maxAutoRayDistance, t);
        runtimeObstacleBrakeDistance = Mathf.Lerp(minAutoBrakeDistance, maxAutoBrakeDistance, t);
        runtimeEmergencyStopDistance = Mathf.Lerp(minAutoEmergencyStopDistance, maxAutoEmergencyStopDistance, t);

        runtimeEmergencyStopDistance = Mathf.Min(
            runtimeEmergencyStopDistance,
            runtimeObstacleBrakeDistance * 0.65f
        );
    }

    private void Accelerate(float targetSpeedKph)
    {
        rb.WakeUp();

        Vector3 currentVelocity = rb.velocity;
        Vector3 desiredVelocity = transform.forward * (targetSpeedKph / 3.6f);
        desiredVelocity.y = currentVelocity.y;

        rb.velocity = Vector3.Lerp(
            currentVelocity,
            desiredVelocity,
            accelerationLerp * Time.fixedDeltaTime
        );
    }

    private void ApplyBrake()
    {
        Vector3 currentVelocity = rb.velocity;
        Vector3 flatVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);

        flatVelocity = Vector3.Lerp(
            flatVelocity,
            Vector3.zero,
            brakeLerp * Time.fixedDeltaTime
        );

        rb.velocity = new Vector3(flatVelocity.x, currentVelocity.y, flatVelocity.z);
    }

    private void EmergencyStop()
    {
        Vector3 currentVelocity = rb.velocity;

        rb.velocity = new Vector3(
            Mathf.Lerp(currentVelocity.x, 0f, brakeLerp * 1.8f * Time.fixedDeltaTime),
            currentVelocity.y,
            Mathf.Lerp(currentVelocity.z, 0f, brakeLerp * 1.8f * Time.fixedDeltaTime)
        );
    }

    private void SteerTowards(float signedAngle)
    {
        float currentSpeedKph = GetSpeedKph();

        float speedFactor = Mathf.Clamp01(currentSpeedKph / 15f);
        speedFactor = Mathf.Max(speedFactor, 0.25f);

        float steeringInput = Mathf.Clamp(signedAngle / 60f, -1f, 1f);

        float activeSteeringStrength = steeringStrength;

        if (currentNode != null && currentNode.overrideSteeringStrength)
        {
            activeSteeringStrength = currentNode.steeringStrength;
        }

        float turnDegreesPerSecond = activeSteeringStrength * 25f;
        float turnAmount = steeringInput * turnDegreesPerSecond * speedFactor * Time.fixedDeltaTime;

        Quaternion turnRotation = Quaternion.Euler(0f, turnAmount, 0f);
        rb.MoveRotation(rb.rotation * turnRotation);
    }

    private bool IsGrounded()
    {
        Vector3 rayStart = transform.position + Vector3.up * 0.4f;

        return Physics.Raycast(
            rayStart,
            Vector3.down,
            groundRayDistance,
            groundLayers,
            QueryTriggerInteraction.Ignore
        );
    }

    private bool IsObstacleAhead(out float hitDistance)
    {
        hitDistance = runtimeObstacleRayDistance;

        Vector3 rayStart = transform.position + Vector3.up * obstacleRayHeight;

        if (Physics.SphereCast(
                rayStart,
                obstacleSphereRadius,
                transform.forward,
                out RaycastHit hit,
                runtimeObstacleRayDistance,
                obstacleLayers,
                QueryTriggerInteraction.Ignore))
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
            {
                return false;
            }

            hitDistance = hit.distance;
            return true;
        }

        return false;
    }

    private void UpdateStuckRecovery(float currentSpeedKph)
    {
        if (timeSinceSpawn < stuckRecoveryStartupDelay)
        {
            stuckTimer = 0f;
            return;
        }

        if (currentNode == null)
        {
            stuckTimer = 0f;
            return;
        }

        if (currentSpeedKph < stuckSpeedThresholdKph)
        {
            stuckTimer += Time.fixedDeltaTime;
        }
        else
        {
            stuckTimer = 0f;
        }

        if (!isReversing && stuckTimer >= stuckTimeBeforeReverse)
        {
            isReversing = true;
            reverseTimer = reverseTime;
            stuckTimer = 0f;
        }
    }

    private void ReverseAway()
    {
        reverseTimer -= Time.fixedDeltaTime;

        Vector3 currentVelocity = rb.velocity;
        Vector3 reverseVelocity = -transform.forward * (reverseSpeedKph / 3.6f);
        reverseVelocity.y = currentVelocity.y;

        rb.velocity = Vector3.Lerp(
            currentVelocity,
            reverseVelocity,
            4f * Time.fixedDeltaTime
        );

        float steerDirection = previousNode != null ? -1f : 1f;

        Quaternion turnRotation = Quaternion.Euler(
            0f,
            steerDirection * steeringStrength * 18f * Time.fixedDeltaTime,
            0f
        );

        rb.MoveRotation(rb.rotation * turnRotation);

        if (reverseTimer <= 0f)
        {
            isReversing = false;
            stuckTimer = 0f;
        }
    }

    private void KeepCarStable()
    {
        if (!dampenBodyRoll)
        {
            return;
        }

        Vector3 angularVelocity = rb.angularVelocity;

        angularVelocity.x *= angularDampingX;
        angularVelocity.z *= angularDampingZ;

        rb.angularVelocity = angularVelocity;
    }

    private float GetSpeedKph()
    {
        return rb.velocity.magnitude * 3.6f;
    }

    private void OnValidate()
    {
        minSpeedKph = Mathf.Max(1f, minSpeedKph);
        maxSpeedKph = Mathf.Max(minSpeedKph, maxSpeedKph);
        chosenSpeedKph = Mathf.Clamp(chosenSpeedKph, minSpeedKph, maxSpeedKph);

        accelerationLerp = Mathf.Max(0.1f, accelerationLerp);
        brakeLerp = Mathf.Max(0.1f, brakeLerp);
        steeringStrength = Mathf.Max(0f, steeringStrength);

        defaultReachDistance = Mathf.Max(0.5f, defaultReachDistance);
        groundRayDistance = Mathf.Max(0.1f, groundRayDistance);

        obstacleRayDistance = Mathf.Max(0.1f, obstacleRayDistance);
        obstacleBrakeDistance = Mathf.Max(0.1f, obstacleBrakeDistance);
        emergencyStopDistance = Mathf.Max(0.05f, emergencyStopDistance);
        obstacleRayHeight = Mathf.Max(0f, obstacleRayHeight);
        obstacleSphereRadius = Mathf.Max(0.05f, obstacleSphereRadius);

        minAutoRayDistance = Mathf.Max(0.1f, minAutoRayDistance);
        maxAutoRayDistance = Mathf.Max(minAutoRayDistance, maxAutoRayDistance);

        minAutoBrakeDistance = Mathf.Max(0.1f, minAutoBrakeDistance);
        maxAutoBrakeDistance = Mathf.Max(minAutoBrakeDistance, maxAutoBrakeDistance);

        minAutoEmergencyStopDistance = Mathf.Max(0.05f, minAutoEmergencyStopDistance);
        maxAutoEmergencyStopDistance = Mathf.Max(minAutoEmergencyStopDistance, maxAutoEmergencyStopDistance);

        autoDistanceMinSpeedKph = Mathf.Max(1f, autoDistanceMinSpeedKph);
        autoDistanceMaxSpeedKph = Mathf.Max(autoDistanceMinSpeedKph + 1f, autoDistanceMaxSpeedKph);

        intersectionWaitBrakeLerp = Mathf.Max(0.1f, intersectionWaitBrakeLerp);

        despawnDelay = Mathf.Max(0f, despawnDelay);
        minimumAliveTimeBeforeDespawn = Mathf.Max(0f, minimumAliveTimeBeforeDespawn);

        stuckRecoveryStartupDelay = Mathf.Max(0f, stuckRecoveryStartupDelay);
        stuckSpeedThresholdKph = Mathf.Max(0.1f, stuckSpeedThresholdKph);
        stuckTimeBeforeReverse = Mathf.Max(0.1f, stuckTimeBeforeReverse);
        reverseTime = Mathf.Max(0.1f, reverseTime);
        reverseSpeedKph = Mathf.Max(0.1f, reverseSpeedKph);
    }

    private void OnDrawGizmos()
    {
        if (!drawDebug)
        {
            return;
        }

        if (currentNode != null)
        {
            Gizmos.color = currentNode.IsCityExit() ? Color.red : Color.green;
            Gizmos.DrawLine(transform.position, currentNode.transform.position);
            Gizmos.DrawSphere(currentNode.transform.position, 0.6f);
        }

        if (reservedIntersectionZone != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position, reservedIntersectionZone.transform.position);
        }

        if (useObstacleAvoidance)
        {
            float drawRayDistance = autoCalculateObstacleDistances
                ? Mathf.Lerp(
                    minAutoRayDistance,
                    maxAutoRayDistance,
                    Mathf.InverseLerp(autoDistanceMinSpeedKph, autoDistanceMaxSpeedKph, chosenSpeedKph)
                )
                : obstacleRayDistance;

            Gizmos.color = Color.red;
            Vector3 rayStart = transform.position + Vector3.up * obstacleRayHeight;
            Gizmos.DrawWireSphere(rayStart, obstacleSphereRadius);
            Gizmos.DrawLine(rayStart, rayStart + transform.forward * drawRayDistance);
        }
    }
}