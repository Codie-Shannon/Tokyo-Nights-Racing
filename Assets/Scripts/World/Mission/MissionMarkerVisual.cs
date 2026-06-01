using UnityEngine;

public class MissionMarkerVisual : MonoBehaviour
{
    [Header("References")]
    public Transform sign;
    public Transform groundRing;

    [Header("Sign Animation")]
    public float bobHeight = 0.12f;
    public float bobSpeed = 2f;
    public float rotateSpeed = 30f;

    [Header("Ring Pulse")]
    public float ringPulseAmount = 0.06f;
    public float ringPulseSpeed = 2f;

    private Vector3 signStartPos;
    private Vector3 ringStartScale;

    void Start()
    {
        if (sign != null) signStartPos = sign.localPosition;
        if (groundRing != null) ringStartScale = groundRing.localScale;
    }

    void Update()
    {
        float t = Time.time;

        if (sign != null)
        {
            Vector3 p = signStartPos;
            p.y += Mathf.Sin(t * bobSpeed) * bobHeight;
            sign.localPosition = p;
            sign.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.Self);
        }

        if (groundRing != null)
        {
            float pulse = 1f + Mathf.Sin(t * ringPulseSpeed) * ringPulseAmount;
            groundRing.localScale = ringStartScale * pulse;
        }
    }
}