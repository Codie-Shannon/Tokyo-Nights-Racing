using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GarageBackButton : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";
    [SerializeField] private string freeroamSceneName = "MainScene";

    [Header("Loading Screen UI")]
    [SerializeField] private GameObject loadingScreenRoot;
    [SerializeField] private CanvasGroup loadingCanvasGroup;
    [SerializeField] private TMP_Text loadingText;

    [Header("Loading Messages")]
    [SerializeField] private string loadingMainMenuMessage = "Returning to Main Menu...";
    [SerializeField] private string loadingFreeroamMessage = "Returning to Freeroam...";

    [Header("Loading Behaviour")]
    [SerializeField] private float fadeDuration = 0.35f;
    [SerializeField] private float minimumLoadingTime = 1.0f;

    private bool isLoading;

    private void Awake()
    {
        if (loadingScreenRoot != null)
            loadingScreenRoot.SetActive(false);

        if (loadingCanvasGroup != null)
            loadingCanvasGroup.alpha = 0f;
    }

    public void Back()
    {
        if (isLoading)
            return;

        string targetScene = GetTargetSceneName();
        string loadingMessage = GetLoadingMessage();

        if (GarageSceneReturnData.ReturnTarget == GarageReturnTarget.MainMenu)
            MainMenuReturnState.RequestItem(MainMenuRequestedItem.Garage);

        StartCoroutine(LoadSceneRoutine(targetScene, loadingMessage));
    }

    private string GetTargetSceneName()
    {
        switch (GarageSceneReturnData.ReturnTarget)
        {
            case GarageReturnTarget.Freeroam:
                return freeroamSceneName;

            case GarageReturnTarget.MainMenu:
            default:
                return mainMenuSceneName;
        }
    }

    private string GetLoadingMessage()
    {
        switch (GarageSceneReturnData.ReturnTarget)
        {
            case GarageReturnTarget.Freeroam:
                return loadingFreeroamMessage;

            case GarageReturnTarget.MainMenu:
            default:
                return loadingMainMenuMessage;
        }
    }

    private IEnumerator LoadSceneRoutine(string sceneName, string message)
    {
        isLoading = true;

        if (loadingText != null)
            loadingText.text = message;

        if (loadingScreenRoot != null)
            loadingScreenRoot.SetActive(true);

        if (loadingCanvasGroup != null)
        {
            yield return FadeCanvasGroup(0f, 1f, fadeDuration);
        }

        float startTime = Time.unscaledTime;

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName);
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
    }
}