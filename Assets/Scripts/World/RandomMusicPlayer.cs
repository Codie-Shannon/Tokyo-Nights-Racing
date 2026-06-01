using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class RandomMusicPlayer : MonoBehaviour
{
    [Header("Playlist")]
    public List<AudioClip> songs = new List<AudioClip>();

    [Header("Options")]
    public bool playOnStart = true;
    public bool loopPlaylist = true;
    public bool dontRepeatUntilAllPlayed = true;

    private AudioSource audioSource;
    private List<int> remainingIndices = new List<int>();

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = false;
    }

    void Start()
    {
        RefillPool();

        if (playOnStart && songs.Count > 0)
        {
            PlayNextSong();
        }
    }

    void Update()
    {
        if (songs.Count == 0)
            return;

        if (!audioSource.isPlaying)
        {
            if (loopPlaylist || remainingIndices.Count > 0)
            {
                PlayNextSong();
            }
        }
    }

    void PlayNextSong()
    {
        if (songs.Count == 0)
            return;

        if (dontRepeatUntilAllPlayed)
        {
            if (remainingIndices.Count == 0)
            {
                if (!loopPlaylist)
                    return;

                RefillPool();
            }

            int randomPoolIndex = Random.Range(0, remainingIndices.Count);
            int songIndex = remainingIndices[randomPoolIndex];
            remainingIndices.RemoveAt(randomPoolIndex);

            audioSource.clip = songs[songIndex];
            audioSource.Play();
        }
        else
        {
            int songIndex = Random.Range(0, songs.Count);
            audioSource.clip = songs[songIndex];
            audioSource.Play();
        }
    }

    void RefillPool()
    {
        remainingIndices.Clear();

        for (int i = 0; i < songs.Count; i++)
        {
            remainingIndices.Add(i);
        }
    }
}