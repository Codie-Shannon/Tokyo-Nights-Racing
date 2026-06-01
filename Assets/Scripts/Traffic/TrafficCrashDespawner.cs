using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TrafficCrashDespawner : MonoBehaviour
{
    [Header("Crash Detection")]
    [Tooltip("Ignore crash despawning for a short time after spawn.")]
    public float spawnGraceTime = 1.5f;

    [Tooltip("Minimum collision impact speed before it counts as a crash.")]
    public float minimumCrashRelativeVelocity = 4f;

    [Tooltip("Layers that count as normal crash/despawn objects. Use Building, Props, TrafficCar, Barriers, etc. Do NOT include Ground/Road.")]
    public LayerMask crashDespawnLayers;

    [Tooltip("Layers that count as the player. Usually Player or Car depending on your setup.")]
    public LayerMask playerLayers;

    [Tooltip("If true, traffic cars despawn immediately when crashing into non-player objects.")]
    public bool despawnImmediatelyOnNonPlayerCrash = true;

    [Header("Player Crash Behaviour")]
    [Tooltip("If true, after hitting the player, the traffic car waits until it is off the player's camera before despawning.")]
    public bool despawnAfterPlayerCameraCannotSee = true;

    [Tooltip("Camera used to decide whether the crashed traffic car is still visible.")]
    public Camera playerCamera;

    [Tooltip("Optional player transform. Used mainly for debugging/assignment clarity.")]
    public Transform player;

    [Tooltip("How long the car must be off-camera before despawning.")]
    public float offCameraTimeBeforeDespawn = 0.75f;

    [Tooltip("If true, disables TrafficCarAI after hitting the player so the traffic car stops trying to drive.")]
    public bool disableTrafficAIOnPlayerCrash = true;

    [Tooltip("If true, strongly slows the car after hitting the player.")]
    public bool brakeAfterPlayerCrash = true;

    [Tooltip("How quickly the car slows while waiting to despawn after player crash.")]
    public float playerCrashBrakeLerp = 12f;

    [Header("Renderer Visibility")]
    [Tooltip("Renderers checked against the player camera. If empty, they are auto-found.")]
    public Renderer[] renderersToCheck;

    [Tooltip("Extra bounds padding used for camera visibility checks.")]
    public float visibilityBoundsPadding = 0.25f;

    [Header("Debug")]
    public bool logCrashEvents = false;
    public bool drawDebugGizmos = true;

    private Rigidbody rb;
    private TrafficCarAI trafficAI;

    private float timeSinceEnabled;
    private float offCameraTimer;

    private bool waitingForPlayerCameraToLoseSight;
    private bool hasCrashedIntoPlayer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        trafficAI = GetComponent<TrafficCarAI>();

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
        offCameraTimer = 0f;
        waitingForPlayerCameraToLoseSight = false;
        hasCrashedIntoPlayer = false;

        if (trafficAI == null)
        {
            trafficAI = GetComponent<TrafficCarAI>();
        }

        if (trafficAI != null)
        {
            trafficAI.enabled = true;
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

        if (!waitingForPlayerCameraToLoseSight)
        {
            return;
        }

        if (brakeAfterPlayerCrash)
        {
            BrakeWhileWaiting();
        }

        bool isVisibleToPlayerCamera = IsVisibleToPlayerCamera();

        if (isVisibleToPlayerCamera)
        {
            offCameraTimer = 0f;
            return;
        }

        offCameraTimer += Time.deltaTime;

        if (offCameraTimer >= offCameraTimeBeforeDespawn)
        {
            Despawn("player crash car is now off-camera");
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (timeSinceEnabled < spawnGraceTime)
        {
            return;
        }

        if (collision == null || collision.collider == null)
        {
            return;
        }

        float impactSpeed = collision.relativeVelocity.magnitude;

        if (impactSpeed < minimumCrashRelativeVelocity)
        {
            return;
        }

        GameObject otherObject = collision.collider.gameObject;
        int otherLayer = otherObject.layer;

        bool hitPlayer =
            IsLayerInMask(otherLayer, playerLayers) ||
            collision.collider.CompareTag("Player") ||
            collision.collider.GetComponentInParent<PlayerRespawn>() != null ||
            collision.collider.GetComponentInParent<CarController>() != null;

        // Player crash must be handled BEFORE normal crash despawn.
        if (hitPlayer)
        {
            HandlePlayerCrash(collision, impactSpeed);
            return;
        }

        bool hitCrashObject = IsLayerInMask(otherLayer, crashDespawnLayers);

        if (hitCrashObject && despawnImmediatelyOnNonPlayerCrash)
        {
            Despawn("crashed into " + otherObject.name + " at " + impactSpeed.ToString("F1") + " m/s");
        }
    }

    private void HandlePlayerCrash(Collision collision, float impactSpeed)
    {
        if (hasCrashedIntoPlayer)
        {
            return;
        }

        hasCrashedIntoPlayer = true;

        if (logCrashEvents)
        {
            Debug.Log(gameObject.name + " crashed into player at " + impactSpeed.ToString("F1") + " m/s");
        }

        if (disableTrafficAIOnPlayerCrash && trafficAI != null)
        {
            trafficAI.enabled = false;
        }

        if (rb != null)
        {
            rb.velocity *= 0.25f;
            rb.angularVelocity *= 0.25f;
        }

        if (despawnAfterPlayerCameraCannotSee)
        {
            waitingForPlayerCameraToLoseSight = true;
            offCameraTimer = 0f;
        }
        else
        {
            Despawn("crashed into player");
        }
    }

    private void BrakeWhileWaiting()
    {
        if (rb == null)
        {
            return;
        }

        Vector3 currentVelocity = rb.velocity;
        Vector3 flatVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);

        flatVelocity = Vector3.Lerp(
            flatVelocity,
            Vector3.zero,
            playerCrashBrakeLerp * Time.deltaTime
        );

        rb.velocity = new Vector3(
            flatVelocity.x,
            currentVelocity.y,
            flatVelocity.z
        );

        rb.angularVelocity = Vector3.Lerp(
            rb.angularVelocity,
            Vector3.zero,
            playerCrashBrakeLerp * Time.deltaTime
        );
    }

    private bool IsVisibleToPlayerCamera()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (playerCamera == null)
        {
            // If there is no camera, do not keep the car alive forever.
            return false;
        }

        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(playerCamera);
        Bounds combinedBounds = GetCombinedRendererBounds();

        if (visibilityBoundsPadding > 0f)
        {
            combinedBounds.Expand(visibilityBoundsPadding);
        }

        return GeometryUtility.TestPlanesAABB(planes, combinedBounds);
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
        if (logCrashEvents)
        {
            Debug.Log(gameObject.name + " despawned because: " + reason);
        }

        waitingForPlayerCameraToLoseSight = false;
        hasCrashedIntoPlayer = false;
        offCameraTimer = 0f;

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        gameObject.SetActive(false);
    }

    private bool IsLayerInMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebugGizmos)
        {
            return;
        }

        Bounds bounds = GetCombinedRendererBounds();

        Gizmos.color = waitingForPlayerCameraToLoseSight ? Color.red : Color.cyan;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }

    private void OnValidate()
    {
        spawnGraceTime = Mathf.Max(0f, spawnGraceTime);
        minimumCrashRelativeVelocity = Mathf.Max(0f, minimumCrashRelativeVelocity);
        offCameraTimeBeforeDespawn = Mathf.Max(0f, offCameraTimeBeforeDespawn);
        playerCrashBrakeLerp = Mathf.Max(0.1f, playerCrashBrakeLerp);
        visibilityBoundsPadding = Mathf.Max(0f, visibilityBoundsPadding);
    }
}