using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GarageEntranceTrigger : MonoBehaviour
{
    [Header("Scene Loading")]
    [SerializeField] private string garageSceneName = "GarageScene";

    [Header("Loading Screen UI")]
    [Tooltip("This object should stay ACTIVE. It is hidden using CanvasGroup, not SetActive(false).")]
    [SerializeField] private GameObject loadingScreenRoot;

    [SerializeField] private CanvasGroup loadingCanvasGroup;
    [SerializeField] private TMP_Text loadingText;

    [Header("Loading Message")]
    [SerializeField] private string loadingGarageMessage = "Loading Garage...";

    [Header("Loading Behaviour")]
    [SerializeField] private float fadeDuration = 0.35f;
    [SerializeField] private float minimumLoadingTime = 1.0f;

    [Header("Prompt UI")]
    [SerializeField] private GameObject promptPanel;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private string promptMessage = "Press E to Enter Garage";

    [Header("Input")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("Player Detection")]
    [SerializeField] private string playerTag = "Player";

    [Header("Return Spawn")]
    [Tooltip("Assign an empty transform outside the garage. When returning from GarageScene, the car will spawn here.")]
    [SerializeField] private Transform returnSpawnPoint;

    private bool playerInside;
    private bool isLoading;

    private void Awake()
    {
        HidePrompt();

        EnsureLoadingCanvasGroup();

        // Important:
        // Do NOT disable loadingScreenRoot.
        // The pause menu and scene loader need this object active so coroutines can run.
        if (loadingScreenRoot != null && !loadingScreenRoot.activeSelf)
            loadingScreenRoot.SetActive(true);

        HideLoadingScreenInstant();
    }

    private void Update()
    {
        if (!playerInside)
            return;

        if (isLoading)
            return;

        if (Input.GetKeyDown(interactKey))
        {
            EnterGarage();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        playerInside = true;
        ShowPrompt();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        playerInside = false;
        HidePrompt();
    }

    private void ShowPrompt()
    {
        if (promptText != null)
            promptText.text = promptMessage;

        if (promptPanel != null)
            promptPanel.SetActive(true);
    }

    private void HidePrompt()
    {
        if (promptPanel != null)
            promptPanel.SetActive(false);
    }

    private void EnterGarage()
    {
        if (isLoading)
            return;

        isLoading = true;
        HidePrompt();

        if (returnSpawnPoint != null)
        {
            GarageSceneReturnData.SetReturnToFreeroam(
                returnSpawnPoint.position,
                returnSpawnPoint.rotation
            );
        }
        else
        {
            GarageSceneReturnData.ReturnTarget = GarageReturnTarget.Freeroam;
        }

        StartCoroutine(LoadGarageRoutine());
    }

    private IEnumerator LoadGarageRoutine()
    {
        if (loadingText != null)
            loadingText.text = loadingGarageMessage;

        ShowLoadingScreenInstant();

        if (loadingCanvasGroup != null)
            yield return FadeCanvasGroup(0f, 1f, fadeDuration);

        float startTime = Time.unscaledTime;

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(garageSceneName);
        loadOperation.allowSceneActivation = false;

        while (loadOperation.progress < 0.9f)
        {
            yield return null;
        }

        float elapsed = Time.unscaledTime - startTime;
        float remainingTime = minimumLoadingTime - elapsed;

        if (remainingTime > 0f)
            yield return new WaitForSecondsRealtime(remainingTime);

        loadOperation.allowSceneActivation = true;
    }

    private void EnsureLoadingCanvasGroup()
    {
        if (loadingCanvasGroup != null)
            return;

        if (loadingScreenRoot != null)
        {
            loadingCanvasGroup = loadingScreenRoot.GetComponent<CanvasGroup>();

            if (loadingCanvasGroup == null)
                loadingCanvasGroup = loadingScreenRoot.AddComponent<CanvasGroup>();
        }
    }

    private void ShowLoadingScreenInstant()
    {
        EnsureLoadingCanvasGroup();

        if (loadingScreenRoot != null && !loadingScreenRoot.activeSelf)
            loadingScreenRoot.SetActive(true);

        if (loadingCanvasGroup != null)
        {
            loadingCanvasGroup.alpha = 1f;
            loadingCanvasGroup.interactable = true;
            loadingCanvasGroup.blocksRaycasts = true;
        }
    }

    private void HideLoadingScreenInstant()
    {
        EnsureLoadingCanvasGroup();

        if (loadingScreenRoot != null && !loadingScreenRoot.activeSelf)
            loadingScreenRoot.SetActive(true);

        if (loadingCanvasGroup != null)
        {
            loadingCanvasGroup.alpha = 0f;
            loadingCanvasGroup.interactable = false;
            loadingCanvasGroup.blocksRaycasts = false;
        }
    }

    private IEnumerator FadeCanvasGroup(float from, float to, float duration)
    {
        if (loadingCanvasGroup == null)
            yield break;

        float timer = 0f;
        loadingCanvasGroup.alpha = from;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = duration <= 0f ? 1f : timer / duration;
            loadingCanvasGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        loadingCanvasGroup.alpha = to;

        loadingCanvasGroup.interactable = to > 0.9f;
        loadingCanvasGroup.blocksRaycasts = to > 0.9f;
    }
}