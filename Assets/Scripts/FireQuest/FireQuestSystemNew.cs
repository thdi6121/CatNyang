using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class FireQuestManager : MonoBehaviour
{
    public static FireQuestManager Instance { get; private set; }

    [SerializeField] private int requiredCount = 6;
    [SerializeField] private Transform player;
    [SerializeField] private float completionJumpForceBonus = 3f;

    [Header("Completion Effect")]
    [SerializeField] private GameObject completionEffectPrefab;
    [SerializeField] private float completionEffectScale = 0.003f;
    [SerializeField] private float completionEffectHeight = 0.2f;
    [SerializeField, Min(0f)] private float completionEffectDelay = 0.8f;

    [Header("Completion Message")]
    [SerializeField, Min(0f)] private float completionMessageDelay = 1f;
    [SerializeField, Min(0.1f)] private float completionMessageDuration = 2.5f;
    [SerializeField, Min(0.05f)] private float completionMessageFadeDuration = 0.45f;

    private bool showCompletionMessage;
    private float completionMessageAlpha;


    [SerializeField] private Camera minimapCamera;
    [SerializeField] private RenderTexture minimapTexture;
    [SerializeField, Range(1f, 30f)] private float minimapRefreshRate = 8f;
    [SerializeField, Min(100)] private int particleMaxBudget = 500;
    private float nextMinimapRenderTime;

    [Header("Minimap Markers")]
    [SerializeField] private Texture2D fireMarkerTexture;
    [SerializeField] private Texture2D playerMarkerTexture;

    [SerializeField] private Color fireMarkerColor = Color.white;
    [SerializeField] private Color playerMarkerColor = Color.white;

    [SerializeField] private float fireMarkerSize = 20f;
    [SerializeField] private float playerMarkerSize = 24f;

    private readonly List<FireCollectible> fires = new List<FireCollectible>();
    private Texture2D whiteTexture;
    private Texture2D questFrameTexture;
    private Texture2D minimapFrameTexture;
    private Texture2D sideQuestFrameTexture;
    private Texture2D completionMessageFrameTexture;
    private Font uiFont;

    [Header("Optional Quests")]
    [SerializeField, Min(1)] private int birdQuestTarget = 2;
    [SerializeField, Min(1)] private int ratQuestTarget = 3;
    [SerializeField, Min(1)] private int fishQuestTarget = 1;
    [SerializeField, Min(1)] private int fortuneQuestTarget = 1;

    private readonly HashSet<BirdFleeController> targetBirds = new HashSet<BirdFleeController>();
    private int birdsFledCount;
    private int ratsCaughtCount;
    private int fishEatenCount;
    private int fortuneViewedCount;

    // Preserve optional quest progress when the main quest completes.
    private int preservedBirdsFledCount;
    private int preservedRatsCaughtCount;
    private int preservedFishEatenCount;
    private int preservedFortuneViewedCount;
    private int postFireCollectedCount;
    private int postFireTargetCount;

    private int collectedCount;
    private bool complete;
    private bool completionRewardApplied;
    private Transform completionEffectTarget;

    private Bounds worldBounds;
    private float nextMarkerRefreshTime;

    public int RequiredCount => requiredCount;
    public int CollectedCount => collectedCount;

private void Awake()
    {
        Instance = this;
        whiteTexture = Texture2D.whiteTexture;
        uiFont = Resources.Load<Font>("Fonts/DNFBitBitv2");
        questFrameTexture = Resources.Load<Texture2D>("UI/Frames/메인퀘스트");
        minimapFrameTexture = Resources.Load<Texture2D>("UI/Frames/미니맵");
        sideQuestFrameTexture = Resources.Load<Texture2D>("UI/Frames/서브퀘스트");
        completionMessageFrameTexture = Resources.Load<Texture2D>("UI/Frames/감사");

        if (uiFont == null)
            Debug.LogWarning("DNFBitBit v2 글꼴을 불러오지 못했습니다. 기본 글꼴을 사용합니다.", this);
    }

    private void Start()
    {
        CreateMinimapCamera();
        CacheWorldBounds();
        SyncFireMarkers();
        InitializeSideQuests();
        LimitParticleBudgets();

        if (player == null)
        {
            GameObject catObject = GameObject.Find("고양이");
            if (catObject != null)
                player = catObject.transform;
        }
    }

    private void Update()
    {
        if (minimapCamera != null && Time.unscaledTime >= nextMinimapRenderTime)
        {
            nextMinimapRenderTime = Time.unscaledTime + 1f / Mathf.Max(1f, minimapRefreshRate);
            minimapCamera.Render();
        }

        if (Time.time < nextMarkerRefreshTime)
            return;

        nextMarkerRefreshTime = Time.time + 0.25f;
        SyncFireMarkers();

        if (player == null)
        {
            KeyboardCatMove cat = FindAnyObjectByType<KeyboardCatMove>();
            if (cat != null)
                player = cat.transform;
        }
    }

    private void LimitParticleBudgets()
    {
        ParticleSystem[] particleSystems = FindObjectsByType<ParticleSystem>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem.MainModule main = particleSystems[i].main;
            if (main.maxParticles > particleMaxBudget)
                main.maxParticles = particleMaxBudget;
        }
    }

private void SyncFireMarkers()
    {
        // FireCollectible instances register themselves through Register().
        // Only remove collected/destroyed entries here to avoid a full-scene search every 0.25 seconds.
        for (int i = fires.Count - 1; i >= 0; i--)
        {
            if (fires[i] == null)
                fires.RemoveAt(i);
        }
    }

    public void Register(FireCollectible fire)
    {
        if (!fires.Contains(fire))
            fires.Add(fire);
    }

public void Collect(FireCollectible fire)
    {
        if (fire == null || fire.Collected)
            return;

        if (complete)
        {
            fire.MarkCollected();
            postFireCollectedCount = Mathf.Min(postFireCollectedCount + 1, postFireTargetCount);
            return;
        }

        fire.MarkCollected();
        collectedCount = Mathf.Min(collectedCount + 1, requiredCount);

        if (collectedCount < requiredCount)
        {
            MemoryRecallUI.ShowMemory(collectedCount);
            return;
        }

        PreserveSideQuestProgress();
        complete = true;
        postFireTargetCount = CountRemainingFires();
        MemoryRecallUI.ShowMemory(collectedCount, ApplyCompletionReward);
    }

    private void InitializeSideQuests()
    {
        BirdFleeController[] birds = FindObjectsByType<BirdFleeController>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.InstanceID
        );

        for (int i = 0; i < birds.Length; i++)
        {
            birds[i].SetSideQuestTarget(true);
            targetBirds.Add(birds[i]);
        }
    }

    public void NotifyBirdFled(BirdFleeController bird)
    {
        if (bird == null || !targetBirds.Remove(bird))
            return;

        birdsFledCount = Mathf.Min(Mathf.Max(birdsFledCount, preservedBirdsFledCount) + 1, birdQuestTarget);
        preservedBirdsFledCount = Mathf.Max(preservedBirdsFledCount, birdsFledCount);
    }


    public void NotifyRatCaught()
    {
        ratsCaughtCount = Mathf.Min(Mathf.Max(ratsCaughtCount, preservedRatsCaughtCount) + 1, ratQuestTarget);
        preservedRatsCaughtCount = Mathf.Max(preservedRatsCaughtCount, ratsCaughtCount);
    }

    public void NotifyFishEaten()
    {
        fishEatenCount = Mathf.Min(Mathf.Max(fishEatenCount, preservedFishEatenCount) + 1, fishQuestTarget);
        preservedFishEatenCount = Mathf.Max(preservedFishEatenCount, fishEatenCount);
    }

    public void NotifyFortuneViewed()
    {
        fortuneViewedCount = Mathf.Min(Mathf.Max(fortuneViewedCount, preservedFortuneViewedCount) + 1, fortuneQuestTarget);
        preservedFortuneViewedCount = Mathf.Max(preservedFortuneViewedCount, fortuneViewedCount);
    }

    private void PreserveSideQuestProgress()
    {
        preservedBirdsFledCount = Mathf.Max(preservedBirdsFledCount, birdsFledCount);
        preservedRatsCaughtCount = Mathf.Max(preservedRatsCaughtCount, ratsCaughtCount);
        preservedFishEatenCount = Mathf.Max(preservedFishEatenCount, fishEatenCount);
        preservedFortuneViewedCount = Mathf.Max(preservedFortuneViewedCount, fortuneViewedCount);
    }
private int CountRemainingFires()
    {
        int count = 0;
        for (int i = 0; i < fires.Count; i++)
        {
            if (fires[i] != null && !fires[i].Collected)
                count++;
        }
        return count;
    }

private void ApplyCompletionReward()
    {
        if (completionRewardApplied)
            return;

        KeyboardCatMoveCharacterController cat = player != null
            ? player.GetComponent<KeyboardCatMoveCharacterController>()
            : null;

        if (cat == null)
            cat = FindAnyObjectByType<KeyboardCatMoveCharacterController>();

        if (cat == null)
        {
            Debug.LogWarning("불꽃 퀘스트 보상을 적용할 고양이를 찾지 못했습니다.", this);
            return;
        }

        cat.IncreaseJumpForce(completionJumpForceBonus);
        completionRewardApplied = true;
        completionEffectTarget = cat.transform;

        if (completionEffectDelay <= 0f)
            PlayDelayedCompletionEffect();
        else
            Invoke(nameof(PlayDelayedCompletionEffect), completionEffectDelay);
    }

private void PlayDelayedCompletionEffect()
    {
        if (completionEffectTarget != null)
            PlayCompletionEffect(completionEffectTarget);

        completionEffectTarget = null;
        StartCoroutine(ShowCompletionMessage());
    }

    private IEnumerator ShowCompletionMessage()
    {
        yield return new WaitForSecondsRealtime(completionMessageDelay);

        showCompletionMessage = true;
        completionMessageAlpha = 0f;

        while (completionMessageAlpha < 1f)
        {
            completionMessageAlpha = Mathf.MoveTowards(
                completionMessageAlpha,
                1f,
                Time.unscaledDeltaTime / completionMessageFadeDuration
            );
            yield return null;
        }

        yield return new WaitForSecondsRealtime(completionMessageDuration);

        while (completionMessageAlpha > 0f)
        {
            completionMessageAlpha = Mathf.MoveTowards(
                completionMessageAlpha,
                0f,
                Time.unscaledDeltaTime / completionMessageFadeDuration
            );
            yield return null;
        }

        showCompletionMessage = false;
    }


private void PlayCompletionEffect(Transform catTransform)
    {
        if (completionEffectPrefab == null)
            return;

        GameObject effect = Instantiate(
            completionEffectPrefab,
            catTransform.position + Vector3.up * completionEffectHeight,
            catTransform.rotation
        );

        effect.transform.localScale *= completionEffectScale;
        Destroy(effect, 6f);
    }


    private void CreateMinimapCamera()
    {
        if (minimapCamera != null)
            return;

        var cameraObject = new GameObject("Fire Quest Top View Minimap");
        cameraObject.transform.SetParent(transform);
        //cameraObject.transform.position = new Vector3(5f, 34f, -10f);
        cameraObject.transform.position = new Vector3(-14.14f, 33.3f, 13.42f);
        cameraObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        minimapCamera = cameraObject.AddComponent<Camera>();
        minimapCamera.orthographic = true;
        minimapCamera.orthographicSize = 45f;
        minimapCamera.clearFlags = CameraClearFlags.SolidColor;
        minimapCamera.backgroundColor = new Color(0.02f, 0.05f, 0.10f, 1f);
        minimapCamera.cullingMask = ~0;
        minimapCamera.depth = -10;
        minimapCamera.enabled = false;

        minimapTexture = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
        minimapTexture.name = "FireQuest_Minimap_RT";
        minimapTexture.Create();
        minimapCamera.targetTexture = minimapTexture;
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

    private Vector2 WorldToMapPosition(Vector3 worldPosition, Rect mapContentRect)
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
            mapContentRect.x + normalizedX * mapContentRect.width,
            mapContentRect.y + (1f - normalizedY) * mapContentRect.height
        );
    }

    private void DrawMapMarkers(Rect mapContentRect, float scale)
    {
        if (player != null)
        {
            Vector2 playerPosition = WorldToMapPosition(player.position, mapContentRect);
            DrawMarker(
    playerPosition,
    playerMarkerSize * scale,
    playerMarkerTexture,
    playerMarkerColor
);
        }

        for (int i = fires.Count - 1; i >= 0; i--)
        {
            FireCollectible fire = fires[i];

            if (fire == null)
            {
                fires.RemoveAt(i);
                continue;
            }

            if (fire.Collected)
                continue;

            Vector2 firePosition = WorldToMapPosition(
                fire.transform.position,
                mapContentRect
            );

            DrawMarker(
    firePosition,
    fireMarkerSize * scale,
    fireMarkerTexture,
    fireMarkerColor
);
        }
    }

    private void DrawMarker(
        Vector2 center,
        float size,
        Texture2D markerTexture,
        Color color
    )
    {
        Texture2D textureToDraw =
            markerTexture != null ? markerTexture : whiteTexture;

        Rect markerRect = new Rect(
            center.x - size * 0.5f,
            center.y - size * 0.5f,
            size,
            size
        );

        GUI.DrawTexture(
            markerRect,
            textureToDraw,
            ScaleMode.ScaleToFit,
            true,
            0f,
            color,
            0f,
            0f
        );
    }

    private void OnGUI()
    {
        if (GameIntroTutorial.IsShowing)
            return;

        if (Event.current.type != EventType.Repaint || whiteTexture == null)
            return;

        float scale = Mathf.Clamp(Screen.height / 900f, 0.75f, 1.25f);
        var questRect = new Rect(24f * scale, 24f * scale, 310f * scale, 148f * scale);
        if (questFrameTexture != null)
            DrawCroppedFrame(questRect, questFrameTexture, new Rect(0.055f, 0.081f, 0.889f, 0.882f));
        else
            DrawPanel(questRect, new Color(0.035f, 0.045f, 0.08f, 0.93f), new Color(1f, 0.45f, 0.12f, 0.9f));

        var titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(17f * scale),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.34f, 0.16f, 0.055f) }
        };
        
        LockTextState(titleStyle);
titleStyle.font = uiFont;

        var bodyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(16f * scale),
            normal = { textColor = new Color(0.22f, 0.12f, 0.055f) }
        };
        
        LockTextState(bodyStyle);
bodyStyle.font = uiFont;


        string questTitle = complete ? "\ub3c4\uae68\ube44\ubd88 \ub354 \ucc3e\uc544\ubcf4\uae30" : "\ubd88\uaf43 \uc218\uc9d1 \ud018\uc2a4\ud2b8";
        GUI.Label(new Rect(questRect.x + 44f * scale, questRect.y + 30f * scale, questRect.width - 88f * scale, 34f * scale), questTitle, titleStyle);
        string progress = complete
            ? "\ub0a8\uc740 \ubd88\uaf43 " + postFireCollectedCount + " / " + postFireTargetCount
            : "\ubd88\uaf43 " + collectedCount + " / " + requiredCount + " \uc218\uc9d1";
        GUI.Label(new Rect(questRect.x + 44f * scale, questRect.y + 73f * scale, questRect.width - 88f * scale, 28f * scale), progress, bodyStyle);

        float barWidth = questRect.width - 96f * scale;
        var barRect = new Rect(questRect.x + 48f * scale, questRect.y + 108f * scale, barWidth, 10f * scale);
        GUI.DrawTexture(barRect, whiteTexture, ScaleMode.StretchToFill, false, 0f, new Color(0.17f, 0.20f, 0.27f, 1f), 0f, 0f);
        float progressRatio = complete
            ? (postFireTargetCount > 0 ? (float)postFireCollectedCount / postFireTargetCount : 1f)
            : (requiredCount > 0 ? (float)collectedCount / requiredCount : 0f);
        GUI.DrawTexture(new Rect(barRect.x, barRect.y, barWidth * progressRatio, barRect.height), whiteTexture, ScaleMode.StretchToFill, false, 0f, new Color(1f, 0.35f, 0.08f, 1f), 0f, 0f);

        float mapSize = 228f * scale;
        var mapRect = new Rect(Screen.width - mapSize - 24f * scale, 24f * scale, mapSize, mapSize);
        if (minimapFrameTexture != null)
            DrawCroppedFrame(mapRect, minimapFrameTexture, new Rect(0.074f, 0.043f, 0.849f, 0.891f));
        else
            DrawPanel(mapRect, new Color(0.01f, 0.02f, 0.04f, 0.94f), new Color(0.30f, 0.82f, 1f, 0.9f));
        Rect mapContentRect = new Rect(
            mapRect.x + 18f * scale,
            mapRect.y + 18f * scale,
            mapRect.width - 36f * scale,
            mapRect.height - 36f * scale
        );

        if (minimapTexture != null)
            GUI.DrawTexture(mapContentRect, minimapTexture, ScaleMode.ScaleAndCrop, false);

        DrawMapMarkers(mapContentRect, scale * 1.1f);
        // The enlarged map content replaces the former minimap title.
        DrawSideQuestPanel(scale, questRect);
        DrawCompletionMessage(scale);
    }

private void DrawSideQuestPanel(float scale, Rect questRect)
    {
        float sideWidth = 210f * scale;
        var panelRect = new Rect(questRect.x, questRect.yMax + 8f * scale, sideWidth, sideWidth / 0.967f);
        if (sideQuestFrameTexture != null)
            DrawCroppedFrame(panelRect, sideQuestFrameTexture, new Rect(0.086f, 0.125f, 0.794f, 0.821f));
        else
            DrawPanel(panelRect, new Color(0.16f, 0.09f, 0.035f, 0.82f), new Color(0.58f, 0.36f, 0.15f, 0.9f));

        var titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(15f * scale),
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.27f, 0.12f, 0.045f) }
        };
        LockTextState(titleStyle);
        titleStyle.font = uiFont;

        var itemStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = Mathf.RoundToInt(12f * scale),
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.20f, 0.10f, 0.055f) }
        };
        LockTextState(itemStyle);
        itemStyle.font = uiFont;

        float itemX = panelRect.x + 43f * scale;
        float itemWidth = panelRect.width - 86f * scale;
        GUI.Label(new Rect(panelRect.x + 38f * scale, panelRect.y + 55f * scale, panelRect.width - 76f * scale, 24f * scale), "\uC11C\uBE0C \uD018\uC2A4\uD2B8", titleStyle);
        GUI.Label(new Rect(itemX, panelRect.y + 87f * scale, itemWidth, 22f * scale), "\uC0C8 \uCAD3\uAE30  " + Mathf.Max(birdsFledCount, preservedBirdsFledCount) + " / " + birdQuestTarget, itemStyle);
        GUI.Label(new Rect(itemX, panelRect.y + 112f * scale, itemWidth, 22f * scale), "\uC950 \uC7A1\uAE30  " + Mathf.Max(ratsCaughtCount, preservedRatsCaughtCount) + " / " + ratQuestTarget, itemStyle);
        GUI.Label(new Rect(itemX, panelRect.y + 137f * scale, itemWidth, 22f * scale), "\uC0DD\uC120 \uBA39\uAE30  " + Mathf.Max(fishEatenCount, preservedFishEatenCount) + " / " + fishQuestTarget, itemStyle);
        GUI.Label(new Rect(itemX, panelRect.y + 162f * scale, itemWidth, 22f * scale), "\uC6B4\uC138 \uBCF4\uAE30  " + Mathf.Max(fortuneViewedCount, preservedFortuneViewedCount) + " / " + fortuneQuestTarget, itemStyle);
    }

    private void DrawCompletionMessage(float scale)
    {
        if (!showCompletionMessage || completionMessageAlpha <= 0f)
            return;

        float width = 560f * scale;
        float height = width / 4.99f;
        var rect = new Rect(
            (Screen.width - width) * 0.5f,
            (Screen.height - height) * 0.5f,
            width,
            height
        );

        if (completionMessageFrameTexture != null)
        {
            Color previousColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, completionMessageAlpha);
            DrawCroppedFrame(rect, completionMessageFrameTexture, new Rect(0.049f, 0.438f, 0.912f, 0.274f));
            GUI.color = previousColor;
        }
        else
        {
            DrawPanel(
                rect,
                new Color(0.035f, 0.025f, 0.06f, 0.92f * completionMessageAlpha),
                new Color(0.95f, 0.68f, 0.28f, completionMessageAlpha)
            );
        }

        var messageStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(25f * scale),
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(1f, 0.94f, 0.78f, completionMessageAlpha) }
        };
        LockTextState(messageStyle);
        messageStyle.font = uiFont;

        Rect textRect = new Rect(rect.x + 100f * scale, rect.y + 20f * scale, rect.width - 145f * scale, rect.height - 16f * scale);
        GUIStyle shadowStyle = new GUIStyle(messageStyle);
        shadowStyle.normal.textColor = new Color(0.055f, 0.025f, 0.075f, 0.90f * completionMessageAlpha);
        LockTextState(shadowStyle);
        GUI.Label(new Rect(textRect.x + 2f * scale, textRect.y + 2f * scale, textRect.width, textRect.height),
            "\uD50C\uB808\uC774 \uD574\uC8FC\uC154\uC11C \uAC10\uC0AC\uD569\uB2C8\uB2E4.", shadowStyle);
        GUI.Label(textRect, "\uD50C\uB808\uC774 \uD574\uC8FC\uC154\uC11C \uAC10\uC0AC\uD569\uB2C8\uB2E4.", messageStyle);
    }

    private static void DrawCroppedFrame(Rect rect, Texture2D texture, Rect uvRect)
    {
        GUI.DrawTextureWithTexCoords(rect, texture, uvRect, true);
    }

    private void DrawPanel(Rect rect, Color fill, Color border)
    {
        GUI.DrawTexture(rect, whiteTexture, ScaleMode.StretchToFill, false, 0f, border, 0f, 0f);
        GUI.DrawTexture(new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, rect.height - 4f), whiteTexture, ScaleMode.StretchToFill, false, 0f, fill, 0f, 0f);
    }

    private void OnDestroy()
    {
        if (minimapTexture != null)
            minimapTexture.Release();
        if (Instance == this)
            Instance = null;
    }


private static void LockTextState(GUIStyle style)
    {
        if (style == null)
            return;

        Color color = style.normal.textColor;
        Color hoverColor = Color.Lerp(color, Color.white, 0.08f);
        style.hover.textColor = hoverColor;
        style.active.textColor = color;
        style.focused.textColor = color;
        style.onNormal.textColor = color;
        style.onHover.textColor = hoverColor;
        style.onActive.textColor = color;
        style.onFocused.textColor = color;
    }
}

[RequireComponent(typeof(SphereCollider))]
internal sealed class LegacyFireCollectible : MonoBehaviour
{
    [SerializeField] private float bobHeight = 0.18f;
    [SerializeField] private float bobSpeed = 2.2f;

    private Vector3 basePosition;
    public bool Collected { get; private set; }

    private void Awake()
    {
        basePosition = transform.position;
        var trigger = GetComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 1.15f;
    }

    private void Start()
    {
        FireQuestManager.Instance?.Register(GetComponent<FireCollectible>());
    }

    private void Update()
    {
        if (!Collected)
        {
            float wave = Mathf.Sin(Time.time * bobSpeed + transform.position.x) * bobHeight;
            transform.position = basePosition + Vector3.up * wave;
            transform.Rotate(Vector3.up, 65f * Time.deltaTime, Space.World);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Collected)
            return;

        if (other.GetComponentInParent<KeyboardCatMove>() != null || other.name == "고양이")
            FireQuestManager.Instance?.Collect(GetComponent<FireCollectible>());
    }

    public void MarkCollected()
    {
        if (Collected)
            return;

        Collected = true;
        Destroy(gameObject);
    }
}

