using TMPro;
using UnityEngine;

public class GarageVehicleStatsDisplay : MonoBehaviour
{
    [Header("Stat Bars")]
    public GarageStatBarUI speedBar;
    public GarageStatBarUI accelerationBar;
    public GarageStatBarUI handlingBar;
    public GarageStatBarUI offroadBar;
    public GarageStatBarUI strengthBar;

    [Header("Extra Info")]
    public TMP_Text vehicleTypeText;
    public TMP_Text canJumpText;

    [Header("Fallback Text")]
    public string noVehicleText = "No Vehicle";

    [Header("Can Jump Display")]
    public string yesText = "Yes";
    public string noText = "No";

    private void Awake()
    {
        ConfigureLabels();
        Clear();
    }

    public void DisplayStats(GarageVehicleStats stats)
    {
        if (stats == null)
        {
            Clear();
            return;
        }

        if (speedBar != null)
            speedBar.SetValue(stats.speed);

        if (accelerationBar != null)
            accelerationBar.SetValue(stats.acceleration);

        if (handlingBar != null)
            handlingBar.SetValue(stats.handling);

        if (offroadBar != null)
            offroadBar.SetValue(stats.offroad);

        if (strengthBar != null)
            strengthBar.SetValue(stats.strength);

        if (vehicleTypeText != null)
            vehicleTypeText.text = stats.vehicleTypeName;

        if (canJumpText != null)
            canJumpText.text = stats.canJump ? yesText : noText;
    }

    public void Clear()
    {
        if (speedBar != null)
            speedBar.SetValue(0f);

        if (accelerationBar != null)
            accelerationBar.SetValue(0f);

        if (handlingBar != null)
            handlingBar.SetValue(0f);

        if (offroadBar != null)
            offroadBar.SetValue(0f);

        if (strengthBar != null)
            strengthBar.SetValue(0f);

        if (vehicleTypeText != null)
            vehicleTypeText.text = noVehicleText;

        if (canJumpText != null)
            canJumpText.text = noText;
    }

    private void ConfigureLabels()
    {
        if (speedBar != null)
            speedBar.label = "Speed";

        if (accelerationBar != null)
            accelerationBar.label = "Acceleration";

        if (handlingBar != null)
            handlingBar.label = "Handling";

        if (offroadBar != null)
            offroadBar.label = "Offroad";

        if (strengthBar != null)
            strengthBar.label = "Strength";
    }
}