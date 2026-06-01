using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Header("Checkpoint")]
    public int checkpointIndex;

    [Header("Player Detection")]
    public string playerTag = "Player";

    [Header("Optional Respawn Override")]
    public Transform respawnPoint;

    private void OnTriggerEnter(Collider other)
    {
        RacerProgress progress = other.GetComponentInParent<RacerProgress>();

        if (progress != null)
        {
            progress.HitCheckpoint(checkpointIndex);
        }

        if (!other.CompareTag(playerTag))
            return;

        if (RaceManager.Instance == null)
        {
            Debug.LogWarning("Checkpoint hit by player, but RaceManager.Instance is missing.");
            return;
        }

        Transform checkpointTransformToUse = respawnPoint != null ? respawnPoint : transform;
        RaceManager.Instance.HitCheckpoint(checkpointIndex, checkpointTransformToUse);
    }
}