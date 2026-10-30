using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DokkaebiFireMapUI : MonoBehaviour
{
    [SerializeField] private Vector2 mapSize = new Vector2(260f, 260f);
    [SerializeField] private Vector2 screenOffset = new Vector2(-24f, -24f);
    [SerializeField] private Color mapColor = new Color(0.04f, 0.09f, 0.14f, 0.88f);
    [SerializeField] private Color fireMarkerColor = new Color(0.72f, 0.25f, 1f, 1f);
    [SerializeField] private Color playerMarkerColor = Color.white;

    private readonly Dictionary<FireCollectible, RectTransform> fireMarkers =
        new Dictionary<FireCollectible, RectTransform>();

    private RectTransform mapRect;
    private RectTransform playerMarker;
    private Transform player;
    private Bounds worldBounds;
    private Texture2D markerTexture;
    private float nextRefreshTime;

    private void Awake()
    {
        markerTexture = Texture2D.whiteTexture;
        CreateMapUI();
        CacheWorldBounds();
        player = GameObject.Find("고양이")?.transform;
        SyncFireMarkers();
    }

    private void Update()
    {
        if (Time.time >= nextRefreshTime)
        {
            nextRefreshTime = Time.time + 0.25f;
            SyncFireMarkers();
        }

        UpdateMarkerPositions();
    }

private void CreateMapUI()
{
    GameObject canvasObject = new GameObject(
        "DokkaebiFireMapCanvas",
        typeof(RectTransform),
        typeof(Canvas),
        typeof(CanvasScaler),
        typeof(GraphicRaycaster)
    );

    Canvas canvas = canvasObject.GetComponent<Canvas>();
    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
    canvas.sortingOrder = 10;

    CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
    scaler.referenceResolution = new Vector2(1920f, 1080f);
    scaler.matchWidthOrHeight = 0.5f;

    GameObject mapObject = new GameObject(
        "DokkaebiFireMap",
        typeof(RectTransform),
        typeof(CanvasRenderer),
        typeof(RawImage)
    );

    mapObject.transform.SetParent(canvasObject.transform, false);
    mapRect = mapObject.GetComponent<RectTransform>();
    mapRect.anchorMin = Vector2.one;
    mapRect.anchorMax = Vector2.one;
    mapRect.pivot = Vector2.one;
    mapRect.anchoredPosition = screenOffset;
    mapRect.sizeDelta = mapSize;

    RawImage mapImage = mapObject.GetComponent<RawImage>();
    mapImage.texture = markerTexture;
    mapImage.color = mapColor;
    mapImage.raycastTarget = false;

    playerMarker = CreateMarker(
        "CatMarker",
        mapRect,
        playerMarkerColor,
        12f
    );
}

    private void CacheWorldBounds()
    {
        GameObject groundObject = GameObject.Find("Ground");
        Collider groundCollider = groundObject != null
            ? groundObject.GetComponent<Collider>()
            : null;

        if (groundCollider != null)
        {
            worldBounds = groundCollider.bounds;
            return;
        }

        worldBounds = new Bounds(Vector3.zero, new Vector3(100f, 1f, 100f));
    }

    private void SyncFireMarkers()
    {
        FireCollectible[] fires = FindObjectsByType<FireCollectible>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        HashSet<FireCollectible> activeFires =
            new HashSet<FireCollectible>(fires);

        List<FireCollectible> removedFires =
            new List<FireCollectible>();

        foreach (KeyValuePair<FireCollectible, RectTransform> pair in fireMarkers)
        {
            if (pair.Key == null || !activeFires.Contains(pair.Key))
            {
                Destroy(pair.Value.gameObject);
                removedFires.Add(pair.Key);
            }
        }

        foreach (FireCollectible fire in removedFires)
        {
            fireMarkers.Remove(fire);
        }

        foreach (FireCollectible fire in fires)
        {
            if (!fireMarkers.ContainsKey(fire))
            {
                fireMarkers.Add(
                    fire,
                    CreateMarker(
                        "DokkaebiFireMarker",
                        mapRect,
                        fireMarkerColor,
                        14f
                    )
                );
            }
        }
    }

    private void UpdateMarkerPositions()
    {
        if (player != null)
        {
            playerMarker.anchoredPosition =
                WorldToMapPosition(player.position);
        }

        foreach (KeyValuePair<FireCollectible, RectTransform> pair in fireMarkers)
        {
            if (pair.Key != null)
            {
                pair.Value.anchoredPosition =
                    WorldToMapPosition(pair.Key.transform.position);
            }
        }
    }

    private RectTransform CreateMarker(
        string markerName,
        RectTransform parent,
        Color color,
        float size
    )
    {
        GameObject markerObject = new GameObject(
            markerName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(RawImage)
        );

        markerObject.transform.SetParent(parent, false);

        RectTransform markerRect =
            markerObject.GetComponent<RectTransform>();
        markerRect.anchorMin = new Vector2(0.5f, 0.5f);
        markerRect.anchorMax = new Vector2(0.5f, 0.5f);
        markerRect.pivot = new Vector2(0.5f, 0.5f);
        markerRect.sizeDelta = new Vector2(size, size);

        RawImage markerImage = markerObject.GetComponent<RawImage>();
        markerImage.texture = markerTexture;
        markerImage.color = color;
        markerImage.raycastTarget = false;

        return markerRect;
    }

    private Vector2 WorldToMapPosition(Vector3 worldPosition)
    {
        float normalizedX = Mathf.InverseLerp(
            worldBounds.min.x,
            worldBounds.max.x,
            worldPosition.x
        );

        float normalizedY = Mathf.InverseLerp(
            worldBounds.min.z,
            worldBounds.max.z,
            worldPosition.z
        );

        return new Vector2(
            (normalizedX - 0.5f) * mapRect.rect.width,
            (normalizedY - 0.5f) * mapRect.rect.height
        );
    }
}
