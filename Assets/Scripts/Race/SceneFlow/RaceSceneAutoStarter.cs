using System.Collections;
using UnityEngine;

public class RaceSceneAutoStarter : MonoBehaviour
{
    [Header("References")]
    public RaceManager raceManager;
    public TrackVariantManager trackVariantManager;

    [Tooltip("Optional. If assigned, this prepares selected AI VehicleData for GridStartManager before RaceManager positions entrants.")]
    public VehicleDatabaseRaceAISpawner aiVehicleSpawner;

    [Tooltip("Fallback if no launch data exists, useful for testing RaceScene directly.")]
    public RaceDefinition defaultRaceDefinition;

    [Header("Behaviour")]
    public bool autoStartOnSceneLoad = true;
    public float startDelay = 0.25f;

    [Header("AI Selection")]
    public bool prepareAIBeforeRaceStart = true;

    [Header("Debug")]
    public bool logDebugMessages = true;

    private IEnumerator Start()
    {
        if (!autoStartOnSceneLoad)
            yield break;

        yield return new WaitForSeconds(startDelay);

        ApplyRequestedTrackVariant();
        StartRaceFromLaunchData();
    }

    private void ApplyRequestedTrackVariant()
    {
        if (trackVariantManager == null)
            trackVariantManager = FindObjectOfType<TrackVariantManager>();

        if (trackVariantManager == null)
        {
            LogWarning("No TrackVariantManager found. Track variant will not be applied.");
            return;
        }

        RaceLoadRequest.TrackVariant variantToApply = RaceLoadRequest.SelectedTrackVariant;

        Log("Applying track variant: " + variantToApply);

        trackVariantManager.ApplyVariant(variantToApply);
    }

    public void StartRaceFromLaunchData()
    {
        if (raceManager == null)
            raceManager = RaceManager.Instance;

        if (raceManager == null)
        {
            LogWarning("No RaceManager found in RaceScene.");
            return;
        }

        RaceDefinition raceToStart = null;

        if (RaceLaunchData.HasRaceLaunchData)
        {
            raceToStart = FindRaceDefinitionByID(RaceLaunchData.RaceID);

            if (raceToStart == null)
            {
                LogWarning("No RaceDefinition found for Race ID: " + RaceLaunchData.RaceID);
            }
        }

        if (raceToStart == null)
            raceToStart = defaultRaceDefinition;

        if (raceToStart == null)
        {
            LogWarning("No RaceDefinition available to start.");
            return;
        }

        if (RaceLaunchData.HasRaceLaunchData && RaceLaunchData.UseOverrideAICount)
        {
            raceToStart.aiCount = RaceLaunchData.OverrideAICount;
        }

        if (prepareAIBeforeRaceStart)
        {
            TryPrepareAI(raceToStart);
        }

        Log("Starting race: " + raceToStart.raceDisplayName + " / ID: " + raceToStart.raceID);

        raceManager.TryStartRace(raceToStart);
    }

    private void TryPrepareAI(RaceDefinition raceDefinition)
    {
        if (aiVehicleSpawner == null)
            aiVehicleSpawner = FindObjectOfType<VehicleDatabaseRaceAISpawner>();

        if (aiVehicleSpawner == null)
        {
            LogWarning("No VehicleDatabaseRaceAISpawner found. GridStartManager will use its fallback AI prefab.");
            return;
        }

        aiVehicleSpawner.PrepareAIForRace(raceDefinition);
    }

    private RaceDefinition FindRaceDefinitionByID(string raceID)
    {
        RaceDefinition[] definitions = FindObjectsOfType<RaceDefinition>(true);

        for (int i = 0; i < definitions.Length; i++)
        {
            if (definitions[i] != null && definitions[i].MatchesRaceID(raceID))
                return definitions[i];
        }

        return null;
    }

    private void Log(string message)
    {
        if (logDebugMessages)
            Debug.Log("[RaceSceneAutoStarter] " + message);
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning("[RaceSceneAutoStarter] " + message);
    }
}
