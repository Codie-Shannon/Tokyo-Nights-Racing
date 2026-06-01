using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Tokyo Nights/Race Modes/Race Mode Database")]
public class RaceModeDatabase : ScriptableObject
{
    [Header("Race Modes")]
    public List<RaceModeDefinition> raceModes = new List<RaceModeDefinition>();

    public List<RaceModeDefinition> GetValidRaceModes(VehicleData vehicle)
    {
        List<RaceModeDefinition> validModes = new List<RaceModeDefinition>();

        if (vehicle == null)
            return validModes;

        for (int i = 0; i < raceModes.Count; i++)
        {
            RaceModeDefinition raceMode = raceModes[i];

            if (raceMode == null)
                continue;

            if (raceMode.CanUseVehicle(vehicle))
                validModes.Add(raceMode);
        }

        return validModes;
    }

    public RaceModeDefinition GetRandomRaceMode(VehicleData vehicle)
    {
        List<RaceModeDefinition> validModes = GetValidRaceModes(vehicle);

        if (validModes.Count == 0)
            return null;

        int randomIndex = Random.Range(0, validModes.Count);
        return validModes[randomIndex];
    }
}