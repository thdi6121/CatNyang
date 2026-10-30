using UnityEngine;

public class BirdFleeController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    //[SerializeField] private BirdIdleController idleController;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip takeOffSound;

    [Header("Detection")]
    [Tooltip("고양이 오브젝트에 지정된 Tag")]
    private string catTag = "Player";

    [Header("Flight")]
    [SerializeField] private float forwardSpeed = 5f;
    [SerializeField] private float upwardSpeed = 2.5f;
    [SerializeField] private float acceleration = 5f;
    [SerializeField] private float destroyDistance = 20f;

    [Tooltip("고양이의 정확한 반대 방향으로 날지 않고 약간 옆으로 틀어지는 최대 각도")]
    [SerializeField] private float randomDirectionAngle = 20f;

    private static readonly int TakeOffHash =
        Animator.StringToHash("FlyAway");

    private Vector3 takeOffPosition;
    private Vector3 flightDirection;

    private float currentForwardSpeed;
    private float currentUpwardSpeed;

    private bool hasTriggeredFlee;
    private bool isFlying;
    private bool isSideQuestTarget;
    [SerializeField] private GameObject questHighlightPrefab;
    private GameObject questHighlightInstance;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (questHighlightPrefab == null)
            questHighlightPrefab = Resources.Load<GameObject>("Effects/BirdSittingGlow");
    }

    private void Update()
    {
        if (isFlying)
            MoveBird();
    }

    public void SetSideQuestTarget(bool active)
    {
        if (hasTriggeredFlee)
            active = false;
        isSideQuestTarget = active;
        if (active)
            CreateQuestHighlight();
        else if (questHighlightInstance != null)
            Destroy(questHighlightInstance);
    }

    private void CreateQuestHighlight()
    {
        if (questHighlightInstance != null || questHighlightPrefab == null)
            return;

        questHighlightInstance = Instantiate(questHighlightPrefab, transform);
        questHighlightInstance.name = "Bird Sitting Quest Glow";
        questHighlightInstance.transform.localPosition = Vector3.up * 0.15f;
        questHighlightInstance.transform.localRotation = Quaternion.identity;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggeredFlee)
            return;

        if (!other.CompareTag(catTag))
            return;

        StartFlee(other.transform);
    }

    private void StartFlee(Transform cat)
    {
        hasTriggeredFlee = true;
        if (isSideQuestTarget)
            FireQuestManager.Instance?.NotifyBirdFled(this);
        SetSideQuestTarget(false);

        SetFlightDirection(cat);

        // 새가 날아갈 방향을 바라보게 함
        if (flightDirection.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(
                flightDirection,
                Vector3.up);
        }

        animator.SetTrigger(TakeOffHash);

        PlayTakeOffSound();

        isFlying = true;
        takeOffPosition = transform.position;
        currentForwardSpeed = 0f;
        currentUpwardSpeed = 0f;
    }

    private void SetFlightDirection(Transform cat)
    {
        // 고양이 반대 방향
        Vector3 awayFromCat = transform.position - cat.position;
        awayFromCat.y = 0f;

        if (awayFromCat.sqrMagnitude < 0.001f)
            awayFromCat = transform.forward;

        awayFromCat.Normalize();

        // 새마다 비행 방향이 완전히 똑같지 않도록 약간 회전
        float randomAngle = Random.Range(
            -randomDirectionAngle,
            randomDirectionAngle);

        flightDirection =
            Quaternion.Euler(0f, randomAngle, 0f) * awayFromCat;

        flightDirection.Normalize();
    }

   
    public void PlayTakeOffSound()
    {
        if (audioSource == null || takeOffSound == null)
            return;

        audioSource.pitch = Random.Range(0.95f, 1.05f);
        audioSource.PlayOneShot(takeOffSound);
    }

    private void MoveBird()
    {
        currentForwardSpeed = Mathf.MoveTowards(
            currentForwardSpeed,
            forwardSpeed,
            acceleration * Time.deltaTime);

        currentUpwardSpeed = Mathf.MoveTowards(
            currentUpwardSpeed,
            upwardSpeed,
            acceleration * Time.deltaTime);

        Vector3 velocity =
            flightDirection * currentForwardSpeed
            + Vector3.up * currentUpwardSpeed;

        transform.position += velocity * Time.deltaTime;

        float traveledSqrDistance =
            (transform.position - takeOffPosition).sqrMagnitude;

        if (traveledSqrDistance >= destroyDistance * destroyDistance)
            Destroy(gameObject);
    }
}