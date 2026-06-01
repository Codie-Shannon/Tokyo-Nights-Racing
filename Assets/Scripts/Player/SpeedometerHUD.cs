using UnityEngine;
using TMPro;

public class SpeedometerHUD : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text speedText;

    [Header("Vehicle Target")]
    public Rigidbody targetRigidbody;
    public GameObject targetVehicle;

    [Header("Auto Find Fallback")]
    public bool autoFindPlayer = true;
    public string playerTag = "Player";

    [Header("Display")]
    public string suffix = " KPH";
    public bool useThreeDigits = true;
    public bool showZeroWhenNoVehicle = true;

    private float nextFindTime;
    private const float FindInterval = 0.25f;

    void Start()
    {
        FindTargetIfNeeded();
        UpdateSpeedText(0);
    }

    void Update()
    {
        if (targetRigidbody == null && autoFindPlayer && Time.time >= nextFindTime)
        {
            nextFindTime = Time.time + FindInterval;
            FindTargetIfNeeded();
        }

        UpdateSpeed();
    }

    public void SetTargetVehicle(GameObject vehicle)
    {
        targetVehicle = vehicle;

        if (targetVehicle == null)
        {
            targetRigidbody = null;
            return;
        }

        targetRigidbody = targetVehicle.GetComponent<Rigidbody>();

        if (targetRigidbody == null)
            targetRigidbody = targetVehicle.GetComponentInChildren<Rigidbody>();

        if (targetRigidbody == null)
            targetRigidbody = targetVehicle.GetComponentInParent<Rigidbody>();

        if (targetRigidbody != null)
            Debug.Log("[SpeedometerHUD] Target vehicle set: " + targetRigidbody.name);
        else
            Debug.LogWarning("[SpeedometerHUD] Target vehicle was set, but no Rigidbody was found.");
    }

    public void SetTargetRigidbody(Rigidbody rb)
    {
        targetRigidbody = rb;
        targetVehicle = rb != null ? rb.gameObject : null;

        if (targetRigidbody != null)
            Debug.Log("[SpeedometerHUD] Target Rigidbody set: " + targetRigidbody.name);
    }

    void FindTargetIfNeeded()
    {
        if (targetRigidbody != null)
            return;

        if (targetVehicle != null)
        {
            SetTargetVehicle(targetVehicle);
            return;
        }

        if (!autoFindPlayer)
            return;

        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);

        if (playerObject != null)
        {
            SetTargetVehicle(playerObject);
            return;
        }

        Rigidbody foundRb = FindObjectOfType<Rigidbody>();

        if (foundRb != null)
            SetTargetRigidbody(foundRb);
    }

    void UpdateSpeed()
    {
        if (speedText == null)
            return;

        if (targetRigidbody == null)
        {
            if (showZeroWhenNoVehicle)
                UpdateSpeedText(0);

            return;
        }

        float kph = targetRigidbody.velocity.magnitude * 3.6f;
        UpdateSpeedText(kph);
    }

    void UpdateSpeedText(float kph)
    {
        if (speedText == null)
            return;

        int roundedSpeed = Mathf.RoundToInt(kph);

        if (useThreeDigits)
            speedText.text = roundedSpeed.ToString("000") + suffix;
        else
            speedText.text = roundedSpeed + suffix;
    }
}