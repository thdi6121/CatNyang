using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class GameIntroTutorial : MonoBehaviour
{
    public static bool IsShowing { get; private set; }

    private enum IntroStage
    {
        Hidden,
        FadeIn,
        ArrivalMessage,
        CameraPan,
        HowToPlay,
        MissionMessage,
        Finished
    }

    [Header("References")]
    [SerializeField] private Texture2D howToPlayImage;
    [SerializeField] private AudioClip trainArrivalSound;
    [SerializeField] private KeyboardCatMoveCharacterController playerMovement;
    [SerializeField] private MainCameraEdited cameraController;
    [SerializeField] private Transform cameraTarget;

    [Header("Timing")]
    
    [SerializeField, Min(0f)] private float blackHoldDuration = 0.8f;
[SerializeField, Min(0.1f)] private float fadeDuration = 1.5f;
    [SerializeField, Min(0.1f)] private float arrivalMessageDuration = 2.8f;
    [SerializeField, Min(0.5f)] private float cameraPanDuration = 4.2f;
    
    [SerializeField, Min(0f)] private float cameraHeightOffset = 1.5f;
    [SerializeField, Range(1f, 2f)] private float cameraDistanceMultiplier = 1.2f;
    [SerializeField, Range(40f, 90f)] private float introFieldOfView = 65f;
[SerializeField, Range(5f, 60f)] private float cameraPanAngle = 28f;
    [SerializeField, Min(0.1f)] private float trainFadeOutDuration = 1.5f;
    [SerializeField, Range(0f, 1f)] private float trainVolume = 0.65f;

    private IntroStage stage = IntroStage.Hidden;
    private IntroStage typingStage = IntroStage.Hidden;
    private float typingStartedAt;
    private bool typingFinished;
    private const float typingCharactersPerSecond = 24f;
    private AudioSource audioSource;
    private Font koreanFont;
    private GUIStyle dialogueStyle;
    private GUIStyle hintStyle;
    
    private Texture2D dialogueFrameTexture;
private Texture2D whiteTexture;

    private Vector3 originalCameraPosition;
    
    private Camera unityCamera;
    private float originalFieldOfView;
private Quaternion originalCameraRotation;
    private bool originalPlayerEnabled;
    private bool originalCameraEnabled;
    private float previousTimeScale = 1f;
    private bool controlsLocked;
    
    private float fadeStartedAt;
private bool finishing;

    private void Awake()
    {
        
        dialogueFrameTexture = Resources.Load<Texture2D>("UI/Frames/\uC2A4\uD06C\uB9BD\uD2B8\uC6A9");
whiteTexture = Texture2D.whiteTexture;
        koreanFont = Resources.Load<Font>("Fonts/DNFBitBitv2");

        audioSource = gameObject.GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 0f;
        audioSource.clip = trainArrivalSound;
    }

    private IEnumerator Start()
    {
        yield return null;

        ResolveReferences();
        if (playerMovement == null || cameraController == null)
        {
            Debug.LogWarning("게임 시작 튜토리얼이 플레이어 또는 메인 카메라를 찾지 못했습니다.", this);
            yield break;
        }

        LockGameplay();
        yield return RunIntro();
    }

private void ResolveReferences()
    {
        if (playerMovement == null)
            playerMovement = FindFirstObjectByType<KeyboardCatMoveCharacterController>();

        if (cameraController == null)
            cameraController = FindFirstObjectByType<MainCameraEdited>();

        if (cameraTarget == null && playerMovement != null)
            cameraTarget = playerMovement.transform;

        if (cameraController != null)
            unityCamera = cameraController.GetComponent<Camera>();
    }
private void LockGameplay()
    {
        if (controlsLocked)
            return;

        originalPlayerEnabled = playerMovement.enabled;
        originalCameraEnabled = cameraController.enabled;
        originalCameraPosition = cameraController.transform.position;
        originalCameraRotation = cameraController.transform.rotation;
        originalFieldOfView = unityCamera != null ? unityCamera.fieldOfView : 60f;
        previousTimeScale = Time.timeScale;

        playerMovement.enabled = false;
        cameraController.enabled = false;
        Time.timeScale = 0f;
        controlsLocked = true;
    }

private IEnumerator RunIntro()
    {
        IsShowing = true;
        stage = IntroStage.FadeIn;
        fadeStartedAt = Time.unscaledTime + blackHoldDuration;

        if (trainArrivalSound != null)
        {
            audioSource.clip = trainArrivalSound;
            audioSource.Play();
            StartCoroutine(FadeAudio(0f, trainVolume, 0.7f, false));
        }

        yield return WaitRealtime(blackHoldDuration + fadeDuration);
        if (finishing) yield break;

        stage = IntroStage.ArrivalMessage;
        while (stage == IntroStage.ArrivalMessage && !finishing)
            yield return null;
        if (finishing) yield break;

        yield return PanCamera();
        if (finishing) yield break;

        StartCoroutine(FadeAudio(audioSource.volume, 0f, trainFadeOutDuration, true));
        stage = IntroStage.HowToPlay;
    }

private IEnumerator PanCamera()
    {
        Transform cameraTransform = cameraController.transform;
        Vector3 pivot = cameraTarget != null
            ? cameraTarget.position + Vector3.up * 1.2f
            : originalCameraPosition + originalCameraRotation * Vector3.forward * 5f;

        Vector3 originalOffset = originalCameraPosition - pivot;
        Vector3 introOffset = originalOffset * cameraDistanceMultiplier + Vector3.up * cameraHeightOffset;

        float elapsed = 0f;
        while (elapsed < cameraPanDuration && !finishing)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / cameraPanDuration);
            float angle;

            if (t < 0.3f)
                angle = Mathf.Lerp(0f, -cameraPanAngle, Smooth01(t / 0.3f));
            else if (t < 0.7f)
                angle = Mathf.Lerp(-cameraPanAngle, cameraPanAngle, Smooth01((t - 0.3f) / 0.4f));
            else
                angle = Mathf.Lerp(cameraPanAngle, 0f, Smooth01((t - 0.7f) / 0.3f));

            float enterBlend = Smooth01(Mathf.Clamp01(t / 0.18f));
            float exitBlend = Smooth01(Mathf.Clamp01((1f - t) / 0.18f));
            float framingBlend = Mathf.Min(enterBlend, exitBlend);
            Vector3 framedOffset = Vector3.Lerp(originalOffset, introOffset, framingBlend);

            cameraTransform.position = pivot + Quaternion.AngleAxis(angle, Vector3.up) * framedOffset;
            cameraTransform.rotation = Quaternion.LookRotation(pivot - cameraTransform.position, Vector3.up);

            if (unityCamera != null)
                unityCamera.fieldOfView = Mathf.Lerp(originalFieldOfView, introFieldOfView, framingBlend);

            yield return null;
        }

        cameraTransform.SetPositionAndRotation(originalCameraPosition, originalCameraRotation);
        if (unityCamera != null)
            unityCamera.fieldOfView = originalFieldOfView;
    }

    private static float Smooth01(float value)
    {
        value = Mathf.Clamp01(value);
        return value * value * (3f - 2f * value);
    }

    private IEnumerator WaitRealtime(float duration)
    {
        float endTime = Time.unscaledTime + duration;
        while (Time.unscaledTime < endTime && !finishing)
            yield return null;
    }

    private IEnumerator FadeAudio(float from, float to, float duration, bool stopAfter)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            audioSource.volume = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        audioSource.volume = to;
        if (stopAfter)
            audioSource.Stop();
    }

    private void Update()
    {
        if (stage == IntroStage.Hidden || stage == IntroStage.Finished)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            FinishIntro();
            return;
        }

        if (!AdvancePressed())
            return;

        bool typingMessage = stage == IntroStage.ArrivalMessage
            || stage == IntroStage.HowToPlay
            || stage == IntroStage.MissionMessage;
        if (typingMessage && !typingFinished)
        {
            typingStartedAt = Time.unscaledTime - 100f;
            typingFinished = true;
            return;
        }

        if (stage == IntroStage.ArrivalMessage)
            stage = IntroStage.CameraPan;
        else if (stage == IntroStage.HowToPlay)
            stage = IntroStage.MissionMessage;
        else if (stage == IntroStage.MissionMessage)
            FinishIntro();
    }

    private void SyncTypingStage()
    {
        if (typingStage == stage)
            return;

        typingStage = stage;
        typingStartedAt = Time.unscaledTime;
        typingFinished = false;
    }

    private string GetTypedMessage(string message)
    {
        SyncTypingStage();
        int count = Mathf.Clamp(
            Mathf.FloorToInt((Time.unscaledTime - typingStartedAt) * typingCharactersPerSecond),
            0,
            message.Length
        );
        typingFinished = count >= message.Length;
        return message.Substring(0, count);
    }
private static bool AdvancePressed()
    {
        return Input.GetKeyDown(KeyCode.Space)
            || Input.GetKeyDown(KeyCode.Return)
            || Input.GetMouseButtonDown(0);
    }

    private void FinishIntro()
    {
        if (finishing)
            return;

        finishing = true;
        StopAllCoroutines();

        if (audioSource != null)
            audioSource.Stop();

        IsShowing = false;
        RestoreGameplay();
        stage = IntroStage.Finished;
    }

private void RestoreGameplay()
    {
        if (!controlsLocked)
            return;

        if (cameraController != null)
        {
            cameraController.transform.SetPositionAndRotation(originalCameraPosition, originalCameraRotation);
            cameraController.enabled = true;
        }

        if (unityCamera != null)
            unityCamera.fieldOfView = originalFieldOfView;

        if (playerMovement != null)
            playerMovement.enabled = true;

        Time.timeScale = 1f;
        controlsLocked = false;
    }

private void OnGUI()
    {
        if (stage == IntroStage.Hidden || stage == IntroStage.Finished)
            return;

        int previousDepth = GUI.depth;
        GUI.depth = -1500;

        if (stage == IntroStage.FadeIn)
        {
            float fadeProgress = (Time.unscaledTime - fadeStartedAt) / Mathf.Max(0.01f, fadeDuration);
            float alpha = 1f - Mathf.Clamp01(fadeProgress);
            DrawSolidRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0f, 0f, 0f, alpha));
        }
        else if (stage == IntroStage.ArrivalMessage)
        {
            DrawDialogue("저승차사 고양이가 삼도마을에 도착했습니다.");
        }
        else if (stage == IntroStage.HowToPlay || stage == IntroStage.MissionMessage)
        {
            DrawSolidRect(
                new Rect(0f, 0f, Screen.width, Screen.height),
                new Color(0.015f, 0.012f, 0.025f, 0.78f)
            );

            DrawHowToPlayImage();

            string message = stage == IntroStage.HowToPlay
                ? "플레이 방법 : WASD 이동 / Space 점프 / Shift 달리기 / R 긴급탈출\n마우스 이동 : 화면 좌우 및 위아래 둘러보기\n미니맵에는 성불시켜야 할 영혼의 위치가 표시됩니다."
                : "당신은 저승차사 고양이입니다. 영혼을 성불해 주세요!";

            DrawDialogue(message);
        }

        if (stage != IntroStage.FadeIn && stage != IntroStage.CameraPan)
            DrawSkipHint();

        GUI.depth = previousDepth;
    }

    private void DrawHowToPlayImage()
    {
        if (howToPlayImage == null)
            return;

        float panelWidth = Mathf.Min(Screen.width * 0.88f, 1150f);
        float panelHeight = Mathf.Clamp(panelWidth / 4.1f, 150f, 280f);
        float dialogueY = Screen.height - panelHeight - Mathf.Max(24f, Screen.height * 0.03f);
        float minimumGap = Mathf.Max(20f, Screen.height * 0.022f);
        float availableHeight = Mathf.Max(60f, dialogueY - minimumGap * 2f);

        float maxWidth = Screen.width * 0.82f;
        float aspect = (float)howToPlayImage.width / howToPlayImage.height;
        float width = maxWidth;
        float height = width / aspect;
        if (height > availableHeight)
        {
            height = availableHeight;
            width = height * aspect;
        }

        float imageY = Mathf.Max(minimumGap, (dialogueY - height) * 0.5f);
        Rect imageRect = new Rect((Screen.width - width) * 0.5f, imageY, width, height);
        DrawSolidRect(new Rect(imageRect.x - 6f, imageRect.y - 6f, imageRect.width + 12f, imageRect.height + 12f), new Color(0f, 0f, 0f, 0.65f));
        GUI.DrawTexture(imageRect, howToPlayImage, ScaleMode.ScaleToFit, true);
    }

    private void DrawDialogue(string message)
    {
        float panelWidth = Mathf.Min(Screen.width * 0.88f, 1150f);
        float panelHeight = Mathf.Clamp(panelWidth / 4.1f, 150f, 280f);
        float panelX = (Screen.width - panelWidth) * 0.5f;
        float panelY = Screen.height - panelHeight - Mathf.Max(24f, Screen.height * 0.03f);
        Rect panelRect = new Rect(panelX, panelY, panelWidth, panelHeight);

        if (dialogueFrameTexture != null)
            GUI.DrawTextureWithTexCoords(panelRect, dialogueFrameTexture, new Rect(0.018f, 0.075f, 0.964f, 0.819f), true);
        else
            DrawSolidRect(panelRect, new Color(0.055f, 0.04f, 0.08f, 0.97f));

        EnsureStyles();

        Rect textRect = new Rect(
            panelRect.x + 82f,
            panelRect.y + 52f,
            panelRect.width - 164f,
            panelRect.height - 108f
        );
        GUI.Label(textRect, GetTypedMessage(message), dialogueStyle);
    }

    private void DrawSkipHint()
    {
        EnsureStyles();

        float panelWidth = Mathf.Min(Screen.width * 0.88f, 1150f);
        float panelHeight = Mathf.Clamp(panelWidth / 4.1f, 150f, 280f);
        float panelX = (Screen.width - panelWidth) * 0.5f;
        float panelY = Screen.height - panelHeight - Mathf.Max(24f, Screen.height * 0.03f);
        Rect hintRect = new Rect(
            panelX + 100f,
            panelY + panelHeight - 43f,
            panelWidth - 200f,
            22f
        );

        string hint = stage == IntroStage.HowToPlay || stage == IntroStage.MissionMessage
            ? "\uD074\uB9AD \uB610\uB294 Space : \uB2E4\uC74C  |  Esc : \uAC74\uB108\uB6F0\uAE30"
            : "Esc : \uAC74\uB108\uB6F0\uAE30";
        float pulse = (Mathf.Sin(Time.unscaledTime * 2.8f) + 1f) * 0.5f;
        float pulseAlpha = Mathf.Lerp(0.48f, 1f, pulse);
        Color pulseColor = new Color(0.38f, 0.20f, 0.075f, pulseAlpha);
        hintStyle.normal.textColor = pulseColor;
        hintStyle.hover.textColor = pulseColor;
        hintStyle.active.textColor = pulseColor;
        hintStyle.focused.textColor = pulseColor;
        GUI.Label(hintRect, hint, hintStyle);
    }

    private void EnsureStyles()
    {
        if (dialogueStyle != null)
            return;

        dialogueStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.028f), 20, 31),
            fontStyle = FontStyle.Normal
        };
        dialogueStyle.normal.textColor = new Color(0.20f, 0.10f, 0.055f, 1f);
        
        LockTextState(dialogueStyle);
dialogueStyle.font = koreanFont;

        hintStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleRight,
            fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.017f), 13, 19)
        };
        hintStyle.normal.textColor = new Color(0.34f, 0.19f, 0.09f, 0.92f);
        
        LockTextState(hintStyle);
hintStyle.font = koreanFont;
    }

    private void DrawSolidRect(Rect rect, Color color)
    {
        GUI.DrawTexture(rect, whiteTexture, ScaleMode.StretchToFill, false, 0f, color, 0f, 0f);
    }

    private void OnDisable()
    {
        IsShowing = false;
        if (Application.isPlaying)
            RestoreGameplay();
    }

    private void OnDestroy()
    {
        IsShowing = false;
        RestoreGameplay();
    }


private void OnEnable()
    {
        if (Application.isPlaying)
            IsShowing = true;
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
