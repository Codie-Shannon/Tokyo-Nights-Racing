public static class RaceLaunchData
{
    public static bool HasRaceLaunchData { get; private set; }
    public static bool ReturningFromRace { get; private set; }

    public static string RaceID { get; private set; }
    public static string RaceDisplayName { get; private set; }
    public static string RaceSceneName { get; private set; }

    public static string ReturnSceneName { get; private set; }
    public static string ReturnMarkerID { get; private set; }

    public static int SelectedVehicleIndex { get; private set; }
    public static int OverrideAICount { get; private set; }
    public static bool UseOverrideAICount { get; private set; }

    public static void SetRaceLaunchData(
        string raceID,
        string raceDisplayName,
        string raceSceneName,
        string returnSceneName,
        string returnMarkerID,
        int selectedVehicleIndex = 0,
        int overrideAICount = -1
    )
    {
        RaceID = raceID;
        RaceDisplayName = raceDisplayName;
        RaceSceneName = raceSceneName;

        ReturnSceneName = returnSceneName;
        ReturnMarkerID = returnMarkerID;

        SelectedVehicleIndex = selectedVehicleIndex;

        if (overrideAICount >= 0)
        {
            OverrideAICount = overrideAICount;
            UseOverrideAICount = true;
        }
        else
        {
            OverrideAICount = 0;
            UseOverrideAICount = false;
        }

        HasRaceLaunchData = true;
        ReturningFromRace = false;
    }

    public static void MarkReturningFromRace()
    {
        ReturningFromRace = true;

        // Safety fallback.
        // If the marker ID somehow gets lost, infer it from the race ID.
        // Example: paved_normal -> paved_normal_marker
        if (string.IsNullOrWhiteSpace(ReturnMarkerID) && !string.IsNullOrWhiteSpace(RaceID))
        {
            ReturnMarkerID = RaceID + "_marker";
        }
    }

    public static void SetReturnMarkerID(string returnMarkerID)
    {
        ReturnMarkerID = returnMarkerID;
    }

    public static void Clear()
    {
        HasRaceLaunchData = false;
        ReturningFromRace = false;

        RaceID = "";
        RaceDisplayName = "";
        RaceSceneName = "";

        ReturnSceneName = "";
        ReturnMarkerID = "";

        SelectedVehicleIndex = 0;
        OverrideAICount = 0;
        UseOverrideAICount = false;
    }
}