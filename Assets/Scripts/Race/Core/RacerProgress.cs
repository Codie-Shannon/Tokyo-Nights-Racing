using UnityEngine;
using System.Linq;

public class RacerProgress : MonoBehaviour
{
    [Header("Race Progress")]
    public int currentLap = 1;
    public int completedLaps = 0;

    [Tooltip("Last checkpoint hit in the current lap. -1 means the racer is at the start of the current lap and is waiting for checkpoint 0.")]
    public int lastCheckpointIndex = -1;

    public int startingGridPosition = -1;

    [Header("Lap Handling")]
    [Tooltip("For circuit-style races, hitting the final checkpoint completes a lap and resets lastCheckpointIndex to -1 so checkpoint 0 becomes next.")]
    public bool completeLapWhenFinalCheckpointHit = true;

    [Header("Finish State")]
    public bool hasFinishedRace = false;
    public float finishTime = -1f;
    public int finishOrder = -1;

    [Header("Checkpoints")]
    public Transform[] checkpoints;
    public Transform checkpointParent;
    public bool autoLoadCheckpointsFromParent = true;

    public int TotalCheckpoints
    {
        get { return checkpoints != null ? checkpoints.Length : 0; }
    }

    private void Start()
    {
        LoadCheckpointsIfNeeded();
    }

    public void LoadCheckpointsIfNeeded()
    {
        if (!autoLoadCheckpointsFromParent)
            return;

        if (checkpointParent == null)
            return;

        Transform[] loaded = new Transform[checkpointParent.childCount];

        for (int i = 0; i < checkpointParent.childCount; i++)
        {
            loaded[i] = checkpointParent.GetChild(i);
        }

        checkpoints = loaded
            .OrderBy(t =>
            {
                Checkpoint cp = t.GetComponent<Checkpoint>();
                return cp != null ? cp.checkpointIndex : int.MaxValue;
            })
            .ToArray();
    }

    public void HitCheckpoint(int checkpointIndex)
    {
        if (hasFinishedRace)
            return;

        if (TotalCheckpoints == 0)
            return;

        int expectedCheckpoint = GetNextCheckpointIndex();

        if (checkpointIndex != expectedCheckpoint)
            return;

        bool hitFinalCheckpoint = checkpointIndex == TotalCheckpoints - 1;

        if (completeLapWhenFinalCheckpointHit && hitFinalCheckpoint)
        {
            MarkLapCompleted();

            // Important:
            // After completing a lap, the racer is now at the start of the next lap.
            // The next expected checkpoint should be 0.
            // Keeping lastCheckpointIndex at the final checkpoint causes ranking glitches.
            lastCheckpointIndex = -1;
            return;
        }

        lastCheckpointIndex = checkpointIndex;
    }

    public void MarkLapCompleted()
    {
        completedLaps++;
        currentLap = completedLaps + 1;
    }

    public void MarkFinished(float time, int order)
    {
        hasFinishedRace = true;
        finishTime = time;
        finishOrder = order;

        // Finished racers are handled first by RacePositionManager finishOrder.
        completedLaps = Mathf.Max(completedLaps, 9999);
        lastCheckpointIndex = Mathf.Max(lastCheckpointIndex, TotalCheckpoints - 1);
    }

    public int GetNextCheckpointIndex()
    {
        if (TotalCheckpoints == 0)
            return -1;

        return (lastCheckpointIndex + 1) % TotalCheckpoints;
    }

    public float DistanceToNextCheckpoint()
    {
        if (hasFinishedRace)
            return 0f;

        if (checkpoints == null || checkpoints.Length == 0)
            return Mathf.Infinity;

        int nextCheckpoint = GetNextCheckpointIndex();

        if (nextCheckpoint < 0 || nextCheckpoint >= checkpoints.Length)
            return Mathf.Infinity;

        if (checkpoints[nextCheckpoint] == null)
            return Mathf.Infinity;

        return Vector3.Distance(transform.position, checkpoints[nextCheckpoint].position);
    }

    public float GetSegmentProgress01()
    {
        if (hasFinishedRace)
            return 1f;

        if (checkpoints == null || checkpoints.Length == 0)
            return 0f;

        if (lastCheckpointIndex < 0 || lastCheckpointIndex >= checkpoints.Length)
            return 0f;

        int nextIndex = GetNextCheckpointIndex();

        Transform from = checkpoints[lastCheckpointIndex];
        Transform to = checkpoints[nextIndex];

        if (from == null || to == null)
            return 0f;

        Vector3 segment = to.position - from.position;
        float segmentLength = segment.magnitude;

        if (segmentLength <= 0.001f)
            return 0f;

        Vector3 racerOffset = transform.position - from.position;
        float dot = Vector3.Dot(racerOffset, segment.normalized);

        return Mathf.Clamp01(dot / segmentLength);
    }

    public int ProgressScore()
    {
        if (hasFinishedRace)
            return int.MaxValue;

        int checkpointCount = Mathf.Max(1, TotalCheckpoints);

        // lastCheckpointIndex -1 means start of this lap.
        // Add 1 so:
        // -1 = 0 progress into current lap
        //  0 = passed checkpoint 0
        //  1 = passed checkpoint 1
        // etc.
        int checkpointProgress = Mathf.Max(0, lastCheckpointIndex + 1);

        return completedLaps * checkpointCount + checkpointProgress;
    }

    public void ResetProgress()
    {
        currentLap = 1;
        completedLaps = 0;
        lastCheckpointIndex = -1;
        hasFinishedRace = false;
        finishTime = -1f;
        finishOrder = -1;
    }

    public void SetCheckpointParent(Transform newCheckpointParent)
    {
        checkpointParent = newCheckpointParent;
        LoadCheckpointsIfNeeded();
        ResetProgress();
    }

    public void SetStartingGridPosition(int gridPosition)
    {
        startingGridPosition = gridPosition;
    }
}