using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarHopInput : MonoBehaviour
{
    [Header("Input")]
    public KeyCode jumpKey = KeyCode.Space;

    [Header("Jump Force")]
    public float upwardJumpForce = 7.5f;

    [Tooltip("Optional small forward kick when jumping.")]
    public float forwardBoostForce = 1.5f;

    [Tooltip("Use VelocityChange for arcade feel.")]
    public ForceMode forceMode = ForceMode.VelocityChange;

    [Header("Ground Check")]
    public Transform groundCheckPoint;
    public LayerMask groundMask = ~0;
    public float groundCheckDistance = 1.2f;

    [Header("Cooldown")]
    public float jumpCooldown = 0.8f;

    [Header("Air Control Safety")]
    [Tooltip("Prevents jump if the car is already moving upward too fast.")]
    public float maxAllowedUpVelocity = 2.0f;

    private Rigidbody rb;
    private float cooldownTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }

        if (Input.GetKeyDown(jumpKey))
        {
            TryJump();
        }
    }

    private void TryJump()
    {
        if (cooldownTimer > 0f)
        {
            return;
        }

        if (!IsGrounded())
        {
            return;
        }

        if (rb.velocity.y > maxAllowedUpVelocity)
        {
            return;
        }

        Vector3 jumpVelocity = Vector3.up * upwardJumpForce;
        Vector3 forwardVelocity = transform.forward * forwardBoostForce;

        rb.AddForce(jumpVelocity + forwardVelocity, forceMode);

        cooldownTimer = jumpCooldown;
    }

    private bool IsGrounded()
    {
        Transform check = groundCheckPoint != null ? groundCheckPoint : transform;

        return Physics.Raycast(
            check.position,
            -transform.up,
            groundCheckDistance,
            groundMask,
            QueryTriggerInteraction.Ignore
        );
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