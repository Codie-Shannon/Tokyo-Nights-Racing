using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class PersistentAudioSettingsLoader : MonoBehaviour
{
    public static PersistentAudioSettingsLoader Instance { get; private set; }

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Exposed Mixer Parameter Names")]
    [SerializeField] private string masterVolumeParameter = "MasterVolume";
    [SerializeField] private string musicVolumeParameter = "MusicVolume";
    [SerializeField] private string sfxVolumeParameter = "SFXVolume";

    [Header("PlayerPrefs Keys")]
    [SerializeField] private string masterVolumeKey = "Settings_MasterVolume";
    [SerializeField] private string musicVolumeKey = "Settings_MusicVolume";
    [SerializeField] private string sfxVolumeKey = "Settings_SFXVolume";

    [Header("Defaults")]
    [Range(0f, 1f)]
    [SerializeField] private float defaultMasterVolume = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float defaultMusicVolume = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float defaultSFXVolume = 1f;

    [Header("Debug")]
    [SerializeField] private bool logDebug = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            if (logDebug)
                Debug.Log("PersistentAudioSettingsLoader: Duplicate found, destroying this copy.");

            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        ApplySavedAudioSettings();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplySavedAudioSettings();

        if (logDebug)
            Debug.Log("PersistentAudioSettingsLoader: Reapplied audio settings after loading scene: " + scene.name);
    }

    public void ApplySavedAudioSettings()
    {
        if (audioMixer == null)
        {
            Debug.LogWarning("PersistentAudioSettingsLoader: No AudioMixer assigned.");
            return;
        }

        float masterVolume = PlayerPrefs.GetFloat(masterVolumeKey, defaultMasterVolume);
        float musicVolume = PlayerPrefs.GetFloat(musicVolumeKey, defaultMusicVolume);
        float sfxVolume = PlayerPrefs.GetFloat(sfxVolumeKey, defaultSFXVolume);

        SetMixerVolume(masterVolumeParameter, masterVolume);
        SetMixerVolume(musicVolumeParameter, musicVolume);
        SetMixerVolume(sfxVolumeParameter, sfxVolume);

        if (logDebug)
        {
            Debug.Log(
                "PersistentAudioSettingsLoader: Applied saved audio settings. " +
                "Master=" + masterVolume +
                " Music=" + musicVolume +
                " SFX=" + sfxVolume
            );
        }
    }

    public void ApplyAndSaveAudioSettings(float masterVolume, float musicVolume, float sfxVolume)
    {
        masterVolume = Mathf.Clamp01(masterVolume);
        musicVolume = Mathf.Clamp01(musicVolume);
        sfxVolume = Mathf.Clamp01(sfxVolume);

        PlayerPrefs.SetFloat(masterVolumeKey, masterVolume);
        PlayerPrefs.SetFloat(musicVolumeKey, musicVolume);
        PlayerPrefs.SetFloat(sfxVolumeKey, sfxVolume);
        PlayerPrefs.Save();

        ApplySavedAudioSettings();
    }

    public void SetMasterVolume(float value)
    {
        value = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(masterVolumeKey, value);
        SetMixerVolume(masterVolumeParameter, value);
    }

    public void SetMusicVolume(float value)
    {
        value = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(musicVolumeKey, value);
        SetMixerVolume(musicVolumeParameter, value);
    }

    public void SetSFXVolume(float value)
    {
        value = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(sfxVolumeKey, value);
        SetMixerVolume(sfxVolumeParameter, value);
    }

    private void SetMixerVolume(string exposedParameterName, float sliderValue)
    {
        if (audioMixer == null)
            return;

        sliderValue = Mathf.Clamp01(sliderValue);

        float volumeDb;

        if (sliderValue <= 0.0001f)
            volumeDb = -80f;
        else
            volumeDb = Mathf.Log10(sliderValue) * 20f;

        bool success = audioMixer.SetFloat(exposedParameterName, volumeDb);

        if (!success)
        {
            Debug.LogWarning(
                "PersistentAudioSettingsLoader: Could not set mixer parameter '" +
                exposedParameterName +
                "'. Check exposed parameter name."
            );
        }
    }
}