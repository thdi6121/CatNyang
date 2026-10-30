using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class RatRunAway : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform cat;

    [Header("Flee Settings")]
    [SerializeField] private float detectionDistance = 5f;
    [SerializeField] private float safeDistance = 8f;
    [SerializeField] private float fleeDistance = 6f;
    [SerializeField] private float repathInterval = 0.2f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string speedParameter = "Speed";

    private NavMeshAgent agent;
    private float repathTimer;
    private bool isFleeing;
    [SerializeField] private float catchRadius = 0.65f;
    private GameObject catchEffectPrefab;
    private const float catchEffectLowering = 0.7f;
    private AudioClip catchSound;
    private bool caught;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
        SphereCollider catchTrigger = gameObject.AddComponent<SphereCollider>();
        catchTrigger.isTrigger = true;
        catchTrigger.radius = catchRadius;
        catchSound = Resources.Load<AudioClip>("Sounds/RatCatch");
        if (catchEffectPrefab == null)
            catchEffectPrefab = Resources.Load<GameObject>("Effects/RatCatchEffect");
    }

    private void Update()
    {
        if (cat == null)
        {
            return;
        }

        float distanceToCat = Vector3.Distance(transform.position, cat.position);

        if (distanceToCat <= detectionDistance)
        {
            isFleeing = true;
        }
        else if (distanceToCat >= safeDistance)
        {
            isFleeing = false;
            agent.ResetPath();
        }

        if (isFleeing)
        {
            repathTimer -= Time.deltaTime;

            if (repathTimer <= 0f)
            {
                FindFleeDestination();
                repathTimer = repathInterval;
            }
        }

        UpdateAnimation();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (caught)
            return;
        if (other.GetComponentInParent<KeyboardCatMove>() == null && other.name != "\uace0\uc591\uc774")
            return;

        caught = true;
        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
        FireQuestManager.Instance?.NotifyRatCaught();
        if (catchSound != null)
            AudioSource.PlayClipAtPoint(catchSound, transform.position, 0.8f);
        CreateCatchEffect();
        gameObject.SetActive(false);
        Destroy(gameObject, 0.2f);
    }

    private void CreateCatchEffect()
    {
        if (catchEffectPrefab == null)
            return;


        //GameObject effect = Instantiate(
        //    catchEffectPrefab,
        //    transform.position + Vector3.down * catchEffectLowering,
        //    Quaternion.identity
        //);
        GameObject effect = Instantiate(
            catchEffectPrefab,
            transform.position,
            Quaternion.identity
        );

        Destroy(effect, 5f);
    }

    private void FindFleeDestination()
    {
        Vector3 directionAwayFromCat = transform.position - cat.position;
        directionAwayFromCat.y = 0f;

        if (directionAwayFromCat.sqrMagnitude < 0.001f)
        {
            directionAwayFromCat = transform.forward;
        }

        directionAwayFromCat.Normalize();

        Vector3 desiredPosition =
            transform.position + directionAwayFromCat * fleeDistance;

        if (NavMesh.SamplePosition(
                desiredPosition,
                out NavMeshHit hit,
                fleeDistance,
                NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    private void UpdateAnimation()
    {
        if (animator == null)
        {
            return;
        }

        float normalizedSpeed = 0f;

        if (agent.speed > 0f)
        {
            normalizedSpeed = agent.velocity.magnitude / agent.speed;
        }

        animator.SetFloat(speedParameter, normalizedSpeed);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, detectionDistance);
    }
}