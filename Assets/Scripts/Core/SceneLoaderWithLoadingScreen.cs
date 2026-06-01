using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoaderWithLoadingScreen : MonoBehaviour
{
    public static SceneLoaderWithLoadingScreen Instance;

    [Header("Loading")]
    public string defaultLoadingMessage = "Loading...";
    public float minimumLoadingTime = 1.5f;

    [Header("Timing")]
    public float fadeInBufferTime = 0.2f;
    public float fadeOutDelayAfterSceneLoad = 0.2f;

    private bool isLoading;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void LoadScene(string sceneName)
    {
        LoadScene(sceneName, defaultLoadingMessage);
    }

    public void LoadScene(StandardSceneLoadRequest request)
    {
        if (request == null)
        {
            Debug.LogWarning("SceneLoaderWithLoadingScreen: LoadScene request was null.");
            return;
        }

        LoadScene(request.sceneName, request.loadingMessage);
    }

    public void LoadScene(string sceneName, string loadingMessage)
    {
        if (isLoading)
            return;

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("SceneLoaderWithLoadingScreen: Scene name is empty.");
            return;
        }

        if (string.IsNullOrWhiteSpace(loadingMessage))
        {
            loadingMessage = defaultLoadingMessage;
        }

        StartCoroutine(LoadSceneRoutine(sceneName, loadingMessage));
    }

    private IEnumerator LoadSceneRoutine(string sceneName, string loadingMessage)
    {
        isLoading = true;

        float startTime = Time.unscaledTime;

        LoadingScreenController loadingScreen = LoadingScreenController.Instance;

        if (loadingScreen != null)
        {
            loadingScreen.Show(loadingMessage);
        }
        else
        {
            Debug.LogWarning("SceneLoaderWithLoadingScreen: No LoadingScreenController found.");
        }

        yield return new WaitForSecondsRealtime(fadeInBufferTime);

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

        if (operation == null)
        {
            Debug.LogError("SceneLoaderWithLoadingScreen: Could not load scene: " + sceneName);
            isLoading = false;
            yield break;
        }

        operation.allowSceneActivation = false;

        while (operation.progress < 0.9f)
        {
            yield return null;
        }

        float elapsed = Time.unscaledTime - startTime;

        if (elapsed < minimumLoadingTime)
        {
            yield return new WaitForSecondsRealtime(minimumLoadingTime - elapsed);
        }

        operation.allowSceneActivation = true;

        while (!operation.isDone)
        {
            yield return null;
        }

        yield return null;
        yield return new WaitForSecondsRealtime(fadeOutDelayAfterSceneLoad);

        loadingScreen = LoadingScreenController.Instance;

        if (loadingScreen != null)
        {
            loadingScreen.Hide();
        }

        isLoading = false;
    }
}