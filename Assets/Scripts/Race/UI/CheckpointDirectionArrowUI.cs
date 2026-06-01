using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CheckpointDirectionArrowUI : MonoBehaviour
{
    [Header("References")]
    public RaceManager raceManager;
    public Transform player;
    public Camera targetCamera;

    [Header("UI")]
    public RectTransform arrowRect;
    public Image arrowImage;
    public TMP_Text distanceText;

    [Header("Behaviour")]
    [Tooltip("If the player is within this radius of the next checkpoint, the arrow points to the following checkpoint instead.")]
    public float switchToFollowingCheckpointRadius = 35f;

    [Tooltip("How quickly the arrow rotates.")]
    public float rotationSmoothSpeed = 12f;

    [Tooltip("Hide the arrow when there is no active race.")]
    public bool hideWhenNoRace = true;

    [Tooltip("If true, the arrow will point to the next checkpoint even while countdown is active.")]
    public bool showDuringCountdown = true;

    [Header("Display")]
    public bool showDistance = true;
    public string distanceSuffix = "m";

    [Header("Debug")]
    public bool logWarnings = false;

    private readonly List<Checkpoint> checkpoints = new List<Checkpoint>();
    private RaceDefinition cachedRace;
    private Transform currentTarget;
    private float currentZRotation;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        SetVisible(false);
    }

    private void Update()
    {
        if (raceManager == null)
            raceManager = RaceManager.Instance;

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (player == null)
            FindPlayer();

        if (!CanShowArrow())
        {
            SetVisible(false);
            return;
        }

        RefreshCheckpointCacheIfNeeded();

        currentTarget = GetVisualTargetCheckpoint();

        if (currentTarget == null)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);
        UpdateArrowRotation();
        UpdateDistanceText();
    }

    private bool CanShowArrow()
    {
        if (raceManager == null)
            return false;

        RaceDefinition currentRace = raceManager.GetCurrentRace();

        if (currentRace == null)
            return false;

        if (hideWhenNoRace)
        {
            bool raceIsActive = raceManager.IsRaceActive();
            bool countdownIsActive = raceManager.IsCountdownActive();

            if (!raceIsActive && !(showDuringCountdown && countdownIsActive))
                return false;
        }

        if (player == null)
            return false;

        if (arrowRect == null)
            return false;

        return true;
    }

    private void RefreshCheckpointCacheIfNeeded()
    {
        RaceDefinition currentRace = raceManager.GetCurrentRace();

        if (currentRace == cachedRace && checkpoints.Count > 0)
            return;

        cachedRace = currentRace;
        checkpoints.Clear();

        if (cachedRace == null || cachedRace.checkpointGroup == null)
            return;

        Checkpoint[] found = cachedRace.checkpointGroup.GetComponentsInChildren<Checkpoint>(true);

        checkpoints.AddRange(
            found
                .Where(cp => cp != null)
                .OrderBy(cp => cp.checkpointIndex)
        );

        if (logWarnings)
            Debug.Log("[CheckpointDirectionArrowUI] Cached checkpoints: " + checkpoints.Count);
    }

    private Transform GetVisualTargetCheckpoint()
    {
        if (checkpoints.Count == 0)
            return null;

        int nextIndex = raceManager.GetNextCheckpointIndex();

        if (nextIndex < 0)
            nextIndex = 0;

        nextIndex = Mathf.Clamp(nextIndex, 0, checkpoints.Count - 1);

        Checkpoint nextCheckpoint = GetCheckpointByIndex(nextIndex);

        if (nextCheckpoint == null)
            return null;

        float distanceToNext = Vector3.Distance(player.position, nextCheckpoint.transform.position);

        if (distanceToNext <= switchToFollowingCheckpointRadius)
        {
            int followingIndex = nextIndex + 1;

            RaceDefinition currentRace = raceManager.GetCurrentRace();

            if (currentRace != null && currentRace.raceType == RaceType.Circuit)
            {
                followingIndex %= checkpoints.Count;
            }
            else
            {
                followingIndex = Mathf.Clamp(followingIndex, 0, checkpoints.Count - 1);
            }

            Checkpoint followingCheckpoint = GetCheckpointByIndex(followingIndex);

            if (followingCheckpoint != null)
                return followingCheckpoint.transform;
        }

        return nextCheckpoint.transform;
    }

    private Checkpoint GetCheckpointByIndex(int checkpointIndex)
    {
        for (int i = 0; i < checkpoints.Count; i++)
        {
            if (checkpoints[i] != null && checkpoints[i].checkpointIndex == checkpointIndex)
                return checkpoints[i];
        }

        if (checkpointIndex >= 0 && checkpointIndex < checkpoints.Count)
            return checkpoints[checkpointIndex];

        return null;
    }

    private void UpdateArrowRotation()
    {
        if (currentTarget == null || player == null || targetCamera == null)
            return;

        Vector3 worldDirection = currentTarget.position - player.position;
        worldDirection.y = 0f;

        if (worldDirection.sqrMagnitude < 0.01f)
            return;

        Vector3 cameraForward = targetCamera.transform.forward;
        cameraForward.y = 0f;
        cameraForward.Normalize();

        Vector3 cameraRight = targetCamera.transform.right;
        cameraRight.y = 0f;
        cameraRight.Normalize();

        float x = Vector3.Dot(worldDirection.normalized, cameraRight);
        float y = Vector3.Dot(worldDirection.normalized, cameraForward);

        float targetAngle = Mathf.Atan2(x, y) * Mathf.Rad2Deg;

        currentZRotation = Mathf.LerpAngle(
            currentZRotation,
            -targetAngle,
            Time.deltaTime * rotationSmoothSpeed
        );

        arrowRect.localRotation = Quaternion.Euler(0f, 0f, currentZRotation);
    }

    private void UpdateDistanceText()
    {
        if (distanceText == null)
            return;

        if (!showDistance || currentTarget == null || player == null)
        {
            distanceText.text = "";
            return;
        }

        float distance = Vector3.Distance(player.position, currentTarget.position);
        distanceText.text = Mathf.RoundToInt(distance) + distanceSuffix;
    }

    private void SetVisible(bool visible)
    {
        if (arrowImage != null)
            arrowImage.enabled = visible;

        if (distanceText != null)
            distanceText.enabled = visible;
    }

    private void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
            player = playerObject.transform;
    }
}