using System.Collections;
using UnityEngine;

public class CatFishCatch : MonoBehaviour
{
    [Header("Fish Attachment")]
    [SerializeField] private Transform mouthSocket;

    [SerializeField] private Vector3 heldLocalPosition;
    [SerializeField] private Vector3 heldLocalEulerAngles;
    [SerializeField] private Vector3 heldLocalScale = Vector3.one;

    [Header("Eat Interaction")]
    [SerializeField] private GameObject eatFishTooltip;
    private KeyCode eatKey = KeyCode.F;
    //[SerializeField] private float eatDuration = 0.7f;

    [Header("Eating Effect")]
    //[SerializeField] private bool moveTowardMouthWhileEating = true;
    [SerializeField] private Vector3 eatingEndLocalPosition = Vector3.zero;

    [Header("Face Blend Shape")]
    [SerializeField] private SkinnedMeshRenderer catMesh;
    [SerializeField] private int fishFaceBlendShapeIndex;
    [SerializeField, Range(0f, 100f)]
    private float fishFaceWeight = 100f;

    [Header("Step Eating Effect")]
    [SerializeField] private float shrinkInterval = 1f;
    [SerializeField] private int shrinkSteps = 3;
    //[SerializeField, Range(0.01f, 0.99f)]
    //private float scaleMultiplierPerStep = 0.6f;


    [Header("Eating Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip eatSound;

    [Header("Smile Expression")]
    [SerializeField] private int smileBlendShapeIndex1;
    [SerializeField] private int smileBlendShapeIndex2;
    [SerializeField] private int smileBlendShapeIndex3;

    [SerializeField, Range(0f, 100f)]
    private float smileWeight1 = 100f;

    [SerializeField, Range(0f, 100f)]
    private float smileWeight2 = 100f;

    [SerializeField, Range(0f, 100f)]
    private float smileWeight3 = 100f;

    [SerializeField] private float smileDuration = 2f;
    [SerializeField] private float smileBlendDuration = 0.2f;

    [Header("Eye Animation Control")]
    [SerializeField] private Animator animator;
    [SerializeField] private string blinkLayerName = "Blink";
    //[SerializeField] private float blinkLayerBlendDuration = 0.1f;
    private static readonly int IsEatingFishHash =
    Animator.StringToHash("IsEatingFish");

    private int blinkLayerIndex = -1;
    private Coroutine blinkLayerCoroutine;
    private Coroutine smileCoroutine;
   
    public bool IsBusyWithFish =>
    isEating || smileCoroutine != null;

    //���⼭���ʹ� Header ���� ����� ����

    private GameObject heldFish;
    private Coroutine eatingCoroutine;
    
    private float eatInputUnlockTime;
private bool isEating;

    public bool HasFish => heldFish != null;
    public bool IsEating => isEating;


    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (animator != null)
        {
            blinkLayerIndex =
                animator.GetLayerIndex(blinkLayerName);

            if (blinkLayerIndex < 0)
            {
                Debug.LogWarning(
                    $"Animator���� '{blinkLayerName}' ���̾ ã�� ���߽��ϴ�.",
                    this
                );
            }
        }

        SetTooltipActive(false);
    }

private void Update()
    {
        if (!HasFish || isEating || Time.unscaledTime < eatInputUnlockTime)
        {
            return;
        }

        if (Input.GetKeyDown(eatKey))
        {
            EatFish();
        }
    }

    public void EquipFish(GameObject fish)
    {
        if (fish == null)
        {
            Debug.LogWarning("������ ������ �����ϴ�.", this);
            return;
        }

        if (mouthSocket == null)
        {
            Debug.LogError("MouthSocket�� ������� �ʾҽ��ϴ�.", this);
            return;
        }

        if (HasFish || isEating)
        {
            Debug.LogWarning("�̹� ������ ���� �ְų� �Դ� ���Դϴ�.", this);
            return;
        }

        heldFish = fish;

        DisableFishPhysics(heldFish);

        Transform fishTransform = heldFish.transform;

        fishTransform.SetParent(mouthSocket, false);
        fishTransform.localPosition = heldLocalPosition;
        fishTransform.localRotation =
            Quaternion.Euler(heldLocalEulerAngles);
        fishTransform.localScale = heldLocalScale;

        heldFish.SetActive(true);

        SetFishFaceWeight(fishFaceWeight);
        
        eatInputUnlockTime = Time.unscaledTime + 0.25f;
SetTooltipActive(true);
    }

    public void EatFish()
    {
        if (!HasFish || isEating)
        {
            return;
        }

        SetFishFaceWeight(0f);


        eatingCoroutine = StartCoroutine(EatFishCoroutine());
    }

    /*private IEnumerator EatFishCoroutine()
    {
        isEating = true;

        // �Ա� ���۰� ���ÿ� ������ ���� �ߺ� �Է��� �����ϴ�.
        SetTooltipActive(false);

        if (audioSource != null && eatSound != null)
        {
            audioSource.PlayOneShot(eatSound);
        }

        GameObject fishToEat = heldFish;
        Transform fishTransform = fishToEat.transform;

        Vector3 startScale = fishTransform.localScale;
        Vector3 startPosition = fishTransform.localPosition;

        float elapsedTime = 0f;

        while (elapsedTime < eatDuration)
        {
            // ���߿� ������ �ٸ� �ڵ�� ������ ��츦 ����մϴ�.
            if (fishToEat == null)
            {
                FinishEating();
                yield break;
            }

            elapsedTime += Time.deltaTime;

            float normalizedTime =
                Mathf.Clamp01(elapsedTime / eatDuration);

            // �ڷ� ������ ���� �� ������ �۾����� ȿ��
            float easedTime =
                normalizedTime * normalizedTime;

            fishTransform.localScale = Vector3.Lerp(
                startScale,
                Vector3.zero,
                easedTime
            );

            if (moveTowardMouthWhileEating)
            {
                fishTransform.localPosition = Vector3.Lerp(
                    startPosition,
                    eatingEndLocalPosition,
                    easedTime
                );
            }

            yield return null;
        }

        if (fishToEat != null)
        {
            Destroy(fishToEat);
        }

        FinishEating();
    }*/

    private IEnumerator EatFishCoroutine()
    {
        isEating = true;
        SetTooltipActive(false);

        SetBlinkLayerEnabled(false);

        SetEatingFishAnimatorState(true);


        if (audioSource != null && eatSound != null)
        {
            audioSource.PlayOneShot(eatSound);
        }

        GameObject fishToEat = heldFish;

        if (fishToEat == null)
        {
            FinishEating();
            yield break;
        }

        /*Transform fishTransform = fishToEat.transform;

        for (int step = 0; step < shrinkSteps; step++)
        {

            if (fishToEat == null)
            {
                FinishEating();
                yield break;
            }

            fishTransform.localScale *= scaleMultiplierPerStep;

            yield return new WaitForSeconds(shrinkInterval);
        }

        Destroy(fishToEat);
        FinishEating();*/

        Transform fishTransform = fishToEat.transform;

        Vector3 startScale = fishTransform.localScale;
        Vector3 startPosition = fishTransform.localPosition;

        for (int step = 1; step <= shrinkSteps; step++)
        {
            // F�� �����ڸ��� ù �ܰ谡 ����˴ϴ�.
            float progress = (float)step / shrinkSteps;
            float remainingRatio = 1f - progress;

            fishTransform.localScale =
                startScale * remainingRatio;

            fishTransform.localPosition =
                Vector3.Lerp(
                    startPosition,
                    eatingEndLocalPosition,
                    progress
                );

            yield return new WaitForSeconds(shrinkInterval);

            if (fishToEat == null)
            {
                FinishEating();
                yield break;
            }
        }

        Destroy(fishToEat);
        FireQuestManager.Instance?.NotifyFishEaten();
        FinishEating();
    }

    private void FinishEating()
    {
        heldFish = null;
        eatingCoroutine = null;
        isEating = false;

        //SetFishFaceWeight(0f);
        SetTooltipActive(false);

        if (smileCoroutine != null)
        {
            StopCoroutine(smileCoroutine);
        }

        smileCoroutine = StartCoroutine(SmileCoroutine());
    }

    private void SetFishFaceWeight(float weight)
    {
        if (catMesh == null)
        {
            return;
        }

        Mesh mesh = catMesh.sharedMesh;

        if (mesh == null ||
            fishFaceBlendShapeIndex < 0 ||
            fishFaceBlendShapeIndex >= mesh.blendShapeCount)
        {
            Debug.LogWarning(
                "Blend Shape �ε����� ��ȿ���� �ʽ��ϴ�.",
                this
            );
            return;
        }

        catMesh.SetBlendShapeWeight(
            fishFaceBlendShapeIndex,
            weight
        );
    }

private void SetTooltipActive(bool active)
    {
        if (eatFishTooltip == null)
            return;

        if (active)
        {
            UnityEngine.UI.Image frameImage = eatFishTooltip.GetComponentInChildren<UnityEngine.UI.Image>(true);
            Texture2D frameTexture = Resources.Load<Texture2D>("UI/Frames/\uC0C1\uD638\uC791\uC6A9 (\uCD5C\uC2E0)");
            if (frameImage != null && frameTexture != null)
            {
                frameImage.sprite = Sprite.Create(frameTexture, new Rect(44f, 337f, 1094f, 425f), new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect);
                frameImage.color = Color.white;
                frameImage.preserveAspect = true;
                RectTransform frameRect = frameImage.rectTransform;
                frameRect.sizeDelta = new Vector2(frameRect.sizeDelta.x, frameRect.sizeDelta.x * 425f / 1094f);
            }

            UnityEngine.UI.Text promptText = eatFishTooltip.GetComponentInChildren<UnityEngine.UI.Text>(true);
            if (promptText != null)
            {
                promptText.font = Resources.Load<Font>("Fonts/DNFBitBitv2");
                if (promptText.font == null)
                    promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                promptText.supportRichText = true;
                promptText.fontSize = 25;
                promptText.fontStyle = FontStyle.Normal;
                promptText.color = Color.white;
                promptText.text = "<size=26><color=#F0D49B>[ F ]  \uC0DD\uC120 \uBA39\uAE30</color></size>";

                RectTransform textRect = promptText.rectTransform;
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(108f, 10f);
                textRect.offsetMax = new Vector2(-28f, -30f);

                UnityEngine.UI.Shadow promptShadow = promptText.GetComponent<UnityEngine.UI.Shadow>();
                if (promptShadow == null)
                    promptShadow = promptText.gameObject.AddComponent<UnityEngine.UI.Shadow>();
                promptShadow.effectColor = new Color(0.06f, 0.025f, 0.015f, 0.88f);
                promptShadow.effectDistance = new Vector2(1f, -1f);
                promptShadow.useGraphicAlpha = true;
            }
        }

        eatFishTooltip.SetActive(active);
    }

    private static void DisableFishPhysics(GameObject fish)
    {
        Rigidbody fishRigidbody =
            fish.GetComponent<Rigidbody>();

        if (fishRigidbody != null)
        {
#if UNITY_6000_0_OR_NEWER
            fishRigidbody.linearVelocity = Vector3.zero;
#else
            fishRigidbody.velocity = Vector3.zero;
#endif
            fishRigidbody.angularVelocity = Vector3.zero;
            fishRigidbody.useGravity = false;
            fishRigidbody.isKinematic = true;
        }

        Collider[] colliders =
            fish.GetComponentsInChildren<Collider>();

        foreach (Collider fishCollider in colliders)
        {
            fishCollider.enabled = false;
        }
    }

    private void OnDisable()
    {
        SetTooltipActive(false);
    }


    private IEnumerator SmileCoroutine()
    {
        //SetBlinkLayerEnabled(false);

        yield return null;

        if (!AreSmileBlendShapesValid())
        {
            SetBlinkLayerEnabled(true);
            SetEatingFishAnimatorState(false);
            smileCoroutine = null;
            yield break;
        }


        // �⺻ ǥ������ ��� ǥ������ ��ȯ
        yield return BlendSmileWeights(
            0f, smileWeight1,
            0f, smileWeight2,
            0f, smileWeight3,
            smileBlendDuration
        );

        // ��� ǥ�� ����
        yield return new WaitForSeconds(smileDuration);

        // ��� ǥ������ �⺻ ǥ������ ����
        yield return BlendSmileWeights(
            smileWeight1, 0f,
            smileWeight2, 0f,
            smileWeight3, 0f,
            smileBlendDuration
        );

        SetSmileWeights(0f, 0f, 0f);
        SetBlinkLayerEnabled(true);
        SetEatingFishAnimatorState(false);
        smileCoroutine = null;
    }

    private IEnumerator BlendSmileWeights(
    float startWeight1,
    float endWeight1,
    float startWeight2,
    float endWeight2,
    float startWeight3,
    float endWeight3,
    float duration)
    {
        if (duration <= 0f)
        {
            SetSmileWeights(
                endWeight1,
                endWeight2,
                endWeight3
            );

            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                elapsed / duration
            );

            // SmoothStep ������ �ε巯�� ����
            t = t * t * (3f - 2f * t);

            float currentWeight1 = Mathf.Lerp(
                startWeight1,
                endWeight1,
                t
            );

            float currentWeight2 = Mathf.Lerp(
                startWeight2,
                endWeight2,
                t
            );

            float currentWeight3 = Mathf.Lerp(
                startWeight3,
                endWeight3,
                t
            );

            SetSmileWeights(
                currentWeight1,
                currentWeight2,
                currentWeight3
            );

            yield return null;
        }

        SetSmileWeights(
            endWeight1,
            endWeight2,
            endWeight3
        );
    }

    private void SetSmileWeights(
    float weight1,
    float weight2,
    float weight3)
    {
        if (!AreSmileBlendShapesValid())
        {
            return;
        }

        catMesh.SetBlendShapeWeight(
            smileBlendShapeIndex1,
            weight1
        );

        catMesh.SetBlendShapeWeight(
            smileBlendShapeIndex2,
            weight2
        );

        catMesh.SetBlendShapeWeight(
            smileBlendShapeIndex3,
            weight3
        );
    }

    private bool AreSmileBlendShapesValid()
    {
        if (catMesh == null ||
            catMesh.sharedMesh == null)
        {
            return false;
        }

        int blendShapeCount =
            catMesh.sharedMesh.blendShapeCount;

        bool firstIndexValid =
            smileBlendShapeIndex1 >= 0 &&
            smileBlendShapeIndex1 < blendShapeCount;

        bool secondIndexValid =
            smileBlendShapeIndex2 >= 0 &&
            smileBlendShapeIndex2 < blendShapeCount;

        bool thirdIndexValid =
            smileBlendShapeIndex3 >= 0 &&
            smileBlendShapeIndex3 < blendShapeCount;

        if (!firstIndexValid ||
            !secondIndexValid ||
            !thirdIndexValid)
        {
            Debug.LogWarning(
                $"��� ǥ�� Blend Shape �ε����� ��ȿ���� �ʽ��ϴ�. " +
                $"���� Blend Shape ����: {blendShapeCount}",
                this
            );

            return false;
        }

        return true;
    }

    private void SetBlinkLayerEnabled(bool enabled)
    {
        if (animator == null || blinkLayerIndex < 0)
        {
            return;
        }

        animator.SetLayerWeight(
            blinkLayerIndex,
            enabled ? 1f : 0f
        );
    }
    private void SetEatingFishAnimatorState(bool value)
    {
        if (animator != null)
        {
            animator.SetBool(IsEatingFishHash, value);
        }
    }

}