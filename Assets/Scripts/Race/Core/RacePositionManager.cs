using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class RacePositionManager : MonoBehaviour
{
    [Header("References")]
    public RacerProgress player;
    public RaceManager raceManager;

    [Header("Filtering")]
    public bool onlyUseCurrentRaceRacers = true;
    public Transform fallbackCheckpointParent;

    [Header("Refresh")]
    public bool autoRefreshRacerList = true;
    public float refreshInterval = 0.1f;

    [Header("Finish Tracking")]
    public int nextFinishOrder = 1;

    [Header("Debug")]
    public bool debugPlayerPosition = false;
    public bool debugSortedRacers = false;
    public float debugInterval = 1f;

    private readonly List<RacerProgress> allRacers = new List<RacerProgress>();
    private readonly List<RacerProgress> sortedRacers = new List<RacerProgress>();

    private float refreshTimer = 0f;
    private float debugTimer = 0f;

    private void Start()
    {
        RefreshRacerList();
    }

    private void Update()
    {
        if (autoRefreshRacerList)
        {
            refreshTimer += Time.deltaTime;

            if (refreshTimer >= refreshInterval)
            {
                refreshTimer = 0f;
                RefreshRacerList();
            }
        }

        DetectFinishedAIRacers();

        if (debugPlayerPosition || debugSortedRacers)
        {
            debugTimer += Time.deltaTime;

            if (debugTimer >= debugInterval)
            {
                debugTimer = 0f;

                if (debugPlayerPosition)
                    Debug.Log("[RacePositionManager] Player Position: " + GetPlayerPosition());

                if (debugSortedRacers)
                    LogSortedRacers();
            }
        }
    }

    public void ResetFinishTracking()
    {
        nextFinishOrder = 1;
    }

    public void SetPlayer(RacerProgress playerProgress)
    {
        player = playerProgress;
        ForceRefreshNow();
    }

    public void RefreshRacerList()
    {
        allRacers.Clear();

        RacerProgress[] foundRacers = FindObjectsOfType<RacerProgress>();
        Transform targetCheckpointParent = GetTargetCheckpointParent();

        for (int i = 0; i < foundRacers.Length; i++)
        {
            RacerProgress racer = foundRacers[i];

            if (racer == null)
                continue;

            if (!racer.gameObject.activeInHierarchy)
                continue;

            if (onlyUseCurrentRaceRacers && targetCheckpointParent != null)
            {
                if (racer.checkpointParent != targetCheckpointParent)
                    continue;
            }

            allRacers.Add(racer);
        }

        DetectFinishedAIRacers();
        RebuildSortedRacerList();
    }

    private void DetectFinishedAIRacers()
    {
        RaceDefinition currentRace = raceManager != null ? raceManager.GetCurrentRace() : null;

        if (currentRace == null)
            return;

        if (currentRace.raceType != RaceType.Circuit)
            return;

        for (int i = 0; i < allRacers.Count; i++)
        {
            RacerProgress racer = allRacers[i];

            if (racer == null)
                continue;

            if (racer == player)
                continue;

            if (racer.hasFinishedRace)
                continue;

            bool completedRequiredLaps = racer.completedLaps >= currentRace.laps;

            if (completedRequiredLaps)
            {
                float time = raceManager != null ? raceManager.GetCurrentRaceTime() : Time.time;
                racer.MarkFinished(time, nextFinishOrder);
                nextFinishOrder++;
            }
        }
    }

    private void RebuildSortedRacerList()
    {
        sortedRacers.Clear();

        sortedRacers.AddRange(
            allRacers
                .Where(r => r != null)

                // Finished racers always rank above unfinished racers.
                .OrderBy(r =>
                {
                    return r.hasFinishedRace ? 0 : 1;
                })

                // Finished racers use locked finish order.
                .ThenBy(r =>
                {
                    return r.hasFinishedRace ? r.finishOrder : int.MaxValue;
                })

                // Unfinished racers use continuous race progress.
                // This prevents checkpoint wrap glitches like 4th -> 1st -> 4th.
                .ThenByDescending(r => r.ProgressScore())

                // Within the same checkpoint segment, compare how far they are through the segment.
                .ThenByDescending(r => r.GetSegmentProgress01())

                // Before anyone has reached checkpoint 0, use grid order.
                .ThenBy(r =>
                {
                    if (r.lastCheckpointIndex < 0 && r.startingGridPosition >= 0)
                        return r.startingGridPosition;

                    return int.MaxValue;
                })

                // Final tie breaker.
                .ThenBy(r => r.DistanceToNextCheckpoint())
        );
    }

    private Transform GetTargetCheckpointParent()
    {
        if (player != null && player.checkpointParent != null)
            return player.checkpointParent;

        if (raceManager != null &&
            raceManager.GetCurrentRace() != null &&
            raceManager.GetCurrentRace().checkpointGroup != null)
        {
            return raceManager.GetCurrentRace().checkpointGroup.transform;
        }

        return fallbackCheckpointParent;
    }

    public int GetPlayerPosition()
    {
        if (player == null)
            return -1;

        if (!autoRefreshRacerList)
            RefreshRacerList();

        RebuildSortedRacerList();

        int index = sortedRacers.IndexOf(player);
        return index >= 0 ? index + 1 : -1;
    }

    public List<RacerProgress> GetSortedRacers()
    {
        if (!autoRefreshRacerList)
            RefreshRacerList();

        RebuildSortedRacerList();

        return new List<RacerProgress>(sortedRacers);
    }

    public RacerProgress GetRacerAtPosition(int position)
    {
        if (!autoRefreshRacerList)
            RefreshRacerList();

        RebuildSortedRacerList();

        if (position <= 0 || position > sortedRacers.Count)
            return null;

        return sortedRacers[position - 1];
    }

    public int GetRacerCount()
    {
        if (!autoRefreshRacerList)
            RefreshRacerList();

        RebuildSortedRacerList();

        return sortedRacers.Count;
    }

    public void ForceRefreshNow()
    {
        refreshTimer = 0f;
        RefreshRacerList();
    }

    private void LogSortedRacers()
    {
        RebuildSortedRacerList();

        string message = "[RacePositionManager] Sorted Racers:\n";

        for (int i = 0; i < sortedRacers.Count; i++)
        {
            RacerProgress r = sortedRacers[i];

            if (r == null)
                continue;

            message +=
                (i + 1) +
                ". " + r.name +
                " | completedLaps=" + r.completedLaps +
                " | currentLap=" + r.currentLap +
                " | lastCheckpoint=" + r.lastCheckpointIndex +
                " | nextCheckpoint=" + r.GetNextCheckpointIndex() +
                " | progressScore=" + r.ProgressScore() +
                " | segment=" + r.GetSegmentProgress01().ToString("0.00") +
                " | dist=" + r.DistanceToNextCheckpoint().ToString("0.0") +
                " | finished=" + r.hasFinishedRace +
                "\n";
        }

        Debug.Log(message);
    }
}