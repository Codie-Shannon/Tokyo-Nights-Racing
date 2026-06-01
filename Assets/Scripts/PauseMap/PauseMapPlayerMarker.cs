using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PauseMapPlayerMarker : MonoBehaviour
{
    [Header("Player Runtime Detection")]
    [SerializeField] private Transform playerCar;

    [Tooltip("Recommended: tag your spawned player car root as Player.")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("Optional fallback. If your spawned car name contains this text, the script can find it.")]
    [SerializeField] private string playerNameContains = "Player";

    [SerializeField] private bool keepSearchingForPlayer = true;
    [SerializeField] private float searchInterval = 0.25f;

    [Header("Generated Pause Map UI Names")]
    [SerializeField] private string pauseCanvasName = "PauseMenuCanvas";
    [SerializeField] private string generatedRootName = "GeneratedPauseMapUI";
    [SerializeField] private string pauseRootName = "PauseRoot";
    [SerializeField] private string mapPanelName = "MapPanel";

    [Header("World Bounds From Capture Tool")]
    [SerializeField] private Transform worldBottomLeft;
    [SerializeField] private Transform worldTopRight;

    [Header("Marker Visual")]
    [SerializeField] private bool createMarkerAutomatically = true;
    [SerializeField] private string markerObjectName = "PlayerMarker";
    [SerializeField] private Vector2 markerSize = new Vector2(80f, 80f);

    [Header("Player Sprite")]
    [SerializeField] private Sprite playerMarkerSprite;
    [SerializeField] private Color playerMarkerTint = Color.white;
    [SerializeField] private bool preservePlayerSpriteAspect = true;
    [SerializeField] private bool useSpriteInsteadOfTextArrow = true;

    [Header("Fallback Text Marker")]
    [SerializeField] private string arrowText = "?";
    [SerializeField] private string labelText = "YOU";
    [SerializeField] private Color arrowColor = new Color(0f, 0.95f, 1f, 1f);
    [SerializeField] private Color labelColor = new Color(0f, 0.95f, 1f, 1f);
    [SerializeField] private float arrowFontSize = 42f;
    [SerializeField] private float labelFontSize = 16f;
    [SerializeField] private bool showLabel = false;

    [Header("Map Position Options")]
    [SerializeField] private bool clampToMap = true;
    [SerializeField] private bool invertX = false;
    [SerializeField] private bool invertY = false;

    [Header("Rotation")]
    [SerializeField] private bool rotateArrowToPlayer = true;
    [SerializeField] private float rotationOffset = 0f;

    [Header("Debug")]
    [SerializeField] private bool logWarnings = true;

    private GameObject pauseRoot;
    private RectTransform mapPanel;
    private RectTransform playerMarkerRoot;
    private RectTransform arrowRect;

    private float nextSearchTime;

    private void Awake()
    {
        AutoFindWorldBoundsIfNeeded();
        FindGeneratedMapUI();
        CreateOrFindPlayerMarker();
    }

    private void OnEnable()
    {
        AutoFindWorldBoundsIfNeeded();
        FindGeneratedMapUI();
        CreateOrFindPlayerMarker();
        TryFindPlayer();
        UpdatePlayerMarker();
    }

    private void Update()
    {
        if (keepSearchingForPlayer && playerCar == null && Time.unscaledTime >= nextSearchTime)
        {
            nextSearchTime = Time.unscaledTime + searchInterval;
            TryFindPlayer();
        }

        if (pauseRoot == null || !pauseRoot.activeInHierarchy)
            return;

        UpdatePlayerMarker();
    }

    [ContextMenu("Find UI And Create Player Marker")]
    public void FindUIAndCreateMarker()
    {
        AutoFindWorldBoundsIfNeeded();
        FindGeneratedMapUI();

        DeleteExistingMarkerIfWrongVisual();
        CreateOrFindPlayerMarker();

        TryFindPlayer();
        UpdatePlayerMarker();
    }

    [ContextMenu("Update Player Marker")]
    public void UpdatePlayerMarker()
    {
        if (playerCar == null)
            TryFindPlayer();

        if (playerCar == null)
        {
            if (logWarnings)
                Debug.LogWarning("PauseMapPlayerMarker: Player car not found yet. Tag the spawned car root as Player, or set Player Name Contains.");

            return;
        }

        if (mapPanel == null)
        {
            FindGeneratedMapUI();

            if (mapPanel == null)
            {
                if (logWarnings)
                    Debug.LogWarning("PauseMapPlayerMarker: MapPanel not found. Build/rebuild the pause map UI first.");

                return;
            }
        }

        if (playerMarkerRoot == null)
        {
            CreateOrFindPlayerMarker();

            if (playerMarkerRoot == null)
                return;
        }

        AutoFindWorldBoundsIfNeeded();

        if (worldBottomLeft == null || worldTopRight == null)
        {
            if (logWarnings)
            {
                Debug.LogWarning(
                    "PauseMapPlayerMarker: World bounds missing. You need MapWorldBottomLeft and MapWorldTopRight. " +
                    "Select MapCaptureTool and run '1. Setup / Fix Wide Top Down Camera'."
                );
            }

            return;
        }

        Vector3 playerPos = playerCar.position;

        float minX = worldBottomLeft.position.x;
        float maxX = worldTopRight.position.x;
        float minZ = worldBottomLeft.position.z;
        float maxZ = worldTopRight.position.z;

        float normalizedX = Mathf.InverseLerp(minX, maxX, playerPos.x);
        float normalizedY = Mathf.InverseLerp(minZ, maxZ, playerPos.z);

        if (invertX)
            normalizedX = 1f - normalizedX;

        if (invertY)
            normalizedY = 1f - normalizedY;

        if (clampToMap)
        {
            normalizedX = Mathf.Clamp01(normalizedX);
            normalizedY = Mathf.Clamp01(normalizedY);
        }

        float mapWidth = mapPanel.rect.width;
        float mapHeight = mapPanel.rect.height;

        float anchoredX = Mathf.Lerp(-mapWidth * 0.5f, mapWidth * 0.5f, normalizedX);
        float anchoredY = Mathf.Lerp(-mapHeight * 0.5f, mapHeight * 0.5f, normalizedY);

        playerMarkerRoot.anchoredPosition = new Vector2(anchoredX, anchoredY);
        playerMarkerRoot.SetAsLastSibling();

        if (rotateArrowToPlayer && arrowRect != null)
        {
            float yRotation = playerCar.eulerAngles.y;
            arrowRect.localRotation = Quaternion.Euler(0f, 0f, -yRotation + rotationOffset);
        }
    }

    private void TryFindPlayer()
    {
        if (playerCar != null)
            return;

        GameObject taggedPlayer = GameObject.FindGameObjectWithTag(playerTag);

        if (taggedPlayer != null)
        {
            playerCar = taggedPlayer.transform;
            return;
        }

        if (!string.IsNullOrWhiteSpace(playerNameContains))
        {
            GameObject[] allObjects = FindObjectsOfType<GameObject>();

            for (int i = 0; i < allObjects.Length; i++)
            {
                GameObject obj = allObjects[i];

                if (!obj.activeInHierarchy)
                    continue;

                if (obj.name.ToLower().Contains(playerNameContains.ToLower()))
                {
                    playerCar = obj.transform;
                    return;
                }
            }
        }
    }

    private void AutoFindWorldBoundsIfNeeded()
    {
        if (worldBottomLeft == null)
        {
            GameObject bottomLeft = GameObject.Find("MapWorldBottomLeft");

            if (bottomLeft != null)
                worldBottomLeft = bottomLeft.transform;
        }

        if (worldTopRight == null)
        {
            GameObject topRight = GameObject.Find("MapWorldTopRight");

            if (topRight != null)
                worldTopRight = topRight.transform;
        }
    }

    private void FindGeneratedMapUI()
    {
        GameObject canvasObject = GameObject.Find(pauseCanvasName);

        if (canvasObject == null)
            return;

        Transform generatedRoot = canvasObject.transform.Find(generatedRootName);

        if (generatedRoot == null)
            return;

        Transform pauseRootTransform = generatedRoot.Find(pauseRootName);

        if (pauseRootTransform == null)
            return;

        pauseRoot = pauseRootTransform.gameObject;

        Transform mapPanelTransform = pauseRootTransform.Find(mapPanelName);

        if (mapPanelTransform == null)
            return;

        mapPanel = mapPanelTransform.GetComponent<RectTransform>();
    }

    private void DeleteExistingMarkerIfWrongVisual()
    {
        if (mapPanel == null)
            return;

        Transform existing = mapPanel.Find(markerObjectName);

        if (existing == null)
            return;

        DestroyImmediateSafe(existing.gameObject);

        playerMarkerRoot = null;
        arrowRect = null;
    }

    private void CreateOrFindPlayerMarker()
    {
        if (mapPanel == null)
            return;

        Transform existing = mapPanel.Find(markerObjectName);

        if (existing != null)
        {
            playerMarkerRoot = existing.GetComponent<RectTransform>();

            Transform arrow = existing.Find("PlayerArrow");

            if (arrow != null)
                arrowRect = arrow.GetComponent<RectTransform>();

            existing.SetAsLastSibling();
            return;
        }

        if (!createMarkerAutomatically)
            return;

        GameObject markerRootObject = new GameObject(markerObjectName);
        markerRootObject.transform.SetParent(mapPanel, false);

        playerMarkerRoot = markerRootObject.AddComponent<RectTransform>();
        playerMarkerRoot.anchorMin = new Vector2(0.5f, 0.5f);
        playerMarkerRoot.anchorMax = new Vector2(0.5f, 0.5f);
        playerMarkerRoot.pivot = new Vector2(0.5f, 0.5f);
        playerMarkerRoot.sizeDelta = markerSize;

        CanvasGroup group = markerRootObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        CreateArrow(markerRootObject.transform);

        if (showLabel)
            CreateLabel(markerRootObject.transform);

        playerMarkerRoot.SetAsLastSibling();
    }

    private void CreateArrow(Transform parent)
    {
        GameObject arrowObject = new GameObject("PlayerArrow");
        arrowObject.transform.SetParent(parent, false);

        arrowRect = arrowObject.AddComponent<RectTransform>();
        arrowRect.anchorMin = new Vector2(0.5f, 0.5f);
        arrowRect.anchorMax = new Vector2(0.5f, 0.5f);
        arrowRect.pivot = new Vector2(0.5f, 0.5f);
        arrowRect.anchoredPosition = Vector2.zero;
        arrowRect.sizeDelta = markerSize;

        if (useSpriteInsteadOfTextArrow && playerMarkerSprite != null)
        {
            Image image = arrowObject.AddComponent<Image>();
            image.sprite = playerMarkerSprite;
            image.color = playerMarkerTint;
            image.type = Image.Type.Simple;
            image.preserveAspect = preservePlayerSpriteAspect;
            image.raycastTarget = false;
        }
        else
        {
            TextMeshProUGUI arrowTMP = arrowObject.AddComponent<TextMeshProUGUI>();
            arrowTMP.text = arrowText;
            arrowTMP.fontSize = arrowFontSize;
            arrowTMP.color = arrowColor;
            arrowTMP.fontStyle = FontStyles.Bold;
            arrowTMP.alignment = TextAlignmentOptions.Center;
            arrowTMP.raycastTarget = false;
        }
    }

    private void CreateLabel(Transform parent)
    {
        GameObject labelObject = new GameObject("PlayerLabel");
        labelObject.transform.SetParent(parent, false);

        RectTransform labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = new Vector2(0f, -44f);
        labelRect.sizeDelta = new Vector2(120f, 30f);

        TextMeshProUGUI labelTMP = labelObject.AddComponent<TextMeshProUGUI>();
        labelTMP.text = labelText;
        labelTMP.fontSize = labelFontSize;
        labelTMP.color = labelColor;
        labelTMP.fontStyle = FontStyles.Bold;
        labelTMP.alignment = TextAlignmentOptions.Center;
        labelTMP.raycastTarget = false;
    }

    private void DestroyImmediateSafe(GameObject obj)
    {
        if (obj == null)
            return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
            DestroyImmediate(obj);
        else
            Destroy(obj);
#else
        Destroy(obj);
#endif
    }
}