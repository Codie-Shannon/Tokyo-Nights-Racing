using System;
using System.Collections;
using UnityEngine;
using TMPro;

public class RaceManager : MonoBehaviour
{
    public static RaceManager Instance;

    [Header("Player")]
    public CarController playerCar;

    [Header("Race Position Manager")]
    public RacePositionManager racePositionManager;

    [Header("UI")]
    [Tooltip("Old status text fallback. You can leave this assigned while migrating.")]
    public TMP_Text statusText;

    [Tooltip("Small corner/side status text. Use this for Lap Complete, Head to Finish, Respawned, Race Cancelled.")]
    public TMP_Text smallStatusText;

    [Tooltip("Big center status text. Use this for countdown, GO, and Race Finished.")]
    public TMP_Text bigStatusText;

    public GameObject racingHUDRoot;
    public ResultsScreenUI resultsScreenUI;

    [Header("Status Display Timing")]
    public float smallStatusDisplayDuration = 2.0f;
    public float raceFinishedDisplayDuration = 3.0f;

    [Header("Loading Screen")]
    public LoadingScreenController loadingScreen;
    public float hideLoadingBeforeCountdownDelay = 0.5f;

    [Header("Countdown")]
    public bool useCountdown = true;
    public float countdownDuration = 3f;
    public float goDisplayDuration = 1f;

    [Header("Results Delay")]
    public float resultsPanelDelay = 3f;

    [Header("Debug")]
    public bool logDebugMessages = true;

    public Action<RaceDefinition> OnRaceStarted;
    public Action<int> OnCheckpointHitEvent;
    public Action<int> OnLapCompleted;
    public Action<RaceResult> OnRaceFinished;
    public Action OnRaceCancelled;

    private RaceDefinition currentRace;
    private RaceResult lastResult;

    private int currentLap = 1;
    private int nextCheckpointIndex = 0;
    private int totalCheckpointCount = 0;

    private bool raceActive = false;
    private bool raceFinished = false;
    private bool countdownActive = false;
    private bool timerRunning = false;
    private bool finishUnlocked = false;
    private bool raceSetupInProgress = false;

    private float countdownTimer = 0f;
    private float goTimer = 0f;
    private float smallStatusTimer = 0f;
    private float bigStatusTimer = 0f;

    private float currentRaceTime = 0f;
    private float finalRaceTime = 0f;
    private float currentLapTime = 0f;
    private float bestLapTime = -1f;

    private Transform lastCheckpointTransform;
    private int lastCheckpointIndex = -1;

    private string currentStatusMessage = "";
    private string currentSmallStatusMessage = "";
    private string currentBigStatusMessage = "";
    private string finalPositionAtFinish = "--";

    private Coroutine showResultsRoutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        SetRacingHUDVisible(false);
        ClearAllStatus();
    }

    private void Update()
    {
        UpdateCountdown();
        UpdateRaceTimer();
        UpdateStatusTimers();
    }

    public void TryStartRace(RaceDefinition raceDefinition)
    {
        if (raceDefinition == null)
        {
            ShowSmallStatus("Race setup missing");
            Log("Cannot start race: RaceDefinition is null.");
            return;
        }

        if (raceActive || countdownActive || raceSetupInProgress)
        {
            ShowSmallStatus("Race already active");
            Log("Cannot start race: another race is already active.");
            return;
        }

        if (showResultsRoutine != null)
        {
            StopCoroutine(showResultsRoutine);
            showResultsRoutine = null;
        }

        if (PlayerGarage.Instance != null)
        {
            if (!CanUseVehicleForRace(raceDefinition))
            {
                string reason = GetRaceStartFailureReason(raceDefinition);
                ShowSmallStatus(reason);
                Log("Cannot start race: " + reason);
                return;
            }

            PrepareVehicleForRace(raceDefinition);
        }

        BeginRaceSetup(raceDefinition);
    }

    private void BeginRaceSetup(RaceDefinition raceDefinition)
    {
        currentRace = raceDefinition;

        SetMissionMarkersActive(false);

        currentLap = 1;
        nextCheckpointIndex = 0;
        totalCheckpointCount = 0;

        raceActive = false;
        raceFinished = false;
        finishUnlocked = false;
        raceSetupInProgress = true;

        countdownActive = false;
        timerRunning = false;

        countdownTimer = 0f;
        goTimer = 0f;
        smallStatusTimer = 0f;
        bigStatusTimer = 0f;

        currentRaceTime = 0f;
        finalRaceTime = 0f;
        currentLapTime = 0f;
        bestLapTime = -1f;

        lastCheckpointTransform = null;
        lastCheckpointIndex = -1;
        lastResult = null;
        finalPositionAtFinish = "--";

        if (racePositionManager != null)
            racePositionManager.ResetFinishTracking();

        if (resultsScreenUI != null)
            resultsScreenUI.HideResults();

        if (playerCar != null)
            playerCar.canDrive = false;

        ClearAllStatus();
        StartCoroutine(DelayedRaceSetup(raceDefinition));
    }

    private IEnumerator DelayedRaceSetup(RaceDefinition raceDefinition)
    {
        yield return new WaitForFixedUpdate();

        if (raceDefinition == null)
        {
            raceSetupInProgress = false;
            yield break;
        }

        if (currentRace == null || currentRace != raceDefinition)
        {
            raceSetupInProgress = false;
            yield break;
        }

        if (currentRace.checkpointGroup != null)
        {
            currentRace.checkpointGroup.SetActive(true);
            totalCheckpointCount = currentRace.checkpointGroup.GetComponentsInChildren<Checkpoint>(true).Length;
        }

        PositionRaceEntrants();
        RefreshRacePositionManager();

        SetRacingHUDVisible(true);

        if (loadingScreen == null)
            loadingScreen = LoadingScreenController.Instance;

        if (loadingScreen != null)
        {
            yield return loadingScreen.HideAfterDelay(hideLoadingBeforeCountdownDelay);
        }

        if (useCountdown)
        {
            countdownActive = true;
            countdownTimer = countdownDuration;
            goTimer = 0f;
            ShowBigStatus(Mathf.CeilToInt(countdownTimer).ToString(), 0f);
        }
        else
        {
            StartRaceNow();
        }

        raceSetupInProgress = false;

        Log("Prepared race: " + currentRace.raceDisplayName + " | Type: " + currentRace.raceType);
    }

    private void PositionRaceEntrants()
    {
        if (currentRace == null)
            return;

        if (currentRace.useGridStart && currentRace.gridStartManager != null)
        {
            currentRace.gridStartManager.gameObject.SetActive(true);
            currentRace.gridStartManager.SetPlayerCar(playerCar != null ? playerCar.transform : null);

            currentRace.gridStartManager.ConfigureForRace(
                currentRace.waypointParent,
                currentRace.checkpointGroup != null ? currentRace.checkpointGroup.transform : null,
                currentRace.aiCount
            );

            currentRace.gridStartManager.SetupRaceGrid();

            if (playerCar != null)
            {
                Rigidbody playerRb = playerCar.GetComponent<Rigidbody>();

                if (playerRb != null)
                {
                    playerRb.velocity = Vector3.zero;
                    playerRb.angularVelocity = Vector3.zero;
                }

                PlayerRespawn playerRespawn = playerCar.GetComponent<PlayerRespawn>();

                if (playerRespawn != null)
                {
                    playerRespawn.SetRespawnPoint(playerCar.transform.position, playerCar.transform.rotation);
                }
            }

            return;
        }

        if (currentRace.startPoint != null && playerCar != null)
        {
            Transform carTransform = playerCar.transform;
            Rigidbody rb = playerCar.GetComponent<Rigidbody>();

            carTransform.position = currentRace.startPoint.position;
            carTransform.rotation = currentRace.startPoint.rotation;

            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            PlayerRespawn playerRespawn = playerCar.GetComponent<PlayerRespawn>();

            if (playerRespawn != null)
            {
                playerRespawn.SetRespawnPoint(carTransform.position, carTransform.rotation);
            }
        }
    }

    private void RefreshRacePositionManager()
    {
        if (racePositionManager == null || playerCar == null)
            return;

        RacerProgress playerProgress = playerCar.GetComponent<RacerProgress>();

        if (playerProgress != null)
        {
            racePositionManager.SetPlayer(playerProgress);
            racePositionManager.raceManager = this;
            racePositionManager.ForceRefreshNow();
        }
    }

    private void StartRaceNow()
    {
        countdownActive = false;
        raceActive = true;
        raceFinished = false;
        timerRunning = true;
        goTimer = goDisplayDuration;

        if (playerCar != null)
            playerCar.canDrive = true;

        if (currentRace != null && currentRace.useGridStart && currentRace.gridStartManager != null)
        {
            var spawnedAI = currentRace.gridStartManager.GetSpawnedAICars();

            for (int i = 0; i < spawnedAI.Count; i++)
            {
                if (spawnedAI[i] == null)
                    continue;

                AICarController ai = spawnedAI[i].GetComponent<AICarController>();

                if (ai != null)
                    ai.canDrive = true;
            }
        }

        ShowBigStatus("GO", goDisplayDuration);

        OnRaceStarted?.Invoke(currentRace);

        Log("Started race: " + currentRace.raceDisplayName + " | Type: " + currentRace.raceType);
    }

    private void UpdateCountdown()
    {
        if (!countdownActive)
            return;

        countdownTimer -= Time.deltaTime;

        if (countdownTimer > 0f)
        {
            ShowBigStatus(Mathf.CeilToInt(countdownTimer).ToString(), 0f);
            return;
        }

        StartRaceNow();
    }

    private void UpdateRaceTimer()
    {
        if (!timerRunning)
            return;

        currentRaceTime += Time.deltaTime;

        if (currentRace != null && currentRace.raceType == RaceType.Circuit)
            currentLapTime += Time.deltaTime;
    }

    private void UpdateStatusTimers()
    {
        if (goTimer > 0f)
        {
            goTimer -= Time.deltaTime;

            if (goTimer <= 0f && currentBigStatusMessage == "GO")
                ShowBigStatus("");
        }

        if (smallStatusTimer > 0f)
        {
            smallStatusTimer -= Time.deltaTime;

            if (smallStatusTimer <= 0f)
                ShowSmallStatus("");
        }

        if (bigStatusTimer > 0f)
        {
            bigStatusTimer -= Time.deltaTime;

            if (bigStatusTimer <= 0f)
                ShowBigStatus("");
        }
    }

    public void HitCheckpoint(int checkpointIndex, Transform checkpointTransform = null)
    {
        if (!raceActive || raceFinished || currentRace == null)
            return;

        if (checkpointIndex != nextCheckpointIndex)
            return;

        lastCheckpointIndex = checkpointIndex;
        lastCheckpointTransform = checkpointTransform;

        OnCheckpointHitEvent?.Invoke(checkpointIndex);

        nextCheckpointIndex++;

        bool reachedEndOfCheckpointSequence = nextCheckpointIndex >= totalCheckpointCount;

        switch (currentRace.raceType)
        {
            case RaceType.PointToPoint:
            case RaceType.Offroad:
                if (reachedEndOfCheckpointSequence)
                {
                    if (UsesFinishTrigger())
                    {
                        finishUnlocked = true;
                        ShowSmallStatus("Head to finish", smallStatusDisplayDuration);
                    }
                    else
                    {
                        FinishRace();
                        return;
                    }
                }
                break;

            case RaceType.Circuit:
                if (reachedEndOfCheckpointSequence)
                {
                    CompleteLap();
                    nextCheckpointIndex = 0;

                    if (currentLap > currentRace.laps)
                    {
                        FinishRace();
                        return;
                    }
                }
                break;
        }
    }

    private void CompleteLap()
    {
        if (currentRace == null)
            return;

        int completedLap = currentLap;

        if (bestLapTime < 0f || currentLapTime < bestLapTime)
            bestLapTime = currentLapTime;

        currentLapTime = 0f;
        currentLap++;

        if (currentLap <= currentRace.laps)
        {
            ShowSmallStatus("Lap " + completedLap + " complete", smallStatusDisplayDuration);
        }

        OnLapCompleted?.Invoke(completedLap);
    }

    public void HitFinishTrigger()
    {
        if (!raceActive || raceFinished || currentRace == null)
            return;

        if (currentRace.raceType == RaceType.Circuit)
            return;

        if (!UsesFinishTrigger())
            return;

        if (!finishUnlocked)
            return;

        FinishRace();
    }

    private void FinishRace()
    {
        raceFinished = true;
        raceActive = false;
        countdownActive = false;
        timerRunning = false;
        raceSetupInProgress = false;
        finalRaceTime = currentRaceTime;

        SetMissionMarkersActive(true);

        if (playerCar != null)
            playerCar.canDrive = false;

        if (currentRace != null && currentRace.checkpointGroup != null)
            currentRace.checkpointGroup.SetActive(false);

        BuildRaceResult();

        finalPositionAtFinish = "--";

        if (racePositionManager != null)
        {
            racePositionManager.ForceRefreshNow();

            int playerPos = racePositionManager.GetPlayerPosition();
            finalPositionAtFinish = FormatPosition(playerPos);
        }

        ClearGridRaceAI();

        SetRacingHUDVisible(false);

        ShowSmallStatus("");
        ShowBigStatus("Race Finished!", raceFinishedDisplayDuration);

        OnRaceFinished?.Invoke(lastResult);

        Log("Race finished: " + (currentRace != null ? currentRace.raceDisplayName : "Unknown"));
        Log("Final player position captured: " + finalPositionAtFinish);

        if (showResultsRoutine != null)
            StopCoroutine(showResultsRoutine);

        showResultsRoutine = StartCoroutine(ShowResultsAfterDelay());
    }

    private IEnumerator ShowResultsAfterDelay()
    {
        yield return new WaitForSeconds(resultsPanelDelay);

        ShowBigStatus("");

        if (resultsScreenUI != null && lastResult != null)
        {
            resultsScreenUI.ShowResults(lastResult, finalPositionAtFinish);
        }
        else
        {
            Debug.LogWarning("[RaceManager] Results screen not shown. ResultsScreenUI or RaceResult is missing.");
        }

        showResultsRoutine = null;
    }

    private void ClearGridRaceAI()
    {
        if (currentRace != null &&
            currentRace.useGridStart &&
            currentRace.gridStartManager != null)
        {
            currentRace.gridStartManager.ClearSpawnedAI();
        }
    }

    private void BuildRaceResult()
    {
        if (currentRace == null)
            return;

        lastResult = new RaceResult
        {
            raceName = currentRace.raceDisplayName,
            raceType = currentRace.raceType,
            totalTime = finalRaceTime,
            bestLapTime = bestLapTime,
            lapsCompleted = currentRace.raceType == RaceType.Circuit ? currentLap - 1 : 1
        };
    }

    public void CancelCurrentRace()
    {
        if (showResultsRoutine != null)
        {
            StopCoroutine(showResultsRoutine);
            showResultsRoutine = null;
        }

        if (currentRace != null && currentRace.checkpointGroup != null)
            currentRace.checkpointGroup.SetActive(false);

        SetMissionMarkersActive(true);
        ClearGridRaceAI();

        currentRace = null;
        currentLap = 1;
        nextCheckpointIndex = 0;
        totalCheckpointCount = 0;

        raceActive = false;
        raceFinished = false;
        countdownActive = false;
        timerRunning = false;
        finishUnlocked = false;
        raceSetupInProgress = false;

        countdownTimer = 0f;
        goTimer = 0f;
        smallStatusTimer = 0f;
        bigStatusTimer = 0f;

        currentRaceTime = 0f;
        finalRaceTime = 0f;
        currentLapTime = 0f;
        bestLapTime = -1f;

        lastCheckpointTransform = null;
        lastCheckpointIndex = -1;
        lastResult = null;
        finalPositionAtFinish = "--";

        if (playerCar != null)
            playerCar.canDrive = true;

        if (resultsScreenUI != null)
            resultsScreenUI.HideResults();

        ShowBigStatus("");
        ShowSmallStatus("Race Cancelled", smallStatusDisplayDuration);
        SetRacingHUDVisible(false);

        OnRaceCancelled?.Invoke();

        Log("Race cancelled.");
    }

    public void SetPlayerCar(CarController newPlayerCar)
    {
        playerCar = newPlayerCar;

        if (playerCar == null)
        {
            Log("Player car cleared.");
            return;
        }

        RacerProgress playerProgress = playerCar.GetComponent<RacerProgress>();

        if (playerProgress == null)
            playerProgress = playerCar.GetComponentInChildren<RacerProgress>(true);

        if (racePositionManager != null && playerProgress != null)
        {
            racePositionManager.SetPlayer(playerProgress);
            racePositionManager.raceManager = this;
        }

        Log("Player car registered: " + playerCar.name);
    }

    public void RespawnAtLastCheckpoint()
    {
        if (playerCar == null || lastCheckpointTransform == null)
            return;

        Rigidbody rb = playerCar.GetComponent<Rigidbody>();

        playerCar.transform.position = lastCheckpointTransform.position;
        playerCar.transform.rotation = lastCheckpointTransform.rotation;

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        ShowSmallStatus("Respawned", smallStatusDisplayDuration);
    }

    private void ShowStatus(string message)
    {
        ShowSmallStatus(message);
    }

    private void ShowSmallStatus(string message, float duration = 0f)
    {
        currentStatusMessage = message;
        currentSmallStatusMessage = message;

        if (smallStatusText != null)
            smallStatusText.text = message;

        if (statusText != null && smallStatusText == null && bigStatusText == null)
            statusText.text = message;

        smallStatusTimer = duration;
    }

    private void ShowBigStatus(string message, float duration = 0f)
    {
        currentStatusMessage = message;
        currentBigStatusMessage = message;

        if (bigStatusText != null)
            bigStatusText.text = message;

        if (statusText != null && smallStatusText == null && bigStatusText == null)
            statusText.text = message;

        bigStatusTimer = duration;
    }

    private void ClearAllStatus()
    {
        currentStatusMessage = "";
        currentSmallStatusMessage = "";
        currentBigStatusMessage = "";

        smallStatusTimer = 0f;
        bigStatusTimer = 0f;

        if (statusText != null)
            statusText.text = "";

        if (smallStatusText != null)
            smallStatusText.text = "";

        if (bigStatusText != null)
            bigStatusText.text = "";
    }

    private string GetRaceStartFailureReason(RaceDefinition race)
    {
        if (race == null)
            return "Race setup missing";

        if (PlayerGarage.Instance == null)
            return "Garage missing";

        switch (race.vehicleUsageRule)
        {
            case VehicleUsageRule.UseCurrentCar:
                return "Current " + race.requiredVehicleType + " car required";

            case VehicleUsageRule.RequireCompatibleOwnedCar:
                return "No owned " + race.requiredVehicleType + " car available";
        }

        return "Cannot start race";
    }

    private void SetRacingHUDVisible(bool visible)
    {
        if (racingHUDRoot != null)
            racingHUDRoot.SetActive(visible);
    }

    private bool CanUseVehicleForRace(RaceDefinition race)
    {
        if (PlayerGarage.Instance == null)
            return true;

        switch (race.vehicleUsageRule)
        {
            case VehicleUsageRule.UseCurrentCar:
                return PlayerGarage.Instance.CurrentCarMatches(race.requiredVehicleType);

            case VehicleUsageRule.RequireCompatibleOwnedCar:
                return PlayerGarage.Instance.HasCompatibleOwnedCar(race.requiredVehicleType);
        }

        return false;
    }

    private void PrepareVehicleForRace(RaceDefinition race)
    {
        if (PlayerGarage.Instance == null)
            return;

        switch (race.vehicleUsageRule)
        {
            case VehicleUsageRule.UseCurrentCar:
                break;

            case VehicleUsageRule.RequireCompatibleOwnedCar:
                CarProfile compatibleCar = PlayerGarage.Instance.GetFirstCompatibleOwnedCar(race.requiredVehicleType);

                if (compatibleCar != null && PlayerGarage.Instance.currentCar != compatibleCar)
                    PlayerGarage.Instance.SwitchToCar(compatibleCar);
                break;
        }
    }

    private string FormatPosition(int pos)
    {
        if (pos <= 0)
            return "--";

        if (pos % 100 >= 11 && pos % 100 <= 13)
            return pos + "th";

        switch (pos % 10)
        {
            case 1:
                return pos + "st";

            case 2:
                return pos + "nd";

            case 3:
                return pos + "rd";

            default:
                return pos + "th";
        }
    }

    private void SetMissionMarkersActive(bool active)
    {
        MissionMarkerInteract.SetAllMarkersActive(active);
    }

    private bool UsesFinishTrigger()
    {
        if (currentRace == null)
            return false;

        return currentRace.useFinishTrigger && currentRace.finishPoint != null;
    }

    private void Log(string message)
    {
        if (logDebugMessages)
            Debug.Log("[RaceManager] " + message);
    }

    public RaceDefinition GetCurrentRace() { return currentRace; }
    public RaceResult GetLastResult() { return lastResult; }
    public int GetCurrentLap() { return currentLap; }
    public int GetNextCheckpointIndex() { return nextCheckpointIndex; }
    public int GetTotalCheckpointCount() { return totalCheckpointCount; }
    public int GetLastCheckpointIndex() { return lastCheckpointIndex; }

    public bool IsRaceActive() { return raceActive; }
    public bool IsRaceFinished() { return raceFinished; }
    public bool IsCountdownActive() { return countdownActive; }
    public bool IsTimerRunning() { return timerRunning; }

    public float GetCountdownTime() { return countdownTimer; }
    public float GetCurrentRaceTime() { return currentRaceTime; }
    public float GetFinalRaceTime() { return finalRaceTime; }
    public float GetCurrentLapTime() { return currentLapTime; }
    public float GetBestLapTime() { return bestLapTime; }

    public string GetStatusText() { return currentStatusMessage; }
    public string GetSmallStatusText() { return currentSmallStatusMessage; }
    public string GetBigStatusText() { return currentBigStatusMessage; }
}

[Serializable]
public class RaceResult
{
    public string raceName;
    public RaceType raceType;
    public float totalTime;
    public float bestLapTime;
    public int lapsCompleted;
}