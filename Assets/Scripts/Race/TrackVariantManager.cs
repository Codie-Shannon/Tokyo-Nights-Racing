using System;
using System.Collections.Generic;
using UnityEngine;

public class TrackVariantManager : MonoBehaviour
{
    [Serializable]
    public class MaterialSwap
    {
        [Tooltip("Renderer whose material should change.")]
        public Renderer targetRenderer;

        [Tooltip("Material slot index on the renderer. Usually 0.")]
        public int materialIndex = 0;

        [Tooltip("Material to apply for this variant.")]
        public Material material;
    }

    [Serializable]
    public class TrackVariantData
    {
        [Header("Variant")]
        public RaceLoadRequest.TrackVariant variant;

        [Header("Objects To Enable")]
        public List<GameObject> enableObjects = new List<GameObject>();

        [Header("Objects To Disable")]
        public List<GameObject> disableObjects = new List<GameObject>();

        [Header("Material Swaps")]
        public List<MaterialSwap> materialSwaps = new List<MaterialSwap>();

        [Header("Optional Lighting")]
        public Light directionalLight;
        public Color lightColor = Color.white;
        public float lightIntensity = 1f;
        public Vector3 lightEulerRotation = new Vector3(45f, -30f, 0f);

        [Header("Optional Fog")]
        public bool overrideFog = false;
        public bool fogEnabled = false;
        public Color fogColor = Color.gray;
        public float fogDensity = 0.01f;
    }

    [Header("Start Behaviour")]
    public bool applyOnStart = true;

    [Tooltip("If true, scene starts using Editor Preview Variant instead of RaceLoadRequest.")]
    public bool useEditorPreviewOnStart = true;

    [Header("Editor Preview")]
    public RaceLoadRequest.TrackVariant editorPreviewVariant = RaceLoadRequest.TrackVariant.Road;

    [Header("Variants")]
    public List<TrackVariantData> variants = new List<TrackVariantData>();

    private void Start()
    {
        if (!applyOnStart)
            return;

        if (useEditorPreviewOnStart)
        {
            ApplyVariant(editorPreviewVariant);
        }
        else
        {
            ApplyVariant(RaceLoadRequest.SelectedTrackVariant);
        }
    }

    [ContextMenu("Preview Editor Variant")]
    public void PreviewEditorVariant()
    {
        ApplyVariant(editorPreviewVariant);
    }

    public void ApplyRoad()
    {
        ApplyVariant(RaceLoadRequest.TrackVariant.Road);
    }

    public void ApplyOffRoad()
    {
        ApplyVariant(RaceLoadRequest.TrackVariant.OffRoad);
    }

    public void ApplyAllTerrain()
    {
        ApplyVariant(RaceLoadRequest.TrackVariant.AllTerrain);
    }

    public void ApplyMonsterTruck()
    {
        ApplyVariant(RaceLoadRequest.TrackVariant.MonsterTruck);
    }

    public void ApplyVariant(RaceLoadRequest.TrackVariant variant)
    {
        TrackVariantData data = GetVariantData(variant);

        if (data == null)
        {
            Debug.LogWarning("No TrackVariantData found for variant: " + variant);
            return;
        }

        ApplyObjects(data);
        ApplyMaterials(data);
        ApplyLighting(data);
        ApplyFog(data);

        Debug.Log("Applied track variant: " + variant);
    }

    private TrackVariantData GetVariantData(RaceLoadRequest.TrackVariant variant)
    {
        for (int i = 0; i < variants.Count; i++)
        {
            if (variants[i] != null && variants[i].variant == variant)
                return variants[i];
        }

        return null;
    }

    private void ApplyObjects(TrackVariantData data)
    {
        for (int i = 0; i < data.disableObjects.Count; i++)
        {
            if (data.disableObjects[i] != null)
                data.disableObjects[i].SetActive(false);
        }

        for (int i = 0; i < data.enableObjects.Count; i++)
        {
            if (data.enableObjects[i] != null)
                data.enableObjects[i].SetActive(true);
        }
    }

    private void ApplyMaterials(TrackVariantData data)
    {
        for (int i = 0; i < data.materialSwaps.Count; i++)
        {
            MaterialSwap swap = data.materialSwaps[i];

            if (swap == null)
                continue;

            if (swap.targetRenderer == null)
            {
                Debug.LogWarning("Material swap missing target renderer.");
                continue;
            }

            Material[] materials = swap.targetRenderer.sharedMaterials;

            if (materials == null || materials.Length == 0)
            {
                Debug.LogWarning("Renderer has no materials: " + swap.targetRenderer.name);
                continue;
            }

            if (swap.materialIndex < 0 || swap.materialIndex >= materials.Length)
            {
                Debug.LogWarning(
                    "Material index out of range on " + swap.targetRenderer.name +
                    ". Index: " + swap.materialIndex +
                    ", Material Count: " + materials.Length
                );
                continue;
            }

            if (swap.material == null)
            {
                Debug.LogWarning("Material swap missing replacement material for renderer: " + swap.targetRenderer.name);
                continue;
            }

            materials[swap.materialIndex] = swap.material;
            swap.targetRenderer.sharedMaterials = materials;
        }
    }

    private void ApplyLighting(TrackVariantData data)
    {
        if (data.directionalLight == null)
            return;

        data.directionalLight.color = data.lightColor;
        data.directionalLight.intensity = data.lightIntensity;
        data.directionalLight.transform.rotation = Quaternion.Euler(data.lightEulerRotation);
    }

    private void ApplyFog(TrackVariantData data)
    {
        if (!data.overrideFog)
            return;

        RenderSettings.fog = data.fogEnabled;
        RenderSettings.fogColor = data.fogColor;
        RenderSettings.fogDensity = data.fogDensity;
    }
}