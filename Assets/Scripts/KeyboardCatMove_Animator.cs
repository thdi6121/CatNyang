using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class KeyboardCatMove : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float runSpeed = 6f;
    [SerializeField] private float rotationSpeed = 12f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 6f;
    [SerializeField] private float groundCheckDistance = 0.2f;
    //[SerializeField] private LayerMask groundMask = ~0;


    [Header("Gravity")]
    [SerializeField] private float fallGravityMultiplier = 2.2f;
    [SerializeField] private float apexGravityMultiplier = 1.4f;
    [SerializeField] private float apexVelocityThreshold = 0.8f;


    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private float animationDampTime = 0.1f;

    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [FormerlySerializedAs("respawnPoint")]
    [SerializeField] private Transform InitialPosition;

    private Rigidbody rb;
    private Collider bodyCollider;

    private Vector2 moveInput;
    private bool jumpRequested;
    private bool isRunning = false;
    
    [Header("Bool Checker")]
    [SerializeField] private bool isGrounded;

    private float previousY;
    private float verticalMovement;
    private bool isFalling;

    private static readonly int SpeedHash =
        Animator.StringToHash("Speed");

    private static readonly int IsGroundedHash =
        Animator.StringToHash("IsGrounded");

    private static readonly int JumpHash =
        Animator.StringToHash("Jump");
    private static readonly int VerticalSpeedHash =
        Animator.StringToHash("VerticalSpeed");

    //고양이 발바닥 자국 출력용
    [Header("FootPrint")]
    [SerializeField] private Transform leftFoot;
    [SerializeField] private Transform rightFoot;
    [SerializeField] private GameObject footprintPrefab;
    [SerializeField] private LayerMask groundMask;    [SerializeField] private float footprintSpacing = 0.35f;

    private Vector3 lastFootprintPosition;
    private bool hasPreviousFootprint;
    private bool nextFootIsLeft = true;
    public void LeaveLeftFootprint() => LeaveFootprint(leftFoot);
    public void LeaveRightFootprint() => LeaveFootprint(rightFoot);

private void Awake()
{
    rb = GetComponent<Rigidbody>();
    bodyCollider = GetComponent<Collider>();

    // 이동 방향 전환은 Y축만 사용하므로, 충돌로 인한 굴림만 막는다.
    rb.constraints |= RigidbodyConstraints.FreezeRotationX |
                      RigidbodyConstraints.FreezeRotationZ;
}

public void IncreaseJumpForce(float amount)
{
    jumpForce = Mathf.Max(0f, jumpForce + amount);
}


    private void Update()
    {

        isGrounded = IsGrounded();

        moveInput = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );

        moveInput = Vector2.ClampMagnitude(moveInput, 1f);

        // 왼쪽 또는 오른쪽 Shift를 누르면 달리기모드로 전환
        if (Input.GetKeyDown(KeyCode.LeftShift) ||
            Input.GetKeyDown(KeyCode.RightShift))
        {
            isRunning = !isRunning;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpRequested = true;

            animator.ResetTrigger(JumpHash);
            animator.SetTrigger(JumpHash);

            if (animator != null)
            {
                animator.SetTrigger(JumpHash);
            }
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            TeleportToInitialPosition();
        }


        UpdateAnimator();
    }

private void FixedUpdate()
{
    // 충돌로 전달된 회전 속도가 다음 프레임까지 남지 않게 한다.
    rb.angularVelocity = Vector3.zero;

    ApplyBetterGravity();
    Move();
    TryLeaveFootprint();
    Jump();
}

private void TryLeaveFootprint()
{
    bool isMoving = moveInput.sqrMagnitude > 0.01f;

    if (!isGrounded || !isMoving)
    {
        hasPreviousFootprint = false;
        return;
    }

    if (hasPreviousFootprint &&
        Vector3.Distance(transform.position, lastFootprintPosition) < footprintSpacing)
    {
        return;
    }

    LeaveFootprint(nextFootIsLeft ? leftFoot : rightFoot);

    nextFootIsLeft = !nextFootIsLeft;
    lastFootprintPosition = transform.position;
    hasPreviousFootprint = true;
}


    private void ApplyBetterGravity()
    {
        float verticalSpeed = rb.linearVelocity.y;

        // 떨어지는 중
        if (verticalSpeed < 0f)
        {
            rb.AddForce(
                Physics.gravity * (fallGravityMultiplier - 1f),
                ForceMode.Acceleration
            );
        }
        // 최고점 근처
        else if (verticalSpeed < apexVelocityThreshold)
        {
            rb.AddForce(
                Physics.gravity * (apexGravityMultiplier - 1f),
                ForceMode.Acceleration
            );
        }
    }

private void Move()
{
    if (cameraTransform == null)
    {
        return;
    }

    Vector3 cameraForward = cameraTransform.forward;
    Vector3 cameraRight = cameraTransform.right;

    cameraForward.y = 0f;
    cameraRight.y = 0f;

    cameraForward.Normalize();
    cameraRight.Normalize();

    Vector3 moveDirection =
        cameraForward * moveInput.y +
        cameraRight * moveInput.x;

    moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);

    RaycastHit groundHit;
    bool hasGroundHit = TryGetGroundHit(out groundHit);

    if (hasGroundHit && moveDirection.sqrMagnitude > 0.001f)
    {
        Vector3 slopeDirection = Vector3.ProjectOnPlane(
            moveDirection,
            groundHit.normal
        );

        if (slopeDirection.sqrMagnitude > 0.001f)
        {
            moveDirection = slopeDirection.normalized;
        }
    }

    float currentMoveSpeed = isRunning ? runSpeed : walkSpeed;
    Vector3 targetVelocity = moveDirection * currentMoveSpeed;
    Vector3 velocity = rb.linearVelocity;

    velocity.x = targetVelocity.x;
    velocity.z = targetVelocity.z;

    // 오르막에서는 투영된 상승 속도를 적용하고,
    // 내리막에서는 기존 중력 속도를 유지한다.
    if (hasGroundHit && targetVelocity.y > velocity.y)
    {
        velocity.y = targetVelocity.y;
    }

    rb.linearVelocity = velocity;

    Vector3 horizontalDirection = new Vector3(
        moveDirection.x,
        0f,
        moveDirection.z
    );

    if (horizontalDirection.sqrMagnitude > 0.001f)
    {
        Quaternion targetRotation =
            Quaternion.LookRotation(horizontalDirection);

        Quaternion nextRotation = Quaternion.Slerp(
            rb.rotation,
            targetRotation,
            rotationSpeed * Time.fixedDeltaTime
        );

        rb.MoveRotation(nextRotation);
    }
}

    private void Jump()
    {
        if (!jumpRequested)
        {
            return;
        }

        jumpRequested = false;

        if (!IsGrounded())
        {
            return;
        }

        Vector3 velocity = rb.linearVelocity;
        velocity.y = 0f;
        rb.linearVelocity = velocity;

        rb.AddForce(
            Vector3.up * jumpForce,
            ForceMode.Impulse
        );

    }

    private void UpdateAnimator()
    {
        if (animator == null)
        {
            return;
        }

        float animationSpeed = 0f;
        float verticalSpeed = rb.linearVelocity.y;

        animator.SetFloat("VerticalSpeed", verticalSpeed);

        if (moveInput.sqrMagnitude > 0.01f)
        {
            animationSpeed = isRunning ? 1f : 0.5f;
        }

        animator.SetFloat(
            SpeedHash,
            animationSpeed,
            animationDampTime,
            Time.deltaTime
        );

        animator.SetFloat(
    VerticalSpeedHash,
    rb.linearVelocity.y
);

        animator.SetBool(
            IsGroundedHash,
            isGrounded
        );
    }

private bool IsGrounded()
{
    RaycastHit groundHit;
    return TryGetGroundHit(out groundHit);
}

private bool TryGetGroundHit(out RaycastHit groundHit)
{
    groundHit = default;

    if (bodyCollider == null)
    {
        return false;
    }

    Bounds bounds = bodyCollider.bounds;
    Vector3 origin = bounds.center;
    float castDistance =
        bounds.extents.y + groundCheckDistance;

    RaycastHit[] hits = Physics.RaycastAll(
        origin,
        Vector3.down,
        castDistance,
        groundMask,
        QueryTriggerInteraction.Ignore
    );

    float closestDistance = float.MaxValue;
    bool foundGround = false;

    foreach (RaycastHit hit in hits)
    {
        if (hit.collider == bodyCollider ||
            hit.collider.transform.IsChildOf(transform) ||
            hit.distance >= closestDistance)
        {
            continue;
        }

        closestDistance = hit.distance;
        groundHit = hit;
        foundGround = true;
    }

    return foundGround;
}


    private void OnDrawGizmosSelected()
    {
        Collider currentCollider = GetComponent<Collider>();

        if (currentCollider == null)
        {
            return;
        }

        Bounds bounds = currentCollider.bounds;

        Gizmos.DrawLine(
            bounds.center,
            bounds.center +
            Vector3.down *
            (bounds.extents.y + groundCheckDistance)
        );
    }

private void TeleportToInitialPosition()
{
    if (InitialPosition == null)
    {
        Debug.LogWarning(
            "InitialPosition이 연결되지 않았습니다.",
            this
        );

        return;
    }

    rb.linearVelocity = Vector3.zero;
    rb.angularVelocity = Vector3.zero;

    rb.position = InitialPosition.position;
    rb.rotation = InitialPosition.rotation;
}

private void LeaveFootprint(Transform foot)
{
    if (footprintPrefab == null || foot == null)
        return;

    Vector3 origin = foot.position + Vector3.up * 0.15f;

    if (!Physics.Raycast(
            origin,
            Vector3.down,
            out RaycastHit hit,
            0.5f,
            groundMask,
            QueryTriggerInteraction.Ignore))
        return;

    Vector3 forward = Vector3.ProjectOnPlane(transform.forward, hit.normal);
    Quaternion rotation = Quaternion.LookRotation(forward, hit.normal);
        rotation *= Quaternion.Euler(0f, 180f, 0f);

        GameObject footprint = Instantiate(
        footprintPrefab,
        hit.point + hit.normal * 0.005f,
        rotation
    );}
}