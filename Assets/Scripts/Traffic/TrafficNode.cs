using System.Collections.Generic;
using UnityEngine;

public enum TrafficNodeType
{
    Normal,
    Intersection,
    CityExitDespawn,
    SpawnOnly,
    DeadEndTurnaround,
    NoTraffic
}

public class TrafficNode : MonoBehaviour
{
    [Header("Node Type")]
    public TrafficNodeType nodeType = TrafficNodeType.Normal;

    [Header("Connections")]
    [Tooltip("The next possible nodes this traffic car can drive to.")]
    public List<TrafficNode> nextNodes = new List<TrafficNode>();

    [Header("Driving")]
    [Tooltip("Recommended speed when heading toward this node.")]
    public float recommendedSpeedKph = 35f;

    [Tooltip("How close the car must get before switching to the next node.")]
    public float reachDistance = 4f;

    [Header("Steering Override")]
    [Tooltip("If true, traffic cars use this node's steering strength instead of their default.")]
    public bool overrideSteeringStrength = false;

    [Tooltip("Custom steering strength used when Override Steering Strength is enabled.")]
    public float steeringStrength = 5f;

    [Header("Intersection Zone Check")]
    [Tooltip("If true, car must reserve/clear this zone before advancing from this node.")]
    public bool requireClearZoneBeforeAdvancing = false;

    [Tooltip("The zone this node checks before allowing the car to advance.")]
    public TrafficIntersectionZone clearZone;

    [Tooltip("If true, this node draws extra gizmos for zone checking.")]
    public bool drawZoneCheckGizmo = true;

    [Header("City Exit / Spawn")]
    [Tooltip("Used by TrafficSpawner. A car can spawn here.")]
    public bool canBeSpawnPoint = false;

    [Tooltip("Used by TrafficSpawner. A car can despawn here.")]
    public bool canBeDespawnPoint = false;

    [Header("Debug")]
    public bool drawGizmos = true;

    public Color normalColor = Color.yellow;
    public Color intersectionColor = new Color(1f, 0.5f, 0f);
    public Color cityExitColor = Color.red;
    public Color spawnOnlyColor = Color.green;
    public Color turnaroundColor = Color.magenta;
    public Color noTrafficColor = Color.gray;
    public Color connectionColor = Color.cyan;
    public Color steeringOverrideColor = Color.blue;
    public Color zoneCheckColor = Color.white;

    public TrafficNode GetNextNode()
    {
        if (nodeType == TrafficNodeType.NoTraffic)
        {
            return null;
        }

        if (nextNodes == null || nextNodes.Count == 0)
        {
            return null;
        }

        List<TrafficNode> validNodes = new List<TrafficNode>();

        foreach (TrafficNode node in nextNodes)
        {
            if (node == null)
            {
                continue;
            }

            if (node.nodeType == TrafficNodeType.NoTraffic)
            {
                continue;
            }

            validNodes.Add(node);
        }

        if (validNodes.Count == 0)
        {
            return null;
        }

        int randomIndex = Random.Range(0, validNodes.Count);
        return validNodes[randomIndex];
    }

    public bool IsCityExit()
    {
        return nodeType == TrafficNodeType.CityExitDespawn || canBeDespawnPoint;
    }

    public bool IsSpawnOnly()
    {
        return nodeType == TrafficNodeType.SpawnOnly || canBeSpawnPoint;
    }

    public Color GetNodeColor()
    {
        switch (nodeType)
        {
            case TrafficNodeType.Intersection:
                return intersectionColor;

            case TrafficNodeType.CityExitDespawn:
                return cityExitColor;

            case TrafficNodeType.SpawnOnly:
                return spawnOnlyColor;

            case TrafficNodeType.DeadEndTurnaround:
                return turnaroundColor;

            case TrafficNodeType.NoTraffic:
                return noTrafficColor;

            case TrafficNodeType.Normal:
            default:
                return normalColor;
        }
    }

    private void OnValidate()
    {
        recommendedSpeedKph = Mathf.Max(1f, recommendedSpeedKph);
        reachDistance = Mathf.Max(0.5f, reachDistance);
        steeringStrength = Mathf.Max(0.1f, steeringStrength);

        if (nodeType == TrafficNodeType.CityExitDespawn)
        {
            canBeDespawnPoint = true;
        }

        if (nodeType == TrafficNodeType.SpawnOnly)
        {
            canBeSpawnPoint = true;
        }
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos)
        {
            return;
        }

        Gizmos.color = GetNodeColor();

        float size = 0.45f;

        if (nodeType == TrafficNodeType.CityExitDespawn)
        {
            size = 0.75f;
        }
        else if (nodeType == TrafficNodeType.SpawnOnly)
        {
            size = 0.65f;
        }
        else if (nodeType == TrafficNodeType.Intersection)
        {
            size = 0.55f;
        }

        Gizmos.DrawSphere(transform.position, size);

        DrawForwardArrow();

        if (overrideSteeringStrength)
        {
            Gizmos.color = steeringOverrideColor;
            Gizmos.DrawWireSphere(transform.position, size + 0.45f);
        }

        if (requireClearZoneBeforeAdvancing && clearZone != null && drawZoneCheckGizmo)
        {
            Gizmos.color = zoneCheckColor;
            Gizmos.DrawLine(transform.position, clearZone.transform.position);
            Gizmos.DrawWireSphere(transform.position, size + 0.8f);
        }

        if (nextNodes == null)
        {
            return;
        }

        Gizmos.color = connectionColor;

        foreach (TrafficNode nextNode in nextNodes)
        {
            if (nextNode == null)
            {
                continue;
            }

            Gizmos.DrawLine(transform.position, nextNode.transform.position);

            Vector3 direction = nextNode.transform.position - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
            {
                continue;
            }

            direction.Normalize();

            Vector3 arrowPoint = Vector3.Lerp(transform.position, nextNode.transform.position, 0.75f);

            Gizmos.DrawLine(
                arrowPoint,
                arrowPoint + Quaternion.Euler(0f, 145f, 0f) * direction * 1f
            );

            Gizmos.DrawLine(
                arrowPoint,
                arrowPoint + Quaternion.Euler(0f, -145f, 0f) * direction * 1f
            );
        }
    }

    private void DrawForwardArrow()
    {
        Vector3 start = transform.position + Vector3.up * 0.1f;
        Vector3 end = start + transform.forward * 2.5f;

        Gizmos.DrawLine(start, end);

        Vector3 leftWing = Quaternion.Euler(0f, 145f, 0f) * transform.forward;
        Vector3 rightWing = Quaternion.Euler(0f, -145f, 0f) * transform.forward;

        Gizmos.DrawLine(end, end + leftWing * 0.7f);
        Gizmos.DrawLine(end, end + rightWing * 0.7f);
    }
}