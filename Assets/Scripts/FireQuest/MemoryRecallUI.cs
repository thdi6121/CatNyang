using System;
using UnityEngine;

[DefaultExecutionOrder(10000)]
public sealed class MemoryRecallUI : MonoBehaviour
{
    private static MemoryRecallUI instance;

    private readonly string[] memoryTexts =
    {
        "익숙한 냄새가 난다.\n이곳에 온 적이 있었던 걸까?",
        "누군가 항상 이 자리에서\n나를 기다리고 있었다.",
        "기억 속의 고양이……\n저건 분명 나야.",
        "그 사람은 돌아오지 않는 나를\n계속 기다리고 있었다.",
        "이곳은 내가 살던 마을.\n저곳은…… 우리 집이다.",
        "마지막 영혼의 얼굴을 기억해냈다.\n나의 주인이었다."
    };

    private readonly Texture2D[] memoryImages = new Texture2D[5];

    private bool visible;
    private int currentMemoryIndex;
    private float inputUnlockTime;
    private float typingStartTime;
    private const float typingCharactersPerSecond = 24f;
    private float previousTimeScale = 1f;
    private Action onClosed;

    
    private Texture2D dialogueFrameTexture;
private Texture2D whiteTexture;
    private Font koreanFont;
    private GUIStyle dialogueStyle;
    private GUIStyle hintStyle;

    public static bool IsShowing => instance != null && instance.visible;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateInstance()
    {
        if (instance != null)
            return;

        GameObject host = new GameObject("Memory Recall UI");
        DontDestroyOnLoad(host);
        instance = host.AddComponent<MemoryRecallUI>();
    }

private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        
        dialogueFrameTexture = Resources.Load<Texture2D>("UI/Frames/\uC2A4\uD06C\uB9BD\uD2B8\uC6A9");
whiteTexture = Texture2D.whiteTexture;

        for (int i = 0; i < memoryImages.Length; i++)
            memoryImages[i] = Resources.Load<Texture2D>("MemoryRecall/memory_" + (i + 2).ToString("00"));

        koreanFont = Resources.Load<Font>("Fonts/DNFBitBitv2");
        if (koreanFont == null)
            Debug.LogWarning("DNFBitBit v2 글꼴을 불러오지 못했습니다. 기본 글꼴을 사용합니다.", this);
    }

    public static void ShowMemory(int collectedCount, Action closed = null)
    {
        if (instance == null)
            CreateInstance();

        if (instance == null || instance.visible)
            return;

        instance.Show(collectedCount, closed);
    }

    private void Show(int collectedCount, Action closed)
    {
        currentMemoryIndex = Mathf.Clamp(collectedCount - 1, 0, memoryTexts.Length - 1);
        onClosed = closed;
        visible = true;
        typingStartTime = Time.unscaledTime;
        inputUnlockTime = Time.unscaledTime + 0.35f;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
    }

    private void Update()
    {
        if (!visible || Time.unscaledTime < inputUnlockTime)
            return;

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Return))
        {
            Hide();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            string fullText = memoryTexts[currentMemoryIndex];
            if (GetVisibleCharacterCount(fullText) < fullText.Length)
            {
                typingStartTime = Time.unscaledTime - fullText.Length / typingCharactersPerSecond;
                return;
            }

            Hide();
        }
    }

    private int GetVisibleCharacterCount(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        return Mathf.Clamp(
            Mathf.FloorToInt((Time.unscaledTime - typingStartTime) * typingCharactersPerSecond),
            0,
            text.Length
        );
    }

    private void Hide()
    {
        if (!visible)
            return;

        visible = false;
        Time.timeScale = previousTimeScale;

        Action callback = onClosed;
        onClosed = null;
        callback?.Invoke();
    }

    private void OnGUI()
    {
        if (!visible || whiteTexture == null)
            return;

        int previousDepth = GUI.depth;
        GUI.depth = -1000;

        DrawSolidRect(
            new Rect(0f, 0f, Screen.width, Screen.height),
            new Color(0.015f, 0.012f, 0.025f, 0.78f)
        );

        Texture2D image = GetCurrentImage();
        if (image != null)
            DrawMemoryImage(image);

        DrawDialogue();

        GUI.depth = previousDepth;
    }

    private Texture2D GetCurrentImage()
    {
        // 첫 번째 기억에는 이미지가 없고, 두 번째부터 5장이 순서대로 나온다.
        if (currentMemoryIndex == 0)
            return null;

        int imageIndex = currentMemoryIndex - 1;
        return imageIndex >= 0 && imageIndex < memoryImages.Length
            ? memoryImages[imageIndex]
            : null;
    }

private void DrawMemoryImage(Texture2D image)
    {
        float dialogueWidth = Mathf.Min(Screen.width * 0.86f, 1100f);
        float dialogueHeight = Mathf.Clamp(dialogueWidth / 4.1f, 150f, 270f);
        float dialogueY = Screen.height - dialogueHeight - Mathf.Max(28f, Screen.height * 0.035f);

        float minimumGap = Mathf.Max(20f, Screen.height * 0.022f);
        float maxWidth = Mathf.Min(Screen.width * 0.594f, 726f);
        float maxHeight = Mathf.Max(54f, dialogueY - minimumGap * 2f);
        float aspect = image.width > 0 && image.height > 0 ? (float)image.width / image.height : 1f;

        float width = maxWidth;
        float height = width / aspect;
        if (height > maxHeight)
        {
            height = maxHeight;
            width = height * aspect;
        }

        float balancedGap = Mathf.Max(minimumGap, (dialogueY - height) * 0.5f);
        float imageY = balancedGap + Mathf.Min(14f, Mathf.Max(0f, balancedGap - minimumGap) * 0.25f);

        Rect shadowRect = new Rect((Screen.width - width) * 0.5f - 6f, imageY - 6f, width + 12f, height + 12f);
        DrawSolidRect(shadowRect, new Color(0f, 0f, 0f, 0.45f));
        Rect imageRect = new Rect((Screen.width - width) * 0.5f, imageY, width, height);
        GUI.DrawTexture(imageRect, image, ScaleMode.ScaleToFit, true);
    }

    private void DrawDialogue()
    {
        float panelWidth = Mathf.Min(Screen.width * 0.86f, 1100f);
        float panelHeight = Mathf.Clamp(panelWidth / 4.1f, 150f, 270f);
        float panelX = (Screen.width - panelWidth) * 0.5f;
        float panelY = Screen.height - panelHeight - Mathf.Max(28f, Screen.height * 0.035f);
        Rect panelRect = new Rect(panelX, panelY, panelWidth, panelHeight);

        if (dialogueFrameTexture != null)
            GUI.DrawTextureWithTexCoords(panelRect, dialogueFrameTexture, new Rect(0.018f, 0.075f, 0.964f, 0.819f), true);
        else
            DrawSolidRect(panelRect, new Color(0.055f, 0.04f, 0.07f, 0.96f));

        if (dialogueStyle == null)
        {
            dialogueStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.029f), 21, 32),
                fontStyle = FontStyle.Normal,
                normal = { textColor = new Color(0.20f, 0.10f, 0.055f, 1f) }
            };
            
            LockTextState(dialogueStyle);
dialogueStyle.font = koreanFont;

            hintStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.018f), 14, 20),
                normal = { textColor = new Color(0.28f, 0.16f, 0.09f, 0.88f) }
            };
            
            LockTextState(hintStyle);
hintStyle.font = koreanFont;
        }

        Rect textRect = new Rect(
            panelRect.x + 112f,
            panelRect.y + 44f,
            panelRect.width - 224f,
            panelRect.height - 68f
        );
        string fullText = memoryTexts[currentMemoryIndex];
        int visibleCharacters = GetVisibleCharacterCount(fullText);
        GUI.Label(textRect, fullText.Substring(0, visibleCharacters), dialogueStyle);

        Rect hintRect = new Rect(
            panelRect.x + 100f,
            panelRect.y + panelRect.height - 45f,
            panelRect.width - 200f,
            22f
        );
        float pulse = (Mathf.Sin(Time.unscaledTime * 2.8f) + 1f) * 0.5f;
        float pulseAlpha = Mathf.Lerp(0.48f, 1f, pulse);
        Color pulseColor = new Color(0.38f, 0.20f, 0.075f, pulseAlpha);
        hintStyle.normal.textColor = pulseColor;
        hintStyle.hover.textColor = pulseColor;
        hintStyle.active.textColor = pulseColor;
        hintStyle.focused.textColor = pulseColor;
        GUI.Label(hintRect, "\uD074\uB9AD \uB610\uB294 Space", hintStyle);
    }

    private void DrawSolidRect(Rect rect, Color color)
    {
        GUI.DrawTexture(
            rect,
            whiteTexture,
            ScaleMode.StretchToFill,
            false,
            0f,
            color,
            0f,
            0f
        );
    }

private void OnDestroy()
    {
        if (visible)
            Time.timeScale = previousTimeScale;

        if (dialogueStyle != null)
            dialogueStyle.font = null;

        if (hintStyle != null)
            hintStyle.font = null;

        koreanFont = null;

        if (instance == this)
            instance = null;
    }


private static void LockTextState(GUIStyle style)
    {
        if (style == null)
            return;

        Color color = style.normal.textColor;
        Color hoverColor = Color.Lerp(color, Color.white, 0.06f);
        style.hover.textColor = hoverColor;
        style.active.textColor = color;
        style.focused.textColor = color;
        style.onNormal.textColor = color;
        style.onHover.textColor = hoverColor;
        style.onActive.textColor = color;
        style.onFocused.textColor = color;
    }
}
