using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Vehicle Detection")]
    public CarController carController;
    public bool autoFindCarController = true;

    [Header("Camera Settings Database")]
    public VehicleCameraSettingsDatabase cameraSettingsDatabase;

    [Tooltip("Used if no database is assigned, no VehicleData override exists, or no matching database variant exists.")]
    public VehicleCameraSettings localFallbackSettings = new VehicleCameraSettings();

    [Header("Distance Modes")]
    public bool enableDistanceModes = true;
    public KeyCode cycleDistanceKey = KeyCode.V;
    public float[] distanceMultipliers = new float[] { 0.85f, 1.0f, 1.2f };
    public int startingDistanceMode = 1;
    public float distanceModeSmoothness = 4f;

    [Header("Mouse Wheel Zoom")]
    public bool enableMouseWheelZoom = true;
    public float mouseWheelZoomSpeed = 0.1f;
    public float minZoomMultiplier = 0.75f;
    public float maxZoomMultiplier = 1.3f;

    [Header("Follow Tuning")]
    public float highSpeedFollowMultiplier = 1.2f;

    [Header("Rotation")]
    public bool followOnlyYaw = true;
    public bool smoothTargetYaw = true;
    public float yawSmoothness = 8f;

    [Header("Reverse Camera")]
    public bool enableReverseCamera = false;
    public float reverseCameraBlendSpeed = 2.5f;
    public float reverseEnterSpeedKPH = 8f;
    public float reverseExitSpeedKPH = 3f;
    public float reverseEnterDelay = 0.35f;
    public float reverseExitDelay = 0.2f;

    [Header("Manual Look Back")]
    public bool enableManualLookBack = true;
    public KeyCode lookBackKey = KeyCode.C;
    public float lookBackBlendSpeed = 7f;
    public bool manualLookBackOverridesReverse = true;

    [Header("Look Ahead")]
    public float lookAheadSmoothness = 2.5f;

    [Range(0f, 1f)]
    public float velocityLookAheadBlend = 0.1f;

    [Header("Turn Framing")]
    public float turnShiftSmoothness = 2.5f;
    public float highSpeedTurnShiftReduction = 0.7f;

    [Header("Camera Roll")]
    public float rollSmoothness = 2.5f;

    [Header("Speed Pullback")]
    public float speedPullbackSmoothness = 2.2f;

    [Header("FOV")]
    public Camera cam;
    public float maxSpeedForFOV = 160f;
    public float fovSmoothness = 3f;

    [Header("Camera Shake")]
    public bool enableCameraShake = true;
    public float shakeDecaySpeed = 4.5f;
    public float shakePositionAmount = 0.16f;
    public float shakeRotationAmount = 1.2f;

    [Header("Startup")]
    public bool snapToTargetOnStart = true;

    [Header("Debug")]
    public bool logCameraPresetChanges = true;

    private Rigidbody targetRb;

    private VehicleData activeVehicleData;
    private VehicleType activeVehicleType = VehicleType.Road;
    private VehicleCameraSettings activeSettings = new VehicleCameraSettings();

    private int currentDistanceMode;
    private float currentDistanceMultiplier = 1f;
    private float manualZoomMultiplier = 1f;

    private float smoothedTargetYaw;
    private bool yawInitialized;

    private Vector3 currentLookAhead;
    private float currentTurnShift;
    private float currentRoll;
    private float currentSpeedPullback;

    private float reverseBlend;
    private float manualLookBackBlend;
    private float reverseEnterTimer;
    private float reverseExitTimer;
    private bool stableReverseCamera;

    private float shakeTrauma;
    private Vector3 shakePositionOffset;
    private Vector3 shakeRotationOffset;

    private void Start()
    {
        SetupTargetReferences();

        if (cam == null)
        {
            cam = GetComponent<Camera>();
        }

        SetupDistanceMode();

        ApplyBestCameraSettingsForCurrentTarget();

        if (snapToTargetOnStart && target != null)
        {
            SnapToTarget();
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        HandleDistanceInput();
        HandleLookBackInput();

        Quaternion targetYawRotation = GetStableTargetRotation();

        float speedKPH = 0f;
        float forwardSpeedKPH = 0f;
        Vector3 flatVelocity = Vector3.zero;

        if (targetRb != null)
        {
            flatVelocity = targetRb.velocity;
            flatVelocity.y = 0f;

            speedKPH = flatVelocity.magnitude * 3.6f;

            Vector3 localVelocity = target.InverseTransformDirection(targetRb.velocity);
            forwardSpeedKPH = localVelocity.z * 3.6f;
        }

        UpdateReverseCamera(forwardSpeedKPH);

        float lookBackBlend = Mathf.Max(reverseBlend, manualLookBackBlend);
        float speed01 = Mathf.Clamp01(speedKPH / maxSpeedForFOV);

        float steerInput = Input.GetAxis("Horizontal");

        float turnReduction = Mathf.Lerp(1f, highSpeedTurnShiftReduction, speed01);
        float desiredTurnShift = steerInput * activeSettings.turnShiftAmount * turnReduction;

        if (lookBackBlend > 0.5f)
        {
            desiredTurnShift *= -1f;
        }

        currentTurnShift = Mathf.Lerp(
            currentTurnShift,
            desiredTurnShift,
            turnShiftSmoothness * Time.deltaTime
        );

        float desiredPullback = speed01 * activeSettings.speedPullbackAmount;

        currentSpeedPullback = Mathf.Lerp(
            currentSpeedPullback,
            desiredPullback,
            speedPullbackSmoothness * Time.deltaTime
        );

        float finalDistanceMultiplier = currentDistanceMultiplier * manualZoomMultiplier;

        Vector3 scaledBaseOffset = ScaleOffsetDistance(activeSettings.offset, finalDistanceMultiplier);
        Vector3 scaledReverseOffset = ScaleOffsetDistance(activeSettings.reverseOffset, finalDistanceMultiplier);

        Vector3 normalDynamicOffset =
            scaledBaseOffset + new Vector3(currentTurnShift, 0f, -currentSpeedPullback);

        Vector3 reverseDynamicOffset =
            scaledReverseOffset + new Vector3(-currentTurnShift, 0f, currentSpeedPullback);

        Vector3 blendedOffset = Vector3.Lerp(
            normalDynamicOffset,
            reverseDynamicOffset,
            lookBackBlend
        );

        Vector3 desiredPosition = target.position + targetYawRotation * blendedOffset;

        float followSpeed = Mathf.Lerp(
            activeSettings.followSpeed,
            activeSettings.followSpeed * highSpeedFollowMultiplier,
            speed01
        );

        UpdateShake();

        Vector3 smoothedPosition = Vector3.Lerp(
            transform.position,
            desiredPosition,
            followSpeed * Time.deltaTime
        );

        transform.position = smoothedPosition + shakePositionOffset;

        Vector3 normalLookAhead = target.forward * activeSettings.lookAheadDistance;

        if (flatVelocity.sqrMagnitude > 1f && velocityLookAheadBlend > 0f)
        {
            Vector3 velocityDirection = flatVelocity.normalized;

            Vector3 blendedDirection = Vector3.Slerp(
                target.forward,
                velocityDirection,
                velocityLookAheadBlend
            );

            normalLookAhead = blendedDirection.normalized * activeSettings.lookAheadDistance;
        }

        Vector3 reverseLookAhead =
            -target.forward * activeSettings.reverseLookBehindDistance;

        Vector3 desiredLookAhead = Vector3.Lerp(
            normalLookAhead,
            reverseLookAhead,
            lookBackBlend
        );

        currentLookAhead = Vector3.Lerp(
            currentLookAhead,
            desiredLookAhead,
            lookAheadSmoothness * Time.deltaTime
        );

        Vector3 lookTarget = target.TransformPoint(activeSettings.lookAtOffset) + currentLookAhead;

        Vector3 toLookTarget = lookTarget - transform.position;

        if (toLookTarget.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion desiredRotation = Quaternion.LookRotation(
            toLookTarget.normalized,
            Vector3.up
        );

        float desiredRoll = -steerInput * activeSettings.cameraRollAmount;

        if (lookBackBlend > 0.5f)
        {
            desiredRoll *= -1f;
        }

        currentRoll = Mathf.Lerp(
            currentRoll,
            desiredRoll,
            rollSmoothness * Time.deltaTime
        );

        Quaternion finalRotation =
            desiredRotation *
            Quaternion.Euler(
                shakeRotationOffset.x,
                shakeRotationOffset.y,
                currentRoll + shakeRotationOffset.z
            );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            finalRotation,
            activeSettings.rotationSpeed * Time.deltaTime
        );

        UpdateFOV(activeSettings.baseFOV, activeSettings.maxFOV, speed01, lookBackBlend);
    }

    private void SetupTargetReferences()
    {
        if (target == null)
        {
            return;
        }

        targetRb = target.GetComponent<Rigidbody>();

        if (autoFindCarController && carController == null)
        {
            carController = target.GetComponent<CarController>();

            if (carController == null)
                carController = target.GetComponentInChildren<CarController>(true);
        }
    }

    private void SetupDistanceMode()
    {
        if (distanceMultipliers == null || distanceMultipliers.Length == 0)
        {
            distanceMultipliers = new float[] { 1f };
        }

        currentDistanceMode = Mathf.Clamp(
            startingDistanceMode,
            0,
            distanceMultipliers.Length - 1
        );

        currentDistanceMultiplier = distanceMultipliers[currentDistanceMode];
    }

    public void ApplyVehicleData(VehicleData vehicleData)
    {
        activeVehicleData = vehicleData;

        VehicleType vehicleType = GetCurrentVehicleType();
        activeVehicleType = vehicleType;

        bool usedVehicleDataOverride = false;
        string source = "Local Fallback";

        if (vehicleData != null && vehicleData.overrideCameraSettings && vehicleData.cameraSettings != null)
        {
            activeSettings.CopyFrom(vehicleData.cameraSettings);
            usedVehicleDataOverride = true;
            source = "VehicleData Direct Override";
        }
        else
        {
            VehicleCameraSettings databaseSettings = null;

            if (cameraSettingsDatabase != null)
                databaseSettings = cameraSettingsDatabase.GetSettingsForVehicle(vehicleData, vehicleType, out source);

            if (databaseSettings != null)
                activeSettings.CopyFrom(databaseSettings);
            else
                activeSettings.CopyFrom(localFallbackSettings);
        }

        if (cam != null)
            cam.fieldOfView = activeSettings.baseFOV;

        if (logCameraPresetChanges)
        {
            string vehicleName = vehicleData != null ? vehicleData.displayName : "No VehicleData";

            Debug.Log(
                "CameraFollow: Applied camera settings. " +
                "Vehicle=" + vehicleName +
                ", VehicleType=" + activeVehicleType +
                ", Source=" + source +
                ", DirectOverride=" + usedVehicleDataOverride
            );
        }

        if (snapToTargetOnStart && target != null)
            SnapToTarget();
    }

    private void ApplyBestCameraSettingsForCurrentTarget()
    {
        VehicleType vehicleType = GetCurrentVehicleType();
        activeVehicleType = vehicleType;

        string source = "Local Fallback";
        VehicleCameraSettings databaseSettings = null;

        if (cameraSettingsDatabase != null)
            databaseSettings = cameraSettingsDatabase.GetSettingsForVehicle(activeVehicleData, vehicleType, out source);

        if (databaseSettings != null)
            activeSettings.CopyFrom(databaseSettings);
        else
            activeSettings.CopyFrom(localFallbackSettings);
    }

    private VehicleType GetCurrentVehicleType()
    {
        if (activeVehicleData != null)
            return activeVehicleData.vehicleType;

        CarProfile profile = null;

        if (target != null)
            profile = target.GetComponent<CarProfile>();

        if (profile == null && target != null)
            profile = target.GetComponentInChildren<CarProfile>(true);

        if (profile != null)
            return profile.vehicleType;

        if (carController != null)
            return carController.vehicleType;

        return VehicleType.Road;
    }

    private Quaternion GetStableTargetRotation()
    {
        if (!followOnlyYaw)
        {
            return target.rotation;
        }

        float rawYaw = target.eulerAngles.y;

        if (!yawInitialized)
        {
            smoothedTargetYaw = rawYaw;
            yawInitialized = true;
        }

        if (smoothTargetYaw)
        {
            smoothedTargetYaw = Mathf.LerpAngle(
                smoothedTargetYaw,
                rawYaw,
                yawSmoothness * Time.deltaTime
            );
        }
        else
        {
            smoothedTargetYaw = rawYaw;
        }

        return Quaternion.Euler(0f, smoothedTargetYaw, 0f);
    }

    private void UpdateReverseCamera(float forwardSpeedKPH)
    {
        bool wantsReverseCamera =
            enableReverseCamera &&
            forwardSpeedKPH < -reverseEnterSpeedKPH;

        bool wantsForwardCamera =
            !enableReverseCamera ||
            forwardSpeedKPH > -reverseExitSpeedKPH;

        if (manualLookBackOverridesReverse && manualLookBackBlend > 0.1f)
        {
            wantsReverseCamera = false;
            wantsForwardCamera = true;
        }

        if (wantsReverseCamera)
        {
            reverseEnterTimer += Time.deltaTime;
            reverseExitTimer = 0f;

            if (reverseEnterTimer >= reverseEnterDelay)
            {
                stableReverseCamera = true;
            }
        }
        else if (wantsForwardCamera)
        {
            reverseExitTimer += Time.deltaTime;
            reverseEnterTimer = 0f;

            if (reverseExitTimer >= reverseExitDelay)
            {
                stableReverseCamera = false;
            }
        }

        reverseBlend = Mathf.Lerp(
            reverseBlend,
            stableReverseCamera ? 1f : 0f,
            reverseCameraBlendSpeed * Time.deltaTime
        );
    }

    private void HandleDistanceInput()
    {
        if (enableDistanceModes && Input.GetKeyDown(cycleDistanceKey))
        {
            currentDistanceMode++;

            if (currentDistanceMode >= distanceMultipliers.Length)
            {
                currentDistanceMode = 0;
            }
        }

        float targetDistanceMultiplier = distanceMultipliers[currentDistanceMode];

        currentDistanceMultiplier = Mathf.Lerp(
            currentDistanceMultiplier,
            targetDistanceMultiplier,
            distanceModeSmoothness * Time.deltaTime
        );

        if (enableMouseWheelZoom)
        {
            float scroll = Input.mouseScrollDelta.y;

            if (Mathf.Abs(scroll) > 0.01f)
            {
                manualZoomMultiplier -= scroll * mouseWheelZoomSpeed;
                manualZoomMultiplier = Mathf.Clamp(
                    manualZoomMultiplier,
                    minZoomMultiplier,
                    maxZoomMultiplier
                );
            }
        }
    }

    private void HandleLookBackInput()
    {
        if (!enableManualLookBack)
        {
            manualLookBackBlend = Mathf.Lerp(
                manualLookBackBlend,
                0f,
                lookBackBlendSpeed * Time.deltaTime
            );

            return;
        }

        bool lookingBack = Input.GetKey(lookBackKey);

        manualLookBackBlend = Mathf.Lerp(
            manualLookBackBlend,
            lookingBack ? 1f : 0f,
            lookBackBlendSpeed * Time.deltaTime
        );
    }

    private Vector3 ScaleOffsetDistance(Vector3 offset, float multiplier)
    {
        Vector3 scaled = offset;

        scaled.x *= multiplier;
        scaled.z *= multiplier;

        scaled.y *= Mathf.Lerp(1f, multiplier, 0.35f);

        return scaled;
    }

    private void UpdateShake()
    {
        if (!enableCameraShake || shakeTrauma <= 0f)
        {
            shakePositionOffset = Vector3.zero;
            shakeRotationOffset = Vector3.zero;
            shakeTrauma = Mathf.Max(0f, shakeTrauma - shakeDecaySpeed * Time.deltaTime);
            return;
        }

        shakeTrauma = Mathf.Clamp01(shakeTrauma);

        float shakePower = shakeTrauma * shakeTrauma;

        shakePositionOffset = new Vector3(
            Random.Range(-1f, 1f) * shakePositionAmount,
            Random.Range(-1f, 1f) * shakePositionAmount,
            0f
        ) * shakePower;

        shakeRotationOffset = new Vector3(
            Random.Range(-1f, 1f) * shakeRotationAmount,
            Random.Range(-1f, 1f) * shakeRotationAmount,
            Random.Range(-1f, 1f) * shakeRotationAmount
        ) * shakePower;

        shakeTrauma = Mathf.Max(0f, shakeTrauma - shakeDecaySpeed * Time.deltaTime);
    }

    private void UpdateFOV(float baseFOV, float maxFOV, float speed01, float lookBackBlend)
    {
        if (cam == null)
        {
            return;
        }

        float targetFOV = Mathf.Lerp(baseFOV, maxFOV, speed01);

        if (lookBackBlend > 0.5f)
        {
            targetFOV -= 2f;
        }

        cam.fieldOfView = Mathf.Lerp(
            cam.fieldOfView,
            targetFOV,
            fovSmoothness * Time.deltaTime
        );
    }

    public void AddCameraShake(float amount)
    {
        if (!enableCameraShake)
        {
            return;
        }

        shakeTrauma = Mathf.Clamp01(shakeTrauma + amount);
    }

    public void SetCameraDistanceMode(int mode)
    {
        if (distanceMultipliers == null || distanceMultipliers.Length == 0)
        {
            return;
        }

        currentDistanceMode = Mathf.Clamp(
            mode,
            0,
            distanceMultipliers.Length - 1
        );
    }

    public void ResetManualZoom()
    {
        manualZoomMultiplier = 1f;
    }

    public void SnapToTarget()
    {
        if (target == null)
        {
            return;
        }

        Vector3 offset = activeSettings.offset;

        Quaternion targetYawRotation = followOnlyYaw
            ? Quaternion.Euler(0f, target.eulerAngles.y, 0f)
            : target.rotation;

        transform.position = target.position + targetYawRotation * offset;

        Vector3 lookTarget =
            target.TransformPoint(activeSettings.lookAtOffset) +
            target.forward * activeSettings.lookAheadDistance;

        transform.rotation = Quaternion.LookRotation(
            lookTarget - transform.position,
            Vector3.up
        );

        if (cam != null)
        {
            cam.fieldOfView = activeSettings.baseFOV;
        }

        smoothedTargetYaw = target.eulerAngles.y;
        yawInitialized = true;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        targetRb = target != null ? target.GetComponent<Rigidbody>() : null;

        if (target != null && autoFindCarController)
        {
            carController = target.GetComponent<CarController>();

            if (carController == null)
                carController = target.GetComponentInChildren<CarController>(true);
        }
        else
        {
            carController = null;
        }

        yawInitialized = false;

        ApplyBestCameraSettingsForCurrentTarget();

        if (snapToTargetOnStart)
        {
            SnapToTarget();
        }
    }
}
