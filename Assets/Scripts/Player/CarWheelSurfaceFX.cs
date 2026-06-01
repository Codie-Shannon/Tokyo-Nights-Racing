using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarWheelSurfaceFX : MonoBehaviour
{
    public enum SurfaceType
    {
        Unknown,
        Road,
        Dirt,
        Grass,
        Mud
    }

    [System.Serializable]
    public class WheelFX
    {
        public string name;
        public Transform wheelPoint;
        public ParticleSystem dustParticles;
        public ParticleSystem smokeParticles;

        [HideInInspector] public SurfaceType currentSurface;
        [HideInInspector] public bool grounded;
    }

    [Header("References")]
    public Rigidbody carRigidbody;
    public WheelFX[] wheels;

    [Header("Ground Detection")]
    public LayerMask groundMask = ~0;
    public float groundCheckDistance = 1.2f;

    [Header("Speed Thresholds")]
    public float dustStartSpeed = 3f;
    public float dustMaxSpeed = 25f;

    [Header("Skid Detection")]
    public float skidSideSpeedThreshold = 4f;
    public float skidForwardSpeedThreshold = 6f;
    public float skidMaxSideSpeed = 12f;

    [Header("Dust Emission")]
    public float dirtDustMinRate = 8f;
    public float dirtDustMaxRate = 75f;

    public float grassDustMinRate = 4f;
    public float grassDustMaxRate = 35f;

    public float mudDustMinRate = 10f;
    public float mudDustMaxRate = 95f;

    [Header("Road Smoke Emission")]
    public float roadSmokeMinRate = 0f;
    public float roadSmokeMaxRate = 55f;

    [Header("Offroad Slide Boost")]
    [Tooltip("Extra dust multiplier when sliding on dirt/grass/mud.")]
    public float offroadSlideDustBoost = 1.6f;

    [Header("Particle Colours")]
    public Color dirtDustColor = new Color(0.55f, 0.36f, 0.18f, 0.75f);
    public Color grassDustColor = new Color(0.38f, 0.42f, 0.20f, 0.65f);
    public Color mudDustColor = new Color(0.20f, 0.12f, 0.07f, 0.80f);
    public Color roadSmokeColor = new Color(0.65f, 0.65f, 0.65f, 0.55f);

    [Header("Smoothing")]
    public float emissionSmooth = 8f;

    [Header("Debug")]
    public bool drawGroundRays = true;

    private float[] currentDustRates;
    private float[] currentSmokeRates;

    private void Reset()
    {
        carRigidbody = GetComponent<Rigidbody>();
    }

    private void Awake()
    {
        if (carRigidbody == null)
        {
            carRigidbody = GetComponent<Rigidbody>();
        }

        int count = wheels != null ? wheels.Length : 0;
        currentDustRates = new float[count];
        currentSmokeRates = new float[count];

        StopAllEmission();
    }

    private void Update()
    {
        if (carRigidbody == null || wheels == null)
        {
            return;
        }

        Vector3 localVelocity = transform.InverseTransformDirection(carRigidbody.velocity);

        float forwardSpeed = Mathf.Abs(localVelocity.z);
        float sideSpeed = Mathf.Abs(localVelocity.x);
        float totalSpeed = carRigidbody.velocity.magnitude;

        float speedT = Mathf.InverseLerp(dustStartSpeed, dustMaxSpeed, totalSpeed);
        float skidT = Mathf.InverseLerp(skidSideSpeedThreshold, skidMaxSideSpeed, sideSpeed);

        bool isSkidding =
            forwardSpeed >= skidForwardSpeedThreshold &&
            sideSpeed >= skidSideSpeedThreshold;

        for (int i = 0; i < wheels.Length; i++)
        {
            UpdateWheelFX(i, speedT, skidT, isSkidding);
        }
    }

    private void UpdateWheelFX(int index, float speedT, float skidT, bool isSkidding)
    {
        WheelFX wheel = wheels[index];

        if (wheel == null || wheel.wheelPoint == null)
        {
            return;
        }

        RaycastHit hit;
        wheel.grounded = Physics.Raycast(
            wheel.wheelPoint.position,
            -transform.up,
            out hit,
            groundCheckDistance,
            groundMask,
            QueryTriggerInteraction.Ignore
        );

        if (drawGroundRays)
        {
            Debug.DrawRay(
                wheel.wheelPoint.position,
                -transform.up * groundCheckDistance,
                wheel.grounded ? Color.green : Color.red
            );
        }

        if (!wheel.grounded)
        {
            SmoothSetRates(index, 0f, 0f);
            return;
        }

        wheel.currentSurface = GetSurfaceType(hit.collider);

        float targetDustRate = 0f;
        float targetSmokeRate = 0f;

        switch (wheel.currentSurface)
        {
            case SurfaceType.Road:
                targetDustRate = 0f;

                if (isSkidding)
                {
                    targetSmokeRate = Mathf.Lerp(roadSmokeMinRate, roadSmokeMaxRate, skidT);
                }

                SetParticleColor(wheel.smokeParticles, roadSmokeColor);
                break;

            case SurfaceType.Dirt:
                targetDustRate = Mathf.Lerp(dirtDustMinRate, dirtDustMaxRate, speedT);

                if (isSkidding)
                {
                    targetDustRate *= offroadSlideDustBoost;
                }

                SetParticleColor(wheel.dustParticles, dirtDustColor);
                break;

            case SurfaceType.Grass:
                targetDustRate = Mathf.Lerp(grassDustMinRate, grassDustMaxRate, speedT);

                if (isSkidding)
                {
                    targetDustRate *= offroadSlideDustBoost;
                }

                SetParticleColor(wheel.dustParticles, grassDustColor);
                break;

            case SurfaceType.Mud:
                targetDustRate = Mathf.Lerp(mudDustMinRate, mudDustMaxRate, speedT);

                if (isSkidding)
                {
                    targetDustRate *= offroadSlideDustBoost;
                }

                SetParticleColor(wheel.dustParticles, mudDustColor);
                break;

            default:
                targetDustRate = Mathf.Lerp(dirtDustMinRate, dirtDustMaxRate * 0.6f, speedT);

                if (isSkidding)
                {
                    targetDustRate *= offroadSlideDustBoost;
                }

                SetParticleColor(wheel.dustParticles, dirtDustColor);
                break;
        }

        if (carRigidbody.velocity.magnitude < dustStartSpeed)
        {
            targetDustRate = 0f;
            targetSmokeRate = 0f;
        }

        SmoothSetRates(index, targetDustRate, targetSmokeRate);
    }

    private SurfaceType GetSurfaceType(Collider col)
    {
        if (col == null)
        {
            return SurfaceType.Unknown;
        }

        if (col.CompareTag("Road"))
        {
            return SurfaceType.Road;
        }

        if (col.CompareTag("Dirt"))
        {
            return SurfaceType.Dirt;
        }

        if (col.CompareTag("Grass"))
        {
            return SurfaceType.Grass;
        }

        if (col.CompareTag("Mud"))
        {
            return SurfaceType.Mud;
        }

        return SurfaceType.Unknown;
    }

    private void SmoothSetRates(int index, float targetDustRate, float targetSmokeRate)
    {
        currentDustRates[index] = Mathf.Lerp(
            currentDustRates[index],
            targetDustRate,
            emissionSmooth * Time.deltaTime
        );

        currentSmokeRates[index] = Mathf.Lerp(
            currentSmokeRates[index],
            targetSmokeRate,
            emissionSmooth * Time.deltaTime
        );

        WheelFX wheel = wheels[index];

        SetEmissionRate(wheel.dustParticles, currentDustRates[index]);
        SetEmissionRate(wheel.smokeParticles, currentSmokeRates[index]);
    }

    private void SetEmissionRate(ParticleSystem ps, float rate)
    {
        if (ps == null)
        {
            return;
        }

        var emission = ps.emission;
        emission.enabled = rate > 0.5f;
        emission.rateOverTime = rate;
    }

    private void SetParticleColor(ParticleSystem ps, Color color)
    {
        if (ps == null)
        {
            return;
        }

        var main = ps.main;
        main.startColor = color;
    }

    private void StopAllEmission()
    {
        if (wheels == null)
        {
            return;
        }

        foreach (WheelFX wheel in wheels)
        {
            if (wheel == null)
            {
                continue;
            }

            SetEmissionRate(wheel.dustParticles, 0f);
            SetEmissionRate(wheel.smokeParticles, 0f);
        }
    }

    private void OnDisable()
    {
        StopAllEmission();
    }

    private void OnDrawGizmosSelected()
    {
        if (wheels == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;

        foreach (WheelFX wheel in wheels)
        {
            if (wheel == null || wheel.wheelPoint == null)
            {
                continue;
            }

            Gizmos.DrawLine(
                wheel.wheelPoint.position,
                wheel.wheelPoint.position - transform.up * groundCheckDistance
            );
        }
    }
}