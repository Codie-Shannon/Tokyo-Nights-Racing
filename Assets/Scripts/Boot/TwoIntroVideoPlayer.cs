using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class TwoIntroVideoPlayer : MonoBehaviour
{
    [Header("References")]
    public VideoPlayer videoPlayer;
    public AudioSource audioSource;

    [Tooltip("Assign the RawImage/GameObject that displays the video.")]
    public GameObject videoDisplayObject;

    [Header("Fade Overlay")]
    [Tooltip("Assign a full-screen black UI Image with a CanvasGroup. It must sit above the video and title card.")]
    public CanvasGroup blackFadeCanvasGroup;

    [Header("Intro Video")]
    public VideoClip introVideo;

    [Header("Title Card Image")]
    [Tooltip("Assign the GameObject showing the Tokyo Nights title card image.")]
    public GameObject titleCardImageObject;

    [Tooltip("How long the title card stays visible before fading out.")]
    public float titleCardDisplaySeconds = 4f;

    [Header("Video Fade Out")]
    [Tooltip("The video fades to black over this duration. The fade always finishes at the end of the video.")]
    public float videoFadeOutSeconds = 2f;

    [Tooltip("How long to hold black after the video ends.")]
    public float blackHoldAfterVideoSeconds = 1f;

    [Header("Title Card Fade Out")]
    [Tooltip("How long the title card takes to fade to black.")]
    public float titleCardFadeOutSeconds = 2f;

    [Tooltip("How long to hold black after the title card fades out before loading the main menu.")]
    public float blackHoldAfterTitleSeconds = 1f;

    [Header("Skip")]
    public KeyCode skipKey = KeyCode.Space;

    [Header("Next Scene")]
    public string nextSceneName = "MainMenuScene";

    private bool skipRequested;
    private bool videoFinished;
    private Coroutine introRoutine;

    private void Start()
    {
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;

            videoPlayer.loopPointReached -= OnVideoFinished;
            videoPlayer.loopPointReached += OnVideoFinished;

            if (audioSource != null)
            {
                videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
                videoPlayer.EnableAudioTrack(0, true);
                videoPlayer.SetTargetAudioSource(0, audioSource);
            }

            videoPlayer.Stop();
        }

        HideEverything();
        SetBlackAlpha(0f);

        introRoutine = StartCoroutine(PlayIntroSequence());
    }

    private void Update()
    {
        if (Input.GetKeyDown(skipKey))
            skipRequested = true;
    }

    private IEnumerator PlayIntroSequence()
    {
        skipRequested = false;

        yield return PlayVideoWithEndFade();

        if (skipRequested)
        {
            LoadNextScene();
            yield break;
        }

        HideVideo();
        HideTitleCard();
        SetBlackAlpha(1f);

        yield return WaitForSecondsOrSkip(blackHoldAfterVideoSeconds);

        if (skipRequested)
        {
            LoadNextScene();
            yield break;
        }

        ShowTitleCard();
        SetBlackAlpha(0f);

        yield return WaitForSecondsOrSkip(titleCardDisplaySeconds);

        if (skipRequested)
        {
            LoadNextScene();
            yield break;
        }

        yield return FadeBlack(0f, 1f, titleCardFadeOutSeconds);

        if (skipRequested)
        {
            LoadNextScene();
            yield break;
        }

        HideTitleCard();
        SetBlackAlpha(1f);

        yield return WaitForSecondsOrSkip(blackHoldAfterTitleSeconds);

        LoadNextScene();
    }

    private IEnumerator PlayVideoWithEndFade()
    {
        if (introVideo == null)
        {
            Debug.LogWarning("Intro Video is not assigned.");
            yield break;
        }

        if (videoPlayer == null)
        {
            Debug.LogWarning("VideoPlayer is not assigned.");
            yield break;
        }

        HideTitleCard();
        ShowVideo();

        videoFinished = false;
        skipRequested = false;

        videoPlayer.Stop();
        videoPlayer.clip = introVideo;

        SetBlackAlpha(0f);

        videoPlayer.Prepare();

        while (!videoPlayer.isPrepared)
        {
            if (skipRequested)
            {
                StopVideoCompletely();
                HideVideo();
                yield break;
            }

            yield return null;
        }

        double clipLength = GetVideoLength();
        double fadeDuration = videoFadeOutSeconds;

        if (fadeDuration < 0.01)
            fadeDuration = 0.01;

        if (fadeDuration > clipLength)
            fadeDuration = clipLength;

        double fadeStartTime = clipLength - fadeDuration;

        videoPlayer.Play();

        while (!videoFinished && !skipRequested)
        {
            double currentTime = videoPlayer.time;

            if (currentTime >= fadeStartTime)
            {
                double fadeRange = clipLength - fadeStartTime;
                float fadeProgress = 1f;

                if (fadeRange > 0.001)
                {
                    fadeProgress = (float)((currentTime - fadeStartTime) / fadeRange);
                }

                fadeProgress = Mathf.Clamp01(fadeProgress);
                SetBlackAlpha(fadeProgress);
            }

            yield return null;
        }

        SetBlackAlpha(1f);

        StopVideoCompletely();
        HideVideo();
    }

    private double GetVideoLength()
    {
        if (videoPlayer != null && videoPlayer.length > 0.01)
            return videoPlayer.length;

        if (introVideo != null && introVideo.length > 0.01)
            return introVideo.length;

        return 1.0;
    }

    private IEnumerator FadeBlack(float fromAlpha, float toAlpha, float duration)
    {
        if (duration < 0.01f)
            duration = 0.01f;

        float timer = 0f;

        while (timer < duration && !skipRequested)
        {
            timer += Time.deltaTime;

            float progress = timer / duration;
            progress = Mathf.Clamp01(progress);

            float alpha = Mathf.Lerp(fromAlpha, toAlpha, progress);
            SetBlackAlpha(alpha);

            yield return null;
        }

        SetBlackAlpha(toAlpha);
    }

    private IEnumerator WaitForSecondsOrSkip(float seconds)
    {
        if (seconds < 0f)
            seconds = 0f;

        float timer = 0f;

        while (timer < seconds && !skipRequested)
        {
            timer += Time.deltaTime;
            yield return null;
        }
    }

    private void ShowVideo()
    {
        if (videoDisplayObject != null)
            videoDisplayObject.SetActive(true);
    }

    private void HideVideo()
    {
        if (videoDisplayObject != null)
            videoDisplayObject.SetActive(false);
    }

    private void ShowTitleCard()
    {
        StopVideoCompletely();
        HideVideo();

        if (titleCardImageObject != null)
            titleCardImageObject.SetActive(true);
        else
            Debug.LogWarning("Title Card Image Object is not assigned.");
    }

    private void HideTitleCard()
    {
        if (titleCardImageObject != null)
            titleCardImageObject.SetActive(false);
    }

    private void HideEverything()
    {
        StopVideoCompletely();
        HideVideo();
        HideTitleCard();
    }

    private void SetBlackAlpha(float alpha)
    {
        if (blackFadeCanvasGroup == null)
            return;

        blackFadeCanvasGroup.gameObject.SetActive(true);
        blackFadeCanvasGroup.alpha = Mathf.Clamp01(alpha);
        blackFadeCanvasGroup.blocksRaycasts = alpha > 0.01f;
        blackFadeCanvasGroup.interactable = false;
    }

    private void StopVideoCompletely()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }

        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        videoFinished = true;
    }

    private void LoadNextScene()
    {
        HideEverything();
        SetBlackAlpha(1f);

        if (!string.IsNullOrWhiteSpace(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogError("Next Scene Name is empty.");
        }
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
            videoPlayer.loopPointReached -= OnVideoFinished;

        if (introRoutine != null)
            StopCoroutine(introRoutine);
    }
}