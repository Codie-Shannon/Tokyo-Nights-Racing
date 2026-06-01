using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SettingsMenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;

    [Header("Audio Mixer")]
    public AudioMixer audioMixer;

    [Header("Audio Sliders")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;

    [Header("Display")]
    public Toggle fullscreenToggle;
    public TMP_Dropdown qualityDropdown;

    [Header("Traffic")]
    [Tooltip("Slider should be Whole Numbers, Min 1, Max 4. Values map to 50, 100, 150, 200.")]
    public Slider trafficDensitySlider;

    [Header("Optional Value Texts")]
    public TMP_Text masterVolumeValueText;
    public TMP_Text musicVolumeValueText;
    public TMP_Text sfxVolumeValueText;
    public TMP_Text trafficDensityValueText;

    private const string MasterVolumeKey = "Settings_MasterVolume";
    private const string MusicVolumeKey = "Settings_MusicVolume";
    private const string SFXVolumeKey = "Settings_SFXVolume";
    private const string FullscreenKey = "Settings_Fullscreen";
    private const string QualityKey = "Settings_Quality";
    private const string TrafficDensityKey = "Settings_TrafficDensity";

    public static int CurrentTrafficDensity
    {
        get { return PlayerPrefs.GetInt(TrafficDensityKey, 100); }
    }

    private void Awake()
    {
        SetupQualityDropdown();
        SetupTrafficDensitySlider();
        LoadUIValues();
        AddListeners();
    }

    private void Start()
    {
        ApplyAllSettings();
        StartCoroutine(ReapplyAudioAfterFrame());
    }

    private IEnumerator ReapplyAudioAfterFrame()
    {
        yield return null;
        ApplyAudioSettingsOnly();
    }

    private void SetupQualityDropdown()
    {
        if (qualityDropdown == null)
            return;

        qualityDropdown.ClearOptions();

        List<string> options = new List<string>();
        string[] qualityNames = QualitySettings.names;

        for (int i = 0; i < qualityNames.Length; i++)
        {
            options.Add(qualityNames[i]);
        }

        qualityDropdown.AddOptions(options);
    }

    private void SetupTrafficDensitySlider()
    {
        if (trafficDensitySlider == null)
            return;

        trafficDensitySlider.minValue = 1f;
        trafficDensitySlider.maxValue = 4f;
        trafficDensitySlider.wholeNumbers = true;
    }

    private void AddListeners()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);

        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);

        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);

        if (qualityDropdown != null)
            qualityDropdown.onValueChanged.AddListener(SetQuality);

        if (trafficDensitySlider != null)
            trafficDensitySlider.onValueChanged.AddListener(SetTrafficDensityFromSlider);
    }

    private void LoadUIValues()
    {
        float masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
        float musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
        float sfxVolume = PlayerPrefs.GetFloat(SFXVolumeKey, 1f);

        int fullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0);
        int quality = PlayerPrefs.GetInt(QualityKey, QualitySettings.GetQualityLevel());
        int trafficDensity = PlayerPrefs.GetInt(TrafficDensityKey, 100);

        if (masterVolumeSlider != null)
            masterVolumeSlider.value = masterVolume;

        if (musicVolumeSlider != null)
            musicVolumeSlider.value = musicVolume;

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.value = sfxVolume;

        if (fullscreenToggle != null)
            fullscreenToggle.isOn = fullscreen == 1;

        if (qualityDropdown != null)
            qualityDropdown.value = Mathf.Clamp(quality, 0, QualitySettings.names.Length - 1);

        if (trafficDensitySlider != null)
            trafficDensitySlider.value = TrafficDensityToSliderValue(trafficDensity);

        UpdateAllValueTexts();
    }

    private void ApplyAllSettings()
    {
        ApplyAudioSettingsOnly();

        if (fullscreenToggle != null)
            Screen.fullScreen = fullscreenToggle.isOn;

        if (qualityDropdown != null && QualitySettings.names.Length > 0)
            QualitySettings.SetQualityLevel(Mathf.Clamp(qualityDropdown.value, 0, QualitySettings.names.Length - 1));

        ApplyTrafficDensitySetting();

        UpdateAllValueTexts();
    }

    private void ApplyAudioSettingsOnly()
    {
        float masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
        float musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
        float sfxVolume = PlayerPrefs.GetFloat(SFXVolumeKey, 1f);

        SetMixerVolume("MasterVolume", masterVolume);
        SetMixerVolume("MusicVolume", musicVolume);
        SetMixerVolume("SFXVolume", sfxVolume);
    }

    private void ApplyTrafficDensitySetting()
    {
        int trafficDensity = PlayerPrefs.GetInt(TrafficDensityKey, 100);

        // Placeholder hook:
        // TrafficSpawner or TrafficManager can read SettingsMenuManager.CurrentTrafficDensity
        // or PlayerPrefs.GetInt("Settings_TrafficDensity", 100).
        PlayerPrefs.SetInt(TrafficDensityKey, ClampTrafficDensity(trafficDensity));
    }

    private void UpdateAllValueTexts()
    {
        if (masterVolumeSlider != null && masterVolumeValueText != null)
            masterVolumeValueText.text = Mathf.RoundToInt(masterVolumeSlider.value * 100f).ToString();

        if (musicVolumeSlider != null && musicVolumeValueText != null)
            musicVolumeValueText.text = Mathf.RoundToInt(musicVolumeSlider.value * 100f).ToString();

        if (sfxVolumeSlider != null && sfxVolumeValueText != null)
            sfxVolumeValueText.text = Mathf.RoundToInt(sfxVolumeSlider.value * 100f).ToString();

        if (trafficDensitySlider != null && trafficDensityValueText != null)
            trafficDensityValueText.text = SliderValueToTrafficDensity(trafficDensitySlider.value).ToString();
    }

    public void OpenSettings()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        SaveSettings();

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
    }

    public void ApplySettings()
    {
        SaveSettings();
        ApplyAllSettings();
        Debug.Log("Settings applied and saved.");
    }

    public void ResetDefaults()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.value = 1f;

        if (musicVolumeSlider != null)
            musicVolumeSlider.value = 1f;

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.value = 1f;

        if (fullscreenToggle != null)
            fullscreenToggle.isOn = true;

        if (qualityDropdown != null)
            qualityDropdown.value = Mathf.Clamp(QualitySettings.names.Length - 1, 0, QualitySettings.names.Length - 1);

        if (trafficDensitySlider != null)
            trafficDensitySlider.value = TrafficDensityToSliderValue(100);

        SaveSettings();
        ApplyAllSettings();
    }

    public void SetMasterVolume(float value)
    {
        value = Mathf.Clamp01(value);

        SetMixerVolume("MasterVolume", value);
        PlayerPrefs.SetFloat(MasterVolumeKey, value);

        if (masterVolumeValueText != null)
            masterVolumeValueText.text = Mathf.RoundToInt(value * 100f).ToString();
    }

    public void SetMusicVolume(float value)
    {
        value = Mathf.Clamp01(value);

        SetMixerVolume("MusicVolume", value);
        PlayerPrefs.SetFloat(MusicVolumeKey, value);

        if (musicVolumeValueText != null)
            musicVolumeValueText.text = Mathf.RoundToInt(value * 100f).ToString();
    }

    public void SetSFXVolume(float value)
    {
        value = Mathf.Clamp01(value);

        SetMixerVolume("SFXVolume", value);
        PlayerPrefs.SetFloat(SFXVolumeKey, value);

        if (sfxVolumeValueText != null)
            sfxVolumeValueText.text = Mathf.RoundToInt(value * 100f).ToString();
    }

    private void SetMixerVolume(string exposedParameterName, float sliderValue)
    {
        if (audioMixer == null)
            return;

        sliderValue = Mathf.Clamp(sliderValue, 0.0001f, 1f);
        float volumeDb = Mathf.Log10(sliderValue) * 20f;

        audioMixer.SetFloat(exposedParameterName, volumeDb);
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt(FullscreenKey, isFullscreen ? 1 : 0);
    }

    public void SetQuality(int qualityIndex)
    {
        if (QualitySettings.names.Length <= 0)
            return;

        qualityIndex = Mathf.Clamp(qualityIndex, 0, QualitySettings.names.Length - 1);

        QualitySettings.SetQualityLevel(qualityIndex);
        PlayerPrefs.SetInt(QualityKey, qualityIndex);
    }

    public void SetTrafficDensityFromSlider(float sliderValue)
    {
        int density = SliderValueToTrafficDensity(sliderValue);

        PlayerPrefs.SetInt(TrafficDensityKey, density);

        if (trafficDensityValueText != null)
            trafficDensityValueText.text = density.ToString();
    }

    public void SetTrafficDensity(int density)
    {
        density = ClampTrafficDensity(density);

        PlayerPrefs.SetInt(TrafficDensityKey, density);

        if (trafficDensitySlider != null)
            trafficDensitySlider.value = TrafficDensityToSliderValue(density);

        if (trafficDensityValueText != null)
            trafficDensityValueText.text = density.ToString();
    }

    private int SliderValueToTrafficDensity(float sliderValue)
    {
        int step = Mathf.RoundToInt(sliderValue);
        step = Mathf.Clamp(step, 1, 4);

        switch (step)
        {
            case 1:
                return 50;

            case 2:
                return 100;

            case 3:
                return 150;

            case 4:
                return 200;

            default:
                return 100;
        }
    }

    private float TrafficDensityToSliderValue(int density)
    {
        density = ClampTrafficDensity(density);

        switch (density)
        {
            case 50:
                return 1f;

            case 100:
                return 2f;

            case 150:
                return 3f;

            case 200:
                return 4f;

            default:
                return 2f;
        }
    }

    private int ClampTrafficDensity(int density)
    {
        if (density <= 50)
            return 50;

        if (density <= 100)
            return 100;

        if (density <= 150)
            return 150;

        return 200;
    }

    public void SaveSettings()
    {
        PlayerPrefs.Save();
    }
}