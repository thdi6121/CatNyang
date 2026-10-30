using UnityEngine;

using System.Collections.Generic;
using UnityEngine.UI;

public class FortuneTellerInteraction : MonoBehaviour
{
    private static readonly List<FortuneTellerInteraction> Instances = new List<FortuneTellerInteraction>();
    private static FortuneTellerInteraction activeWindowOwner;

    [SerializeField] private float interactionDistance = 5f;

    private readonly string[] fortunes =
    {
        "작은 발걸음이 오늘의 행운을 데려옵니다.",
        "비가 그친 뒤, 반가운 소식이 찾아올 거예요.",
        "잊고 있던 인연이 따뜻한 도움을 건넵니다.",
        "서두르지 말고 냄새를 따라가 보세요. 좋은 일이 기다립니다.",
        "오늘의 호기심은 뜻밖의 발견으로 이어집니다.",
        "포근한 쉼이 내일의 용기를 채워 줄 거예요."
    };

    private Transform player;
    private GameObject promptPanel;
    private GameObject fortunePanel;
    private Text fortuneText;

    private void Awake()
    {
        FindPlayer();
        CreateUi();
    }

private void OnEnable()
    {
        if (!Instances.Contains(this))
            Instances.Add(this);
    }

    private void OnDisable()
    {
        Instances.Remove(this);
        if (activeWindowOwner == this)
            activeWindowOwner = null;
    }

    private bool IsClosestToPlayer()
    {
        if (player == null)
            return false;

        float myDistance = Vector3.SqrMagnitude(new Vector3(player.position.x - transform.position.x, 0f, player.position.z - transform.position.z));

        foreach (FortuneTellerInteraction other in Instances)
        {
            if (other == null || other == this || !other.isActiveAndEnabled)
                continue;

            float otherDistance = Vector3.SqrMagnitude(new Vector3(player.position.x - other.transform.position.x, 0f, player.position.z - other.transform.position.z));

            if (otherDistance < myDistance ||
                (Mathf.Approximately(otherDistance, myDistance) && other.GetInstanceID() < GetInstanceID()))
                return false;
        }

        return true;
    }


private void Update()
    {
        if (player == null)
        {
            FindPlayer();
            return;
        }

        bool isClosest = IsClosestToPlayer();
        bool inRange = isClosest && IsPlayerInRange();

        promptPanel.SetActive(inRange && !fortunePanel.activeSelf && activeWindowOwner == null);

        if (inRange && !fortunePanel.activeSelf && activeWindowOwner == null && Input.GetKeyDown(KeyCode.F))
        {
            ShowFortune();
            return;
        }

        if (fortunePanel.activeSelf && (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Escape)))
        {
            fortunePanel.SetActive(false);
            if (activeWindowOwner == this)
                activeWindowOwner = null;
        }
    }

private void ShowFortune()
    {
        if (activeWindowOwner != null && activeWindowOwner != this)
            activeWindowOwner.fortunePanel.SetActive(false);

        activeWindowOwner = this;
        fortuneText.text = fortunes[Random.Range(0, fortunes.Length)];
        fortuneText.font.RequestCharactersInTexture(fortuneText.text, fortuneText.fontSize, fortuneText.fontStyle);
        fortunePanel.SetActive(true);
        promptPanel.SetActive(false);
        FireQuestManager.Instance?.NotifyFortuneViewed();
    }


    private void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            player = playerObject.transform;
    }

    private bool IsPlayerInRange()
    {
        Vector3 a = player.position;
        Vector3 b = transform.position;
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b) <= interactionDistance;
    }

    private void CreateUi()
    {
        GameObject canvasObject = new GameObject("Fortune Teller UI");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();

        Font font = CreateFont();
        
        promptPanel = CreatePanel(canvasObject.transform, "Fortune Prompt", new Vector2(0f, 110f), new Vector2(352f, 352f * 425f / 1094f), new Color(0.10f, 0.055f, 0.14f, 0.94f));
        ApplyInteractionFrame(promptPanel);
        Text promptLabel = CreateText(
            promptPanel.transform,
            "<size=26><color=#F0D49B>[ F ]  \uC6B4\uC138 \uBCF4\uAE30</color></size>",
            25,
            FontStyle.Normal,
            Color.white,
            TextAnchor.MiddleCenter,
            new Vector2(38f, -10f),
            new Vector2(238f, 70f),
            font
        );
        Shadow promptShadow = promptLabel.gameObject.AddComponent<Shadow>();
        promptShadow.effectColor = new Color(0.06f, 0.025f, 0.015f, 0.88f);
        promptShadow.effectDistance = new Vector2(1f, -1f);
        promptShadow.useGraphicAlpha = true;

        fortunePanel = CreatePanel(canvasObject.transform, "Fortune Window", Vector2.zero, new Vector2(700f, 380f), new Color(0.12f, 0.06f, 0.17f, 0.98f));
        CreateText(fortunePanel.transform, "오늘의 운세", 40, FontStyle.Bold, new Color(1f, 0.82f, 0.38f), TextAnchor.MiddleCenter, new Vector2(0f, 115f), new Vector2(620f, 55f), font);
        fortuneText = CreateText(fortunePanel.transform, "", 30, FontStyle.Normal, new Color(1f, 0.96f, 0.9f), TextAnchor.MiddleCenter, Vector2.zero, new Vector2(570f, 130f), font);
        fortuneText.horizontalOverflow = HorizontalWrapMode.Wrap;
        fortuneText.verticalOverflow = VerticalWrapMode.Overflow;
        CreateText(fortunePanel.transform, "[ F ] 또는 [ Esc ] 닫기", 20, FontStyle.Normal, new Color(0.74f, 0.67f, 0.8f), TextAnchor.MiddleCenter, new Vector2(0f, -135f), new Vector2(500f, 35f), font);

        promptPanel.SetActive(false);
        fortunePanel.SetActive(false);
    }

    private static GameObject CreatePanel(Transform parent, string name, Vector2 position, Vector2 size, Color color)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        panel.GetComponent<Image>().color = color;
        return panel;
    }

    private static Text CreateText(Transform parent, string value, int size, FontStyle style, Color color, TextAnchor alignment, Vector2 position, Vector2 area, Font font)
    {
        GameObject label = new GameObject("Text", typeof(RectTransform), typeof(Text));
        label.transform.SetParent(parent, false);
        RectTransform rect = label.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = area;
        Text text = label.GetComponent<Text>();
        text.font = font;
        text.font.RequestCharactersInTexture(value, size, style);
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        return text;
    }

private static Font CreateFont()
    {
        Font koreanFont = Resources.Load<Font>("Fonts/DNFBitBitv2");
        if (koreanFont == null)
            koreanFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return koreanFont;
    }


private static void ApplyInteractionFrame(GameObject panel)
    {
        Texture2D promptFrame = Resources.Load<Texture2D>("UI/Frames/\uC0C1\uD638\uC791\uC6A9 (\uCD5C\uC2E0)");
        if (promptFrame == null)
            return;

        Image image = panel.GetComponent<Image>();
        image.sprite = Sprite.Create(promptFrame, new Rect(44f, 337f, 1094f, 425f), new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect);
        
        image.preserveAspect = true;
image.color = Color.white;
    }
}