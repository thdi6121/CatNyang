using UnityEngine.SceneManagement;
using UnityEngine;

public sealed class ExitGameUI : MonoBehaviour
{
    [SerializeField] private Texture2D pawIcon;
    private Texture2D exitFrameTexture;
    private Texture2D confirmationFrameTexture;
    private Texture2D confirmationButtonTexture;

    private bool showConfirmation;

    private void Awake()
    {
        exitFrameTexture = Resources.Load<Texture2D>("UI/Frames/\uB098\uAC00\uAE30");
        confirmationFrameTexture = Resources.Load<Texture2D>("UI/Frames/\uB098\uAC00\uAE30 \uBA54\uC778\uCC3D");
        confirmationButtonTexture = Resources.Load<Texture2D>("UI/Frames/\uB098\uAC00\uAE30 1");
    }

    
private void OnGUI()
    {
        if (GameTitleSequence.IsShowing || GameIntroTutorial.IsShowing)
            return;

        float scale = Mathf.Clamp(Screen.height / 900f, 0.75f, 1.25f);
        float buttonSize = 72f * scale;
        Rect exitButtonRect = new Rect(
            Screen.width - buttonSize - 28f * scale,
            Screen.height - buttonSize - 56f * scale,
            buttonSize,
            buttonSize
        );

        bool exitHovered = exitButtonRect.Contains(Event.current.mousePosition);
        Color previousExitColor = GUI.color;
        GUI.color = exitHovered
            ? Color.white
            : new Color(0.82f, 0.82f, 0.82f, 0.94f);

        if (exitFrameTexture != null)
            GUI.DrawTextureWithTexCoords(exitButtonRect, exitFrameTexture, new Rect(0.276f, 0.194f, 0.446f, 0.638f), true);
        else
            GUI.Box(exitButtonRect, GUIContent.none);

        GUI.color = previousExitColor;

        if (GUI.Button(exitButtonRect, new GUIContent(string.Empty, "\uAC8C\uC784 \uC885\uB8CC"), GUIStyle.none))
            showConfirmation = true;

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(13f * scale),
            fontStyle = FontStyle.Bold,
            normal = { textColor = exitHovered ? new Color(0.42f, 0.21f, 0.50f) : new Color(0.20f, 0.08f, 0.24f) }
        };
        
        labelStyle.hover.textColor = labelStyle.normal.textColor;
        labelStyle.active.textColor = labelStyle.normal.textColor;
        labelStyle.focused.textColor = labelStyle.normal.textColor;
labelStyle.font = Resources.Load<Font>("Fonts/DNFBitBitv2");
        GUI.Label(
            new Rect(exitButtonRect.x - 10f, exitButtonRect.yMax + 5f, buttonSize + 20f, 22f * scale),
            "\uB098\uAC00\uAE30",
            labelStyle
        );

        if (exitHovered)
        {
            GUIStyle tooltipStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = Mathf.RoundToInt(12f * scale),
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.48f, 0.27f, 0.57f) }
            };
            
            tooltipStyle.hover.textColor = tooltipStyle.normal.textColor;
            tooltipStyle.hover.background = tooltipStyle.normal.background;
tooltipStyle.font = labelStyle.font;
            GUI.Label(
                new Rect(exitButtonRect.x - 18f, exitButtonRect.y - 30f * scale, buttonSize + 36f, 24f * scale),
                "\uAC8C\uC784 \uC885\uB8CC",
                tooltipStyle
            );
        }

        if (showConfirmation)
            DrawConfirmation(scale);
    }

private void DrawConfirmation(float scale)
    {
        GUI.color = new Color(0f, 0f, 0f, 0.68f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float panelWidth = Mathf.Min(680f * scale, Screen.width * 0.88f);
        float panelHeight = panelWidth / 2.1608f;
        Rect panelRect = new Rect(
            (Screen.width - panelWidth) * 0.5f,
            (Screen.height - panelHeight) * 0.5f,
            panelWidth,
            panelHeight
        );

        if (confirmationFrameTexture != null)
            GUI.DrawTextureWithTexCoords(panelRect, confirmationFrameTexture, new Rect(0.015625f, 0.016925f, 0.953776f, 0.956276f), true);
        else
            GUI.Box(panelRect, GUIContent.none);

        Font font = Resources.Load<Font>("Fonts/DNFBitBitv2");
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(25f * scale),
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(1f, 0.86f, 0.52f) }
        };
        titleStyle.font = font;
        titleStyle.hover.textColor = titleStyle.normal.textColor;

        GUIStyle messageStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(16f * scale),
            wordWrap = true,
            normal = { textColor = new Color(0.90f, 0.86f, 0.98f) }
        };
        messageStyle.font = font;
        messageStyle.hover.textColor = messageStyle.normal.textColor;

        GUIStyle actionButtonStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(15f * scale),
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(1f, 0.88f, 0.60f) }
        };
        actionButtonStyle.font = font;
        actionButtonStyle.hover.textColor = actionButtonStyle.normal.textColor;

        GUI.Label(
            new Rect(panelRect.x + 95f * scale, panelRect.y + 116f * scale, panelRect.width - 190f * scale, 36f * scale),
            "\uAC8C\uC784\uC744 \uB098\uAC08\uAE4C\uC694?",
            titleStyle
        );
        GUI.Label(
            new Rect(panelRect.x + 105f * scale, panelRect.y + 160f * scale, panelRect.width - 210f * scale, 42f * scale),
            "\uC9C4\uD589 \uC0C1\uD669\uC774 \uC800\uC7A5\uB418\uC9C0 \uC54A\uC744 \uC218 \uC788\uC5B4\uC694.",
            messageStyle
        );

        float buttonWidth = 158f * scale;
        float buttonHeight = buttonWidth / 2.5414f;
        float gap = 12f * scale;
        float buttonsWidth = buttonWidth * 3f + gap * 2f;
        float firstButtonX = panelRect.center.x - buttonsWidth * 0.5f;
        float buttonsY = panelRect.yMax - buttonHeight - 38f * scale;

        bool restart = DrawConfirmationButton(
            new Rect(firstButtonX, buttonsY, buttonWidth, buttonHeight),
            "\uB2E4\uC2DC\uD558\uAE30",
            actionButtonStyle
        );
        bool continueGame = DrawConfirmationButton(
            new Rect(firstButtonX + buttonWidth + gap, buttonsY, buttonWidth, buttonHeight),
            "\uACC4\uC18D\uD558\uAE30",
            actionButtonStyle
        );
        bool confirmExit = DrawConfirmationButton(
            new Rect(firstButtonX + (buttonWidth + gap) * 2f, buttonsY, buttonWidth, buttonHeight),
            "\uB098\uAC00\uAE30",
            actionButtonStyle
        );

        if (restart)
            RestartGame();
        else if (continueGame)
            showConfirmation = false;
        else if (confirmExit)
            QuitGame();
    }

    private bool DrawConfirmationButton(Rect rect, string label, GUIStyle labelStyle)
    {
        bool hovered = rect.Contains(Event.current.mousePosition);
        Color previousColor = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, hovered ? 1f : 0.90f);

        if (confirmationButtonTexture != null)
            GUI.DrawTextureWithTexCoords(rect, confirmationButtonTexture, new Rect(0.018519f, 0.227451f, 0.946502f, 0.709804f), true);
        else
            GUI.Box(rect, GUIContent.none);

        GUI.color = previousColor;
        bool clicked = GUI.Button(rect, GUIContent.none, GUIStyle.none);
        GUI.Label(new Rect(rect.x + 12f, rect.y + 6f, rect.width - 24f, rect.height - 4f), label, labelStyle);
        return clicked;
    }

    private static void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }


private static void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
