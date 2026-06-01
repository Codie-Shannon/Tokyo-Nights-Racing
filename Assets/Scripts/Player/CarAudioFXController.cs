using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody))]
public class CarAudioFXController : MonoBehaviour
{
    [Serializable]
    public class LayerCollisionSound
    {
        [Header("Layer Match")]
        public string label = "Wall";
        public LayerMask collisionLayers;

        [Header("Audio")]
        public AudioClip clip;

        [Header("Impact Settings")]
        public float minimumImpactSpeed = 5f;
        public float maxImpactSpeed = 18f;
        public float maxVolume = 0.35f;

        [Tooltip("Optional pitch variation for this collision type.")]
        public float minPitch = 0.95f;
        public float maxPitch = 1.05f;
    }

    [Header("References")]
    public Rigidbody carRigidbody;
    public Transform groundCheckPoint;

    [Header("Ground Check")]
    public LayerMask groundMask = ~0;
    public float groundCheckDistance = 1.4f;

    [Header("AI / Non Player")]
    public bool isAI = false;

    [Tooltip("When AI is enabled, fake throttle from speed increase.")]
    public float aiThrottleSensitivity = 2f;

    [Tooltip("AI sounds should usually be quieter than player sounds.")]
    public float aiMasterVolumeMultiplier = 0.45f;

    [Header("Engine Audio Sources")]
    public AudioSource idleEngineSource;
    public AudioSource driveEngineSource;

    [Header("Engine Clips")]
    public AudioClip idleEngineLoopClip;
    public AudioClip accelerationLoopClip;

    [Tooltip("Speed where engine reaches max drive pitch/volume.")]
    public float maxSpeedForAudio = 25f;

    [Header("Idle Engine Loop")]
    public float idlePitch = 0.75f;
    public float idleLowSpeedVolume = 0.18f;
    public float idleHighSpeedVolume = 0.06f;

    [Header("Drive Engine Loop")]
    public float driveIdlePitch = 0.55f;
    public float driveMaxPitch = 1.05f;
    public float driveMinVolume = 0.0f;
    public float driveMaxVolume = 0.32f;

    [Header("Throttle Boost")]
    public bool useThrottleBoost = true;
    public string throttleAxis = "Vertical";
    public float throttlePitchBoost = 0.05f;
    public float throttleVolumeBoost = 0.08f;

    [Header("Deceleration Feel")]
    [Tooltip("Drive loop stays audible but quieter when not accelerating.")]
    public float decelerationVolumeMultiplier = 0.45f;

    [Tooltip("Drive pitch drops slightly during deceleration.")]
    public float decelerationPitchDrop = 0.08f;

    [Header("Engine Smoothing")]
    public float engineVolumeSmooth = 5f;
    public float enginePitchSmooth = 5f;

    [Header("Skid Audio")]
    public AudioSource skidSource;
    public AudioClip skidLoopClip;

    public float skidSideSpeedThreshold = 4f;
    public float skidForwardSpeedThreshold = 6f;
    public float skidMaxSideSpeed = 12f;
    public float skidMaxVolume = 0.18f;
    public float skidPitch = 0.9f;
    public float skidFadeSpeed = 6f;

    [Header("Landing Audio")]
    public AudioSource oneShotSource;
    public AudioClip landingThudClip;
    public AudioClip dustImpactClip;

    public float minimumLandingSpeed = 4f;
    public float landingMaxSpeed = 12f;
    public float landingThudMaxVolume = 0.35f;
    public float dustImpactMaxVolume = 0.22f;

    [Header("Crash Audio - Fallback")]
    [Tooltip("Used if no layer-specific collision sound matches.")]
    public AudioClip crashClip;

    public float minimumCrashSpeed = 5f;
    public float crashMaxSpeed = 18f;
    public float crashMaxVolume = 0.35f;
    public float crashCooldown = 0.25f;

    [Header("Crash Audio - Layer Based")]
    public bool useLayerBasedCrashSounds = true;

    [Tooltip("If enabled, layer-specific sounds override the fallback crash clip.")]
    public LayerCollisionSound[] layerCollisionSounds;

    [Header("Airborne Engine")]
    public bool pitchUpInAir = true;
    public float airbornePitchBoost = 0.05f;

    [Header("Low Pass Filter")]
    public bool useEngineLowPass = true;
    public float lowPassCutoff = 3500f;
    public float lowPassResonance = 1f;

    private bool isGrounded;
    private bool wasGrounded;
    private float previousVerticalVelocity;
    private float previousSpeed;
    private float crashTimer;

    private AudioLowPassFilter idleLowPass;
    private AudioLowPassFilter driveLowPass;

    private void Reset()
    {
        carRigidbody = GetComponent<Rigidbody>();
    }

    private void Awake()
    {
        if (carRigidbody == null)
            carRigidbody = GetComponent<Rigidbody>();

        SetupIdleEngineSource();
        SetupDriveEngineSource();
        SetupSkidSource();
        SetupOneShotSource();
        SetupLowPassFilters();

        if (isAI)
            ApplyAISourceSettings();
    }

    private void Update()
    {
        if (carRigidbody == null)
            return;

        if (crashTimer > 0f)
            crashTimer -= Time.deltaTime;

        UpdateGrounded();
        UpdateEngineAudio();
        UpdateSkidAudio();
        UpdateLandingAudio();

        previousVerticalVelocity = carRigidbody.velocity.y;
        wasGrounded = isGrounded;
        previousSpeed = carRigidbody.velocity.magnitude;
    }

    private void SetupIdleEngineSource()
    {
        if (idleEngineSource == null)
        {
            GameObject obj = new GameObject("IdleEngineAudioSource");
            obj.transform.SetParent(transform);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;

            idleEngineSource = obj.AddComponent<AudioSource>();
        }

        if (idleEngineLoopClip != null)
            idleEngineSource.clip = idleEngineLoopClip;

        idleEngineSource.loop = true;
        idleEngineSource.playOnAwake = false;
        idleEngineSource.spatialBlend = isAI ? 1f : 0.35f;
        idleEngineSource.volume = idleLowSpeedVolume;
        idleEngineSource.pitch = idlePitch;

        if (idleEngineSource.clip != null && !idleEngineSource.isPlaying)
            idleEngineSource.Play();
    }

    private void SetupDriveEngineSource()
    {
        if (driveEngineSource == null)
        {
            GameObject obj = new GameObject("DriveEngineAudioSource");
            obj.transform.SetParent(transform);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;

            driveEngineSource = obj.AddComponent<AudioSource>();
        }

        if (accelerationLoopClip != null)
            driveEngineSource.clip = accelerationLoopClip;

        driveEngineSource.loop = true;
        driveEngineSource.playOnAwake = false;
        driveEngineSource.spatialBlend = isAI ? 1f : 0.35f;
        driveEngineSource.volume = driveMinVolume;
        driveEngineSource.pitch = driveIdlePitch;

        if (driveEngineSource.clip != null && !driveEngineSource.isPlaying)
            driveEngineSource.Play();
    }

    private void SetupSkidSource()
    {
        if (skidSource == null)
        {
            GameObject skidObj = new GameObject("SkidAudioSource");
            skidObj.transform.SetParent(transform);
            skidObj.transform.localPosition = Vector3.zero;
            skidObj.transform.localRotation = Quaternion.identity;

            skidSource = skidObj.AddComponent<AudioSource>();
        }

        if (skidLoopClip != null)
            skidSource.clip = skidLoopClip;

        skidSource.loop = true;
        skidSource.playOnAwake = false;
        skidSource.spatialBlend = isAI ? 1f : 0.5f;
        skidSource.volume = 0f;
        skidSource.pitch = skidPitch;

        if (skidSource.clip != null && !skidSource.isPlaying)
            skidSource.Play();
    }

    private void SetupOneShotSource()
    {
        if (oneShotSource == null)
        {
            GameObject oneShotObj = new GameObject("OneShotAudioSource");
            oneShotObj.transform.SetParent(transform);
            oneShotObj.transform.localPosition = Vector3.zero;
            oneShotObj.transform.localRotation = Quaternion.identity;

            oneShotSource = oneShotObj.AddComponent<AudioSource>();
        }

        oneShotSource.loop = false;
        oneShotSource.playOnAwake = false;
        oneShotSource.spatialBlend = isAI ? 1f : 0.45f;
        oneShotSource.volume = 1f;
    }

    private void SetupLowPassFilters()
    {
        if (!useEngineLowPass)
            return;

        if (idleEngineSource != null)
        {
            idleLowPass = idleEngineSource.GetComponent<AudioLowPassFilter>();

            if (idleLowPass == null)
                idleLowPass = idleEngineSource.gameObject.AddComponent<AudioLowPassFilter>();

            idleLowPass.cutoffFrequency = lowPassCutoff;
            idleLowPass.lowpassResonanceQ = lowPassResonance;
        }

        if (driveEngineSource != null)
        {
            driveLowPass = driveEngineSource.GetComponent<AudioLowPassFilter>();

            if (driveLowPass == null)
                driveLowPass = driveEngineSource.gameObject.AddComponent<AudioLowPassFilter>();

            driveLowPass.cutoffFrequency = lowPassCutoff;
            driveLowPass.lowpassResonanceQ = lowPassResonance;
        }
    }

    private void ApplyAISourceSettings()
    {
        if (idleEngineSource != null)
        {
            idleEngineSource.spatialBlend = 1f;
            idleEngineSource.minDistance = 4f;
            idleEngineSource.maxDistance = 45f;
            idleEngineSource.rolloffMode = AudioRolloffMode.Logarithmic;
        }

        if (driveEngineSource != null)
        {
            driveEngineSource.spatialBlend = 1f;
            driveEngineSource.minDistance = 4f;
            driveEngineSource.maxDistance = 45f;
            driveEngineSource.rolloffMode = AudioRolloffMode.Logarithmic;
        }

        if (skidSource != null)
        {
            skidSource.spatialBlend = 1f;
            skidSource.minDistance = 3f;
            skidSource.maxDistance = 30f;
            skidSource.rolloffMode = AudioRolloffMode.Logarithmic;
        }

        if (oneShotSource != null)
        {
            oneShotSource.spatialBlend = 1f;
            oneShotSource.minDistance = 5f;
            oneShotSource.maxDistance = 50f;
            oneShotSource.rolloffMode = AudioRolloffMode.Logarithmic;
        }
    }

    private void UpdateGrounded()
    {
        Transform check = groundCheckPoint != null ? groundCheckPoint : transform;

        isGrounded = Physics.Raycast(
            check.position,
            -transform.up,
            groundCheckDistance,
            groundMask,
            QueryTriggerInteraction.Ignore
        );
    }

    private void UpdateEngineAudio()
    {
        if (idleEngineSource != null && idleEngineSource.clip != null && !idleEngineSource.isPlaying)
            idleEngineSource.Play();

        if (driveEngineSource != null && driveEngineSource.clip != null && !driveEngineSource.isPlaying)
            driveEngineSource.Play();

        float speed = carRigidbody.velocity.magnitude;
        float speedT = Mathf.InverseLerp(0f, maxSpeedForAudio, speed);

        float throttle = GetThrottleAmount(speed);
        bool accelerating = throttle > 0.05f;

        float masterVolume = isAI ? aiMasterVolumeMultiplier : 1f;

        float targetIdleVolume = Mathf.Lerp(
            idleLowSpeedVolume,
            idleHighSpeedVolume,
            speedT
        ) * masterVolume;

        float targetDriveVolume = Mathf.Lerp(
            driveMinVolume,
            driveMaxVolume,
            speedT
        ) * masterVolume;

        float targetDrivePitch = Mathf.Lerp(
            driveIdlePitch,
            driveMaxPitch,
            speedT
        );

        if (accelerating)
        {
            targetDriveVolume += throttle * throttleVolumeBoost * masterVolume;
            targetDrivePitch += throttle * throttlePitchBoost;
        }
        else
        {
            targetDriveVolume *= decelerationVolumeMultiplier;
            targetDrivePitch -= decelerationPitchDrop;
        }

        if (!isGrounded && pitchUpInAir)
            targetDrivePitch += airbornePitchBoost;

        targetIdleVolume = Mathf.Clamp01(targetIdleVolume);
        targetDriveVolume = Mathf.Clamp01(targetDriveVolume);
        targetDrivePitch = Mathf.Clamp(targetDrivePitch, 0.35f, 2f);

        if (idleEngineSource != null)
        {
            idleEngineSource.volume = Mathf.Lerp(
                idleEngineSource.volume,
                targetIdleVolume,
                engineVolumeSmooth * Time.deltaTime
            );

            idleEngineSource.pitch = Mathf.Lerp(
                idleEngineSource.pitch,
                idlePitch,
                enginePitchSmooth * Time.deltaTime
            );
        }

        if (driveEngineSource != null)
        {
            driveEngineSource.volume = Mathf.Lerp(
                driveEngineSource.volume,
                targetDriveVolume,
                engineVolumeSmooth * Time.deltaTime
            );

            driveEngineSource.pitch = Mathf.Lerp(
                driveEngineSource.pitch,
                targetDrivePitch,
                enginePitchSmooth * Time.deltaTime
            );
        }

        UpdateLowPassFilters();
    }

    private float GetThrottleAmount(float currentSpeed)
    {
        if (!useThrottleBoost)
            return 0f;

        if (isAI)
        {
            float speedDelta = currentSpeed - previousSpeed;
            return Mathf.Clamp01(speedDelta * aiThrottleSensitivity);
        }

        return Mathf.Abs(Input.GetAxisRaw(throttleAxis));
    }

    private void UpdateLowPassFilters()
    {
        if (!useEngineLowPass)
            return;

        if (idleLowPass != null)
        {
            idleLowPass.cutoffFrequency = lowPassCutoff;
            idleLowPass.lowpassResonanceQ = lowPassResonance;
        }

        if (driveLowPass != null)
        {
            driveLowPass.cutoffFrequency = lowPassCutoff;
            driveLowPass.lowpassResonanceQ = lowPassResonance;
        }
    }

    private void UpdateSkidAudio()
    {
        if (skidSource == null)
            return;

        if (skidSource.clip != null && !skidSource.isPlaying)
            skidSource.Play();

        if (!isGrounded)
        {
            FadeSkidTo(0f);
            return;
        }

        Vector3 localVelocity = transform.InverseTransformDirection(carRigidbody.velocity);

        float forwardSpeed = Mathf.Abs(localVelocity.z);
        float sideSpeed = Mathf.Abs(localVelocity.x);

        bool isSkidding =
            forwardSpeed >= skidForwardSpeedThreshold &&
            sideSpeed >= skidSideSpeedThreshold;

        if (!isSkidding)
        {
            FadeSkidTo(0f);
            return;
        }

        float skidT = Mathf.InverseLerp(
            skidSideSpeedThreshold,
            skidMaxSideSpeed,
            sideSpeed
        );

        float masterVolume = isAI ? aiMasterVolumeMultiplier : 1f;
        float targetVolume = Mathf.Lerp(0f, skidMaxVolume, skidT) * masterVolume;

        FadeSkidTo(targetVolume);
    }

    private void FadeSkidTo(float targetVolume)
    {
        if (skidSource == null)
            return;

        skidSource.volume = Mathf.Lerp(
            skidSource.volume,
            targetVolume,
            skidFadeSpeed * Time.deltaTime
        );
    }

    private void UpdateLandingAudio()
    {
        if (oneShotSource == null)
            return;

        bool justLanded = !wasGrounded && isGrounded;

        if (!justLanded)
            return;

        float landingSpeed = Mathf.Abs(previousVerticalVelocity);

        if (landingSpeed < minimumLandingSpeed)
            return;

        float landingT = Mathf.InverseLerp(
            minimumLandingSpeed,
            landingMaxSpeed,
            landingSpeed
        );

        float masterVolume = isAI ? aiMasterVolumeMultiplier : 1f;

        if (landingThudClip != null)
        {
            float volume = Mathf.Lerp(0.08f, landingThudMaxVolume, landingT) * masterVolume;
            oneShotSource.PlayOneShot(landingThudClip, volume);
        }

        if (dustImpactClip != null)
        {
            float volume = Mathf.Lerp(0.05f, dustImpactMaxVolume, landingT) * masterVolume;
            oneShotSource.PlayOneShot(dustImpactClip, volume);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (oneShotSource == null)
            return;

        if (crashTimer > 0f)
            return;

        float impactSpeed = collision.relativeVelocity.magnitude;

        LayerCollisionSound matchedLayerSound = null;

        if (useLayerBasedCrashSounds)
            matchedLayerSound = GetLayerCollisionSound(collision.gameObject.layer);

        if (matchedLayerSound != null && matchedLayerSound.clip != null)
        {
            PlayLayerCollisionSound(matchedLayerSound, impactSpeed);
            return;
        }

        PlayFallbackCrashSound(impactSpeed);
    }

    private LayerCollisionSound GetLayerCollisionSound(int collisionLayer)
    {
        if (layerCollisionSounds == null || layerCollisionSounds.Length == 0)
            return null;

        int collisionLayerMask = 1 << collisionLayer;

        for (int i = 0; i < layerCollisionSounds.Length; i++)
        {
            LayerCollisionSound sound = layerCollisionSounds[i];

            if (sound == null)
                continue;

            if ((sound.collisionLayers.value & collisionLayerMask) != 0)
                return sound;
        }

        return null;
    }

    private void PlayLayerCollisionSound(LayerCollisionSound sound, float impactSpeed)
    {
        if (sound == null || sound.clip == null)
            return;

        if (impactSpeed < sound.minimumImpactSpeed)
            return;

        float impactT = Mathf.InverseLerp(
            sound.minimumImpactSpeed,
            sound.maxImpactSpeed,
            impactSpeed
        );

        float masterVolume = isAI ? aiMasterVolumeMultiplier : 1f;
        float volume = Mathf.Lerp(0.08f, sound.maxVolume, impactT) * masterVolume;

        float oldPitch = oneShotSource.pitch;
        oneShotSource.pitch = UnityEngine.Random.Range(sound.minPitch, sound.maxPitch);
        oneShotSource.PlayOneShot(sound.clip, volume);
        oneShotSource.pitch = oldPitch;

        crashTimer = crashCooldown;
    }

    private void PlayFallbackCrashSound(float impactSpeed)
    {
        if (crashClip == null)
            return;

        if (impactSpeed < minimumCrashSpeed)
            return;

        float impactT = Mathf.InverseLerp(
            minimumCrashSpeed,
            crashMaxSpeed,
            impactSpeed
        );

        float masterVolume = isAI ? aiMasterVolumeMultiplier : 1f;
        float volume = Mathf.Lerp(0.08f, crashMaxVolume, impactT) * masterVolume;

        oneShotSource.PlayOneShot(crashClip, volume);
        crashTimer = crashCooldown;
    }

    public void StopEngine()
    {
        if (idleEngineSource != null)
            idleEngineSource.Stop();

        if (driveEngineSource != null)
            driveEngineSource.Stop();
    }

    public void StartEngine()
    {
        if (idleEngineSource != null && idleEngineSource.clip != null && !idleEngineSource.isPlaying)
            idleEngineSource.Play();

        if (driveEngineSource != null && driveEngineSource.clip != null && !driveEngineSource.isPlaying)
            driveEngineSource.Play();
    }

    private void OnDrawGizmosSelected()
    {
        Transform check = groundCheckPoint != null ? groundCheckPoint : transform;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(
            check.position,
            check.position - transform.up * groundCheckDistance
        );
    }
}