using UnityEngine;

[System.Serializable]
public class VehicleCameraSettings
{
    [Header("Position")]
    public Vector3 offset = new Vector3(0f, 1.55f, -4.35f);
    public Vector3 reverseOffset = new Vector3(0f, 1.55f, 4.35f);
    public Vector3 lookAtOffset = new Vector3(0f, 0.9f, 0f);

    [Header("Follow")]
    public float followSpeed = 4.5f;
    public float rotationSpeed = 4.2f;

    [Header("Look")]
    public float lookAheadDistance = 3.2f;
    public float reverseLookBehindDistance = 4.2f;

    [Header("Framing")]
    public float turnShiftAmount = 0.12f;
    public float cameraRollAmount = 0.4f;
    public float speedPullbackAmount = 0.45f;

    [Header("FOV")]
    public float baseFOV = 72f;
    public float maxFOV = 80f;

    public void CopyFrom(VehicleCameraSettings other)
    {
        if (other == null)
            return;

        offset = other.offset;
        reverseOffset = other.reverseOffset;
        lookAtOffset = other.lookAtOffset;

        followSpeed = other.followSpeed;
        rotationSpeed = other.rotationSpeed;

        lookAheadDistance = other.lookAheadDistance;
        reverseLookBehindDistance = other.reverseLookBehindDistance;

        turnShiftAmount = other.turnShiftAmount;
        cameraRollAmount = other.cameraRollAmount;
        speedPullbackAmount = other.speedPullbackAmount;

        baseFOV = other.baseFOV;
        maxFOV = other.maxFOV;
    }
}
