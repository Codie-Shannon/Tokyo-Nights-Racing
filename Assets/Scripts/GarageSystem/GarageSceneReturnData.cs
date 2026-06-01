using UnityEngine;

public enum GarageReturnTarget
{
    MainMenu,
    Freeroam
}

public static class GarageSceneReturnData
{
    public static GarageReturnTarget ReturnTarget = GarageReturnTarget.MainMenu;

    public static bool HasFreeroamReturnPoint = false;
    public static Vector3 FreeroamReturnPosition;
    public static Quaternion FreeroamReturnRotation;

    public static void SetReturnToMainMenu()
    {
        ReturnTarget = GarageReturnTarget.MainMenu;
        HasFreeroamReturnPoint = false;
    }

    public static void SetReturnToFreeroam(Vector3 position, Quaternion rotation)
    {
        ReturnTarget = GarageReturnTarget.Freeroam;
        HasFreeroamReturnPoint = true;
        FreeroamReturnPosition = position;
        FreeroamReturnRotation = rotation;
    }
}