using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AICarController : MonoBehaviour
{
    private const string DefaultVehicleMaskLayerName = "Car";

    [Header("Waypoints")]
    public Transform[] waypoints;
    public Transform waypointParent;
    public bool autoLoadWaypointsFromParent = true;

    [Header("Race Progress")]
    public RacerProgress racerProgress;

    [Header("Driving")]
    public float acceleration = 12f;
    public float maxSpeed = 18f;
    public float turnSpeed = 5f;
    public float waypointReachDistance = 8f;
    public float sideGrip = 2.5f;

    [Header("Race Start")]
    public bool canDrive = false;

    [Header("Start Launch")]
    public float startBoostDuration = 1.2f;
    public float startAccelerationMultiplier = 1.35f;

    [Tooltip("Avoidance is disabled briefly so cars launch cleanly.")]
    public float avoidanceDisableAtStartTime = 1.0f;

    [Header("Setup")]
    public bool findClosestWaypointOnStart = true;

    [Header("Lane Offset")]
    [Tooltip("Give each AI a different lane offset: -4, -2, 0, 2, 4 etc.")]
    public float laneOffset = 0f;

    public bool randomizeLaneOffsetOnStart = false;
    public float randomLaneOffsetRange = 2.5f;

    [Header("Vehicle Detection")]
    [Tooltip("Set this to Vehicle layer only.")]
    public LayerMask vehicleMask;

    public float rayHeight = 0.7f;
    public float forwardCheckDistance = 7f;
    public float sideCheckDistance = 3.5f;
    public float raySideSpacing = 1.3f;

    [Header("Front Car Behaviour")]
    [Tooltip("Only slow down when another car is in front.")]
    public float followDistance = 5f;

    [Range(0.35f, 1f)]
    public float frontBlockedSpeedMultiplier = 0.65f;

    public float frontBrakeForce = 4f;

    [Header("Side Car Behaviour")]
    [Tooltip("Radius used to detect nearby cars beside this AI.")]
    public float sideAwarenessRadius = 4f;

    [Tooltip("Gentle side nudge strength. Keep this low.")]
    public float sideNudgeStrength = 0.35f;

    [Tooltip("How fast the side nudge smooths in/out.")]
    public float sideNudgeSmooth = 5f;

    [Tooltip("Side cars reduce speed slightly, but do not cause hard braking.")]
    [Range(0.7f, 1f)]
    public float sideBySideSpeedMultiplier = 0.88f;

    [Header("Recovery")]
    public float stuckSpeedThreshold = 0.2f;
    public float stuckTimeThreshold = 8f;
    public float fallYThreshold = -20f;
    public float respawnHeightOffset = 1.5f;

    private Rigidbody rb;
    private int currentWaypoint = 0;

    private float stuckTimer = 0f;
    private Vector3 lastWaypointPosition;
    private Quaternion lastWaypointRotation;

    private float raceStartTimer;

    private bool carInFront;
    private float speedMultiplier = 1f;
    private float targetSideNudge = 0f;
    private float currentSideNudge = 0f;

    private void Reset()
    {
        AutoAssignDefaults();
    }

    private void OnValidate()
    {
        AutoAssignDefaults();
    }

    private void Awake()
    {
        AutoAssignDefaults();

        rb = GetComponent<Rigidbody>();

        if (rb != null)
            rb.centerOfMass = new Vector3(0f, -0.9f, 0f);
    }

    private void AutoAssignDefaults()
    {
        if (racerProgress == null)
        {
            racerProgress = GetComponent<RacerProgress>();

            if (racerProgress == null)
                racerProgress = GetComponentInChildren<RacerProgress>(true);
        }

        if (vehicleMask.value == 0)
        {
            int carLayer = LayerMask.NameToLayer(DefaultVehicleMaskLayerName);

            if (carLayer >= 0)
                vehicleMask = 1 << carLayer;
        }
    }

    void Start()
    {
        LoadWaypointsIfNeeded();

        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogError($"{name}: No waypoints assigned.");
            enabled = false;
            return;
        }

        if (randomizeLaneOffsetOnStart)
        {
            laneOffset = Random.Range(-randomLaneOffsetRange, randomLaneOffsetRange);
        }

        if (findClosestWaypointOnStart)
        {
            currentWaypoint = GetClosestWaypointIndex();
        }

        SaveLastWaypointTransform();
    }

    void FixedUpdate()
    {
        if (!canDrive) return;
        if (waypoints == null || waypoints.Length == 0) return;

        raceStartTimer += Time.fixedDeltaTime;

        CheckRecovery();

        if (raceStartTimer > avoidanceDisableAtStartTime)
        {
            DetectVehicles();
        }
        else
        {
            carInFront = false;
            speedMultiplier = 1f;
            targetSideNudge = 0f;
        }

        currentSideNudge = Mathf.Lerp(
            currentSideNudge,
            targetSideNudge,
            sideNudgeSmooth * Time.fixedDeltaTime
        );

        DriveToWaypoint();
        ApplyGrip();
        CheckWaypointReached();
    }

    void LoadWaypointsIfNeeded()
    {
        if (!autoLoadWaypointsFromParent) return;
        if (waypointParent == null) return;

        waypoints = new Transform[waypointParent.childCount];

        for (int i = 0; i < waypointParent.childCount; i++)
        {
            waypoints[i] = waypointParent.GetChild(i);
        }
    }

    void DetectVehicles()
    {
        carInFront = false;
        speedMultiplier = 1f;
        targetSideNudge = 0f;

        Vector3 originCenter = transform.position + Vector3.up * rayHeight;
        Vector3 originLeft = originCenter - transform.right * raySideSpacing;
        Vector3 originRight = originCenter + transform.right * raySideSpacing;

        bool centerHit = Physics.Raycast(
            originCenter,
            transform.forward,
            out RaycastHit centerHitInfo,
            forwardCheckDistance,
            vehicleMask,
            QueryTriggerInteraction.Ignore
        );

        bool leftFrontHit = Physics.Raycast(
            originLeft,
            transform.forward,
            out RaycastHit leftHitInfo,
            sideCheckDistance,
            vehicleMask,
            QueryTriggerInteraction.Ignore
        );

        bool rightFrontHit = Physics.Raycast(
            originRight,
            transform.forward,
            out RaycastHit rightHitInfo,
            sideCheckDistance,
            vehicleMask,
            QueryTriggerInteraction.Ignore
        );

        if (centerHit && centerHitInfo.collider.attachedRigidbody != rb)
        {
            carInFront = true;

            float distanceT = Mathf.InverseLerp(1.5f, followDistance, centerHitInfo.distance);
            speedMultiplier = Mathf.Lerp(frontBlockedSpeedMultiplier, 1f, distanceT);

            if (leftFrontHit && !rightFrontHit)
            {
                targetSideNudge = 1f;
            }
            else if (rightFrontHit && !leftFrontHit)
            {
                targetSideNudge = -1f;
            }
            else
            {
                targetSideNudge = ChoosePreferredSide();
            }
        }

        DetectSideBySideCars();

        Debug.DrawRay(originCenter, transform.forward * forwardCheckDistance, carInFront ? Color.red : Color.green);
        Debug.DrawRay(originLeft, transform.forward * sideCheckDistance, leftFrontHit ? Color.red : Color.yellow);
        Debug.DrawRay(originRight, transform.forward * sideCheckDistance, rightFrontHit ? Color.red : Color.yellow);
    }

    void DetectSideBySideCars()
    {
        Collider[] nearby = Physics.OverlapSphere(
            transform.position,
            sideAwarenessRadius,
            vehicleMask,
            QueryTriggerInteraction.Ignore
        );

        float leftPressure = 0f;
        float rightPressure = 0f;
        bool sideCarFound = false;

        foreach (Collider col in nearby)
        {
            if (col == null) continue;

            Rigidbody otherRb = col.attachedRigidbody;
            if (otherRb == rb) continue;

            Vector3 toOther = col.transform.position - transform.position;
            toOther.y = 0f;

            float distance = toOther.magnitude;
            if (distance < 0.01f) continue;

            Vector3 local = transform.InverseTransformDirection(toOther.normalized);

            if (local.z < -0.3f)
                continue;

            float pressure = 1f - Mathf.Clamp01(distance / sideAwarenessRadius);

            if (local.x > 0.25f)
            {
                rightPressure += pressure;
                sideCarFound = true;
            }
            else if (local.x < -0.25f)
            {
                leftPressure += pressure;
                sideCarFound = true;
            }
        }

        if (!sideCarFound)
            return;

        if (rightPressure > leftPressure)
        {
            targetSideNudge = Mathf.Min(targetSideNudge, -0.7f);
        }
        else if (leftPressure > rightPressure)
        {
            targetSideNudge = Mathf.Max(targetSideNudge, 0.7f);
        }

        speedMultiplier = Mathf.Min(speedMultiplier, sideBySideSpeedMultiplier);
    }

    float ChoosePreferredSide()
    {
        if (laneOffset > 0.2f) return 1f;
        if (laneOffset < -0.2f) return -1f;

        return Random.value > 0.5f ? 1f : -1f;
    }

    void DriveToWaypoint()
    {
        Vector3 targetPosition = GetOffsetWaypointPosition();

        Vector3 targetDirection = targetPosition - transform.position;
        targetDirection.y = 0f;

        if (targetDirection.sqrMagnitude < 0.01f)
            return;

        targetDirection.Normalize();

        if (Mathf.Abs(currentSideNudge) > 0.01f)
        {
            Vector3 nudgeDirection = transform.right * currentSideNudge;

            targetDirection = Vector3.Lerp(
                targetDirection,
                nudgeDirection,
                sideNudgeStrength
            ).normalized;
        }

        Quaternion targetRotation = Quaternion.LookRotation(targetDirection, Vector3.up);

        rb.MoveRotation(
            Quaternion.Slerp(
                rb.rotation,
                targetRotation,
                turnSpeed * Time.fixedDeltaTime
            )
        );

        float currentMaxSpeed = maxSpeed * Mathf.Clamp(speedMultiplier, 0.55f, 1f);

        float launchMultiplier = raceStartTimer < startBoostDuration
            ? startAccelerationMultiplier
            : 1f;

        if (rb.velocity.magnitude < currentMaxSpeed)
        {
            rb.AddForce(transform.forward * acceleration * launchMultiplier, ForceMode.Acceleration);
        }

        if (carInFront && rb.velocity.magnitude > currentMaxSpeed)
        {
            rb.AddForce(-transform.forward * frontBrakeForce, ForceMode.Acceleration);
        }
    }

    Vector3 GetOffsetWaypointPosition()
    {
        Transform target = waypoints[currentWaypoint];

        int nextIndex = (currentWaypoint + 1) % waypoints.Length;

        Vector3 forward = waypoints[nextIndex].position - target.position;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.01f)
        {
            forward = transform.forward;
        }

        forward.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        return target.position + right * laneOffset;
    }

    void ApplyGrip()
    {
        Vector3 localVel = transform.InverseTransformDirection(rb.velocity);
        localVel.x *= Mathf.Clamp01(1f - sideGrip * Time.fixedDeltaTime);
        rb.velocity = transform.TransformDirection(localVel);
    }

    void CheckWaypointReached()
    {
        Vector3 targetPosition = GetOffsetWaypointPosition();

        Vector3 toTarget = targetPosition - transform.position;
        toTarget.y = 0f;

        float distance = toTarget.magnitude;

        int nextIndex = (currentWaypoint + 1) % waypoints.Length;

        Vector3 waypointForward = waypoints[nextIndex].position - waypoints[currentWaypoint].position;
        waypointForward.y = 0f;

        bool hasPassedWaypoint = false;

        if (waypointForward.sqrMagnitude > 0.01f)
        {
            waypointForward.Normalize();

            Vector3 fromWaypointToCar = transform.position - targetPosition;
            fromWaypointToCar.y = 0f;

            hasPassedWaypoint = Vector3.Dot(fromWaypointToCar, waypointForward) > 0f;
        }

        if (distance <= waypointReachDistance || hasPassedWaypoint)
        {
            SaveLastWaypointTransform();
            currentWaypoint = nextIndex;

            // Important:
            // Do NOT call RacerProgress.HitCheckpoint() here.
            // AI waypoints are for steering only.
            // Race progress must come from Checkpoint trigger gates for both player and AI.
        }
    }

    void CheckRecovery()
    {
        if (transform.position.y < fallYThreshold)
        {
            Debug.Log($"{name}: Respawning because it fell below fall threshold.");
            RespawnAtLastWaypoint();
            return;
        }

        if (raceStartTimer < 3f)
        {
            stuckTimer = 0f;
            return;
        }

        if (carInFront)
        {
            stuckTimer = 0f;
            return;
        }

        bool isMovingTooSlow = rb.velocity.magnitude < stuckSpeedThreshold;

        if (isMovingTooSlow)
        {
            stuckTimer += Time.fixedDeltaTime;

            if (stuckTimer >= stuckTimeThreshold)
            {
                Debug.Log($"{name}: Respawning because stuck timer reached {stuckTimeThreshold} seconds.");
                RespawnAtLastWaypoint();
            }
        }
        else
        {
            stuckTimer = 0f;
        }
    }

    void RespawnAtLastWaypoint()
    {
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        transform.position = lastWaypointPosition + Vector3.up * respawnHeightOffset;
        transform.rotation = lastWaypointRotation;

        currentWaypoint = GetClosestWaypointIndex();
        stuckTimer = 0f;
    }

    void SaveLastWaypointTransform()
    {
        if (waypoints == null || waypoints.Length == 0)
            return;

        Transform wp = waypoints[currentWaypoint];
        lastWaypointPosition = wp.position;

        int nextIndex = (currentWaypoint + 1) % waypoints.Length;

        Vector3 forwardDir = waypoints[nextIndex].position - wp.position;
        forwardDir.y = 0f;

        if (forwardDir.sqrMagnitude < 0.01f)
        {
            forwardDir = transform.forward;
        }

        lastWaypointRotation = Quaternion.LookRotation(forwardDir.normalized, Vector3.up);
    }

    int GetClosestWaypointIndex()
    {
        int closestIndex = 0;
        float closestDistance = Mathf.Infinity;

        for (int i = 0; i < waypoints.Length; i++)
        {
            float distance = Vector3.Distance(transform.position, waypoints[i].position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    public void SetWaypointParent(Transform newWaypointParent)
    {
        waypointParent = newWaypointParent;
        LoadWaypointsIfNeeded();

        if (waypoints != null && waypoints.Length > 0 && findClosestWaypointOnStart)
        {
            currentWaypoint = GetClosestWaypointIndex();
            SaveLastWaypointTransform();
        }
    }

    public void ResetToClosestWaypoint()
    {
        if (waypoints != null && waypoints.Length > 0)
        {
            currentWaypoint = GetClosestWaypointIndex();
            SaveLastWaypointTransform();
        }
    }

    public void ResetRaceStartTimer()
    {
        raceStartTimer = 0f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, sideAwarenessRadius);

        if (waypoints != null && waypoints.Length > 0 && currentWaypoint >= 0 && currentWaypoint < waypoints.Length)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(GetOffsetWaypointPosition(), waypointReachDistance);
        }
    }
}
