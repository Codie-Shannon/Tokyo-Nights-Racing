using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum PauseMapMode
{
    Freeroam,
    Race
}

public enum PauseMapMarkerType
{
    Garage,
    Race,
    Checkpoint,
    Finish,
    Custom
}

public enum StandardSceneLoaderCallMode
{
    SceneNameOnly,
    RequestObject
}

public class PauseMapMenuBuilderAndController : MonoBehaviour
{
    [System.Serializable]
    public class PauseMapMarker
    {
        [Header("Marker Info")]
        [Tooltip("Fallback label for Garage/Custom markers. Race markers use Linked Mission Marker name instead.")]
        public string editorLabel = "Marker";

        public PauseMapMarkerType markerType = PauseMapMarkerType.Custom;

        [Tooltip("Position on the fake pause map. X/Y from 0 to 1. 0,0 = bottom-left. 1,1 = top-right.")]
        public Vector2 mapPosition = new Vector2(0.5f, 0.5f);

        [Header("Mission Marker Sync")]
        [Tooltip("Required for Race markers. This is the source of truth for race name, race scene, vehicle rules, and race launch.")]
        public MissionMarkerInteract linkedMissionMarker;

        [Tooltip("If ON and Waypoint Target is empty, the waypoint target becomes the linked mission marker transform.")]
        public bool useLinkedMissionMarkerAsWaypointTarget = true;

        [Tooltip("If ON and Can Enter is true, direct ENTER from the pause map calls linkedMissionMarker.StartAssignedRace().")]
        public bool useLinkedMissionMarkerForEnter = true;

        [Header("Marker Sprite")]
        public Sprite markerSprite;
        public Vector2 markerSize = new Vector2(72f, 72f);
        public Color markerTint = Color.white;
        public bool preserveSpriteAspect = true;
        public bool showFallbackLetterIfNoSprite = true;

        [Header("World Target")]
        [Tooltip("Optional override. If empty and this is a linked race marker, the linked mission marker transform is used.")]
        public Transform waypointTarget;

        [Header("Waypoint")]
        public bool canSetWaypoint = true;

        [Header("Enter Action")]
        public bool canEnter = true;
        public string enterButtonText = "ENTER";

        [Tooltip("Used for Garage and Custom markers. Linked race markers use linkedMissionMarker.raceSceneName instead.")]
        public string targetSceneName = "";

        [Header("Garage Return")]
        public Transform garageReturnSpawnPoint;

        [Header("Custom Enter Event")]
        public bool useCustomEnterEvent = false;
        public UnityEvent customEnterEvent;

        [Header("Visibility")]
        public bool showInFreeroam = true;
        public bool showInRace = true;
    }

    [Header("Mode")]
    [SerializeField] private PauseMapMode mode = PauseMapMode.Freeroam;

    [Header("Input")]
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
    [SerializeField] private bool logInputDebug = true;

    [Header("Player")]
    [SerializeField] private Transform playerCar;
    [SerializeField] private string playerTag = "Player";

    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";

    [Header("Vehicle Filter Data")]
    [Tooltip("Needed for linked race marker filtering. Assign your VehicleDatabase here.")]
    [SerializeField] private VehicleDatabase vehicleDatabase;

    [Tooltip("Fallback roster if VehicleDatabase is not assigned.")]
    [SerializeField] private VehicleData[] vehicleRoster;

    [Tooltip("If no saved/equipped vehicle is found, use the first vehicle in the database/roster.")]
    [SerializeField] private bool useFirstVehicleIfNoSave = true;

    [Tooltip("If ON, linked race markers are hidden when the current equipped vehicle cannot enter their linked mission marker.")]
    [SerializeField] private bool hideInvalidRaceMarkers = true;

    [Tooltip("If ON, marker buttons are hidden/shown again every time pause opens.")]
    [SerializeField] private bool refreshMarkerVisibilityOnPause = true;

    [Tooltip("If ON, logs why race markers are shown/hidden.")]
    [SerializeField] private bool logRaceMarkerFiltering = false;

    [Header("HUD Visibility")]
    [Tooltip("Assign SpeedText, SpeedometerRoot, RaceHudRoot, or the HUD object you want hidden while paused.")]
    [SerializeField] private GameObject speedometerRoot;

    [SerializeField] private bool hideSpeedometerWhenPaused = true;
    [SerializeField] private bool restoreSpeedometerWhenUnpaused = true;

    [Header("Markers")]
    [SerializeField] private List<PauseMapMarker> markers = new List<PauseMapMarker>();

    [Header("Map Visuals")]
    [SerializeField] private Sprite mapBackgroundSprite;
    [SerializeField] private string freeroamTitle = "FREEROAM MAP";
    [SerializeField] private string raceTitle = "RACE TRACK";
    [SerializeField] private bool drawRaceLines = true;
    [SerializeField] private float raceLineThickness = 5f;

    [Header("Waypoint HUD")]
    [SerializeField] private bool enableWaypointHud = true;
    [SerializeField] private float waypointArriveDistance = 8f;
    [SerializeField] private bool hideWaypointWhenArrived = true;

    [Header("Waypoint HUD Inspector Overrides")]
    [SerializeField] private GameObject waypointHudRootOverride;
    [SerializeField] private RectTransform waypointArrowOverride;
    [SerializeField] private TMP_Text waypointDistanceTextOverride;

    [Header("Standard Loading System")]
    [SerializeField] private bool useStandardLoadingSystem = true;
    [SerializeField] private bool fallbackToDirectSceneLoad = true;

    [Tooltip("Drag your existing SceneLoader / LoadingScreenController object here.")]
    [SerializeField] private GameObject sceneLoaderObject;

    [Tooltip("Method name on your loading system. Example: LoadScene, LoadSceneWithLoadingScreen, LoadSceneByName.")]
    [SerializeField] private string sceneLoaderMethodName = "LoadScene";

    [SerializeField] private StandardSceneLoaderCallMode sceneLoaderCallMode = StandardSceneLoaderCallMode.SceneNameOnly;

    [Header("Loading Messages")]
    [SerializeField] private string loadingMainMenuMessage = "Returning to Main Menu...";
    [SerializeField] private string loadingGarageMessage = "Loading Garage...";
    [SerializeField] private string loadingRaceMessage = "Loading Race...";
    [SerializeField] private string loadingDefaultMessage = "Loading...";

    [Header("Cursor")]
    [SerializeField] private bool unlockCursorWhenPaused = true;
    [SerializeField] private bool lockCursorWhenUnpaused = false;

    [Header("Generated UI")]
    [SerializeField] private string pauseCanvasName = "PauseMenuCanvas";
    [SerializeField] private string generatedRootName = "GeneratedPauseMapUI";
    [SerializeField] private bool deleteExistingGeneratedUI = true;

    private Canvas canvas;

    private GameObject generatedRoot;
    private GameObject pauseRoot;
    private GameObject mapPanelObject;
    private RectTransform mapPanelRect;
    private TMP_Text titleText;

    private GameObject bottomBarRoot;

    private GameObject popupRoot;
    private RectTransform popupRect;
    private TMP_Text popupTitleText;
    private Button popupWaypointButton;
    private TMP_Text popupWaypointButtonText;
    private Button popupEnterButton;
    private TMP_Text popupEnterButtonText;

    private GameObject waypointHudRoot;
    private RectTransform waypointArrowRect;
    private TMP_Text waypointDistanceText;

    private PauseMapMarker selectedMarker;
    private Transform activeWaypointTarget;

    private bool isPaused;
    private bool isLoading;
    private bool hasWaypoint;
    private bool speedometerWasActiveBeforePause;

    private readonly Vector2 mapSize = new Vector2(1120f, 560f);

    private readonly Color cyan = new Color(0f, 0.9f, 1f, 1f);
    private readonly Color magenta = new Color(1f, 0.02f, 0.72f, 1f);
    private readonly Color darkPanel = new Color(0.015f, 0.025f, 0.04f, 0.94f);
    private readonly Color darkerPanel = new Color(0.005f, 0.008f, 0.014f, 0.97f);

    private void Awake()
    {
        EnsureEventSystem();
        EnsureCanvas();

        CacheGeneratedReferences();

        if (generatedRoot == null || pauseRoot == null)
        {
            if (logInputDebug)
                Debug.Log("PauseMapMenu: UI references missing. Building pause map UI automatically.");

            BuildPauseMapUI();
            CacheGeneratedReferences();
        }

        HidePauseImmediate();
        HidePopup();
        HideWaypointHud();

        RefreshGeneratedMarkerVisibility();
        RebindGeneratedButtonEvents();

        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    private void Update()
    {
        if (isLoading)
            return;

        if (Input.GetKeyDown(pauseKey))
        {
            if (logInputDebug)
                Debug.Log("PauseMapMenu: Escape/pause key detected.");

            if (isPaused)
                Resume();
            else
                Pause();
        }

        UpdateWaypointHud();
    }

    [ContextMenu("Build / Rebuild Pause Map UI")]
    public void BuildPauseMapUI()
    {
        EnsureEventSystem();
        EnsureCanvas();

        if (deleteExistingGeneratedUI)
            DeleteExistingGeneratedUI();

        GameObject root = CreateUIObject(generatedRootName, canvas.transform);
        generatedRoot = root;

        RectTransform rootRT = root.GetComponent<RectTransform>();
        StretchFull(rootRT);
        root.SetActive(true);

        BuildPauseRoot(root.transform);
        BuildMapPanel();
        BuildPopup();
        BuildBottomBar();
        BuildWaypointHud(root.transform);

        CacheGeneratedReferences();

        HidePauseImmediate();
        HidePopup();
        HideWaypointHud();

        RefreshGeneratedMarkerVisibility();
        RebindGeneratedButtonEvents();

#if UNITY_EDITOR
        EditorUtility.SetDirty(gameObject);
        EditorUtility.SetDirty(canvas.gameObject);
#endif

        Debug.Log("PauseMapMenu: UI built/rebuilt successfully.");
    }

    [ContextMenu("Refresh Generated Marker Visibility")]
    public void RefreshGeneratedMarkerVisibility()
    {
        CacheGeneratedReferences();

        if (generatedRoot == null)
            return;

        for (int i = 0; i < markers.Count; i++)
        {
            PauseMapMarker marker = markers[i];

            if (marker == null)
                continue;

            string markerButtonName = GetMarkerButtonName(marker);
            Button markerButton = FindButtonByName(markerButtonName);

            if (markerButton == null)
                continue;

            bool shouldShow = ShouldShowMarker(marker);
            markerButton.gameObject.SetActive(shouldShow);

            if (!shouldShow && selectedMarker == marker)
                HidePopup();
        }
    }

    [ContextMenu("Rebind Generated Button Events")]
    private void RebindGeneratedButtonEvents()
    {
        CacheGeneratedReferences();

        if (generatedRoot == null || pauseRoot == null)
        {
            Debug.LogWarning("PauseMapMenu: Cannot rebind buttons because generated UI is missing.");
            return;
        }

        RebindButtonByName("ResumeButton", Resume);
        RebindButtonByName("MainMenuButton", ReturnToMainMenu);
        RebindButtonByName("QuitButton", QuitGame);

        if (popupWaypointButton != null)
        {
            popupWaypointButton.onClick.RemoveAllListeners();
            popupWaypointButton.onClick.AddListener(() =>
            {
                if (selectedMarker != null)
                    SetWaypoint(selectedMarker);
            });
        }

        if (popupEnterButton != null)
        {
            popupEnterButton.onClick.RemoveAllListeners();
            popupEnterButton.onClick.AddListener(() =>
            {
                if (selectedMarker != null)
                    EnterMarker(selectedMarker);
            });
        }

        for (int i = 0; i < markers.Count; i++)
        {
            PauseMapMarker marker = markers[i];

            if (marker == null)
                continue;

            if (!ShouldGenerateMarkerButton(marker))
                continue;

            string markerButtonName = GetMarkerButtonName(marker);
            Button markerButton = FindButtonByName(markerButtonName);

            if (markerButton == null)
            {
                Debug.LogWarning("PauseMapMenu: Could not find marker button named: " + markerButtonName);
                continue;
            }

            RectTransform markerRect = markerButton.GetComponent<RectTransform>();
            PauseMapMarker capturedMarker = marker;

            markerButton.onClick.RemoveAllListeners();
            markerButton.onClick.AddListener(() =>
            {
                if (!ShouldShowMarker(capturedMarker))
                {
                    HidePopup();
                    return;
                }

                ShowPopup(capturedMarker, markerRect);
            });
        }

        Debug.Log("PauseMapMenu: Generated button events rebound.");
    }

    public void Pause()
    {
        if (isLoading)
            return;

        CacheGeneratedReferences();

        if (pauseRoot == null)
        {
            Debug.LogWarning("PauseMapMenu: PauseRoot missing. Rebuilding UI.");
            BuildPauseMapUI();
            CacheGeneratedReferences();
        }

        if (refreshMarkerVisibilityOnPause)
            RefreshGeneratedMarkerVisibility();

        RebindGeneratedButtonEvents();

        if (refreshMarkerVisibilityOnPause)
            RefreshGeneratedMarkerVisibility();

        isPaused = true;
        Time.timeScale = 0f;

        HideSpeedometerForPause();

        if (generatedRoot != null)
            generatedRoot.SetActive(true);

        if (pauseRoot != null)
            pauseRoot.SetActive(true);

        if (bottomBarRoot != null)
            bottomBarRoot.SetActive(true);

        if (titleText != null)
            titleText.text = mode == PauseMapMode.Race ? raceTitle : freeroamTitle;

        PauseMapPlayerMarker playerMarker = GetComponent<PauseMapPlayerMarker>();

        if (playerMarker != null)
            playerMarker.UpdatePlayerMarker();

        if (unlockCursorWhenPaused)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        if (logInputDebug)
            Debug.Log("PauseMapMenu: Pause menu opened.");
    }

    public void Resume()
    {
        if (isLoading)
            return;

        isPaused = false;
        Time.timeScale = 1f;

        HidePopup();

        if (pauseRoot != null)
            pauseRoot.SetActive(false);

        if (bottomBarRoot != null)
            bottomBarRoot.SetActive(false);

        RestoreSpeedometerAfterPause();

        if (lockCursorWhenUnpaused)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        if (logInputDebug)
            Debug.Log("PauseMapMenu: Pause menu closed.");
    }

    public void ReturnToMainMenu()
    {
        if (isLoading)
            return;

        StartCoroutine(LoadSceneRoutine(mainMenuSceneName, loadingMainMenuMessage));
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        RestoreSpeedometerAfterPause();

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void HideSpeedometerForPause()
    {
        if (!hideSpeedometerWhenPaused)
            return;

        if (speedometerRoot == null)
            return;

        speedometerWasActiveBeforePause = speedometerRoot.activeSelf;
        speedometerRoot.SetActive(false);
    }

    private void RestoreSpeedometerAfterPause()
    {
        if (!restoreSpeedometerWhenUnpaused)
            return;

        if (speedometerRoot == null)
            return;

        speedometerRoot.SetActive(speedometerWasActiveBeforePause);
    }

    private void SetWaypoint(PauseMapMarker marker)
    {
        if (marker == null)
            return;

        if (!ShouldShowMarker(marker))
        {
            HidePopup();
            return;
        }

        Transform target = GetMarkerWaypointTarget(marker);

        if (target == null)
        {
            Debug.LogWarning("PauseMapMenu: No waypoint target assigned for " + GetMarkerDisplayName(marker));
            return;
        }

        activeWaypointTarget = target;
        hasWaypoint = true;

        GameObject hudRoot = waypointHudRootOverride != null ? waypointHudRootOverride : waypointHudRoot;

        if (enableWaypointHud && hudRoot != null)
            hudRoot.SetActive(true);

        Resume();
    }

    private void EnterMarker(PauseMapMarker marker)
    {
        if (marker == null || isLoading)
            return;

        if (!ShouldShowMarker(marker))
        {
            HidePopup();
            return;
        }

        Debug.Log("PauseMapMenu: EnterMarker clicked: " + GetMarkerDisplayName(marker) +
                  " | Type: " + marker.markerType +
                  " | Target Scene: " + GetMarkerTargetSceneName(marker) +
                  " | Custom Event: " + marker.useCustomEnterEvent);

        if (!marker.canEnter)
            return;

        if (marker.useCustomEnterEvent)
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            Resume();
            marker.customEnterEvent.Invoke();
            return;
        }

        if (marker.markerType == PauseMapMarkerType.Race)
        {
            if (marker.linkedMissionMarker == null)
            {
                Debug.LogWarning("PauseMapMenu: Race marker '" + GetMarkerDisplayName(marker) + "' has no linked MissionMarkerInteract.");
                return;
            }

            if (marker.useLinkedMissionMarkerForEnter)
            {
                Time.timeScale = 1f;
                AudioListener.pause = false;

                HidePopup();

                if (pauseRoot != null)
                    pauseRoot.SetActive(false);

                if (bottomBarRoot != null)
                    bottomBarRoot.SetActive(false);

                RestoreSpeedometerAfterPause();

                isPaused = false;

                marker.linkedMissionMarker.StartAssignedRace();
                return;
            }
        }

        string sceneName = GetMarkerTargetSceneName(marker);

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("PauseMapMenu: No target scene name assigned for " + GetMarkerDisplayName(marker));
            return;
        }

        if (marker.markerType == PauseMapMarkerType.Garage)
        {
            Transform returnPoint = marker.garageReturnSpawnPoint != null
                ? marker.garageReturnSpawnPoint
                : GetMarkerWaypointTarget(marker);

            if (returnPoint != null)
            {
                GarageSceneReturnData.SetReturnToFreeroam(returnPoint.position, returnPoint.rotation);
            }
            else
            {
                GarageSceneReturnData.ReturnTarget = GarageReturnTarget.Freeroam;
            }

            StartCoroutine(LoadSceneRoutine(sceneName, loadingGarageMessage));
            return;
        }

        if (marker.markerType == PauseMapMarkerType.Race)
        {
            StartCoroutine(LoadSceneRoutine(sceneName, loadingRaceMessage));
            return;
        }

        StartCoroutine(LoadSceneRoutine(sceneName, loadingDefaultMessage));
    }

    private IEnumerator LoadSceneRoutine(string sceneName, string message)
    {
        isLoading = true;
        isPaused = false;

        Time.timeScale = 1f;
        AudioListener.pause = false;

        HidePopup();
        RestoreSpeedometerAfterPause();

        if (pauseRoot != null)
            pauseRoot.SetActive(false);

        if (bottomBarRoot != null)
            bottomBarRoot.SetActive(false);

        if (useStandardLoadingSystem && sceneLoaderObject != null)
        {
            if (sceneLoaderCallMode == StandardSceneLoaderCallMode.SceneNameOnly)
            {
                sceneLoaderObject.SendMessage(
                    sceneLoaderMethodName,
                    sceneName,
                    SendMessageOptions.DontRequireReceiver
                );
            }
            else
            {
                StandardSceneLoadRequest request = new StandardSceneLoadRequest
                {
                    sceneName = sceneName,
                    loadingMessage = message
                };

                sceneLoaderObject.SendMessage(
                    sceneLoaderMethodName,
                    request,
                    SendMessageOptions.DontRequireReceiver
                );
            }

            yield break;
        }

        if (fallbackToDirectSceneLoad)
        {
            SceneManager.LoadScene(sceneName);
            yield break;
        }

        Debug.LogWarning("PauseMapMenu: No standard scene loader assigned and fallback loading is disabled.");
        isLoading = false;
    }

    private void UpdateWaypointHud()
    {
        if (!hasWaypoint || activeWaypointTarget == null)
            return;

        FindPlayerIfNeeded();

        if (playerCar == null)
            return;

        float distance = Vector3.Distance(playerCar.position, activeWaypointTarget.position);

        TMP_Text activeDistanceText = waypointDistanceTextOverride != null
            ? waypointDistanceTextOverride
            : waypointDistanceText;

        RectTransform activeArrowRect = waypointArrowOverride != null
            ? waypointArrowOverride
            : waypointArrowRect;

        if (activeDistanceText != null)
            activeDistanceText.text = Mathf.RoundToInt(distance) + " m";

        if (activeArrowRect != null && Camera.main != null)
        {
            Vector3 viewportPoint = Camera.main.WorldToViewportPoint(activeWaypointTarget.position);

            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector2 targetScreenPos = Camera.main.WorldToScreenPoint(activeWaypointTarget.position);

            if (viewportPoint.z < 0f)
                targetScreenPos = screenCenter - (targetScreenPos - screenCenter);

            Vector2 direction = (targetScreenPos - screenCenter).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            activeArrowRect.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
        }

        if (hideWaypointWhenArrived && distance <= waypointArriveDistance)
        {
            hasWaypoint = false;
            activeWaypointTarget = null;
            HideWaypointHud();
        }
    }

    private void FindPlayerIfNeeded()
    {
        if (playerCar != null)
            return;

        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);

        if (playerObject != null)
            playerCar = playerObject.transform;
    }

    private void BuildPauseRoot(Transform rootParent)
    {
        pauseRoot = CreateUIObject("PauseRoot", rootParent);
        RectTransform rt = pauseRoot.GetComponent<RectTransform>();
        StretchFull(rt);

        Image dim = pauseRoot.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.48f);
        dim.raycastTarget = false;
    }

    private void BuildMapPanel()
    {
        GameObject headerObject = CreateUIObject("MapHeader", pauseRoot.transform);
        RectTransform headerRT = headerObject.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0.5f, 1f);
        headerRT.anchorMax = new Vector2(0.5f, 1f);
        headerRT.pivot = new Vector2(0.5f, 1f);
        headerRT.anchoredPosition = new Vector2(0f, -45f);
        headerRT.sizeDelta = new Vector2(1120f, 90f);

        Image headerImage = headerObject.AddComponent<Image>();
        headerImage.color = darkerPanel;
        headerImage.raycastTarget = false;

        Outline headerOutline = headerObject.AddComponent<Outline>();
        headerOutline.effectColor = cyan;
        headerOutline.effectDistance = new Vector2(2f, -2f);

        titleText = CreateText(
            "MapTitleText",
            headerObject.transform,
            mode == PauseMapMode.Race ? raceTitle : freeroamTitle,
            38,
            cyan
        );

        RectTransform titleRT = titleText.GetComponent<RectTransform>();
        StretchFull(titleRT);
        titleRT.offsetMin = new Vector2(28f, 0f);
        titleRT.offsetMax = new Vector2(-28f, 0f);
        titleText.alignment = TextAlignmentOptions.Left;
        titleText.raycastTarget = false;

        mapPanelObject = CreateUIObject("MapPanel", pauseRoot.transform);
        mapPanelRect = mapPanelObject.GetComponent<RectTransform>();
        mapPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
        mapPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
        mapPanelRect.pivot = new Vector2(0.5f, 0.5f);
        mapPanelRect.anchoredPosition = new Vector2(0f, 30f);
        mapPanelRect.sizeDelta = mapSize;

        Image panelImage = mapPanelObject.AddComponent<Image>();
        panelImage.color = new Color(0.005f, 0.008f, 0.014f, 0.96f);
        panelImage.raycastTarget = false;

        Outline outline = mapPanelObject.AddComponent<Outline>();
        outline.effectColor = magenta;
        outline.effectDistance = new Vector2(2f, -2f);

        GameObject mapImageObject = CreateUIObject("MapBackgroundImage", mapPanelRect);
        RectTransform mapImageRT = mapImageObject.GetComponent<RectTransform>();
        StretchFull(mapImageRT);

        Image mapBackgroundImage = mapImageObject.AddComponent<Image>();
        mapBackgroundImage.raycastTarget = false;

        if (mapBackgroundSprite != null)
        {
            mapBackgroundImage.sprite = mapBackgroundSprite;
            mapBackgroundImage.color = Color.white;
            mapBackgroundImage.type = Image.Type.Simple;
            mapBackgroundImage.preserveAspect = false;
        }
        else
        {
            mapBackgroundImage.color = darkPanel;
        }

        BuildDecorativeMapGrid();

        if (mode == PauseMapMode.Race && drawRaceLines)
            DrawRaceLines();

        BuildMarkerButtons();
    }

    private void BuildDecorativeMapGrid()
    {
        for (int i = 1; i < 4; i++)
        {
            float x = Mathf.Lerp(-mapSize.x * 0.5f, mapSize.x * 0.5f, i / 4f);
            CreateMapLine("GridVertical_" + i, new Vector2(x, -mapSize.y * 0.5f), new Vector2(x, mapSize.y * 0.5f), new Color(0f, 0.9f, 1f, 0.12f));
        }

        for (int i = 1; i < 3; i++)
        {
            float y = Mathf.Lerp(-mapSize.y * 0.5f, mapSize.y * 0.5f, i / 3f);
            CreateMapLine("GridHorizontal_" + i, new Vector2(-mapSize.x * 0.5f, y), new Vector2(mapSize.x * 0.5f, y), new Color(1f, 0.02f, 0.72f, 0.12f));
        }
    }

    private void DrawRaceLines()
    {
        List<PauseMapMarker> raceMarkers = new List<PauseMapMarker>();

        foreach (PauseMapMarker marker in markers)
        {
            if (marker == null)
                continue;

            if (!ShouldGenerateMarkerButton(marker))
                continue;

            if (marker.markerType == PauseMapMarkerType.Race ||
                marker.markerType == PauseMapMarkerType.Checkpoint ||
                marker.markerType == PauseMapMarkerType.Finish)
            {
                raceMarkers.Add(marker);
            }
        }

        for (int i = 0; i < raceMarkers.Count - 1; i++)
        {
            Vector2 a = MapPositionToAnchored(raceMarkers[i].mapPosition);
            Vector2 b = MapPositionToAnchored(raceMarkers[i + 1].mapPosition);
            CreateMapLine("RaceTrackLine_" + i, a, b, cyan);
        }
    }

    private void BuildMarkerButtons()
    {
        foreach (PauseMapMarker marker in markers)
        {
            if (marker == null)
                continue;

            if (!ShouldGenerateMarkerButton(marker))
                continue;

            PauseMapMarker capturedMarker = marker;

            GameObject buttonObject = CreateUIObject(GetMarkerButtonName(capturedMarker), mapPanelRect);
            RectTransform rt = buttonObject.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = MapPositionToAnchored(capturedMarker.mapPosition);
            rt.sizeDelta = capturedMarker.markerSize;

            Image image = buttonObject.AddComponent<Image>();
            image.raycastTarget = true;

            if (capturedMarker.markerSprite != null)
            {
                image.sprite = capturedMarker.markerSprite;
                image.color = capturedMarker.markerTint;
                image.type = Image.Type.Simple;
                image.preserveAspect = capturedMarker.preserveSpriteAspect;
            }
            else
            {
                image.color = GetMarkerColor(capturedMarker.markerType);
            }

            Button button = buttonObject.AddComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                if (!ShouldShowMarker(capturedMarker))
                {
                    HidePopup();
                    return;
                }

                ShowPopup(capturedMarker, rt);
            });

            if (capturedMarker.markerSprite == null && capturedMarker.showFallbackLetterIfNoSprite)
            {
                TMP_Text iconText = CreateText(
                    "IconText",
                    buttonObject.transform,
                    GetMarkerIcon(capturedMarker.markerType),
                    32,
                    Color.white
                );

                RectTransform iconRT = iconText.GetComponent<RectTransform>();
                StretchFull(iconRT);
                iconText.alignment = TextAlignmentOptions.Center;
                iconText.raycastTarget = false;

                Outline textOutline = iconText.gameObject.AddComponent<Outline>();
                textOutline.effectColor = Color.black;
                textOutline.effectDistance = new Vector2(1.5f, -1.5f);
            }

            if (capturedMarker.markerSprite == null)
            {
                Outline outline = buttonObject.AddComponent<Outline>();
                outline.effectColor = Color.white;
                outline.effectDistance = new Vector2(1.5f, -1.5f);
            }
        }
    }

    private void BuildPopup()
    {
        popupRoot = CreateUIObject("MarkerActionPopup", pauseRoot.transform);
        popupRect = popupRoot.GetComponent<RectTransform>();
        popupRect.sizeDelta = new Vector2(360f, 230f);

        Image image = popupRoot.AddComponent<Image>();
        image.color = darkerPanel;
        image.raycastTarget = false;

        Outline outline = popupRoot.AddComponent<Outline>();
        outline.effectColor = magenta;
        outline.effectDistance = new Vector2(2f, -2f);

        popupTitleText = CreateText("PopupTitleText", popupRoot.transform, "MARKER", 26, Color.white);
        RectTransform titleRT = popupTitleText.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 1f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.anchoredPosition = new Vector2(0f, -16f);
        titleRT.sizeDelta = new Vector2(-30f, 42f);
        popupTitleText.alignment = TextAlignmentOptions.Center;
        popupTitleText.raycastTarget = false;

        popupWaypointButton = CreateButton("SetWaypointButton", popupRoot.transform, "SET WAYPOINT", cyan);
        RectTransform waypointRT = popupWaypointButton.GetComponent<RectTransform>();
        waypointRT.anchorMin = new Vector2(0.5f, 0.5f);
        waypointRT.anchorMax = new Vector2(0.5f, 0.5f);
        waypointRT.anchoredPosition = new Vector2(0f, -8f);
        waypointRT.sizeDelta = new Vector2(300f, 52f);

        popupEnterButton = CreateButton("EnterButton", popupRoot.transform, "ENTER", magenta);
        RectTransform enterRT = popupEnterButton.GetComponent<RectTransform>();
        enterRT.anchorMin = new Vector2(0.5f, 0.5f);
        enterRT.anchorMax = new Vector2(0.5f, 0.5f);
        enterRT.anchoredPosition = new Vector2(0f, -78f);
        enterRT.sizeDelta = new Vector2(300f, 52f);

        popupWaypointButtonText = popupWaypointButton.GetComponentInChildren<TMP_Text>();
        popupEnterButtonText = popupEnterButton.GetComponentInChildren<TMP_Text>();
    }

    private void BuildBottomBar()
    {
        bottomBarRoot = CreateUIObject("BottomPauseBar", pauseRoot.transform);
        RectTransform rt = bottomBarRoot.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 35f);
        rt.sizeDelta = new Vector2(1220f, 92f);

        Image bg = bottomBarRoot.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.12f);
        bg.raycastTarget = false;

        HorizontalLayoutGroup layout = bottomBarRoot.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 30f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        Button resumeButton = CreateButton("ResumeButton", bottomBarRoot.transform, "RESUME", magenta);
        SetButtonSize(resumeButton, 360f, 70f);

        Button mainMenuButton = CreateButton("MainMenuButton", bottomBarRoot.transform, "MAIN MENU", cyan);
        SetButtonSize(mainMenuButton, 360f, 70f);

        Button quitButton = CreateButton("QuitButton", bottomBarRoot.transform, "QUIT", cyan);
        SetButtonSize(quitButton, 360f, 70f);
    }

    private void BuildWaypointHud(Transform rootParent)
    {
        waypointHudRoot = CreateUIObject("WaypointHUD", rootParent);
        RectTransform rootRT = waypointHudRoot.GetComponent<RectTransform>();
        StretchFull(rootRT);

        TMP_Text arrowText = CreateText("WaypointArrow", waypointHudRoot.transform, "?", 46, magenta);
        waypointArrowRect = arrowText.GetComponent<RectTransform>();
        waypointArrowRect.anchorMin = new Vector2(0.5f, 0.82f);
        waypointArrowRect.anchorMax = new Vector2(0.5f, 0.82f);
        waypointArrowRect.pivot = new Vector2(0.5f, 0.5f);
        waypointArrowRect.anchoredPosition = Vector2.zero;
        waypointArrowRect.sizeDelta = new Vector2(90f, 90f);
        arrowText.raycastTarget = false;

        waypointDistanceText = CreateText("WaypointDistanceText", waypointHudRoot.transform, "0 m", 26, cyan);
        RectTransform distanceRT = waypointDistanceText.GetComponent<RectTransform>();
        distanceRT.anchorMin = new Vector2(0.5f, 0.82f);
        distanceRT.anchorMax = new Vector2(0.5f, 0.82f);
        distanceRT.pivot = new Vector2(0.5f, 0.5f);
        distanceRT.anchoredPosition = new Vector2(0f, -60f);
        distanceRT.sizeDelta = new Vector2(240f, 50f);
        waypointDistanceText.alignment = TextAlignmentOptions.Center;
        waypointDistanceText.raycastTarget = false;
    }

    private void ShowPopup(PauseMapMarker marker, RectTransform clickedButton)
    {
        selectedMarker = marker;

        if (popupTitleText != null)
            popupTitleText.text = GetMarkerDisplayName(marker);

        if (popupWaypointButton != null)
            popupWaypointButton.gameObject.SetActive(marker.canSetWaypoint);

        if (popupWaypointButtonText != null)
            popupWaypointButtonText.text = "SET WAYPOINT";

        bool showEnter = marker.canEnter;

        if (mode == PauseMapMode.Race && marker.markerType == PauseMapMarkerType.Checkpoint)
            showEnter = false;

        if (popupEnterButton != null)
            popupEnterButton.gameObject.SetActive(showEnter);

        if (popupEnterButtonText != null)
            popupEnterButtonText.text = marker.enterButtonText;

        if (popupRect != null && clickedButton != null)
            popupRect.position = clickedButton.position + new Vector3(230f, 0f, 0f);

        if (popupRoot != null)
            popupRoot.SetActive(true);
    }

    private void HidePopup()
    {
        selectedMarker = null;

        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    private void HidePauseImmediate()
    {
        if (pauseRoot != null)
            pauseRoot.SetActive(false);

        if (bottomBarRoot != null)
            bottomBarRoot.SetActive(false);
    }

    private void HideWaypointHud()
    {
        GameObject hudRoot = waypointHudRootOverride != null ? waypointHudRootOverride : waypointHudRoot;

        if (hudRoot != null)
            hudRoot.SetActive(false);
    }

    private bool ShouldGenerateMarkerButton(PauseMapMarker marker)
    {
        if (marker == null)
            return false;

        if (mode == PauseMapMode.Freeroam)
            return marker.showInFreeroam;

        return marker.showInRace;
    }

    private bool ShouldShowMarker(PauseMapMarker marker)
    {
        if (!ShouldGenerateMarkerButton(marker))
            return false;

        if (!hideInvalidRaceMarkers)
            return true;

        if (marker.markerType != PauseMapMarkerType.Race)
            return true;

        if (marker.linkedMissionMarker == null)
        {
            if (logRaceMarkerFiltering)
                Debug.LogWarning("PauseMapMenu: Hiding race marker '" + GetMarkerDisplayName(marker) + "' because it has no linked MissionMarkerInteract.");

            return false;
        }

        VehicleData equippedVehicle = FindEquippedVehicle();

        if (equippedVehicle == null)
        {
            if (logRaceMarkerFiltering)
                Debug.Log("PauseMapMenu: Hiding race marker '" + GetMarkerDisplayName(marker) + "' because no equipped vehicle was found.");

            return false;
        }

        bool allowed = IsVehicleAllowedForLinkedMissionMarker(equippedVehicle.vehicleType, marker.linkedMissionMarker);

        if (logRaceMarkerFiltering)
        {
            Debug.Log(
                "PauseMapMenu: Race marker filter. Marker=" +
                GetMarkerDisplayName(marker) +
                " | CurrentVehicle=" +
                equippedVehicle.displayName +
                " | CurrentType=" +
                equippedVehicle.vehicleType +
                " | LinkedMissionMarker=" +
                marker.linkedMissionMarker.name +
                " | Allowed=" +
                allowed
            );
        }

        return allowed;
    }

    private bool IsVehicleAllowedForLinkedMissionMarker(VehicleType vehicleType, MissionMarkerInteract linkedMissionMarker)
    {
        if (linkedMissionMarker == null)
            return false;

        if (!linkedMissionMarker.requireAllowedVehicleType)
            return true;

        if (linkedMissionMarker.allowedVehicleTypes != null &&
            linkedMissionMarker.allowedVehicleTypes.Length > 0)
        {
            for (int i = 0; i < linkedMissionMarker.allowedVehicleTypes.Length; i++)
            {
                VehicleType allowedType = linkedMissionMarker.allowedVehicleTypes[i];

                if (allowedType == VehicleType.Any)
                    return true;

                if (allowedType == vehicleType)
                    return true;
            }

            return false;
        }

        if (!linkedMissionMarker.useTrackVariantFallbackRules)
            return true;

        return IsVehicleAllowedByTrackVariant(vehicleType, linkedMissionMarker.trackVariant);
    }

    private bool IsVehicleAllowedByTrackVariant(VehicleType vehicleType, RaceLoadRequest.TrackVariant trackVariant)
    {
        switch (trackVariant)
        {
            case RaceLoadRequest.TrackVariant.Road:
                return vehicleType == VehicleType.Road;

            case RaceLoadRequest.TrackVariant.OffRoad:
                return vehicleType == VehicleType.OffRoad ||
                       vehicleType == VehicleType.AllTerrain;

            case RaceLoadRequest.TrackVariant.AllTerrain:
                return vehicleType == VehicleType.AllTerrain ||
                       vehicleType == VehicleType.OffRoad ||
                       vehicleType == VehicleType.MonsterTruck;

            case RaceLoadRequest.TrackVariant.MonsterTruck:
                return vehicleType == VehicleType.MonsterTruck;

            default:
                return true;
        }
    }

    private VehicleData FindEquippedVehicle()
    {
        VehicleData[] roster = GetActiveRoster();

        if (roster == null || roster.Length == 0)
        {
            if (logRaceMarkerFiltering)
                Debug.LogWarning("PauseMapMenu: Vehicle roster is empty.");

            return null;
        }

        string fallbackId = "";

        if (useFirstVehicleIfNoSave && roster[0] != null)
            fallbackId = roster[0].vehicleId;

        string equippedVehicleId = GarageSaveSystem.LoadEquippedVehicleId(fallbackId);

        for (int i = 0; i < roster.Length; i++)
        {
            VehicleData vehicle = roster[i];

            if (vehicle == null)
                continue;

            if (vehicle.vehicleId == equippedVehicleId)
                return vehicle;
        }

        if (useFirstVehicleIfNoSave)
            return roster[0];

        return null;
    }

    private VehicleData[] GetActiveRoster()
    {
        if (vehicleDatabase != null && vehicleDatabase.HasVehicles())
            return vehicleDatabase.GetVehicles();

        return vehicleRoster;
    }

    private string GetMarkerDisplayName(PauseMapMarker marker)
    {
        if (marker == null)
            return "Marker";

        if (marker.linkedMissionMarker != null &&
            !string.IsNullOrWhiteSpace(marker.linkedMissionMarker.raceDisplayName))
        {
            return marker.linkedMissionMarker.raceDisplayName;
        }

        if (marker.linkedMissionMarker != null)
            return marker.linkedMissionMarker.name;

        if (!string.IsNullOrWhiteSpace(marker.editorLabel))
            return marker.editorLabel;

        return marker.markerType.ToString();
    }

    private string GetMarkerButtonName(PauseMapMarker marker)
    {
        if (marker == null)
            return "MarkerButton_Null";

        return "MarkerButton_" + SanitizeUIName(GetMarkerDisplayName(marker));
    }

    private string SanitizeUIName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
            return "Unnamed";

        string cleanName = rawName.Trim();

        cleanName = cleanName.Replace("/", "_");
        cleanName = cleanName.Replace("\\", "_");
        cleanName = cleanName.Replace(":", "_");
        cleanName = cleanName.Replace("*", "_");
        cleanName = cleanName.Replace("?", "_");
        cleanName = cleanName.Replace("\"", "_");
        cleanName = cleanName.Replace("<", "_");
        cleanName = cleanName.Replace(">", "_");
        cleanName = cleanName.Replace("|", "_");

        return cleanName;
    }

    private Transform GetMarkerWaypointTarget(PauseMapMarker marker)
    {
        if (marker == null)
            return null;

        if (marker.waypointTarget != null)
            return marker.waypointTarget;

        if (marker.linkedMissionMarker != null && marker.useLinkedMissionMarkerAsWaypointTarget)
            return marker.linkedMissionMarker.transform;

        return null;
    }

    private string GetMarkerTargetSceneName(PauseMapMarker marker)
    {
        if (marker == null)
            return "";

        if (marker.markerType == PauseMapMarkerType.Race &&
            marker.linkedMissionMarker != null &&
            !string.IsNullOrWhiteSpace(marker.linkedMissionMarker.raceSceneName))
        {
            return marker.linkedMissionMarker.raceSceneName;
        }

        return marker.targetSceneName;
    }

    private Vector2 MapPositionToAnchored(Vector2 normalized)
    {
        float x = Mathf.Lerp(-mapSize.x * 0.5f, mapSize.x * 0.5f, normalized.x);
        float y = Mathf.Lerp(-mapSize.y * 0.5f, mapSize.y * 0.5f, normalized.y);
        return new Vector2(x, y);
    }

    private Color GetMarkerColor(PauseMapMarkerType type)
    {
        switch (type)
        {
            case PauseMapMarkerType.Garage:
                return magenta;

            case PauseMapMarkerType.Race:
            case PauseMapMarkerType.Checkpoint:
            case PauseMapMarkerType.Finish:
                return cyan;

            default:
                return new Color(0.4f, 0.5f, 1f, 1f);
        }
    }

    private string GetMarkerIcon(PauseMapMarkerType type)
    {
        switch (type)
        {
            case PauseMapMarkerType.Garage:
                return "G";

            case PauseMapMarkerType.Race:
                return "R";

            case PauseMapMarkerType.Checkpoint:
                return "C";

            case PauseMapMarkerType.Finish:
                return "F";

            default:
                return "!";
        }
    }

    private void CreateMapLine(string name, Vector2 start, Vector2 end, Color color)
    {
        GameObject lineObject = CreateUIObject(name, mapPanelRect);
        RectTransform rt = lineObject.GetComponent<RectTransform>();

        Vector2 direction = end - start;
        float length = direction.magnitude;

        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = start;
        rt.sizeDelta = new Vector2(length, raceLineThickness);

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rt.localRotation = Quaternion.Euler(0f, 0f, angle);

        Image image = lineObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    private Button CreateButton(string name, Transform parent, string text, Color accentColor)
    {
        GameObject buttonObject = CreateUIObject(name, parent);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(accentColor.r * 0.25f, accentColor.g * 0.25f, accentColor.b * 0.25f, 0.94f);
        image.raycastTarget = true;

        Outline outline = buttonObject.AddComponent<Outline>();
        outline.effectColor = accentColor;
        outline.effectDistance = new Vector2(2f, -2f);

        Button button = buttonObject.AddComponent<Button>();

        TMP_Text buttonText = CreateText("Text", buttonObject.transform, text, 28, Color.white);
        RectTransform textRT = buttonText.GetComponent<RectTransform>();
        StretchFull(textRT);
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.raycastTarget = false;

        return button;
    }

    private void SetButtonSize(Button button, float width, float height)
    {
        RectTransform rt = button.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, height);

        LayoutElement layout = button.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = height;
    }

    private TMP_Text CreateText(string name, Transform parent, string text, int fontSize, Color color)
    {
        GameObject textObject = CreateUIObject(name, parent);
        TextMeshProUGUI tmp = textObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.fontStyle = FontStyles.Bold | FontStyles.Italic;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableAutoSizing = false;
        tmp.raycastTarget = false;
        return tmp;
    }

    private GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        return obj;
    }

    private void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private void RebindButtonByName(string buttonName, UnityAction action)
    {
        Button button = FindButtonByName(buttonName);

        if (button == null)
        {
            Debug.LogWarning("PauseMapMenu: Could not find button named: " + buttonName);
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private Button FindButtonByName(string buttonName)
    {
        if (generatedRoot == null)
            CacheGeneratedReferences();

        if (generatedRoot == null)
            return null;

        Button[] buttons = generatedRoot.GetComponentsInChildren<Button>(true);

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].name == buttonName)
                return buttons[i];
        }

        return null;
    }

    private void EnsureCanvas()
    {
        GameObject existingPauseCanvas = GameObject.Find(pauseCanvasName);

        if (existingPauseCanvas != null)
        {
            canvas = existingPauseCanvas.GetComponent<Canvas>();

            if (canvas == null)
                canvas = existingPauseCanvas.AddComponent<Canvas>();

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;

            CanvasScaler scaler = existingPauseCanvas.GetComponent<CanvasScaler>();

            if (scaler == null)
                scaler = existingPauseCanvas.AddComponent<CanvasScaler>();

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            if (existingPauseCanvas.GetComponent<GraphicRaycaster>() == null)
                existingPauseCanvas.AddComponent<GraphicRaycaster>();

            return;
        }

        GameObject canvasObject = new GameObject(pauseCanvasName);
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        CanvasScaler newScaler = canvasObject.AddComponent<CanvasScaler>();
        newScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        newScaler.referenceResolution = new Vector2(1920f, 1080f);
        newScaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
    }

    private void EnsureEventSystem()
    {
        EventSystem existing = FindObjectOfType<EventSystem>();

        if (existing != null)
            return;

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private void DeleteExistingGeneratedUI()
    {
        Transform existing = null;

        if (canvas != null)
            existing = canvas.transform.Find(generatedRootName);

        if (existing == null)
            return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
            DestroyImmediate(existing.gameObject);
        else
            Destroy(existing.gameObject);
#else
        Destroy(existing.gameObject);
#endif
    }

    private void CacheGeneratedReferences()
    {
        GameObject pauseCanvasObject = GameObject.Find(pauseCanvasName);

        if (pauseCanvasObject != null)
            canvas = pauseCanvasObject.GetComponent<Canvas>();
        else
            canvas = FindObjectOfType<Canvas>();

        if (canvas == null)
            return;

        Transform root = canvas.transform.Find(generatedRootName);
        generatedRoot = root != null ? root.gameObject : null;

        if (root == null)
            return;

        Transform pause = root.Find("PauseRoot");
        pauseRoot = pause != null ? pause.gameObject : null;

        Transform bottom = pause != null ? pause.Find("BottomPauseBar") : null;
        bottomBarRoot = bottom != null ? bottom.gameObject : null;

        Transform map = pause != null ? pause.Find("MapPanel") : null;
        mapPanelObject = map != null ? map.gameObject : null;
        mapPanelRect = map != null ? map.GetComponent<RectTransform>() : null;

        Transform popup = pause != null ? pause.Find("MarkerActionPopup") : null;
        popupRoot = popup != null ? popup.gameObject : null;
        popupRect = popup != null ? popup.GetComponent<RectTransform>() : null;

        if (popup != null)
        {
            Transform title = popup.Find("PopupTitleText");
            popupTitleText = title != null ? title.GetComponent<TMP_Text>() : null;

            Transform waypoint = popup.Find("SetWaypointButton");
            popupWaypointButton = waypoint != null ? waypoint.GetComponent<Button>() : null;
            popupWaypointButtonText = waypoint != null ? waypoint.GetComponentInChildren<TMP_Text>() : null;

            Transform enter = popup.Find("EnterButton");
            popupEnterButton = enter != null ? enter.GetComponent<Button>() : null;
            popupEnterButtonText = enter != null ? enter.GetComponentInChildren<TMP_Text>() : null;
        }

        Transform header = pause != null ? pause.Find("MapHeader") : null;

        if (header != null)
        {
            Transform title = header.Find("MapTitleText");
            titleText = title != null ? title.GetComponent<TMP_Text>() : null;
        }

        Transform waypointHud = root.Find("WaypointHUD");
        waypointHudRoot = waypointHud != null ? waypointHud.gameObject : null;

        if (waypointHud != null)
        {
            Transform arrow = waypointHud.Find("WaypointArrow");
            waypointArrowRect = arrow != null ? arrow.GetComponent<RectTransform>() : null;

            Transform distance = waypointHud.Find("WaypointDistanceText");
            waypointDistanceText = distance != null ? distance.GetComponent<TMP_Text>() : null;
        }
    }
}

[System.Serializable]
public class StandardSceneLoadRequest
{
    public string sceneName;
    public string loadingMessage;
}