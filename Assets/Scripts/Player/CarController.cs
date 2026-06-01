using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    [Header("Vehicle Type")]
    public VehicleType vehicleType = VehicleType.Road;
    public bool useVehicleTypeDefaultsOnStart = false;

    [Header("Speed")]
    public float acceleration = 13f;
    public float reverseAcceleration = 10f;
    public float maxSpeedKPH = 80f;
    public float reverseMaxSpeedKPH = 35f;
    public float coastingDrag = 0.5f;
    public float brakingDrag = 2f;

    [Header("Braking / Reverse")]
    [Tooltip("Drag applied while holding reverse when still moving forward.")]
    public float reverseBrakeDrag = 1.2f;

    [Tooltip("Gentle braking force applied while holding reverse before reversing.")]
    public float reverseBrakeForce = 4f;

    [Tooltip("Car must be slower than this before reverse actually engages.")]
    public float reverseEngageSpeedKPH = 3f;

    [Header("Steering")]
    public float steeringPower = 130f;
    public float steeringAtHighSpeed = 60f;
    public float highSpeedSteeringStartKPH = 35f;
    public float highSpeedSteeringEndKPH = 90f;
    public float steerResponse = 7f;

    [Tooltip("Lower = less sharp steering while reversing.")]
    public float reverseSteeringMultiplier = 0.35f;

    [Header("Grip")]
    public float sideGripStraight = 8f;
    public float sideGripTurning = 5.2f;
    public float offRoadGripMultiplier = 0.85f;
    public float monsterTruckGripMultiplier = 0.75f;

    [Header("Ground Check")]
    public LayerMask groundLayers = 3;
    public Transform groundCheckPoint;
    public float groundCheckRadius = 0.45f;

    [Header("Stability")]
    public Vector3 centerOfMassOffset = new Vector3(0f, -0.75f, 0f);
    public float antiRollStrength = 3.5f;
    public float uprightAssistStrength = 1.5f;
    public float monsterTruckUprightAssist = 3f;

    [Header("Airborne")]
    [Tooltip("Drag used while airborne. Keep low so the car does not stop mid-air.")]
    public float airDrag = 0.02f;

    [Tooltip("If false, there is no steering, acceleration, or air control while airborne.")]
    public bool allowAirControl = false;

    [Tooltip("Only used if Allow Air Control is enabled.")]
    public float airControlTorque = 2f;

    [Header("Monster Truck")]
    public bool monsterTruckMode;
    public float monsterTruckAccelerationMultiplier = 1.25f;
    public float monsterTruckMaxSpeedMultiplier = 0.75f;
    public float monsterTruckSteeringMultiplier = 0.8f;
    public float monsterTruckBounceDamping = 0.92f;
    public float monsterTruckObstacleClimbForce = 6f;

    [Header("Wheel Visuals")]
    public Transform frontLeftSteer;
    public Transform frontRightSteer;
    public Transform frontLeftSpin;
    public Transform frontRightSpin;
    public Transform rearLeftSpin;
    public Transform rearRightSpin;
    public float wheelTurnAngle = 32f;
    public float wheelSpinDegreesPerMeter = 540f;

    [Header("Body Lean")]
    public Transform bodyVisual;
    public float bodyRollAmount = 5f;
    public float bodyPitchAmount = 2f;
    public float bodyLeanSmoothing = 7f;

    [Header("Options")]
    public bool canDrive = true;

    private Rigidbody rb;
    private float moveInput;
    private float steerInput;
    private float smoothedSteerInput;
    private float wheelSpinAngle;
    private Quaternion bodyStartLocalRotation;
    private bool isGrounded;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (useVehicleTypeDefaultsOnStart)
            ApplyVehicleTypeDefaults();

        rb.centerOfMass = centerOfMassOffset;

        if (bodyVisual != null)
            bodyStartLocalRotation = bodyVisual.localRotation;
    }

    void Update()
    {
        if (!canDrive)
        {
            moveInput = 0f;
            steerInput = 0f;
            smoothedSteerInput = 0f;
            return;
        }

        moveInput = Input.GetAxis("Vertical");
        steerInput = Input.GetAxis("Horizontal");

        smoothedSteerInput = Mathf.Lerp(
            smoothedSteerInput,
            steerInput,
            steerResponse * Time.deltaTime
        );

        UpdateWheelVisuals();
        UpdateBodyLean();
    }

    void FixedUpdate()
    {
        isGrounded = CheckGrounded();

        if (!canDrive)
            return;

        if (isGrounded)
        {
            Move();
            Steer();
            Grip();
            ApplyAntiRoll();
            ApplyUprightAssist();

            if (monsterTruckMode || vehicleType == VehicleType.MonsterTruck)
                ApplyMonsterTruckGroundAssist();
        }
        else
        {
            ApplyAirborneDrag();

            if (allowAirControl)
                ApplyAirControl();
        }
    }

    public void ApplyVehicleTypeDefaults()
    {
        monsterTruckMode = false;

        switch (vehicleType)
        {
            case VehicleType.Road:
                acceleration = 13f;
                reverseAcceleration = 10f;
                maxSpeedKPH = 80f;
                reverseMaxSpeedKPH = 35f;

                coastingDrag = 0.5f;
                brakingDrag = 2f;
                reverseBrakeDrag = 1.2f;
                reverseBrakeForce = 4f;
                reverseEngageSpeedKPH = 3f;

                steeringPower = 130f;
                steeringAtHighSpeed = 60f;
                highSpeedSteeringStartKPH = 35f;
                highSpeedSteeringEndKPH = 90f;
                steerResponse = 7f;
                reverseSteeringMultiplier = 0.35f;

                sideGripStraight = 8f;
                sideGripTurning = 5.2f;

                centerOfMassOffset = new Vector3(0f, -0.75f, 0f);
                antiRollStrength = 3.5f;
                uprightAssistStrength = 1.5f;

                bodyRollAmount = 5f;
                bodyPitchAmount = 2f;
                break;

            case VehicleType.OffRoad:
                acceleration = 12f;
                reverseAcceleration = 10f;
                maxSpeedKPH = 70f;
                reverseMaxSpeedKPH = 35f;

                coastingDrag = 0.6f;
                brakingDrag = 2.2f;
                reverseBrakeDrag = 1.1f;
                reverseBrakeForce = 3.8f;
                reverseEngageSpeedKPH = 3f;

                steeringPower = 125f;
                steeringAtHighSpeed = 65f;
                highSpeedSteeringStartKPH = 30f;
                highSpeedSteeringEndKPH = 80f;
                steerResponse = 6f;
                reverseSteeringMultiplier = 0.35f;

                sideGripStraight = 7f;
                sideGripTurning = 4.8f;

                centerOfMassOffset = new Vector3(0f, -0.65f, 0f);
                antiRollStrength = 3.8f;
                uprightAssistStrength = 2f;

                bodyRollAmount = 7f;
                bodyPitchAmount = 3f;
                break;

            case VehicleType.AllTerrain:
                acceleration = 13f;
                reverseAcceleration = 11f;
                maxSpeedKPH = 75f;
                reverseMaxSpeedKPH = 38f;

                coastingDrag = 0.55f;
                brakingDrag = 2.2f;
                reverseBrakeDrag = 1.1f;
                reverseBrakeForce = 4f;
                reverseEngageSpeedKPH = 3f;

                steeringPower = 130f;
                steeringAtHighSpeed = 62f;
                highSpeedSteeringStartKPH = 35f;
                highSpeedSteeringEndKPH = 85f;
                steerResponse = 6.5f;
                reverseSteeringMultiplier = 0.35f;

                sideGripStraight = 7.8f;
                sideGripTurning = 5f;

                centerOfMassOffset = new Vector3(0f, -0.7f, 0f);
                antiRollStrength = 4.2f;
                uprightAssistStrength = 2.2f;

                bodyRollAmount = 6f;
                bodyPitchAmount = 2.8f;
                break;

            case VehicleType.MonsterTruck:
                monsterTruckMode = true;

                acceleration = 15f;
                reverseAcceleration = 12f;
                maxSpeedKPH = 60f;
                reverseMaxSpeedKPH = 30f;

                coastingDrag = 0.7f;
                brakingDrag = 2.4f;
                reverseBrakeDrag = 1.0f;
                reverseBrakeForce = 3.5f;
                reverseEngageSpeedKPH = 3f;

                steeringPower = 95f;
                steeringAtHighSpeed = 45f;
                highSpeedSteeringStartKPH = 25f;
                highSpeedSteeringEndKPH = 65f;
                steerResponse = 5f;
                reverseSteeringMultiplier = 0.25f;

                sideGripStraight = 5.8f;
                sideGripTurning = 4.2f;

                centerOfMassOffset = new Vector3(0f, -1.2f, 0f);
                antiRollStrength = 7f;
                uprightAssistStrength = 3.5f;

                bodyRollAmount = 10f;
                bodyPitchAmount = 5f;

                wheelTurnAngle = 36f;
                wheelSpinDegreesPerMeter = 360f;
                break;
        }

        if (rb != null)
            rb.centerOfMass = centerOfMassOffset;
    }

    bool CheckGrounded()
    {
        if (groundCheckPoint == null)
            return true;

        return Physics.CheckSphere(
            groundCheckPoint.position,
            groundCheckRadius,
            groundLayers,
            QueryTriggerInteraction.Ignore
        );
    }

    void Move()
    {
        Vector3 localVelocity = transform.InverseTransformDirection(rb.velocity);
        float forwardSpeedMS = localVelocity.z;

        float currentAcceleration = acceleration;
        float currentMaxSpeedKPH = maxSpeedKPH;

        if (monsterTruckMode || vehicleType == VehicleType.MonsterTruck)
        {
            currentAcceleration *= monsterTruckAccelerationMultiplier;
            currentMaxSpeedKPH *= monsterTruckMaxSpeedMultiplier;
        }

        float maxForwardSpeedMS = KPHToMS(currentMaxSpeedKPH);
        float maxReverseSpeedMS = KPHToMS(reverseMaxSpeedKPH);

        if (moveInput > 0f)
        {
            if (forwardSpeedMS < maxForwardSpeedMS)
            {
                rb.AddForce(
                    transform.forward * moveInput * currentAcceleration,
                    ForceMode.Acceleration
                );
            }

            rb.drag = 0f;
        }
        else if (moveInput < 0f)
        {
            if (forwardSpeedMS > KPHToMS(reverseEngageSpeedKPH))
            {
                rb.AddForce(
                    -transform.forward * reverseBrakeForce,
                    ForceMode.Acceleration
                );

                rb.drag = reverseBrakeDrag;
            }
            else
            {
                if (forwardSpeedMS > -maxReverseSpeedMS)
                {
                    rb.AddForce(
                        transform.forward * moveInput * reverseAcceleration,
                        ForceMode.Acceleration
                    );
                }

                rb.drag = 0f;
            }
        }
        else
        {
            rb.drag = coastingDrag;
        }

        ClampSpeed(currentMaxSpeedKPH);
    }

    void Steer()
    {
        float speedKPH = GetSpeedKPH();

        if (speedKPH < 1f)
            return;

        float t = Mathf.InverseLerp(
            highSpeedSteeringStartKPH,
            highSpeedSteeringEndKPH,
            speedKPH
        );

        float currentSteering = Mathf.Lerp(steeringPower, steeringAtHighSpeed, t);

        if (monsterTruckMode || vehicleType == VehicleType.MonsterTruck)
            currentSteering *= monsterTruckSteeringMultiplier;

        Vector3 localVelocity = transform.InverseTransformDirection(rb.velocity);
        bool reversing = localVelocity.z < -0.5f;

        float direction = reversing ? -1f : 1f;

        if (reversing)
            currentSteering *= reverseSteeringMultiplier;

        float turn = smoothedSteerInput * currentSteering * direction * Time.fixedDeltaTime;

        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, turn, 0f));
    }

    void Grip()
    {
        Vector3 localVelocity = transform.InverseTransformDirection(rb.velocity);

        float currentGrip = Mathf.Abs(smoothedSteerInput) > 0.1f
            ? sideGripTurning
            : sideGripStraight;

        if (vehicleType == VehicleType.OffRoad)
            currentGrip *= offRoadGripMultiplier;

        if (vehicleType == VehicleType.MonsterTruck || monsterTruckMode)
            currentGrip *= monsterTruckGripMultiplier;

        localVelocity.x = Mathf.Lerp(
            localVelocity.x,
            0f,
            currentGrip * Time.fixedDeltaTime
        );

        rb.velocity = transform.TransformDirection(localVelocity);
    }

    void ApplyAntiRoll()
    {
        if (antiRollStrength <= 0f)
            return;

        Vector3 localAngularVelocity = transform.InverseTransformDirection(rb.angularVelocity);

        localAngularVelocity.z = Mathf.Lerp(
            localAngularVelocity.z,
            0f,
            antiRollStrength * Time.fixedDeltaTime
        );

        rb.angularVelocity = transform.TransformDirection(localAngularVelocity);
    }

    void ApplyUprightAssist()
    {
        float assist = uprightAssistStrength;

        if (monsterTruckMode || vehicleType == VehicleType.MonsterTruck)
            assist = monsterTruckUprightAssist;

        if (assist <= 0f)
            return;

        Vector3 predictedUp = rb.rotation * Vector3.up;
        Vector3 torqueVector = Vector3.Cross(predictedUp, Vector3.up);

        rb.AddTorque(torqueVector * assist, ForceMode.Acceleration);
    }

    void ApplyMonsterTruckGroundAssist()
    {
        Vector3 localVelocity = transform.InverseTransformDirection(rb.velocity);

        if (localVelocity.y > 0f)
            localVelocity.y *= monsterTruckBounceDamping;

        rb.velocity = transform.TransformDirection(localVelocity);

        if (moveInput > 0.1f && rb.velocity.magnitude < 8f)
        {
            rb.AddForce(
                (transform.forward + Vector3.up * 0.15f) * monsterTruckObstacleClimbForce,
                ForceMode.Acceleration
            );
        }
    }

    void ApplyAirborneDrag()
    {
        rb.drag = airDrag;
    }

    void ApplyAirControl()
    {
        float pitchInput = -moveInput;
        float rollInput = -steerInput;

        Vector3 torque =
            transform.right * pitchInput * airControlTorque +
            transform.forward * rollInput * airControlTorque;

        rb.AddTorque(torque, ForceMode.Acceleration);
    }

    void ClampSpeed(float currentMaxSpeedKPH)
    {
        Vector3 flatVelocity = rb.velocity;
        flatVelocity.y = 0f;

        float maxFlatSpeed = KPHToMS(currentMaxSpeedKPH);

        if (flatVelocity.magnitude > maxFlatSpeed)
        {
            Vector3 limited = flatVelocity.normalized * maxFlatSpeed;
            rb.velocity = new Vector3(limited.x, rb.velocity.y, limited.z);
        }
    }

    void UpdateWheelVisuals()
    {
        float steerVisual = smoothedSteerInput * wheelTurnAngle;

        if (frontLeftSteer != null)
            frontLeftSteer.localRotation = Quaternion.Euler(0f, steerVisual, 0f);

        if (frontRightSteer != null)
            frontRightSteer.localRotation = Quaternion.Euler(0f, steerVisual, 0f);

        float speed = rb != null ? rb.velocity.magnitude : 0f;
        float direction = 1f;

        if (rb != null)
        {
            Vector3 localVelocity = transform.InverseTransformDirection(rb.velocity);

            if (Mathf.Abs(localVelocity.z) > 0.1f)
                direction = Mathf.Sign(localVelocity.z);
        }

        wheelSpinAngle += speed * direction * wheelSpinDegreesPerMeter * Time.deltaTime;

        if (frontLeftSpin != null)
            frontLeftSpin.localRotation = Quaternion.Euler(wheelSpinAngle, 0f, 0f);

        if (frontRightSpin != null)
            frontRightSpin.localRotation = Quaternion.Euler(wheelSpinAngle, 0f, 0f);

        if (rearLeftSpin != null)
            rearLeftSpin.localRotation = Quaternion.Euler(wheelSpinAngle, 0f, 0f);

        if (rearRightSpin != null)
            rearRightSpin.localRotation = Quaternion.Euler(wheelSpinAngle, 0f, 0f);
    }

    void UpdateBodyLean()
    {
        if (bodyVisual == null)
            return;

        float groundedMoveInput = isGrounded ? moveInput : 0f;
        float groundedSteerInput = isGrounded ? smoothedSteerInput : 0f;

        float roll = -groundedSteerInput * bodyRollAmount;
        float pitch = -groundedMoveInput * bodyPitchAmount;

        Quaternion targetRotation =
            bodyStartLocalRotation *
            Quaternion.Euler(pitch, 0f, roll);

        bodyVisual.localRotation = Quaternion.Slerp(
            bodyVisual.localRotation,
            targetRotation,
            bodyLeanSmoothing * Time.deltaTime
        );
    }

    public float GetSpeedKPH()
    {
        Vector3 flatVelocity = rb.velocity;
        flatVelocity.y = 0f;
        return flatVelocity.magnitude * 3.6f;
    }

    float KPHToMS(float kph)
    {
        return kph / 3.6f;
    }

    public void SetCanDrive(bool value)
    {
        canDrive = value;

        if (!canDrive && rb != null)
        {
            rb.drag = brakingDrag;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void ResetVehicleVelocity()
    {
        if (rb == null)
            return;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    void OnValidate()
    {
        groundCheckRadius = Mathf.Max(0.01f, groundCheckRadius);
        maxSpeedKPH = Mathf.Max(1f, maxSpeedKPH);
        reverseMaxSpeedKPH = Mathf.Max(1f, reverseMaxSpeedKPH);
        reverseEngageSpeedKPH = Mathf.Max(0f, reverseEngageSpeedKPH);
        reverseSteeringMultiplier = Mathf.Clamp(reverseSteeringMultiplier, 0f, 1f);
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheckPoint == null)
            return;

        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
    }
}