using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    public enum RespawnMode
    {
        Freeroam,
        Race
    }

    [Header("Respawn")]
    public RespawnMode respawnMode = RespawnMode.Freeroam;

    [Header("Current Respawn Point")]
    public Vector3 respawnPosition;
    public Quaternion respawnRotation = Quaternion.identity;

    [Header("Fallback")]
    public Transform fallbackRespawnPoint;

    [Header("Input")]
    public KeyCode respawnKey = KeyCode.R;
    public bool allowManualRespawn = true;

    [Header("Physics")]
    public bool clearVelocityOnRespawn = true;
    public float heightOffset = 0.5f;

    [Header("Debug")]
    public bool logRespawns = false;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (fallbackRespawnPoint != null)
        {
            SetRespawnPoint(fallbackRespawnPoint.position, fallbackRespawnPoint.rotation);
        }
        else
        {
            SetRespawnPoint(transform.position, transform.rotation);
        }
    }

    private void Update()
    {
        if (!allowManualRespawn)
            return;

        if (Input.GetKeyDown(respawnKey))
            Respawn();
    }

    public void ConfigureForFreeroam(Transform startPoint)
    {
        respawnMode = RespawnMode.Freeroam;

        if (startPoint != null)
            SetRespawnPoint(startPoint.position, startPoint.rotation);
        else
            SetRespawnPoint(transform.position, transform.rotation);

        if (logRespawns)
            Debug.Log("[PlayerRespawn] Configured for Freeroam.");
    }

    public void ConfigureForRace(Transform checkpointPoint)
    {
        respawnMode = RespawnMode.Race;

        if (checkpointPoint != null)
            SetRespawnPoint(checkpointPoint.position, checkpointPoint.rotation);
        else
            SetRespawnPoint(transform.position, transform.rotation);

        if (logRespawns)
            Debug.Log("[PlayerRespawn] Configured for Race.");
    }

    public void SetRespawnPoint(Vector3 position, Quaternion rotation)
    {
        respawnPosition = position;
        respawnRotation = rotation;
    }

    public void Respawn()
    {
        Vector3 finalPosition = respawnPosition + Vector3.up * heightOffset;

        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            if (!rb.isKinematic && clearVelocityOnRespawn)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            rb.Sleep();
        }

        transform.position = finalPosition;
        transform.rotation = respawnRotation;

        if (rb != null)
        {
            rb.WakeUp();

            if (!rb.isKinematic && clearVelocityOnRespawn)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        if (logRespawns)
        {
            Debug.Log(
                "[PlayerRespawn] Respawned to " +
                finalPosition +
                " | Mode=" +
                respawnMode
            );
        }
    }
}