using UnityEngine;

public class PlayerCheckpointTracker : MonoBehaviour
{
    [Header("References")]
    public PlayerRespawn playerRespawn;

    private void OnTriggerEnter(Collider other)
    {
        Checkpoint checkpoint = other.GetComponent<Checkpoint>();

        if (checkpoint == null)
            return;

        if (playerRespawn == null)
            return;

        Transform respawnTransform = checkpoint.respawnPoint != null
            ? checkpoint.respawnPoint
            : checkpoint.transform;

        playerRespawn.SetRespawnPoint(
            respawnTransform.position,
            respawnTransform.rotation
        );
    }
}