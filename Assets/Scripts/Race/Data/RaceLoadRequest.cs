using UnityEngine;

public static class RaceLoadRequest
{
    public enum TrackVariant
    {
        Road,
        OffRoad,
        AllTerrain,
        MonsterTruck
    }

    public static TrackVariant SelectedTrackVariant = TrackVariant.Road;

    public static void SetVariant(TrackVariant variant)
    {
        SelectedTrackVariant = variant;
        Debug.Log("RaceLoadRequest selected variant: " + variant);
    }
}