using TMPro;
using UnityEngine;

public class MissionMarkerUIController : MonoBehaviour
{
    public static MissionMarkerUIController Instance { get; private set; }

    [Header("Root")]
    public CanvasGroup canvasGroup;
    public GameObject panelRoot;

    [Header("Text")]
    public TMP_Text raceNameText;
    public TMP_Text raceTypeText;
    public TMP_Text requiredVehicleText;
    public TMP_Text currentVehicleText;
    public TMP_Text actionText;
    public TMP_Text hintText;

    [Header("Behaviour")]
    public bool hideOnStart = true;
    public bool useFade = true;
    public float fadeSpeed = 10f;

    [Header("Default Text")]
    public string fallbackRaceName = "UNKNOWN RACE";
    public string fallbackRaceType = "Race";
    public string fallbackRequiredVehicle = "Any Vehicle";
    public string fallbackCurrentVehicle = "Unknown Vehicle";

    [Header("Allowed State")]
    public string allowedActionText = "Press E to Start";
    public string allowedHintText = "Enter Race";

    [Header("Blocked State")]
    public string blockedActionPrefix = "Need ";

    private float targetAlpha;
    private bool isVisible;

    private void Awake()
    {
        Instance = this;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (panelRoot == null)
            panelRoot = gameObject;

        if (hideOnStart)
            HideInstant();
        else
            ShowInstant();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (!useFade || canvasGroup == null)
            return;

        canvasGroup.alpha = Mathf.MoveTowards(
            canvasGroup.alpha,
            targetAlpha,
            fadeSpeed * Time.unscaledDeltaTime
        );

        bool shouldBlock = canvasGroup.alpha > 0.01f;

        canvasGroup.interactable = shouldBlock;
        canvasGroup.blocksRaycasts = shouldBlock;

        if (panelRoot != null && !panelRoot.activeSelf && targetAlpha > 0f)
            panelRoot.SetActive(true);

        if (panelRoot != null && panelRoot.activeSelf && targetAlpha <= 0f && canvasGroup.alpha <= 0.01f)
            panelRoot.SetActive(false);
    }

    public void ShowMission(
        string raceName,
        string raceType,
        string requiredVehicle,
        string currentVehicle,
        bool canStart
    )
    {
        SetText(raceNameText, CleanText(raceName, fallbackRaceName));
        SetText(raceTypeText, CleanText(raceType, fallbackRaceType));
        SetText(requiredVehicleText, "Required: " + CleanText(requiredVehicle, fallbackRequiredVehicle));
        SetText(currentVehicleText, "Current: " + CleanText(currentVehicle, fallbackCurrentVehicle));

        if (canStart)
        {
            SetText(actionText, allowedActionText);
            SetText(hintText, allowedHintText);
        }
        else
        {
            string required = CleanText(requiredVehicle, fallbackRequiredVehicle);

            SetText(actionText, blockedActionPrefix + required);
            SetText(hintText, "Change Vehicle");
        }

        Show();
    }

    public void ShowAllowed(
        string raceName,
        string raceType,
        string requiredVehicle,
        string currentVehicle
    )
    {
        ShowMission(raceName, raceType, requiredVehicle, currentVehicle, true);
    }

    public void ShowBlocked(
        string raceName,
        string raceType,
        string requiredVehicle,
        string currentVehicle
    )
    {
        ShowMission(raceName, raceType, requiredVehicle, currentVehicle, false);
    }

    public void ShowSetupIncomplete(string message = "Race setup incomplete")
    {
        SetText(raceNameText, fallbackRaceName);
        SetText(raceTypeText, message);
        SetText(requiredVehicleText, "Required: Unknown");
        SetText(currentVehicleText, "Current: Unknown");
        SetText(actionText, "Cannot Start");
        SetText(hintText, "Check Marker Setup");

        Show();
    }

    public void Show()
    {
        isVisible = true;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (canvasGroup == null)
            return;

        if (useFade)
        {
            targetAlpha = 1f;
        }
        else
        {
            ShowInstant();
        }
    }

    public void Hide()
    {
        isVisible = false;

        if (canvasGroup == null)
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);

            return;
        }

        if (useFade)
        {
            targetAlpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        else
        {
            HideInstant();
        }
    }

    public void ShowInstant()
    {
        isVisible = true;
        targetAlpha = 1f;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }

    public void HideInstant()
    {
        isVisible = false;
        targetAlpha = 0f;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public bool IsVisible()
    {
        return isVisible;
    }

    private void SetText(TMP_Text text, string value)
    {
        if (text == null)
            return;

        text.text = value;
    }

    private string CleanText(string value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        return value;
    }
}