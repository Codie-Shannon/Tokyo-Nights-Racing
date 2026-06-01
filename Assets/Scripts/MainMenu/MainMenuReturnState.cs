public enum MainMenuRequestedItem
{
    Default,
    Play,
    Garage,
    RaceModes,
    Trophies,
    Settings,
    Exit
}

public static class MainMenuReturnState
{
    private static bool hasRequest;
    private static MainMenuRequestedItem requestedItem;

    public static void RequestItem(MainMenuRequestedItem item)
    {
        requestedItem = item;
        hasRequest = true;
    }

    public static bool TryConsumeRequest(out MainMenuRequestedItem item)
    {
        item = requestedItem;

        if (!hasRequest)
            return false;

        hasRequest = false;
        requestedItem = MainMenuRequestedItem.Default;
        return true;
    }
}