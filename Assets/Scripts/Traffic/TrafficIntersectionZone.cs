using UnityEngine;

public class TrafficIntersectionZone : MonoBehaviour
{
    [Header("Zone Shape")]
    [Tooltip("Local-space centre offset for the intersection box.")]
    public Vector3 centerOffset = Vector3.zero;

    [Tooltip("Size of the intersection box. X/Z cover the road area. Y should be tall enough to include cars.")]
    public Vector3 zoneSize = new Vector3(12f, 4f, 12f);

    [Header("Blocking")]
    [Tooltip("Layers that block the intersection. Usually Player/Car + TrafficCar. Do NOT include Ground/Road.")]
    public LayerMask blockingLayers = ~0;

    [Tooltip("If true, trigger colliders count as blockers.")]
    public bool includeTriggers = false;

    [Tooltip("Extra padding added to the zone when checking if it is clear.")]
    public float checkPadding = 0.5f;

    [Header("Reservation")]
    [Tooltip("Only one traffic car can reserve this zone at a time.")]
    public bool useReservation = true;

    [Tooltip("If the owner never clears the zone, reservation is released after this many seconds.")]
    public float maxReservationTime = 8f;

    [Tooltip("Once the owner has entered the zone, it releases after leaving the box by this distance.")]
    public float releasePadding = 2f;

    [Header("Debug")]
    public bool drawGizmos = true;
    public Color freeColor = new Color(0f, 1f, 0f, 0.25f);
    public Color reservedColor = new Color(1f, 0.6f, 0f, 0.25f);
    public Color blockedColor = new Color(1f, 0f, 0f, 0.25f);

    private TrafficCarAI reservedBy;
    private float reservationTimer;
    private bool reservedCarEnteredZone;

    public bool TryReserve(TrafficCarAI car)
    {
        if (car == null)
        {
            return false;
        }

        UpdateReservationState();

        if (!IsPhysicallyClearFor(car))
        {
            return false;
        }

        if (!useReservation)
        {
            return true;
        }

        if (reservedBy == null)
        {
            reservedBy = car;
            reservationTimer = 0f;
            reservedCarEnteredZone = ContainsPoint(car.transform.position);
            return true;
        }

        if (reservedBy == car)
        {
            return true;
        }

        return false;
    }

    public bool IsReservedBy(TrafficCarAI car)
    {
        return reservedBy != null && reservedBy == car;
    }

    public void ReleaseIfOwner(TrafficCarAI car)
    {
        if (reservedBy == car)
        {
            reservedBy = null;
            reservationTimer = 0f;
            reservedCarEnteredZone = false;
        }
    }

    public void UpdateOwnerState(TrafficCarAI car)
    {
        if (car == null)
        {
            return;
        }

        if (reservedBy != car)
        {
            return;
        }

        bool ownerInside = ContainsPoint(car.transform.position);

        if (ownerInside)
        {
            reservedCarEnteredZone = true;
        }

        if (reservedCarEnteredZone && !ContainsPointWithPadding(car.transform.position, releasePadding))
        {
            ReleaseIfOwner(car);
        }
    }

    private void Update()
    {
        UpdateReservationState();
    }

    private void UpdateReservationState()
    {
        if (reservedBy == null)
        {
            reservationTimer = 0f;
            reservedCarEnteredZone = false;
            return;
        }

        if (!reservedBy.gameObject.activeInHierarchy)
        {
            reservedBy = null;
            reservationTimer = 0f;
            reservedCarEnteredZone = false;
            return;
        }

        reservationTimer += Time.deltaTime;

        if (reservationTimer >= maxReservationTime)
        {
            reservedBy = null;
            reservationTimer = 0f;
            reservedCarEnteredZone = false;
        }
    }

    public bool IsPhysicallyClearFor(TrafficCarAI askingCar)
    {
        Vector3 worldCenter = GetWorldCenter();
        Vector3 halfExtents = GetHalfExtentsWithPadding(checkPadding);

        QueryTriggerInteraction triggerMode = includeTriggers
            ? QueryTriggerInteraction.Collide
            : QueryTriggerInteraction.Ignore;

        Collider[] hits = Physics.OverlapBox(
            worldCenter,
            halfExtents,
            transform.rotation,
            blockingLayers,
            triggerMode
        );

        foreach (Collider hit in hits)
        {
            if (hit == null)
            {
                continue;
            }

            if (askingCar != null)
            {
                if (hit.transform == askingCar.transform || hit.transform.IsChildOf(askingCar.transform))
                {
                    continue;
                }

                TrafficCarAI hitTrafficCar = hit.GetComponentInParent<TrafficCarAI>();

                if (hitTrafficCar == askingCar)
                {
                    continue;
                }
            }

            return false;
        }

        return true;
    }

    public bool ContainsPoint(Vector3 worldPoint)
    {
        Vector3 localPoint = transform.InverseTransformPoint(worldPoint) - centerOffset;
        Vector3 half = zoneSize * 0.5f;

        return Mathf.Abs(localPoint.x) <= half.x &&
               Mathf.Abs(localPoint.y) <= half.y &&
               Mathf.Abs(localPoint.z) <= half.z;
    }

    public bool ContainsPointWithPadding(Vector3 worldPoint, float padding)
    {
        Vector3 localPoint = transform.InverseTransformPoint(worldPoint) - centerOffset;
        Vector3 half = zoneSize * 0.5f + Vector3.one * padding;

        return Mathf.Abs(localPoint.x) <= half.x &&
               Mathf.Abs(localPoint.y) <= half.y &&
               Mathf.Abs(localPoint.z) <= half.z;
    }

    private Vector3 GetWorldCenter()
    {
        return transform.TransformPoint(centerOffset);
    }

    private Vector3 GetHalfExtentsWithPadding(float padding)
    {
        Vector3 paddedSize = zoneSize + Vector3.one * padding * 2f;
        return paddedSize * 0.5f;
    }

    private bool IsBlockedForGizmo()
    {
        Vector3 worldCenter = GetWorldCenter();
        Vector3 halfExtents = GetHalfExtentsWithPadding(checkPadding);

        QueryTriggerInteraction triggerMode = includeTriggers
            ? QueryTriggerInteraction.Collide
            : QueryTriggerInteraction.Ignore;

        Collider[] hits = Physics.OverlapBox(
            worldCenter,
            halfExtents,
            transform.rotation,
            blockingLayers,
            triggerMode
        );

        return hits != null && hits.Length > 0;
    }

    private void OnValidate()
    {
        zoneSize.x = Mathf.Max(0.1f, zoneSize.x);
        zoneSize.y = Mathf.Max(0.1f, zoneSize.y);
        zoneSize.z = Mathf.Max(0.1f, zoneSize.z);

        checkPadding = Mathf.Max(0f, checkPadding);
        releasePadding = Mathf.Max(0f, releasePadding);
        maxReservationTime = Mathf.Max(0.5f, maxReservationTime);
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos)
        {
            return;
        }

        Color colorToUse = freeColor;

        if (reservedBy != null)
        {
            colorToUse = reservedColor;
        }
        else if (Application.isPlaying && IsBlockedForGizmo())
        {
            colorToUse = blockedColor;
        }

        Matrix4x4 oldMatrix = Gizmos.matrix;

        Gizmos.matrix = Matrix4x4.TRS(
            GetWorldCenter(),
            transform.rotation,
            Vector3.one
        );

        Gizmos.color = colorToUse;
        Gizmos.DrawCube(Vector3.zero, zoneSize);

        Gizmos.color = new Color(colorToUse.r, colorToUse.g, colorToUse.b, 1f);
        Gizmos.DrawWireCube(Vector3.zero, zoneSize);

        Gizmos.matrix = oldMatrix;
    }
}