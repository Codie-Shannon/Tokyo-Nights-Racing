using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarLandingDustFX : MonoBehaviour
{
    [Header("References")]
    public Rigidbody carRigidbody;
    public ParticleSystem landingDustParticles;
    public Transform groundCheckPoint;

    [Header("Ground Check")]
    public LayerMask groundMask = ~0;
    public float groundCheckDistance = 1.4f;

    [Header("Landing Dust")]
    public float minimumLandingSpeed = 4f;
    public float dustBurstMultiplier = 8f;
    public int minimumBurstParticles = 12;
    public int maximumBurstParticles = 80;

    [Header("Positioning")]
    public bool moveDustToGroundHitPoint = true;
    public float dustHeightOffset = 0.05f;

    private bool wasGrounded;
    private bool isGrounded;
    private float previousVerticalVelocity;

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
    }

    private void FixedUpdate()
    {
        wasGrounded = isGrounded;

        RaycastHit hit;
        isGrounded = CheckGrounded(out hit);

        float verticalVelocity = carRigidbody != null ? carRigidbody.velocity.y : 0f;

        bool justLanded = !wasGrounded && isGrounded;

        if (justLanded)
        {
            float landingSpeed = Mathf.Abs(previousVerticalVelocity);

            if (landingSpeed >= minimumLandingSpeed)
            {
                SpawnLandingDust(landingSpeed, hit);
            }
        }

        previousVerticalVelocity = verticalVelocity;
    }

    private bool CheckGrounded(out RaycastHit hit)
    {
        Transform check = groundCheckPoint != null ? groundCheckPoint : transform;

        return Physics.Raycast(
            check.position,
            -transform.up,
            out hit,
            groundCheckDistance,
            groundMask,
            QueryTriggerInteraction.Ignore
        );
    }

    private void SpawnLandingDust(float landingSpeed, RaycastHit hit)
    {
        if (landingDustParticles == null)
        {
            return;
        }

        if (moveDustToGroundHitPoint)
        {
            landingDustParticles.transform.position = hit.point + Vector3.up * dustHeightOffset;
        }

        int burstCount = Mathf.RoundToInt(landingSpeed * dustBurstMultiplier);
        burstCount = Mathf.Clamp(burstCount, minimumBurstParticles, maximumBurstParticles);

        landingDustParticles.Emit(burstCount);
    }

    private void OnDrawGizmosSelected()
    {
        Transform check = groundCheckPoint != null ? groundCheckPoint : transform;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(
            check.position,
            check.position - transform.up * groundCheckDistance
        );
    }
}