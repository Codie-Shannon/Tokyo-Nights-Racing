using UnityEngine;

[System.Serializable]
public class GarageVehicleStats
{
    [Range(0f, 10f)] public float speed;
    [Range(0f, 10f)] public float acceleration;
    [Range(0f, 10f)] public float handling;
    [Range(0f, 10f)] public float offroad;
    [Range(0f, 10f)] public float strength;

    public bool canJump;
    public string vehicleTypeName;

    public GarageVehicleStats(
        float speed,
        float acceleration,
        float handling,
        float offroad,
        float strength,
        bool canJump,
        string vehicleTypeName
    )
    {
        this.speed = Mathf.Clamp(speed, 0f, 10f);
        this.acceleration = Mathf.Clamp(acceleration, 0f, 10f);
        this.handling = Mathf.Clamp(handling, 0f, 10f);
        this.offroad = Mathf.Clamp(offroad, 0f, 10f);
        this.strength = Mathf.Clamp(strength, 0f, 10f);
        this.canJump = canJump;
        this.vehicleTypeName = vehicleTypeName;
    }
}