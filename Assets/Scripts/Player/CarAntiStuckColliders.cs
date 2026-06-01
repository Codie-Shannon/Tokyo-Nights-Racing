using UnityEngine;

[DisallowMultipleComponent]
public class CarAntiStuckColliders : MonoBehaviour
{
    [Header("Source Collider")]
    [Tooltip("Leave empty to auto-find the largest collider on this car.")]
    public Collider sourceCollider;

    [Header("Generated Collider Parent")]
    public string generatedParentName = "AntiStuckColliders";

    [Header("Generated Collider Layer")]
    [Tooltip("Generated anti-stuck collider objects will be assigned to this layer. Create this layer in Unity first.")]
    public string generatedColliderLayerName = "CarAntiStuck";

    [Header("Generated Colliders")]
    public bool addFrontBumper = true;
    public bool addRearBumper = true;
    public bool addSideGuards = true;
    public bool addBottomSkidPlate = true;

    [Header("Rounded Bumper Settings")]
    [Tooltip("Use capsule bumpers instead of box bumpers. Better for sliding over kerbs.")]
    public bool useRoundedBumpers = true;

    [Tooltip("Radius/thickness of the rounded bumper capsule.")]
    public float roundedBumperRadius = 0.16f;

    [Tooltip("How wide the rounded bumper is compared to the source collider.")]
    public float roundedBumperWidthMultiplier = 0.82f;

    [Tooltip("Moves rounded bumpers down. Negative values place them lower near the front lip.")]
    public float roundedBumperVerticalOffset = -0.18f;

    [Tooltip("Moves rounded bumpers slightly outward from the car.")]
    public float roundedBumperOutwardOffset = 0.02f;

    [Header("Fallback Box Bumper Settings")]
    [Tooltip("Only used if Use Rounded Bumpers is false.")]
    public float bumperWidthMultiplier = 0.82f;

    [Tooltip("Only used if Use Rounded Bumpers is false.")]
    public float bumperHeightMultiplier = 0.28f;

    [Tooltip("Only used if Use Rounded Bumpers is false.")]
    public float bumperDepth = 0.16f;

    [Tooltip("Only used if Use Rounded Bumpers is false.")]
    public float bumperOutwardOffset = 0.02f;

    [Header("Side Guard Settings")]
    [Tooltip("How thick the side guard colliders are.")]
    public float sideGuardThickness = 0.04f;

    [Tooltip("How tall the side guards are compared to the source collider.")]
    public float sideGuardHeightMultiplier = 0.25f;

    [Tooltip("How long the side guards are compared to the source collider.")]
    public float sideGuardLengthMultiplier = 0.72f;

    [Tooltip("Positive values pull side guards inward so they do not stick out too far.")]
    public float sideGuardInset = 0.18f;

    [Tooltip("Positive values push side guards outward. Keep this at 0 for most Cars-style vehicles.")]
    public float sideOutwardOffset = 0f;

    [Header("Skid Plate Settings")]
    [Tooltip("How thick the bottom skid plate is.")]
    public float skidPlateHeight = 0.12f;

    [Tooltip("How wide the skid plate is compared to the source collider.")]
    public float skidPlateWidthMultiplier = 0.75f;

    [Tooltip("How long the skid plate is compared to the source collider.")]
    public float skidPlateLengthMultiplier = 0.85f;

    [Tooltip("Moves bottom skid plate lower. Useful for kerbs.")]
    public float skidPlateDownOffset = 0.04f;

    [Header("Vertical Placement")]
    [Tooltip("Moves box bumpers and side guard colliders upward/downward relative to the source collider center.")]
    public float sideAndBumperVerticalOffset = -0.08f;

    [Header("Physics Material")]
    [Tooltip("Assign a low-friction physics material to make the car slide off kerbs better.")]
    public PhysicMaterial antiStuckMaterial;

    [Header("Debug")]
    public bool regenerateOnStart = true;
    public bool drawGizmos = true;

    private Transform generatedParent;
    private int generatedColliderLayer = -1;

    private void Start()
    {
        if (regenerateOnStart)
            GenerateColliders();
    }

    [ContextMenu("Generate Anti-Stuck Colliders")]
    public void GenerateColliders()
    {
        generatedColliderLayer = LayerMask.NameToLayer(generatedColliderLayerName);

        if (generatedColliderLayer == -1)
        {
            Debug.LogWarning(
                "Layer '" + generatedColliderLayerName + "' does not exist. " +
                "Create it in Unity under Tags and Layers. Generated colliders will use the car's current layer instead."
            );
        }

        if (sourceCollider == null)
            sourceCollider = FindLargestCollider();

        if (sourceCollider == null)
        {
            Debug.LogWarning("No source collider found. Add a main BoxCollider to the car first.");
            return;
        }

        DeleteGeneratedColliders();

        GameObject parentObject = new GameObject(generatedParentName);
        parentObject.transform.SetParent(transform);
        parentObject.transform.localPosition = Vector3.zero;
        parentObject.transform.localRotation = Quaternion.identity;
        parentObject.transform.localScale = Vector3.one;
        ApplyGeneratedLayer(parentObject);

        generatedParent = parentObject.transform;

        Bounds localBounds = GetLocalBounds(sourceCollider);

        Vector3 center = localBounds.center;
        Vector3 size = localBounds.size;

        float width = size.x;
        float height = size.y;
        float length = size.z;

        float guardY = center.y + sideAndBumperVerticalOffset;

        if (addFrontBumper)
        {
            if (useRoundedBumpers)
            {
                CreateCapsule(
                    "Front_Rounded_Bumper_AntiStuck",
                    new Vector3(
                        center.x,
                        center.y + roundedBumperVerticalOffset,
                        center.z + length * 0.5f + roundedBumperRadius + roundedBumperOutwardOffset
                    ),
                    roundedBumperRadius,
                    width * roundedBumperWidthMultiplier,
                    0
                );
            }
            else
            {
                CreateBox(
                    "Front_Bumper_AntiStuck",
                    new Vector3(
                        center.x,
                        guardY,
                        center.z + length * 0.5f + bumperDepth * 0.5f + bumperOutwardOffset
                    ),
                    new Vector3(
                        width * bumperWidthMultiplier,
                        height * bumperHeightMultiplier,
                        bumperDepth
                    )
                );
            }
        }

        if (addRearBumper)
        {
            if (useRoundedBumpers)
            {
                CreateCapsule(
                    "Rear_Rounded_Bumper_AntiStuck",
                    new Vector3(
                        center.x,
                        center.y + roundedBumperVerticalOffset,
                        center.z - length * 0.5f - roundedBumperRadius - roundedBumperOutwardOffset
                    ),
                    roundedBumperRadius,
                    width * roundedBumperWidthMultiplier,
                    0
                );
            }
            else
            {
                CreateBox(
                    "Rear_Bumper_AntiStuck",
                    new Vector3(
                        center.x,
                        guardY,
                        center.z - length * 0.5f - bumperDepth * 0.5f - bumperOutwardOffset
                    ),
                    new Vector3(
                        width * bumperWidthMultiplier,
                        height * bumperHeightMultiplier,
                        bumperDepth
                    )
                );
            }
        }

        if (addSideGuards)
        {
            float leftX =
                center.x
                - width * 0.5f
                + sideGuardThickness * 0.5f
                + sideGuardInset
                - sideOutwardOffset;

            float rightX =
                center.x
                + width * 0.5f
                - sideGuardThickness * 0.5f
                - sideGuardInset
                + sideOutwardOffset;

            CreateBox(
                "Left_Side_Guard_AntiStuck",
                new Vector3(leftX, guardY, center.z),
                new Vector3(
                    sideGuardThickness,
                    height * sideGuardHeightMultiplier,
                    length * sideGuardLengthMultiplier
                )
            );

            CreateBox(
                "Right_Side_Guard_AntiStuck",
                new Vector3(rightX, guardY, center.z),
                new Vector3(
                    sideGuardThickness,
                    height * sideGuardHeightMultiplier,
                    length * sideGuardLengthMultiplier
                )
            );
        }

        if (addBottomSkidPlate)
        {
            CreateBox(
                "Bottom_SkidPlate_AntiStuck",
                new Vector3(
                    center.x,
                    center.y - height * 0.5f - skidPlateHeight * 0.5f - skidPlateDownOffset,
                    center.z
                ),
                new Vector3(
                    width * skidPlateWidthMultiplier,
                    skidPlateHeight,
                    length * skidPlateLengthMultiplier
                )
            );
        }

        Debug.Log(
            "Generated anti-stuck colliders for " + gameObject.name +
            " on layer: " + GetGeneratedLayerDebugName()
        );
    }

    [ContextMenu("Delete Anti-Stuck Colliders")]
    public void DeleteGeneratedColliders()
    {
        Transform existing = transform.Find(generatedParentName);

        if (existing != null)
        {
            if (Application.isPlaying)
                Destroy(existing.gameObject);
            else
                DestroyImmediate(existing.gameObject);
        }
    }

    private void CreateBox(string objectName, Vector3 localPosition, Vector3 localSize)
    {
        GameObject obj = new GameObject(objectName);
        obj.transform.SetParent(generatedParent);
        obj.transform.localPosition = localPosition;
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;
        ApplyGeneratedLayer(obj);

        BoxCollider box = obj.AddComponent<BoxCollider>();
        box.size = localSize;
        box.center = Vector3.zero;
        box.isTrigger = false;

        if (antiStuckMaterial != null)
            box.material = antiStuckMaterial;
    }

    private void CreateCapsule(
        string objectName,
        Vector3 localPosition,
        float radius,
        float height,
        int direction
    )
    {
        GameObject obj = new GameObject(objectName);
        obj.transform.SetParent(generatedParent);
        obj.transform.localPosition = localPosition;
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;
        ApplyGeneratedLayer(obj);

        CapsuleCollider capsule = obj.AddComponent<CapsuleCollider>();
        capsule.radius = radius;
        capsule.height = Mathf.Max(height, radius * 2f);
        capsule.direction = direction;
        capsule.center = Vector3.zero;
        capsule.isTrigger = false;

        if (antiStuckMaterial != null)
            capsule.material = antiStuckMaterial;
    }

    private void ApplyGeneratedLayer(GameObject obj)
    {
        if (obj == null)
            return;

        if (generatedColliderLayer != -1)
            obj.layer = generatedColliderLayer;
        else
            obj.layer = gameObject.layer;
    }

    private string GetGeneratedLayerDebugName()
    {
        if (generatedColliderLayer == -1)
            return gameObject.layer + " / " + LayerMask.LayerToName(gameObject.layer);

        return generatedColliderLayer + " / " + LayerMask.LayerToName(generatedColliderLayer);
    }

    private Collider FindLargestCollider()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();

        Collider largest = null;
        float largestVolume = 0f;

        foreach (Collider col in colliders)
        {
            if (col == null)
                continue;

            if (col.isTrigger)
                continue;

            if (col.transform.name.Contains("AntiStuck"))
                continue;

            if (col.transform.parent != null && col.transform.parent.name == generatedParentName)
                continue;

            Bounds b = col.bounds;
            float volume = b.size.x * b.size.y * b.size.z;

            if (volume > largestVolume)
            {
                largestVolume = volume;
                largest = col;
            }
        }

        return largest;
    }

    private Bounds GetLocalBounds(Collider col)
    {
        Bounds worldBounds = col.bounds;

        Vector3 localCenter = transform.InverseTransformPoint(worldBounds.center);

        Vector3 localSize = new Vector3(
            worldBounds.size.x / Mathf.Abs(transform.lossyScale.x),
            worldBounds.size.y / Mathf.Abs(transform.lossyScale.y),
            worldBounds.size.z / Mathf.Abs(transform.lossyScale.z)
        );

        return new Bounds(localCenter, localSize);
    }

    private void OnValidate()
    {
        roundedBumperRadius = Mathf.Max(0.01f, roundedBumperRadius);
        roundedBumperWidthMultiplier = Mathf.Max(0.01f, roundedBumperWidthMultiplier);

        bumperWidthMultiplier = Mathf.Max(0.01f, bumperWidthMultiplier);
        bumperHeightMultiplier = Mathf.Max(0.01f, bumperHeightMultiplier);
        bumperDepth = Mathf.Max(0.01f, bumperDepth);

        sideGuardThickness = Mathf.Max(0.01f, sideGuardThickness);
        sideGuardHeightMultiplier = Mathf.Max(0.01f, sideGuardHeightMultiplier);
        sideGuardLengthMultiplier = Mathf.Max(0.01f, sideGuardLengthMultiplier);
        sideGuardInset = Mathf.Max(0f, sideGuardInset);

        skidPlateHeight = Mathf.Max(0.01f, skidPlateHeight);
        skidPlateWidthMultiplier = Mathf.Max(0.01f, skidPlateWidthMultiplier);
        skidPlateLengthMultiplier = Mathf.Max(0.01f, skidPlateLengthMultiplier);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
            return;

        Transform parent = transform.Find(generatedParentName);

        if (parent == null)
            return;

        foreach (Collider col in parent.GetComponentsInChildren<Collider>())
        {
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = col.transform.localToWorldMatrix;

            Gizmos.color = new Color(0f, 1f, 1f, 0.25f);

            if (col is BoxCollider box)
            {
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (col is CapsuleCollider capsule)
            {
                Gizmos.color = Color.cyan;

                Vector3 size;

                if (capsule.direction == 0)
                    size = new Vector3(capsule.height, capsule.radius * 2f, capsule.radius * 2f);
                else if (capsule.direction == 1)
                    size = new Vector3(capsule.radius * 2f, capsule.height, capsule.radius * 2f);
                else
                    size = new Vector3(capsule.radius * 2f, capsule.radius * 2f, capsule.height);

                Gizmos.DrawWireCube(capsule.center, size);
            }

            Gizmos.matrix = oldMatrix;
        }
    }
}