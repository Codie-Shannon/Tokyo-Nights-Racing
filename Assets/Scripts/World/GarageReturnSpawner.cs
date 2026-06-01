using UnityEngine;

public class GarageReturnSpawner : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Transform playerCar;

    [Header("Options")]
    [SerializeField] private bool clearVelocityOnReturn = true;

    private void Start()
    {
        // Race return has priority over garage return.
        if (RaceLaunchData.ReturningFromRace || RaceLaunchData.HasRaceLaunchData)
        {
            GarageSceneReturnData.HasFreeroamReturnPoint = false;
            return;
        }

        if (!GarageSceneReturnData.HasFreeroamReturnPoint)
            return;

        if (playerCar == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
                playerCar = playerObject.transform;
        }

        if (playerCar == null)
        {
            Debug.LogWarning("GarageReturnSpawner: No player car assigned or found with Player tag.");
            return;
        }

        playerCar.position = GarageSceneReturnData.FreeroamReturnPosition;
        playerCar.rotation = GarageSceneReturnData.FreeroamReturnRotation;

        if (clearVelocityOnReturn)
        {
            Rigidbody rb = playerCar.GetComponent<Rigidbody>();

            if (rb != null && !rb.isKinematic)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        GarageSceneReturnData.HasFreeroamReturnPoint = false;
    }
}