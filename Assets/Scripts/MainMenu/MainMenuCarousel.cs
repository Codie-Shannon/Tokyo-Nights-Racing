using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class MainMenuCarousel : MonoBehaviour
{
    [System.Serializable]
    public class MenuItem
    {
        public string label;
        [TextArea] public string description;
    }

    [Header("Menu Data")]
    public List<MenuItem> items = new();

    [Header("Scene Names")]
    [Tooltip("Scene loaded when selecting Play.")]
    public string playSceneName = "MainCity";

    [Tooltip("Scene loaded when selecting Garage. Leave blank if not ready yet.")]
    public string garageSceneName = "GarageScene";

    [Tooltip("Fallback scene loaded when selecting Race Modes if no MainMenuRaceModesLauncher is assigned.")]
    public string raceModesSceneName = "RaceModesScene";

    [Header("Race Modes Launcher")]
    [Tooltip("If assigned, selecting Race Modes launches a compatible race from the RaceModeDatabase instead of loading RaceModesScene.")]
    public MainMenuRaceModesLauncher raceModesLauncher;

    [Header("Panels")]
    [Tooltip("Main menu buttons/carousel root. Optional.")]
    public GameObject mainMenuPanel;

    [Tooltip("Settings panel shown when selecting Settings.")]
    public GameObject settingsPanel;

    [Tooltip("Optional panel for trophies/achievements if you add one later.")]
    public GameObject trophiesPanel;

    [Header("Title Card")]
    public TMP_Text titleCardText;
    public string mainMenuTitle = "MAIN MENU";
    public string settingsTitle = "SETTINGS";
    public string trophiesTitle = "TROPHIES";

    [Header("Icon Roots")]
    public List<RectTransform> iconRoots = new();
    public List<CanvasGroup> iconGroups = new();

    [Header("Text")]
    public TMP_Text descriptionText;
    public TMP_Text selectedText;

    [Header("Animation")]
    public float moveSpeed = 16f;
    public float scaleSpeed = 18f;
    public float alphaSpeed = 14f;
    public float snapThreshold = 2f;

    [Header("Slots")]
    public Vector2 farLeft = new Vector2(-430f, 0f);
    public Vector2 left = new Vector2(-230f, 0f);
    public Vector2 center = new Vector2(0f, 0f);
    public Vector2 right = new Vector2(230f, 0f);
    public Vector2 farRight = new Vector2(430f, 0f);

    public float farScale = 0.68f;
    public float sideScale = 0.82f;
    public float centerScale = 1.12f;

    public float farAlpha = 0.75f;
    public float sideAlpha = 0.9f;
    public float centerAlpha = 1f;

    [Header("Input Lock")]
    public float inputCooldown = 0.12f;

    private int selectedIndex = 0;
    private bool inputLocked = false;
    private bool menuInputEnabled = true;

    private Coroutine inputLockRoutine;

    private void Start()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (trophiesPanel != null)
            trophiesPanel.SetActive(false);

        if (raceModesLauncher == null)
            raceModesLauncher = FindFirstObjectByType<MainMenuRaceModesLauncher>();

        SetTitleCard(mainMenuTitle);

        ApplyRequestedStartItem();
        RefreshCarousel();
    }

    private void OnEnable()
    {
        RefreshCarousel();
    }

    private void Update()
    {
        if (!menuInputEnabled)
            return;

        HandleKeyboardInput();
        AnimateToTargets();

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            SelectCurrent();
        }
    }

    private void ApplyRequestedStartItem()
    {
        if (!MainMenuReturnState.TryConsumeRequest(out MainMenuRequestedItem requestedItem))
            return;

        switch (requestedItem)
        {
            case MainMenuRequestedItem.Play:
                SelectItemByLabel("Play");
                break;

            case MainMenuRequestedItem.Garage:
                SelectItemByLabel("Garage");
                break;

            case MainMenuRequestedItem.RaceModes:
                SelectItemByAnyLabel("Race Modes", "RaceModes", "Race Mode");
                break;

            case MainMenuRequestedItem.Trophies:
                SelectItemByAnyLabel("Trophies", "Achievements");
                break;

            case MainMenuRequestedItem.Settings:
                SelectItemByLabel("Settings");
                break;

            case MainMenuRequestedItem.Exit:
                SelectItemByAnyLabel("Exit", "Quit");
                break;
        }
    }

    public void SelectGarageItem()
    {
        SelectItemByLabel("Garage");
        RefreshCarousel();
    }

    public void SelectPlayItem()
    {
        SelectItemByLabel("Play");
        RefreshCarousel();
    }

    public void SelectRaceModesItem()
    {
        SelectItemByAnyLabel("Race Modes", "RaceModes", "Race Mode");
        RefreshCarousel();
    }

    private bool SelectItemByLabel(string label)
    {
        if (items == null || items.Count == 0)
            return false;

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == null)
                continue;

            if (items[i].label.Trim() == label)
            {
                selectedIndex = i;
                return true;
            }
        }

        Debug.LogWarning("MainMenuCarousel: Could not find menu item label: " + label);
        return false;
    }

    private bool SelectItemByAnyLabel(params string[] labels)
    {
        if (labels == null)
            return false;

        for (int i = 0; i < labels.Length; i++)
        {
            if (SelectItemByLabel(labels[i]))
                return true;
        }

        return false;
    }

    public void RefreshCarousel()
    {
        StopInputLock();

        inputLocked = false;
        menuInputEnabled = true;

        if (items == null)
            items = new List<MenuItem>();

        if (items.Count <= 0)
        {
            if (selectedText != null)
                selectedText.text = "";

            if (descriptionText != null)
                descriptionText.text = "";

            return;
        }

        selectedIndex = Mathf.Clamp(selectedIndex, 0, items.Count - 1);

        ApplyImmediateLayout();
        UpdateLabels();
    }

    public void EnableMenuInputAndRefresh()
    {
        menuInputEnabled = true;
        inputLocked = false;
        RefreshCarousel();
    }

    public void DisableMenuInput()
    {
        menuInputEnabled = false;
    }

    private void HandleKeyboardInput()
    {
        if (inputLocked)
            return;

        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            MoveRight();
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            MoveLeft();
        }
    }

    public void MoveLeft()
    {
        if (inputLocked || items.Count == 0)
            return;

        selectedIndex = WrapIndex(selectedIndex - 1, items.Count);
        UpdateLabels();
        StartInputLock();
    }

    public void MoveRight()
    {
        if (inputLocked || items.Count == 0)
            return;

        selectedIndex = WrapIndex(selectedIndex + 1, items.Count);
        UpdateLabels();
        StartInputLock();
    }

    public void SelectCurrent()
    {
        if (items.Count == 0)
            return;

        string selected = items[selectedIndex].label.Trim();

        Debug.Log("Selected: " + selected);

        switch (selected)
        {
            case "Play":
                LoadSceneFromInspector(playSceneName, "Loading Freeroam...");
                break;

            case "Garage":
                LoadSceneFromInspector(garageSceneName, "Loading Garage...");
                break;

            case "Race Modes":
            case "RaceModes":
            case "Race Mode":
                SelectRaceModes();
                break;

            case "Trophies":
            case "Achievements":
                OpenTrophiesPanel();
                break;

            case "Settings":
                OpenSettingsPanel();
                break;

            case "Exit":
            case "Quit":
                QuitGame();
                break;

            default:
                Debug.LogWarning("No action set up for menu item: " + selected);
                break;
        }
    }

    private void SelectRaceModes()
    {
        if (raceModesLauncher == null)
            raceModesLauncher = FindFirstObjectByType<MainMenuRaceModesLauncher>();

        if (raceModesLauncher != null)
        {
            raceModesLauncher.LaunchRandomRaceForEquippedVehicle();
            return;
        }

        LoadSceneFromInspector(raceModesSceneName, "Loading Race Modes...");
    }

    public void OpenSettingsPanel()
    {
        menuInputEnabled = false;
        inputLocked = true;

        SetTitleCard(settingsTitle);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Settings selected, but no Settings Panel is assigned.");
            SetTitleCard(mainMenuTitle);
            EnableMenuInputAndRefresh();
        }
    }

    public void CloseSettingsPanel()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);

        SetTitleCard(mainMenuTitle);

        EnableMenuInputAndRefresh();
    }

    public void OpenTrophiesPanel()
    {
        menuInputEnabled = false;
        inputLocked = true;

        SetTitleCard(trophiesTitle);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (trophiesPanel != null)
        {
            trophiesPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Trophies selected, but no Trophies Panel is assigned.");

            if (mainMenuPanel != null)
                mainMenuPanel.SetActive(true);

            SetTitleCard(mainMenuTitle);
            EnableMenuInputAndRefresh();
        }
    }

    public void CloseTrophiesPanel()
    {
        if (trophiesPanel != null)
            trophiesPanel.SetActive(false);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);

        SetTitleCard(mainMenuTitle);

        EnableMenuInputAndRefresh();
    }

    private void SetTitleCard(string title)
    {
        if (titleCardText != null)
            titleCardText.text = title;
    }

    private void StartInputLock()
    {
        StopInputLock();
        inputLockRoutine = StartCoroutine(LockInputBriefly());
    }

    private void StopInputLock()
    {
        if (inputLockRoutine != null)
        {
            StopCoroutine(inputLockRoutine);
            inputLockRoutine = null;
        }
    }

    private IEnumerator LockInputBriefly()
    {
        inputLocked = true;
        yield return new WaitForSeconds(inputCooldown);
        inputLocked = false;
        inputLockRoutine = null;
    }

    private void UpdateLabels()
    {
        if (items.Count == 0)
            return;

        selectedIndex = Mathf.Clamp(selectedIndex, 0, items.Count - 1);

        if (selectedText != null)
            selectedText.text = items[selectedIndex].label;

        if (descriptionText != null)
            descriptionText.text = items[selectedIndex].description;
    }

    private void ApplyImmediateLayout()
    {
        if (items.Count == 0)
            return;

        int count = Mathf.Min(iconRoots.Count, items.Count);

        for (int i = 0; i < count; i++)
        {
            if (iconRoots[i] == null)
                continue;

            int rel = GetRelativeOffset(i, selectedIndex, items.Count);

            iconRoots[i].anchoredPosition = GetTargetPosition(rel);
            iconRoots[i].localScale = Vector3.one * GetTargetScale(rel);

            if (i < iconGroups.Count && iconGroups[i] != null)
                iconGroups[i].alpha = GetTargetAlpha(rel);
        }
    }

    private void AnimateToTargets()
    {
        if (items.Count == 0)
            return;

        int count = Mathf.Min(iconRoots.Count, items.Count);

        for (int i = 0; i < count; i++)
        {
            if (iconRoots[i] == null)
                continue;

            int rel = GetRelativeOffset(i, selectedIndex, items.Count);

            Vector2 targetPos = GetTargetPosition(rel);
            float targetScale = GetTargetScale(rel);
            float targetAlpha = GetTargetAlpha(rel);

            RectTransform rt = iconRoots[i];

            rt.anchoredPosition = Vector2.Lerp(
                rt.anchoredPosition,
                targetPos,
                Time.deltaTime * moveSpeed
            );

            rt.localScale = Vector3.Lerp(
                rt.localScale,
                Vector3.one * targetScale,
                Time.deltaTime * scaleSpeed
            );

            if (i < iconGroups.Count && iconGroups[i] != null)
            {
                iconGroups[i].alpha = Mathf.Lerp(
                    iconGroups[i].alpha,
                    targetAlpha,
                    Time.deltaTime * alphaSpeed
                );
            }

            if (Vector2.Distance(rt.anchoredPosition, targetPos) < snapThreshold)
                rt.anchoredPosition = targetPos;

            if (Vector3.Distance(rt.localScale, Vector3.one * targetScale) < 0.01f)
                rt.localScale = Vector3.one * targetScale;

            if (i < iconGroups.Count && iconGroups[i] != null &&
                Mathf.Abs(iconGroups[i].alpha - targetAlpha) < 0.01f)
            {
                iconGroups[i].alpha = targetAlpha;
            }
        }
    }

    private int WrapIndex(int index, int count)
    {
        if (count <= 0)
            return 0;

        if (index < 0)
            return count - 1;

        if (index >= count)
            return 0;

        return index;
    }

    private int GetRelativeOffset(int itemIndex, int selected, int count)
    {
        int diff = itemIndex - selected;

        if (diff > count / 2)
            diff -= count;

        if (diff < -count / 2)
            diff += count;

        return Mathf.Clamp(diff, -2, 2);
    }

    private Vector2 GetTargetPosition(int rel)
    {
        switch (rel)
        {
            case -2:
                return farLeft;

            case -1:
                return left;

            case 0:
                return center;

            case 1:
                return right;

            case 2:
                return farRight;

            default:
                return new Vector2(9999f, 0f);
        }
    }

    private float GetTargetScale(int rel)
    {
        switch (rel)
        {
            case -2:
                return farScale;

            case -1:
                return sideScale;

            case 0:
                return centerScale;

            case 1:
                return sideScale;

            case 2:
                return farScale;

            default:
                return 0.5f;
        }
    }

    private float GetTargetAlpha(int rel)
    {
        switch (rel)
        {
            case -2:
                return farAlpha;

            case -1:
                return sideAlpha;

            case 0:
                return centerAlpha;

            case 1:
                return sideAlpha;

            case 2:
                return farAlpha;

            default:
                return 0f;
        }
    }

    private void LoadSceneFromInspector(string sceneName)
    {
        LoadSceneFromInspector(sceneName, "");
    }

    private void LoadSceneFromInspector(string sceneName, string loadingMessage)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("Scene name is empty. Assign it in the MainMenuCarousel Inspector.");
            return;
        }

        StartCoroutine(LoadSceneRoutine(sceneName, loadingMessage));
    }

    private IEnumerator LoadSceneRoutine(string sceneName, string loadingMessage)
    {
        yield return new WaitForSeconds(0.1f);

        if (SceneLoaderWithLoadingScreen.Instance != null)
        {
            if (string.IsNullOrWhiteSpace(loadingMessage))
            {
                SceneLoaderWithLoadingScreen.Instance.LoadScene(sceneName);
            }
            else
            {
                SceneLoaderWithLoadingScreen.Instance.LoadScene(sceneName, loadingMessage);
            }
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    private void QuitGame()
    {
        Debug.Log("Quit Game");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}