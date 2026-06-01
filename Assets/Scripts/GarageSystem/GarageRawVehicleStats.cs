using UnityEngine;

[System.Serializable]
public class GarageRawVehicleStats
{
    public float speedRaw;
    public float accelerationRaw;
    public float handlingRaw;
    public float offroadRaw;
    public float strengthRaw;

    public bool canJump;
    public string vehicleTypeName;

    public GarageRawVehicleStats(
        float speedRaw,
        float accelerationRaw,
        float handlingRaw,
        float offroadRaw,
        float strengthRaw,
        bool canJump,
        string vehicleTypeName
    )
    {
        this.speedRaw = speedRaw;
        this.accelerationRaw = accelerationRaw;
        this.handlingRaw = handlingRaw;
        this.offroadRaw = offroadRaw;
        this.strengthRaw = strengthRaw;
        this.canJump = canJump;
        this.vehicleTypeName = vehicleTypeName;
    }
}