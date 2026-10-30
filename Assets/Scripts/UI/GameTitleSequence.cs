using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
public sealed class GameTitleSequence : MonoBehaviour
{
    public static bool IsShowing { get; private set; }

    [Header("Camera Route")]
    [SerializeField] private Vector3 startPosition = new Vector3(-22f, 27f, -44f);
    [SerializeField] private Vector3 endPosition = new Vector3(17f, 23f, -33f);
    [SerializeField] private Vector3 startLookTarget = new Vector3(-14f, 5f, 13f);
    [SerializeField] private Vector3 endLookTarget = new Vector3(1f, 4f, 7f);
    [SerializeField, Min(3f)] private float travelDuration = 14f;
    [SerializeField, Min(0f)] private float cameraArcHeight = 1.8f;
    [SerializeField, Range(35f, 80f)] private float titleFieldOfView = 55f;

    [Header("Title Screen")]
    [SerializeField, Min(0f)] private float inputDelay = 1f;
    [SerializeField, Min(0.1f)] private float fadeDuration = 0.65f;
    [SerializeField, Range(0.25f, 0.9f)] private float titleWidthRatio = 0.78f;

    [Header("Title Effects (Uncheck To Restore)")]
    [SerializeField] private bool enableTitleEffects = true;
    [SerializeField, Min(0.1f)] private float titleFadeInDuration = 1f;
    [SerializeField, Range(0f, 0.08f)] private float titleBreathAmount = 0.03f;
    [SerializeField, Min(0.1f)] private float titleBreathSpeed = 1.2f;
    [SerializeField, Range(0f, 0.35f)] private float titleGlowAlpha = 0.12f;

    private GameIntroTutorial introTutorial;
    private KeyboardCatMoveCharacterController playerMovement;
    private MainCameraEdited cameraController;
    private Camera unityCamera;
    private Texture2D titleTexture;
    private Texture2D whiteTexture;
    private Font uiFont;

    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    private float originalFieldOfView;
    private bool originalPlayerEnabled;
    private bool originalCameraEnabled;
    private float previousTimeScale;
    private float startedAt;
    private float exitFadeAlpha;
    private bool exiting;
    private FireQuestManager fireQuestUi;
    private ExitGameUI exitGameUi;
    private bool originalFireQuestUiEnabled;
    private bool originalExitUiEnabled;

    private void Awake()
    {
        introTutorial = GetComponent<GameIntroTutorial>();
        if (introTutorial == null)
            introTutorial = FindAnyObjectByType<GameIntroTutorial>();
        if (introTutorial != null)
            introTutorial.enabled = false;

        playerMovement = FindAnyObjectByType<KeyboardCatMoveCharacterController>();
        cameraController = FindAnyObjectByType<MainCameraEdited>();
        unityCamera = cameraController != null ? cameraController.GetComponent<Camera>() : Camera.main;
        titleTexture = Resources.Load<Texture2D>("UI/Title/GameTitle");
        uiFont = Resources.Load<Font>("Fonts/DNFBitBitv2");
        whiteTexture = Texture2D.whiteTexture;

        if (unityCamera == null)
        {
            enabled = false;
            if (introTutorial != null)
                introTutorial.enabled = true;
            return;
        }

        originalCameraPosition = unityCamera.transform.position;
        originalCameraRotation = unityCamera.transform.rotation;
        originalFieldOfView = unityCamera.fieldOfView;
        originalPlayerEnabled = playerMovement != null && playerMovement.enabled;
        originalCameraEnabled = cameraController != null && cameraController.enabled;
        previousTimeScale = Time.timeScale;

        fireQuestUi = FindAnyObjectByType<FireQuestManager>();
        exitGameUi = FindAnyObjectByType<ExitGameUI>();
        originalFireQuestUiEnabled = fireQuestUi != null && fireQuestUi.enabled;
        originalExitUiEnabled = exitGameUi != null && exitGameUi.enabled;
        if (fireQuestUi != null)
            fireQuestUi.enabled = false;
        if (exitGameUi != null)
            exitGameUi.enabled = false;

        if (playerMovement != null)
            playerMovement.enabled = false;
        if (cameraController != null)
            cameraController.enabled = false;
        Time.timeScale = 0f;
        unityCamera.fieldOfView = titleFieldOfView;
        IsShowing = true;
        startedAt = Time.unscaledTime;
        UpdateCamera(0f);
    }

    private void Update()
    {
        if (!IsShowing || exiting)
            return;

        float elapsed = Time.unscaledTime - startedAt;
        float normalized = Mathf.PingPong(elapsed / Mathf.Max(0.01f, travelDuration), 1f);
        UpdateCamera(Mathf.SmoothStep(0f, 1f, normalized));

        if (elapsed < inputDelay)
            return;

        if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
            StartCoroutine(ExitTitleScreen());
    }

    private void UpdateCamera(float t)
    {
        Vector3 position = Vector3.Lerp(startPosition, endPosition, t);
        position.y += Mathf.Sin(t * Mathf.PI) * cameraArcHeight;
        Vector3 lookTarget = Vector3.Lerp(startLookTarget, endLookTarget, t);
        unityCamera.transform.position = position;
        unityCamera.transform.rotation = Quaternion.LookRotation(lookTarget - position, Vector3.up);
    }

    private IEnumerator ExitTitleScreen()
    {
        exiting = true;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            exitFadeAlpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }

        unityCamera.transform.SetPositionAndRotation(originalCameraPosition, originalCameraRotation);
        unityCamera.fieldOfView = originalFieldOfView;
        if (playerMovement != null)
            playerMovement.enabled = originalPlayerEnabled;
        if (cameraController != null)
            cameraController.enabled = originalCameraEnabled;
        if (fireQuestUi != null)
            fireQuestUi.enabled = originalFireQuestUiEnabled;
        if (exitGameUi != null)
            exitGameUi.enabled = originalExitUiEnabled;
        Time.timeScale = previousTimeScale;
        IsShowing = false;

        if (introTutorial != null)
            introTutorial.enabled = true;
        enabled = false;
    }

    private void OnGUI()
    {
        if (!IsShowing || Event.current.type != EventType.Repaint)
            return;

        int previousDepth = GUI.depth;
        GUI.depth = -2000;
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), whiteTexture, ScaleMode.StretchToFill, false, 0f, new Color(0f, 0f, 0f, 0.14f), 0f, 0f);

        if (titleTexture != null)
        {
            float maxWidth = Screen.width * 0.78f;
            float maxHeight = Screen.height * 0.68f;
            float aspect = (float)titleTexture.width / titleTexture.height;
            float width = Mathf.Min(maxWidth, maxHeight * aspect);
            float height = (width / aspect) * 1.18f;
            Rect titleRect = new Rect((Screen.width - width) * 0.5f, Screen.height * 0.03f, width, height);

            float effectElapsed = Time.unscaledTime - startedAt;
            float titleAlpha = enableTitleEffects
                ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(effectElapsed / titleFadeInDuration))
                : 1f;
            float breathScale = enableTitleEffects
                ? 1f + Mathf.Sin(effectElapsed * titleBreathSpeed) * titleBreathAmount
                : 1f;
            Rect animatedTitleRect = new Rect(
                titleRect.center.x - titleRect.width * breathScale * 0.5f,
                titleRect.center.y - titleRect.height * breathScale * 0.5f,
                titleRect.width * breathScale,
                titleRect.height * breathScale);

            Color previousGuiColor = GUI.color;
            if (enableTitleEffects)
            {
                float glowPulse = titleGlowAlpha * (0.8f + Mathf.Sin(effectElapsed * 1.5f) * 0.2f) * titleAlpha;
                GUI.color = new Color(0.58f, 0.28f, 1f, glowPulse);
                const float glowOffset = 4f;
                GUI.DrawTexture(new Rect(animatedTitleRect.x - glowOffset, animatedTitleRect.y, animatedTitleRect.width, animatedTitleRect.height), titleTexture, ScaleMode.StretchToFill, true);
                GUI.DrawTexture(new Rect(animatedTitleRect.x + glowOffset, animatedTitleRect.y, animatedTitleRect.width, animatedTitleRect.height), titleTexture, ScaleMode.StretchToFill, true);
                GUI.DrawTexture(new Rect(animatedTitleRect.x, animatedTitleRect.y - glowOffset, animatedTitleRect.width, animatedTitleRect.height), titleTexture, ScaleMode.StretchToFill, true);
                GUI.DrawTexture(new Rect(animatedTitleRect.x, animatedTitleRect.y + glowOffset, animatedTitleRect.width, animatedTitleRect.height), titleTexture, ScaleMode.StretchToFill, true);
            }

            GUI.color = new Color(1f, 1f, 1f, titleAlpha);
            GUI.DrawTexture(animatedTitleRect, titleTexture, ScaleMode.StretchToFill, true);
            GUI.color = previousGuiColor;
        }

        float pulse = 0.62f + Mathf.Sin(Time.unscaledTime * 2.4f) * 0.28f;
        var hintStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(Mathf.Clamp(Screen.height / 900f, 0.75f, 1.25f) * 32f),
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(1f, 0.92f, 0.72f, pulse) }
        };
        hintStyle.font = uiFont;
        Rect hintRect = new Rect(0f, Screen.height - 105f, Screen.width, 42f);
        GUI.Label(hintRect, "\uc544\ubb34 \ud0a4\ub098 \ub20c\ub7ec\uc8fc\uc138\uc694", hintStyle);

        if (exitFadeAlpha > 0f)
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), whiteTexture, ScaleMode.StretchToFill, false, 0f, new Color(0f, 0f, 0f, exitFadeAlpha), 0f, 0f);
        GUI.depth = previousDepth;
    }

    private void OnDisable()
    {
        if (!Application.isPlaying || !IsShowing)
            return;

        if (unityCamera != null)
        {
            unityCamera.transform.SetPositionAndRotation(originalCameraPosition, originalCameraRotation);
            unityCamera.fieldOfView = originalFieldOfView;
        }
        if (playerMovement != null)
            playerMovement.enabled = originalPlayerEnabled;
        if (cameraController != null)
            cameraController.enabled = originalCameraEnabled;
        if (fireQuestUi != null)
            fireQuestUi.enabled = originalFireQuestUiEnabled;
        if (exitGameUi != null)
            exitGameUi.enabled = originalExitUiEnabled;
        Time.timeScale = previousTimeScale;
        IsShowing = false;
    }
}







