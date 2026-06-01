using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FreeroamReturnManager : MonoBehaviour
{
    [System.Serializable]
    public class ReturnPoint
    {
        public string markerID;
        public Transform returnTransform;
    }

    [Header("Player")]
    public Transform playerCar;
    public CarController playerCarController;
    public Rigidbody playerRigidbody;

    [Header("Spawned Vehicle Source")]
    public SelectedVehicleSpawner selectedVehicleSpawner;

    [Header("Return Points")]
    public List<ReturnPoint> returnPoints = new List<ReturnPoint>();

    [Header("Mission Marker Fallback")]
    [Tooltip("If no manual return point matches, search MissionMarkerInteract objects by Return Marker ID or Race ID.")]
    public bool searchMissionMarkersIfReturnPointMissing = true;

    [Tooltip("When returning to a mission marker, offset the player slightly so they do not spawn inside the trigger.")]
    public float missionMarkerReturnForwardOffset = -6f;

    [Tooltip("Optional vertical offset when using a mission marker as the return point.")]
    public float missionMarkerReturnHeightOffset = 0.25f;

    [Header("Fallback")]
    public Transform fallbackReturnPoint;

    [Header("Loading Screen")]
    public LoadingScreenController loadingScreen;
    public float hideLoadingAfterReturnDelay = 0.25f;

    [Header("Timing")]
    public float returnDelay = 0.5f;
    public float playerFindTimeout = 5f;

    [Header("Ground Placement")]
    [Tooltip("Raycasts down from the return point so the car is placed on the ground instead of falling from above.")]
    public bool snapToGroundOnReturn = true;

    [Tooltip("Set this to your road/ground layer if possible. Leave as Everything for testing.")]
    public LayerMask groundLayerMask = ~0;

    [Tooltip("How high above the return point the ground raycast starts.")]
    public float groundRaycastHeight = 25f;

    [Tooltip("How far down the raycast checks for ground.")]
    public float groundRaycastDistance = 80f;

    [Tooltip("How far above the detected ground point the car is placed.")]
    public float groundHeightOffset = 0.6f;

    [Header("Debug")]
    public bool logDebugMessages = true;
    public bool drawGroundRayDebug = true;

    private bool returnInProgress = false;

    private IEnumerator Start()
    {
        yield return null;

        if (!RaceLaunchData.ReturningFromRace)
        {
            Log("Not returning from race. No return action needed.");
            yield break;
        }

        if (returnInProgress)
            yield break;

        returnInProgress = true;

        // Race return has priority over old garage return data.
        GarageSceneReturnData.HasFreeroamReturnPoint = false;

        Log("Returning from race. Target marker ID: " + RaceLaunchData.ReturnMarkerID);

        if (loadingScreen == null)
            loadingScreen = LoadingScreenController.Instance;

        if (loadingScreen != null)
            loadingScreen.ShowImmediate("Returning to Freeroam...");

        yield return new WaitForSeconds(returnDelay);

        // Let SelectedVehicleSpawner and other Start() methods finish first.
        yield return new WaitForEndOfFrame();

        yield return StartCoroutine(ReturnPlayerFromRaceRoutine());
    }

    public void ReturnPlayerFromRace()
    {
        if (returnInProgress)
            return;

        returnInProgress = true;
        StartCoroutine(ReturnPlayerFromRaceRoutine());
    }

    private IEnumerator ReturnPlayerFromRaceRoutine()
    {
        float timer = 0f;

        while (playerCar == null && timer < playerFindTimeout)
        {
            FindPlayerIfNeeded();

            if (playerCar != null)
                break;

            timer += Time.deltaTime;
            yield return null;
        }

        if (playerCar == null)
        {
            LogWarning("No player car assigned or found.");
            FinishReturnWithoutMoving();
            yield break;
        }

        if (!TryResolveReturnPoint(out Vector3 returnPosition, out Quaternion returnRotation))
        {
            LogWarning("No return point found. Player will not be moved. Marker ID was: " + RaceLaunchData.ReturnMarkerID);
            FinishReturnWithoutMoving();
            yield break;
        }

        Vector3 groundedPosition = GetGroundedReturnPosition(returnPosition);

        PlacePlayerAtReturnPoint(groundedPosition, returnRotation);

        Log(
            "Returned player to freeroam marker. Marker ID: " +
            RaceLaunchData.ReturnMarkerID +
            " | Position: " +
            groundedPosition
        );

        if (loadingScreen == null)
            loadingScreen = LoadingScreenController.Instance;

        if (loadingScreen != null)
            yield return loadingScreen.HideAfterDelay(hideLoadingAfterReturnDelay);

        RaceLaunchData.Clear();
        GarageSceneReturnData.HasFreeroamReturnPoint = false;
        returnInProgress = false;
    }

    private void FinishReturnWithoutMoving()
    {
        RaceLaunchData.Clear();
        GarageSceneReturnData.HasFreeroamReturnPoint = false;
        returnInProgress = false;

        if (loadingScreen == null)
            loadingScreen = LoadingScreenController.Instance;

        if (loadingScreen != null)
            StartCoroutine(loadingScreen.HideAfterDelay(hideLoadingAfterReturnDelay));
    }

    private void PlacePlayerAtReturnPoint(Vector3 position, Quaternion rotation)
    {
        FindPlayerIfNeeded();

        if (playerCar == null)
            return;

        if (playerCarController == null)
            playerCarController = playerCar.GetComponent<CarController>();

        if (playerRigidbody == null)
            playerRigidbody = playerCar.GetComponent<Rigidbody>();

        if (playerCarController != null)
            playerCarController.canDrive = false;

        if (playerRigidbody != null)
        {
            if (!playerRigidbody.isKinematic)
            {
                playerRigidbody.velocity = Vector3.zero;
                playerRigidbody.angularVelocity = Vector3.zero;
            }

            playerRigidbody.Sleep();
        }

        playerCar.position = position;
        playerCar.rotation = rotation;

        if (playerRigidbody != null)
        {
            playerRigidbody.WakeUp();

            if (!playerRigidbody.isKinematic)
            {
                playerRigidbody.velocity = Vector3.zero;
                playerRigidbody.angularVelocity = Vector3.zero;
            }
        }

        PlayerRespawn respawn = playerCar.GetComponent<PlayerRespawn>();

        if (respawn != null)
            respawn.SetRespawnPoint(playerCar.position, playerCar.rotation);

        if (playerCarController != null)
            playerCarController.canDrive = true;
    }

    private bool TryResolveReturnPoint(out Vector3 returnPosition, out Quaternion returnRotation)
    {
        returnPosition = Vector3.zero;
        returnRotation = Quaternion.identity;

        string targetMarkerID = RaceLaunchData.ReturnMarkerID;

        if (!string.IsNullOrWhiteSpace(targetMarkerID))
        {
            for (int i = 0; i < returnPoints.Count; i++)
            {
                ReturnPoint point = returnPoints[i];

                if (point == null)
                    continue;

                if (point.returnTransform == null)
                    continue;

                if (point.markerID == targetMarkerID)
                {
                    returnPosition = point.returnTransform.position;
                    returnRotation = point.returnTransform.rotation;

                    Log("Using manual return point: " + targetMarkerID);
                    return true;
                }
            }

            LogWarning("Could not find manual return point for marker ID: " + targetMarkerID);
        }

        if (searchMissionMarkersIfReturnPointMissing)
        {
            MissionMarkerInteract marker = FindMissionMarkerForReturnID(targetMarkerID);

            if (marker != null)
            {
                returnPosition =
                    marker.transform.position +
                    marker.transform.forward * missionMarkerReturnForwardOffset +
                    Vector3.up * missionMarkerReturnHeightOffset;

                returnRotation = marker.transform.rotation;

                Log("Using MissionMarkerInteract as return point: " + marker.name);
                return true;
            }
        }

        if (fallbackReturnPoint != null)
        {
            returnPosition = fallbackReturnPoint.position;
            returnRotation = fallbackReturnPoint.rotation;

            Log("Using fallback return point.");
            return true;
        }

        return false;
    }

    private MissionMarkerInteract FindMissionMarkerForReturnID(string targetMarkerID)
    {
        MissionMarkerInteract[] markers = FindObjectsOfType<MissionMarkerInteract>(true);

        for (int i = 0; i < markers.Length; i++)
        {
            MissionMarkerInteract marker = markers[i];

            if (marker == null)
                continue;

            if (!string.IsNullOrWhiteSpace(targetMarkerID))
            {
                if (marker.returnMarkerID == targetMarkerID)
                    return marker;

                if (marker.raceID == targetMarkerID)
                    return marker;

                if (marker.raceID + "_marker" == targetMarkerID)
                    return marker;
            }

            if (!string.IsNullOrWhiteSpace(RaceLaunchData.RaceID))
            {
                if (marker.raceID == RaceLaunchData.RaceID)
                    return marker;

                if (marker.returnMarkerID == RaceLaunchData.RaceID + "_marker")
                    return marker;
            }
        }

        return null;
    }

    private Vector3 GetGroundedReturnPosition(Vector3 basePosition)
    {
        if (!snapToGroundOnReturn)
            return basePosition;

        Vector3 rayOrigin = basePosition + Vector3.up * groundRaycastHeight;

        if (drawGroundRayDebug)
        {
            Debug.DrawRay(
                rayOrigin,
                Vector3.down * groundRaycastDistance,
                Color.green,
                2f
            );
        }

        bool hitGround = Physics.Raycast(
            rayOrigin,
            Vector3.down,
            out RaycastHit hit,
            groundRaycastDistance,
            groundLayerMask,
            QueryTriggerInteraction.Ignore
        );

        if (hitGround)
            return hit.point + Vector3.up * groundHeightOffset;

        LogWarning("Ground raycast failed. Using return point position instead.");
        return basePosition;
    }

    private void FindPlayerIfNeeded()
    {
        if (playerCar != null)
            return;

        if (selectedVehicleSpawner == null)
            selectedVehicleSpawner = FindObjectOfType<SelectedVehicleSpawner>();

        if (selectedVehicleSpawner != null)
        {
            GameObject spawned = selectedVehicleSpawner.GetSpawnedVehicle();

            if (spawned != null)
            {
                playerCar = spawned.transform;
                playerCarController = spawned.GetComponent<CarController>();
                playerRigidbody = spawned.GetComponent<Rigidbody>();

                Log("Found player from SelectedVehicleSpawner: " + spawned.name);
                return;
            }
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            playerCar = playerObject.transform;
            playerCarController = playerObject.GetComponent<CarController>();
            playerRigidbody = playerObject.GetComponent<Rigidbody>();

            Log("Found player by Player tag fallback: " + playerObject.name);
        }
    }

    private void Log(string message)
    {
        if (logDebugMessages)
            Debug.Log("[FreeroamReturnManager] " + message);
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning("[FreeroamReturnManager] " + message);
    }
}