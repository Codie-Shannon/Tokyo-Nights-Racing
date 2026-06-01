using System.Collections;
using TMPro;
using UnityEngine;

public class LoadingScreenController : MonoBehaviour
{
    public static LoadingScreenController Instance;

    private static float savedAudioVolume = 1f;
    private static bool audioWasMutedByLoadingScreen = false;

    [Header("UI")]
    public CanvasGroup canvasGroup;
    public TMP_Text loadingText;

    [Header("Behaviour")]
    public bool showOnAwakeForRaceFlow = true;
    public bool startHiddenIfNoRaceFlow = true;
    public float fadeSpeed = 8f;

    [Header("Audio")]
    public bool muteAudioWhileVisible = true;

    [Tooltip("Fallback volume to restore to if the previous volume was already 0 because of a scene load.")]
    public float fallbackRestoreVolume = 1f;

    private bool isVisible = false;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        Instance = this;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        bool shouldShowOnAwake =
            showOnAwakeForRaceFlow &&
            (RaceLaunchData.HasRaceLaunchData || RaceLaunchData.ReturningFromRace);

        if (shouldShowOnAwake)
        {
            ShowImmediate("Loading...");
        }
        else if (startHiddenIfNoRaceFlow)
        {
            HideImmediate();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void ShowImmediate(string message = "Loading...")
    {
        if (loadingText != null)
            loadingText.text = message;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        MuteAudioIfNeeded();

        isVisible = true;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }
    }

    public void HideImmediate()
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        isVisible = false;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        RestoreAudioIfNeeded();
    }

    public void Show(string message = "Loading...")
    {
        if (loadingText != null)
            loadingText.text = message;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeTo(1f, true));
    }

    public void Hide()
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeTo(0f, false));
    }

    public IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

        Hide();

        while (canvasGroup != null && canvasGroup.alpha > 0.01f)
            yield return null;
    }

    private IEnumerator FadeTo(float targetAlpha, bool showing)
    {
        if (canvasGroup == null)
            yield break;

        if (showing)
        {
            MuteAudioIfNeeded();

            isVisible = true;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        while (!Mathf.Approximately(canvasGroup.alpha, targetAlpha))
        {
            canvasGroup.alpha = Mathf.MoveTowards(
                canvasGroup.alpha,
                targetAlpha,
                Time.unscaledDeltaTime * fadeSpeed
            );

            yield return null;
        }

        canvasGroup.alpha = targetAlpha;

        if (!showing)
        {
            isVisible = false;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            RestoreAudioIfNeeded();
        }

        fadeRoutine = null;
    }

    private void MuteAudioIfNeeded()
    {
        if (!muteAudioWhileVisible)
            return;

        if (audioWasMutedByLoadingScreen)
            return;

        // Only save the volume if it is actually audible.
        // This prevents saving 0 after a scene transition where the previous loading screen muted it.
        if (AudioListener.volume > 0.001f)
            savedAudioVolume = AudioListener.volume;
        else if (savedAudioVolume <= 0.001f)
            savedAudioVolume = fallbackRestoreVolume;

        AudioListener.volume = 0f;
        audioWasMutedByLoadingScreen = true;
    }

    private void RestoreAudioIfNeeded()
    {
        if (!muteAudioWhileVisible)
            return;

        float restoreVolume = savedAudioVolume;

        if (restoreVolume <= 0.001f)
            restoreVolume = fallbackRestoreVolume;

        AudioListener.volume = restoreVolume;
        audioWasMutedByLoadingScreen = false;
    }

    [ContextMenu("Force Restore Audio")]
    public void ForceRestoreAudio()
    {
        AudioListener.volume = fallbackRestoreVolume;
        audioWasMutedByLoadingScreen = false;
        savedAudioVolume = fallbackRestoreVolume;
    }
}