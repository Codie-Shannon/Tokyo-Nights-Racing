using System.Collections;
using UnityEngine;

public class DelayedBootAudioPlayer : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float delay = 0.05f;

    private IEnumerator Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        yield return new WaitForSecondsRealtime(delay);

        if (PersistentAudioSettingsLoader.Instance != null)
            PersistentAudioSettingsLoader.Instance.ApplySavedAudioSettings();

        if (audioSource != null)
            audioSource.Play();
    }
}