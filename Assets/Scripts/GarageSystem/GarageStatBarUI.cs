using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GarageStatBarUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text labelText;

    [Tooltip("Drag Segment_01 to Segment_10 here, or leave empty and enable Auto Find Segments.")]
    public Image[] segmentImages;

    [Header("Auto Setup")]
    public bool autoFindSegments = true;
    public string segmentParentName = "Slider";

    [Header("Settings")]
    public string label = "Stat";
    public float maxValue = 10f;

    [Header("Segment Colours")]
    public Color filledColor = new Color(1f, 0.05f, 0.75f, 1f);
    public Color emptyColor = new Color(0.015f, 0.06f, 0.085f, 0.9f);

    [Header("Display")]
    [Tooltip("Use this only for previewing in the Inspector.")]
    [Range(0f, 10f)]
    public float previewValue = 5f;

    private void Awake()
    {
        TryAutoFindSegments();
        RefreshLabel();
    }

    private void Reset()
    {
        TryAutoFindSegments();
        RefreshLabel();
        SetValue(previewValue);
    }

    public void SetValue(float value)
    {
        TryAutoFindSegments();

        float clampedValue = Mathf.Clamp(value, 0f, maxValue);
        float normalizedValue = maxValue <= 0f ? 0f : clampedValue / maxValue;

        int segmentCount = segmentImages != null ? segmentImages.Length : 0;

        if (segmentCount <= 0)
        {
            RefreshLabel();
            return;
        }

        float filledSegmentsFloat = normalizedValue * segmentCount;

        for (int i = 0; i < segmentCount; i++)
        {
            if (segmentImages[i] == null)
                continue;

            bool isFilled = i < Mathf.RoundToInt(filledSegmentsFloat);
            segmentImages[i].color = isFilled ? filledColor : emptyColor;
        }

        RefreshLabel();
    }

    public void Clear()
    {
        if (segmentImages == null)
            return;

        for (int i = 0; i < segmentImages.Length; i++)
        {
            if (segmentImages[i] != null)
                segmentImages[i].color = emptyColor;
        }

        RefreshLabel();
    }

    private void RefreshLabel()
    {
        if (labelText != null)
        {
            labelText.text = label;
        }
    }

    private void TryAutoFindSegments()
    {
        if (!autoFindSegments)
            return;

        if (segmentImages != null && segmentImages.Length > 0)
            return;

        Transform sliderParent = transform.Find(segmentParentName);

        if (sliderParent == null)
        {
            sliderParent = transform;
        }

        Image[] foundImages = sliderParent.GetComponentsInChildren<Image>(true);

        // The first image might be the Slider background.
        // We only want children named Segment_01, Segment_02, etc.
        System.Collections.Generic.List<Image> segments = new System.Collections.Generic.List<Image>();

        for (int i = 0; i < foundImages.Length; i++)
        {
            if (foundImages[i] == null)
                continue;

            string objectName = foundImages[i].gameObject.name.ToLowerInvariant();

            if (objectName.StartsWith("segment_"))
            {
                segments.Add(foundImages[i]);
            }
        }

        segments.Sort((a, b) => string.CompareOrdinal(a.gameObject.name, b.gameObject.name));

        segmentImages = segments.ToArray();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        TryAutoFindSegments();
        RefreshLabel();

        if (!Application.isPlaying)
        {
            SetValue(previewValue);
        }
    }
#endif
}