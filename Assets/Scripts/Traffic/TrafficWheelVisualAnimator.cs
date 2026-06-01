using UnityEngine;

public class TrafficWheelVisualAnimator : MonoBehaviour
{
    [Header("References")]
    public Rigidbody rb;

    public Transform frontLeftSteer;
    public Transform frontRightSteer;

    public Transform frontLeftSpin;
    public Transform frontRightSpin;
    public Transform rearAxleSpin;

    [Header("Spin")]
    public bool spinWheels = true;
    public float wheelRadius = 0.35f;
    public Vector3 localSpinAxis = Vector3.right;

    [Header("Steer Visual")]
    public bool steerFrontWheels = true;
    public float maxVisualSteerAngle = 28f;
    public float steerLerpSpeed = 8f;

    [Header("Debug")]
    public bool logMissingReferences = false;

    private Vector3 previousForward;
    private float currentSteerAngle;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        previousForward = transform.forward;
    }

    private void LateUpdate()
    {
        if (rb == null)
        {
            if (logMissingReferences)
                Debug.LogWarning("TrafficWheelVisualAnimator: Rigidbody is missing on " + name);

            return;
        }

        if (spinWheels)
            UpdateWheelSpin();

        if (steerFrontWheels)
            UpdateFrontWheelSteer();

        previousForward = transform.forward;
    }

    private void UpdateWheelSpin()
    {
        float forwardSpeed = Vector3.Dot(rb.velocity, transform.forward);

        float safeRadius = Mathf.Max(0.05f, wheelRadius);
        float degreesPerSecond = (forwardSpeed / (2f * Mathf.PI * safeRadius)) * 360f;
        float spinDegrees = degreesPerSecond * Time.deltaTime;

        if (frontLeftSpin != null)
            frontLeftSpin.Rotate(localSpinAxis, spinDegrees, Space.Self);

        if (frontRightSpin != null)
            frontRightSpin.Rotate(localSpinAxis, spinDegrees, Space.Self);

        if (rearAxleSpin != null)
            rearAxleSpin.Rotate(localSpinAxis, spinDegrees, Space.Self);
    }

    private void UpdateFrontWheelSteer()
    {
        float yawDelta = Vector3.SignedAngle(previousForward, transform.forward, Vector3.up);

        float targetSteerAngle = Mathf.Clamp(
            yawDelta * 10f,
            -maxVisualSteerAngle,
            maxVisualSteerAngle
        );

        currentSteerAngle = Mathf.Lerp(
            currentSteerAngle,
            targetSteerAngle,
            steerLerpSpeed * Time.deltaTime
        );

        Quaternion steerRotation = Quaternion.Euler(0f, currentSteerAngle, 0f);

        if (frontLeftSteer != null)
            frontLeftSteer.localRotation = steerRotation;

        if (frontRightSteer != null)
            frontRightSteer.localRotation = steerRotation;
    }
}