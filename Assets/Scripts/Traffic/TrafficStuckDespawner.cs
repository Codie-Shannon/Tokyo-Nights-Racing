using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TrafficStuckDespawner : MonoBehaviour
{
    [Header("References")]
    public TrafficCarAI trafficAI;
    public Rigidbody rb;
    public Camera playerCamera;

    [Header("Stuck Detection")]
    [Tooltip("Ignore stuck checking for this long after spawning.")]
    public float spawnGraceTime = 4f;

    [Tooltip("Car is considered stopped/stuck below this speed.")]
    public float stoppedSpeedKph = 1.2f;

    [Tooltip("If the car is stopped with an obstacle ahead outside an intersection for this long, despawn.")]
    public float blockedByObstacleDespawnTime = 12f;

    [Tooltip("If the car is stopped without a clear obstacle for this long, despawn.")]
    public float generalStuckDespawnTime = 8f;

    [Tooltip("If true, obstacle-based waiting gets a longer timer than general stuck.")]
    public bool useLongerTimerWhenObstacleAhead = true;

    [Header("Intersection Safety")]
    [Tooltip("If true, never despawn while the AI says it is waiting for an intersection zone.")]
    public bool ignoreWhileWaitingForIntersection = true;

    [Tooltip("If true, never despawn while the AI has reserved an intersection zone.")]
    public bool ignoreWhileReservedIntersection = true;

    [Tooltip("If true, never despawn while the current node requires a clear intersection zone.")]
    public bool ignoreIfCurrentNodeUsesClearZone = true;

    [Tooltip("Extra grace time after leaving an intersection/wait state.")]
    public float afterIntersectionGraceTime = 3f;

    [Header("Player Visibility Safety")]
    [Tooltip("If true, do not despawn while the player's camera can see this traffic car.")]
    public bool avoidDespawningOnCamera = true;

    [Tooltip("If true, after stuck timeout is reached, wait until the car is off-camera before despawning.")]
    public bool waitUntilOffCameraAfterTimeout = true;

    [Tooltip("How long the car must be off-camera before despawning.")]
    public float offCameraTimeBeforeDespawn = 0.75f;

    [Header("Renderer Visibility")]
    public Renderer[] renderersToCheck;
    public float visibilityBoundsPadding = 0.25f;

    [Header("Debug")]
    public bool logDespawns = false;
    public bool drawDebugGizmos = true;

    private float timeSinceEnabled;
    private float stoppedTimer;
    private float afterIntersectionTimer;
    private float offCameraTimer;

    private bool stuckTimeoutReached;

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        if (trafficAI == null)
        {
            trafficAI = GetComponent<TrafficCarAI>();
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (renderersToCheck == null || renderersToCheck.Length == 0)
        {
            renderersToCheck = GetComponentsInChildren<Renderer>(true);
        }
    }

    private void OnEnable()
    {
        timeSinceEnabled = 0f;
        stoppedTimer = 0f;
        afterIntersectionTimer = 0f;
        offCameraTimer = 0f;
        stuckTimeoutReached = false;

        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        if (trafficAI == null)
        {
            trafficAI = GetComponent<TrafficCarAI>();
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (renderersToCheck == null || renderersToCheck.Length == 0)
        {
            renderersToCheck = GetComponentsInChildren<Renderer>(true);
        }
    }

    private void Update()
    {
        timeSinceEnabled += Time.deltaTime;

        if (timeSinceEnabled < spawnGraceTime)
        {
            ResetStuckTimers();
            return;
        }

        if (trafficAI == null || rb == null)
        {
            return;
        }

        if (trafficAI.IsDespawning || trafficAI.IsReversing)
        {
            ResetStuckTimers();
            return;
        }

        if (ShouldIgnoreBecauseOfIntersection())
        {
            ResetStuckTimers();
            afterIntersectionTimer = afterIntersectionGraceTime;
            return;
        }

        if (afterIntersectionTimer > 0f)
        {
            afterIntersectionTimer -= Time.deltaTime;
            ResetStuckTimers();
            return;
        }

        float speedKph = rb.velocity.magnitude * 3.6f;

        if (speedKph > stoppedSpeedKph)
        {
            ResetStuckTimers();
            return;
        }

        stoppedTimer += Time.deltaTime;

        bool obstacleAhead = trafficAI.HasObstacleAhead;
        float requiredTime = generalStuckDespawnTime;

        if (useLongerTimerWhenObstacleAhead && obstacleAhead)
        {
            requiredTime = blockedByObstacleDespawnTime;
        }

        if (stoppedTimer < requiredTime && !stuckTimeoutReached)
        {
            return;
        }

        stuckTimeoutReached = true;

        if (avoidDespawningOnCamera || waitUntilOffCameraAfterTimeout)
        {
            if (IsVisibleToPlayerCamera())
            {
                offCameraTimer = 0f;
                return;
            }

            offCameraTimer += Time.deltaTime;

            if (offCameraTimer < offCameraTimeBeforeDespawn)
            {
                return;
            }
        }

        Despawn("stuck too long. Obstacle ahead: " + obstacleAhead + ", stopped timer: " + stoppedTimer.ToString("F1"));
    }

    private bool ShouldIgnoreBecauseOfIntersection()
    {
        if (trafficAI == null)
        {
            return false;
        }

        if (ignoreWhileWaitingForIntersection && trafficAI.IsWaitingForIntersection)
        {
            return true;
        }

        if (ignoreWhileReservedIntersection && trafficAI.HasReservedIntersection)
        {
            return true;
        }

        if (ignoreIfCurrentNodeUsesClearZone &&
            trafficAI.currentNode != null &&
            trafficAI.currentNode.requireClearZoneBeforeAdvancing)
        {
            return true;
        }

        return false;
    }

    private void ResetStuckTimers()
    {
        stoppedTimer = 0f;
        offCameraTimer = 0f;
        stuckTimeoutReached = false;
    }

    private bool IsVisibleToPlayerCamera()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (playerCamera == null)
        {
            return false;
        }

        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(playerCamera);
        Bounds bounds = GetCombinedRendererBounds();

        if (visibilityBoundsPadding > 0f)
        {
            bounds.Expand(visibilityBoundsPadding);
        }

        return GeometryUtility.TestPlanesAABB(planes, bounds);
    }

    private Bounds GetCombinedRendererBounds()
    {
        if (renderersToCheck == null || renderersToCheck.Length == 0)
        {
            return new Bounds(transform.position, Vector3.one * 2f);
        }

        bool hasBounds = false;
        Bounds combinedBounds = new Bounds(transform.position, Vector3.zero);

        foreach (Renderer rend in renderersToCheck)
        {
            if (rend == null)
            {
                continue;
            }

            if (!rend.enabled)
            {
                continue;
            }

            if (!hasBounds)
            {
                combinedBounds = rend.bounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(rend.bounds);
            }
        }

        if (!hasBounds)
        {
            combinedBounds = new Bounds(transform.position, Vector3.one * 2f);
        }

        return combinedBounds;
    }

    private void Despawn(string reason)
    {
        if (logDespawns)
        {
            Debug.Log(gameObject.name + " stuck-despawned because: " + reason);
        }

        stoppedTimer = 0f;
        offCameraTimer = 0f;
        stuckTimeoutReached = false;

        if (trafficAI != null)
        {
            trafficAI.ForceDespawn();
            return;
        }

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        gameObject.SetActive(false);
    }

    private void OnValidate()
    {
        spawnGraceTime = Mathf.Max(0f, spawnGraceTime);
        stoppedSpeedKph = Mathf.Max(0f, stoppedSpeedKph);
        blockedByObstacleDespawnTime = Mathf.Max(0.1f, blockedByObstacleDespawnTime);
        generalStuckDespawnTime = Mathf.Max(0.1f, generalStuckDespawnTime);
        afterIntersectionGraceTime = Mathf.Max(0f, afterIntersectionGraceTime);
        offCameraTimeBeforeDespawn = Mathf.Max(0f, offCameraTimeBeforeDespawn);
        visibilityBoundsPadding = Mathf.Max(0f, visibilityBoundsPadding);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebugGizmos)
        {
            return;
        }

        Bounds bounds = GetCombinedRendererBounds();

        if (stuckTimeoutReached)
        {
            Gizmos.color = Color.red;
        }
        else if (trafficAI != null && ShouldIgnoreBecauseOfIntersection())
        {
            Gizmos.color = Color.green;
        }
        else
        {
            Gizmos.color = Color.cyan;
        }

        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}