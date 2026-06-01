using System.Collections.Generic;
using UnityEngine;

public static class GarageVehicleStatCalculator
{
    private const float NoRangeFallbackScore = 5f;

    public static GarageVehicleStats CalculateRelativeStats(
        VehicleData selectedVehicle,
        IList<VehicleData> fullRoster,
        float minimumVisibleScore = 1f,
        float maximumVisibleScore = 10f
    )
    {
        minimumVisibleScore = Mathf.Clamp(minimumVisibleScore, 0f, 10f);
        maximumVisibleScore = Mathf.Clamp(maximumVisibleScore, minimumVisibleScore, 10f);

        if (selectedVehicle == null || selectedVehicle.gameplayPrefab == null)
        {
            return new GarageVehicleStats(0, 0, 0, 0, 0, false, "Unknown");
        }

        GarageRawVehicleStats selectedRaw = CalculateRawStats(selectedVehicle.gameplayPrefab);

        if (fullRoster == null || fullRoster.Count == 0)
        {
            return ConvertRawToNeutralStats(selectedRaw);
        }

        List<GarageRawVehicleStats> rawRoster = new List<GarageRawVehicleStats>();

        for (int i = 0; i < fullRoster.Count; i++)
        {
            if (fullRoster[i] == null || fullRoster[i].gameplayPrefab == null)
                continue;

            rawRoster.Add(CalculateRawStats(fullRoster[i].gameplayPrefab));
        }

        if (rawRoster.Count <= 1)
        {
            return ConvertRawToNeutralStats(selectedRaw);
        }

        float speed = NormalizeAgainstRoster(selectedRaw.speedRaw, rawRoster, StatType.Speed, minimumVisibleScore, maximumVisibleScore);
        float acceleration = NormalizeAgainstRoster(selectedRaw.accelerationRaw, rawRoster, StatType.Acceleration, minimumVisibleScore, maximumVisibleScore);
        float handling = NormalizeAgainstRoster(selectedRaw.handlingRaw, rawRoster, StatType.Handling, minimumVisibleScore, maximumVisibleScore);
        float offroad = NormalizeAgainstRoster(selectedRaw.offroadRaw, rawRoster, StatType.Offroad, minimumVisibleScore, maximumVisibleScore);
        float strength = NormalizeAgainstRoster(selectedRaw.strengthRaw, rawRoster, StatType.Strength, minimumVisibleScore, maximumVisibleScore);

        return new GarageVehicleStats(
            speed,
            acceleration,
            handling,
            offroad,
            strength,
            selectedRaw.canJump,
            selectedRaw.vehicleTypeName
        );
    }

    public static GarageRawVehicleStats CalculateRawStats(GameObject vehiclePrefab)
    {
        if (vehiclePrefab == null)
        {
            return new GarageRawVehicleStats(0, 0, 0, 0, 0, false, "Unknown");
        }

        CarController controller = vehiclePrefab.GetComponentInChildren<CarController>(true);
        CarHopInput hopInput = vehiclePrefab.GetComponentInChildren<CarHopInput>(true);
        Rigidbody rb = vehiclePrefab.GetComponentInChildren<Rigidbody>(true);

        if (controller == null)
        {
            Debug.LogWarning($"GarageVehicleStatCalculator: No CarController found on {vehiclePrefab.name}.");
            return new GarageRawVehicleStats(0, 0, 0, 0, 0, hopInput != null && hopInput.enabled, "Unknown");
        }

        float mass = rb != null ? Mathf.Max(rb.mass, 1f) : 1200f;

        float speedRaw = GetEffectiveTopSpeed(controller);
        float accelerationRaw = GetEffectiveAcceleration(controller, mass);
        float handlingRaw = GetHandlingRaw(controller, mass);
        float offroadRaw = GetOffroadRaw(controller);
        float strengthRaw = GetStrengthRaw(controller, mass);

        bool canJump = hopInput != null && hopInput.enabled;

        return new GarageRawVehicleStats(
            speedRaw,
            accelerationRaw,
            handlingRaw,
            offroadRaw,
            strengthRaw,
            canJump,
            controller.vehicleType.ToString()
        );
    }

    private static GarageVehicleStats ConvertRawToNeutralStats(GarageRawVehicleStats raw)
    {
        return new GarageVehicleStats(
            NoRangeFallbackScore,
            NoRangeFallbackScore,
            NoRangeFallbackScore,
            NoRangeFallbackScore,
            NoRangeFallbackScore,
            raw != null && raw.canJump,
            raw != null ? raw.vehicleTypeName : "Unknown"
        );
    }

    private static float NormalizeAgainstRoster(
        float selectedValue,
        List<GarageRawVehicleStats> roster,
        StatType statType,
        float minimumVisibleScore,
        float maximumVisibleScore
    )
    {
        float min = float.MaxValue;
        float max = float.MinValue;

        for (int i = 0; i < roster.Count; i++)
        {
            float value = GetRawValue(roster[i], statType);

            if (value < min)
                min = value;

            if (value > max)
                max = value;
        }

        if (Mathf.Approximately(min, max))
            return NoRangeFallbackScore;

        float t = Mathf.InverseLerp(min, max, selectedValue);

        return Mathf.Lerp(minimumVisibleScore, maximumVisibleScore, t);
    }

    private static float GetRawValue(GarageRawVehicleStats stats, StatType statType)
    {
        if (stats == null)
            return 0f;

        switch (statType)
        {
            case StatType.Speed:
                return stats.speedRaw;

            case StatType.Acceleration:
                return stats.accelerationRaw;

            case StatType.Handling:
                return stats.handlingRaw;

            case StatType.Offroad:
                return stats.offroadRaw;

            case StatType.Strength:
                return stats.strengthRaw;

            default:
                return 0f;
        }
    }

    private static float GetEffectiveTopSpeed(CarController controller)
    {
        float topSpeed = controller.maxSpeedKPH;

        if (controller.monsterTruckMode || controller.vehicleType == VehicleType.MonsterTruck)
        {
            topSpeed *= controller.monsterTruckMaxSpeedMultiplier;
        }

        return topSpeed;
    }

    private static float GetEffectiveAcceleration(CarController controller, float mass)
    {
        float acceleration = controller.acceleration;

        if (controller.monsterTruckMode || controller.vehicleType == VehicleType.MonsterTruck)
        {
            acceleration *= controller.monsterTruckAccelerationMultiplier;
        }

        float massPenalty = Mathf.Clamp01(Mathf.InverseLerp(3000f, 900f, mass));

        return acceleration * Mathf.Lerp(0.85f, 1f, massPenalty);
    }

    private static float GetHandlingRaw(CarController controller, float mass)
    {
        float steering = controller.steeringPower;
        float highSpeedSteering = controller.steeringAtHighSpeed;
        float grip = controller.sideGripTurning;
        float straightGrip = controller.sideGripStraight;
        float response = controller.steerResponse;

        float massPenalty = Mathf.Clamp01(Mathf.InverseLerp(3000f, 900f, mass));

        float handling =
            steering * 0.20f +
            highSpeedSteering * 0.25f +
            grip * 8f +
            straightGrip * 3f +
            response * 5f +
            massPenalty * 15f;

        if (controller.vehicleType == VehicleType.MonsterTruck || controller.monsterTruckMode)
        {
            handling *= 0.75f;
        }

        return handling;
    }

    private static float GetOffroadRaw(CarController controller)
    {
        float score = 0f;

        switch (controller.vehicleType)
        {
            case VehicleType.Road:
                score = 20f;
                break;

            case VehicleType.OffRoad:
                score = 80f;
                break;

            case VehicleType.AllTerrain:
                score = 70f;
                break;

            case VehicleType.MonsterTruck:
                score = 95f;
                break;
        }

        score += controller.offRoadGripMultiplier * 15f;

        if (controller.monsterTruckMode)
        {
            score += 10f;
        }

        return score;
    }

    private static float GetStrengthRaw(CarController controller, float mass)
    {
        float strength =
            mass * 0.004f +
            controller.antiRollStrength * 2.5f +
            controller.uprightAssistStrength * 3f;

        if (controller.vehicleType == VehicleType.MonsterTruck || controller.monsterTruckMode)
        {
            strength += 20f;
        }

        return strength;
    }

    private enum StatType
    {
        Speed,
        Acceleration,
        Handling,
        Offroad,
        Strength
    }
}