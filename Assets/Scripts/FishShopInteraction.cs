using UnityEngine;
using UnityEngine.UI;

public class FishShopInteraction : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private float interactionDistance = 5f;

    [Header("Fish")]
    [SerializeField] private GameObject sceneFish;

    private Transform player;
    private CatFishCatch CatFishCatch;

    private GameObject promptPanel;
    private bool interactionCompleted;

    private void Awake()
    {
        FindPlayer();
        CreatePromptUi();
    }

    private void Update()
    {
        if (player == null)
        {
            FindPlayer();
            return;
        }

        bool canInteract =
            !interactionCompleted &&
            IsPlayerInRange();

        if (promptPanel != null)
        {
            promptPanel.SetActive(canInteract);
        }

        if (canInteract && Input.GetKeyDown(KeyCode.F))
        {
            CompleteInteraction();
        }
    }

    private void FindPlayer()
    {
        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
        {
            return;
        }

        player = playerObject.transform;
        CatFishCatch = playerObject.GetComponent<CatFishCatch>();

        if (CatFishCatch == null)
        {
            CatFishCatch =
                playerObject.GetComponentInParent<CatFishCatch>();
        }
    }

    private bool IsPlayerInRange()
    {
        Vector3 playerPosition = player.position;
        Vector3 shopPosition = transform.position;

        playerPosition.y = 0f;
        shopPosition.y = 0f;

        return Vector3.Distance(
            playerPosition,
            shopPosition
        ) <= interactionDistance;
    }

    private void CompleteInteraction()
    {
        if (CatFishCatch == null)
        {
            Debug.LogError(
                "�÷��̾�� CatFishCatch�� �����ϴ�.",
                gameObject
            );
            return;
        }

        if (sceneFish == null)
        {
            Debug.LogError(
                "Scene Fish�� ������� �ʾҽ��ϴ�.",
                gameObject
            );
            return;
        }

        CatFishCatch.EquipFish(sceneFish);

        interactionCompleted = true;

        if (promptPanel != null)
        {
            promptPanel.SetActive(false);
        }
    }

    private void CreatePromptUi()
    {
        GameObject canvasObject =
            new GameObject("Fish Shop UI");

        Canvas canvas =
            canvasObject.AddComponent<Canvas>();

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler =
            canvasObject.AddComponent<CanvasScaler>();

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;

        scaler.referenceResolution =
            new Vector2(1920f, 1080f);

        canvasObject.AddComponent<GraphicRaycaster>();

        promptPanel = new GameObject(
            "Fish Shop Prompt",
            typeof(RectTransform),
            typeof(Image)
        );

        promptPanel.transform.SetParent(
            canvasObject.transform,
            false
        );

        RectTransform panelRect =
            promptPanel.GetComponent<RectTransform>();

        panelRect.anchorMin =
            panelRect.anchorMax =
                new Vector2(0.5f, 0.5f);

        panelRect.anchoredPosition =
            new Vector2(0f, 110f);

        panelRect.sizeDelta =
            new Vector2(384f, 384f * 425f / 1094f);

        Image promptImage = promptPanel.GetComponent<Image>();
        Texture2D promptFrame = Resources.Load<Texture2D>("UI/Frames/\uC0C1\uD638\uC791\uC6A9 (\uCD5C\uC2E0)");
        if (promptFrame != null)
        {
            promptImage.sprite = Sprite.Create(promptFrame, new Rect(44f, 337f, 1094f, 425f), new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect);
            
            promptImage.preserveAspect = true;
promptImage.color = Color.white;
        }
        else
        {
            promptImage.color = new Color(0.1f, 0.055f, 0.14f, 0.94f);
        }

        GameObject textObject = new GameObject(
            "Text",
            typeof(RectTransform),
            typeof(Text)
        );

        textObject.transform.SetParent(
            promptPanel.transform,
            false
        );

        RectTransform textRect =
            textObject.GetComponent<RectTransform>();

        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(108f, 10f);
        textRect.offsetMax = new Vector2(-28f, -30f);

        Text promptText = textObject.GetComponent<Text>();

        promptText.font = Resources.Load<Font>("Fonts/DNFBitBitv2");
        if (promptText.font == null)
            promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        promptText.supportRichText = true;
        promptText.text = "<size=26><color=#F0D49B>[ F ]  \uC0DD\uC120 \uBC1B\uAE30</color></size>";
        promptText.fontSize = 25;
        promptText.alignment = TextAnchor.MiddleCenter;
        promptText.color = new Color(0.20f, 0.10f, 0.055f);

        Shadow promptShadow = textObject.AddComponent<Shadow>();
        promptShadow.effectColor = new Color(0.06f, 0.025f, 0.015f, 0.88f);
        promptShadow.effectDistance = new Vector2(1f, -1f);
        promptShadow.useGraphicAlpha = true;

        promptPanel.SetActive(false);
    }
}