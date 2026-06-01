using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ResultsScreenUI : MonoBehaviour
{
    [Header("Panel")]
    public GameObject resultsPanel;

    [Header("Text")]
    public TMP_Text titleText;
    public TMP_Text raceNameText;
    public TMP_Text positionText;
    public TMP_Text totalTimeText;
    public TMP_Text bestLapText;
    public TMP_Text countdownText;

    [Header("Buttons")]
    public Button retryButton;
    public Button exitButton;

    [Header("References")]
    public RaceManager raceManager;

    [Header("Retry AI Setup")]
    [Tooltip("Assign VehicleDatabaseRaceAISpawner here. This makes Retry rebuild the correct AI list for the current race before RaceManager starts the grid.")]
    public VehicleDatabaseRaceAISpawner raceAISpawner;

    [Tooltip("If true, retry will prepare vehicle-database AI before restarting the race.")]
    public bool prepareAIBeforeRetry = true;

    [Header("Return")]
    public bool autoReturnToReturnScene = true;
    public float returnCountdownSeconds = 20f;

    [Tooltip("Used only if RaceLaunchData has no return scene.")]
    public string fallbackReturnSceneName = "MainScene - Tokyo";

    [Tooltip("If returning to this scene, the main menu carousel will select Race Modes.")]
    public string mainMenuSceneName = "MainMenuScene";

    [Header("Loading")]
    public string returnLoadingMessage = "Returning...";

    private Coroutine returnCountdownRoutine;
    private RaceDefinition lastRace;

    private void Awake()
    {
        if (resultsPanel != null)
            resultsPanel.SetActive(false);
    }

    private void Start()
    {
        if (retryButton != null)
        {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(OnRetryClicked);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(ReturnToTargetSceneNow);
        }

        if (countdownText != null)
            countdownText.text = "";

        if (raceManager == null)
            raceManager = RaceManager.Instance;

        if (raceAISpawner == null)
            raceAISpawner = FindFirstObjectByType<VehicleDatabaseRaceAISpawner>();
    }

    public void ShowResults(RaceResult result, string formattedPosition = "--")
    {
        if (result == null)
            return;

        if (raceManager == null)
            raceManager = RaceManager.Instance;

        if (raceManager != null)
            lastRace = raceManager.GetCurrentRace();

        if (resultsPanel != null)
            resultsPanel.SetActive(true);

        if (titleText != null)
            titleText.text = "Race Complete";

        if (raceNameText != null)
            raceNameText.text = result.raceName;

        if (positionText != null)
            positionText.text = formattedPosition;

        if (totalTimeText != null)
            totalTimeText.text = FormatTime(result.totalTime);

        if (bestLapText != null)
        {
            if (result.bestLapTime > 0f)
                bestLapText.text = FormatTime(result.bestLapTime);
            else
                bestLapText.text = "--";
        }

        if (autoReturnToReturnScene)
            StartReturnCountdown();
    }

    public void HideResults()
    {
        if (returnCountdownRoutine != null)
        {
            StopCoroutine(returnCountdownRoutine);
            returnCountdownRoutine = null;
        }

        if (resultsPanel != null)
            resultsPanel.SetActive(false);

        if (countdownText != null)
            countdownText.text = "";
    }

    private void StartReturnCountdown()
    {
        if (returnCountdownRoutine != null)
            StopCoroutine(returnCountdownRoutine);

        returnCountdownRoutine = StartCoroutine(ReturnCountdownRoutine());
    }

    private IEnumerator ReturnCountdownRoutine()
    {
        float timer = returnCountdownSeconds;

        while (timer > 0f)
        {
            if (countdownText != null)
                countdownText.text = Mathf.CeilToInt(timer) + "s";

            timer -= Time.deltaTime;
            yield return null;
        }

        ReturnToTargetSceneNow();
    }

    public void ReturnToTargetSceneNow()
    {
        if (returnCountdownRoutine != null)
        {
            StopCoroutine(returnCountdownRoutine);
            returnCountdownRoutine = null;
        }

        if (resultsPanel != null)
            resultsPanel.SetActive(false);

        string sceneToLoad = fallbackReturnSceneName;

        if (RaceLaunchData.HasRaceLaunchData && !string.IsNullOrWhiteSpace(RaceLaunchData.ReturnSceneName))
        {
            sceneToLoad = RaceLaunchData.ReturnSceneName;
        }

        if (string.IsNullOrWhiteSpace(sceneToLoad))
        {
            Debug.LogWarning("[ResultsScreenUI] No return scene name assigned.");
            return;
        }

        bool returningToMainMenu = sceneToLoad == mainMenuSceneName;

        if (returningToMainMenu)
        {
            MainMenuReturnState.RequestItem(MainMenuRequestedItem.RaceModes);

            // Important:
            // Main menu race mode returns do not go through FreeroamReturnManager,
            // so RaceLaunchData must be cleared here.
            RaceLaunchData.Clear();
            GarageSceneReturnData.HasFreeroamReturnPoint = false;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(RaceLaunchData.ReturnMarkerID) &&
                RaceLaunchData.HasRaceLaunchData &&
                !string.IsNullOrWhiteSpace(RaceLaunchData.RaceID))
            {
                RaceLaunchData.SetReturnMarkerID(RaceLaunchData.RaceID + "_marker");
            }

            RaceLaunchData.MarkReturningFromRace();
        }

        Debug.Log(
            "[ResultsScreenUI] Loading return scene: " +
            sceneToLoad +
            " | Returning To Main Menu: " +
            returningToMainMenu
        );

        if (SceneLoaderWithLoadingScreen.Instance != null)
        {
            SceneLoaderWithLoadingScreen.Instance.LoadScene(sceneToLoad, returnLoadingMessage);
        }
        else
        {
            SceneManager.LoadScene(sceneToLoad);
        }
    }

    private void OnRetryClicked()
    {
        if (returnCountdownRoutine != null)
        {
            StopCoroutine(returnCountdownRoutine);
            returnCountdownRoutine = null;
        }

        if (resultsPanel != null)
            resultsPanel.SetActive(false);

        if (LoadingScreenController.Instance != null)
            LoadingScreenController.Instance.ShowImmediate("Restarting Race...");

        if (raceManager == null)
            raceManager = RaceManager.Instance;

        if (raceManager == null)
        {
            Debug.LogWarning("[ResultsScreenUI] Cannot retry race because RaceManager is missing.");
            return;
        }

        RaceDefinition raceToRetry = lastRace != null ? lastRace : raceManager.GetCurrentRace();

        if (raceToRetry == null)
        {
            Debug.LogWarning("[ResultsScreenUI] Cannot retry race because RaceDefinition is missing.");
            return;
        }

        raceManager.CancelCurrentRace();

        PrepareRetryAI(raceToRetry);

        raceManager.TryStartRace(raceToRetry);
    }

    private void PrepareRetryAI(RaceDefinition raceToRetry)
    {
        if (!prepareAIBeforeRetry)
            return;

        if (raceToRetry == null)
            return;

        if (raceAISpawner == null)
            raceAISpawner = FindFirstObjectByType<VehicleDatabaseRaceAISpawner>();

        if (raceAISpawner == null)
        {
            Debug.LogWarning("[ResultsScreenUI] Retry AI not prepared because VehicleDatabaseRaceAISpawner was not found. GridStartManager may fall back to default AI prefab.");
            return;
        }

        raceAISpawner.PrepareAIForRace(raceToRetry);

        Debug.Log("[ResultsScreenUI] Prepared retry AI for race: " + raceToRetry.raceDisplayName);
    }

    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        float seconds = time % 60f;

        return string.Format("{0:00}:{1:00.00}", minutes, seconds);
    }
}