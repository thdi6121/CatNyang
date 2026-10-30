using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(CharacterController))]
public class KeyboardCatMoveCharacterController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float runSpeed = 6f;
    [SerializeField] private float rotationSpeed = 12f;

    [Header("Jump")]
    [Tooltip("점프할 때 적용되는 초기 수직 속도입니다.")]
    [SerializeField] private float jumpForce = 6f;
    [SerializeField] private float groundCheckDistance = 0.2f;

    [Header("Gravity")]
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float fallGravityMultiplier = 2.2f;
    [SerializeField] private float apexGravityMultiplier = 1.4f;
    [SerializeField] private float apexVelocityThreshold = 0.8f;
    [SerializeField] private float groundedVerticalSpeed = -2f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private float animationDampTime = 0.1f;

    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [FormerlySerializedAs("respawnPoint")]
    [SerializeField] private Transform InitialPosition;

    [Header("Teleport Effect")]
    [SerializeField] private GameObject teleportEffectPrefab;
    [SerializeField, Range(0.1f, 1f)] private float teleportEffectScale = 0.55f;

    // 텔레포트_이펙트가 씬에서 사용 중인 기준 크기입니다.
    private static readonly Vector3 TeleportEffectBaseScale =
        Vector3.one * 0.0030144001f;

    [Header("Bool Checker")]
    [SerializeField] private bool isGrounded;

    [Header("FootPrint")]
    [SerializeField] private Transform leftFoot;
    [SerializeField] private Transform rightFoot;
    [SerializeField] private GameObject footprintPrefab;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float footprintSpacing = 0.35f;

    [Header("Auto Sit")]
    [SerializeField] private float sitDelay = 10f;
    [SerializeField] private CatFishCatch catFishCatch;


    private float idleTimer;
    private bool isSitting;

    private static readonly int IsSittingHash =
        Animator.StringToHash("IsSitting");


    private CharacterController controller;
    private Vector2 moveInput;
    private Vector3 moveDirection;
    private float verticalVelocity;
    private bool jumpRequested;
    private bool isRunning;

    private Vector3 lastFootprintPosition;
    private bool hasPreviousFootprint;
    private bool nextFootIsLeft = true;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int JumpHash = Animator.StringToHash("Jump");
    private static readonly int VerticalSpeedHash = Animator.StringToHash("VerticalSpeed");

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

    }

    private void Update()
    {
        ReadInput();
        UpdateGroundedState();
        UpdateSittingState();
        HandleJump();
        ApplyGravity();
        Move();
        Rotate();
        TryLeaveFootprint();
        UpdateAnimator();

        if (Input.GetKeyDown(KeyCode.R))
        {
            TeleportToInitialPosition();
        }
    }

    private void ReadInput()
    {
        moveInput = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );

        moveInput = Vector2.ClampMagnitude(moveInput, 1f);

        if (Input.GetKeyDown(KeyCode.LeftShift) ||
            Input.GetKeyDown(KeyCode.RightShift))
        {
            isRunning = !isRunning;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpRequested = true;
        }
    }

    private void UpdateGroundedState()
    {
        // CharacterController.isGrounded는 마지막 Move 호출의 충돌 결과를 사용한다.
        // 작은 SphereCast를 함께 사용해 경사면이나 미세한 단차에서의 접지를 보완한다.
        isGrounded = controller.isGrounded || CheckGroundBelow();

        if (isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundedVerticalSpeed;
        }
    }

    private bool CheckGroundBelow()
    {
        Vector3 worldCenter = transform.TransformPoint(controller.center);
        float radius = Mathf.Max(0.01f, controller.radius * 0.9f);
        float bottomOffset = Mathf.Max(0f, controller.height * 0.5f - controller.radius);
        Vector3 sphereOrigin = worldCenter + Vector3.down * bottomOffset;

        return Physics.SphereCast(
            sphereOrigin,
            radius,
            Vector3.down,
            out _,
            groundCheckDistance,
            groundMask,
            QueryTriggerInteraction.Ignore
        );
    }

    private void HandleJump()
    {
        if (!jumpRequested)
        {
            return;
        }

        jumpRequested = false;

        if (!isGrounded)
        {
            return;
        }

        verticalVelocity = jumpForce;
        isGrounded = false;

        if (animator != null)
        {
            animator.ResetTrigger(JumpHash);
            animator.SetTrigger(JumpHash);
        }
    }

    private void ApplyGravity()
    {
        if (isGrounded && verticalVelocity <= 0f)
        {
            return;
        }

        float gravityMultiplier = 1f;

        if (verticalVelocity < 0f)
        {
            gravityMultiplier = fallGravityMultiplier;
        }
        else if (verticalVelocity < apexVelocityThreshold)
        {
            gravityMultiplier = apexGravityMultiplier;
        }

        verticalVelocity += gravity * gravityMultiplier * Time.deltaTime;
    }

    private void Move()
    {
        moveDirection = GetCameraRelativeMoveDirection();

        float currentSpeed = isRunning ? runSpeed : walkSpeed;
        Vector3 velocity = moveDirection * currentSpeed;
        velocity.y = verticalVelocity;

        CollisionFlags collisionFlags = controller.Move(velocity * Time.deltaTime);

        if ((collisionFlags & CollisionFlags.Below) != 0 && verticalVelocity < 0f)
        {
            isGrounded = true;
            verticalVelocity = groundedVerticalSpeed;
        }

        if ((collisionFlags & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
        {
            verticalVelocity = 0f;
        }
    }

    private Vector3 GetCameraRelativeMoveDirection()
    {
        if (cameraTransform == null)
        {
            return Vector3.zero;
        }

        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 direction =
            cameraForward * moveInput.y +
            cameraRight * moveInput.x;

        return Vector3.ClampMagnitude(direction, 1f);
    }

    private void Rotate()
    {
        Vector3 horizontalDirection = new Vector3(
            moveDirection.x,
            0f,
            moveDirection.z
        );

        if (horizontalDirection.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(horizontalDirection);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    public void IncreaseJumpForce(float amount)
    {
        jumpForce = Mathf.Max(0f, jumpForce + amount);
    }

    private void UpdateAnimator()
    {
        if (animator == null)
        {
            return;
        }

        float animationSpeed = 0f;

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

        animator.SetFloat(VerticalSpeedHash, verticalVelocity);
        animator.SetBool(IsGroundedHash, isGrounded);

        if (idleTimer >= sitDelay)
        {
            isSitting = true;
        }

        animator.SetBool(IsSittingHash, isSitting);
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

    public void LeaveLeftFootprint()
    {
        LeaveFootprint(leftFoot);
    }

    public void LeaveRightFootprint()
    {
        LeaveFootprint(rightFoot);
    }

    private void LeaveFootprint(Transform foot)
    {
        if (footprintPrefab == null || foot == null)
        {
            return;
        }

        Vector3 origin = foot.position + Vector3.up * 0.15f;

        if (!Physics.Raycast(
                origin,
                Vector3.down,
                out RaycastHit hit,
                0.5f,
                groundMask,
                QueryTriggerInteraction.Ignore))
        {
            return;
        }

        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, hit.normal);

        if (forward.sqrMagnitude <= 0.001f)
        {
            forward = Vector3.ProjectOnPlane(transform.up, hit.normal);
        }

        Quaternion rotation = Quaternion.LookRotation(forward.normalized, hit.normal);
        rotation *= Quaternion.Euler(0f, 180f, 0f);

        Instantiate(
            footprintPrefab,
            hit.point + hit.normal * 0.005f,
            rotation
        );
    }

    private void TeleportToInitialPosition()
    {
        if (InitialPosition == null)
        {
            Debug.LogWarning("InitialPosition이 연결되지 않았습니다.", this);
            return;
        }

        // CharacterController가 활성화된 상태에서 transform을 직접 옮기면
        // 내부 충돌 상태가 남을 수 있으므로 잠시 비활성화한다.
        controller.enabled = false;
        transform.SetPositionAndRotation(
            InitialPosition.position,
            InitialPosition.rotation
        );
        controller.enabled = true;

        PlayTeleportEffect();

        verticalVelocity = 0f;
        jumpRequested = false;
        hasPreviousFootprint = false;
    }

private void PlayTeleportEffect()
    {
        if (teleportEffectPrefab == null)
            return;

        GameObject effect = Instantiate(
            teleportEffectPrefab,
            transform.position,
            transform.rotation
        );

        effect.transform.localScale =
            TeleportEffectBaseScale * teleportEffectScale;

        Destroy(effect, 6f);
    }

    private void OnDrawGizmosSelected()
    {
        CharacterController currentController = GetComponent<CharacterController>();

        if (currentController == null)
        {
            return;
        }

        Vector3 worldCenter = transform.TransformPoint(currentController.center);
        float radius = Mathf.Max(0.01f, currentController.radius * 0.9f);
        float bottomOffset = Mathf.Max(
            0f,
            currentController.height * 0.5f - currentController.radius
        );
        Vector3 sphereOrigin = worldCenter + Vector3.down * bottomOffset;

        Gizmos.DrawWireSphere(sphereOrigin, radius);
        Gizmos.DrawLine(
            sphereOrigin,
            sphereOrigin + Vector3.down * groundCheckDistance
        );
    }
    private void UpdateSittingState()
    {

        if (catFishCatch != null && catFishCatch.IsBusyWithFish)
        {
            idleTimer = 0f;
            isSitting = false;

            animator.SetBool(IsSittingHash, false);

            return;
        }


        bool hasMoveInput =
            moveInput.sqrMagnitude > 0.001f;

        bool hasActionInput =
            hasMoveInput ||
            Input.GetKeyDown(KeyCode.Space);

        if (hasActionInput)
        {
            idleTimer = 0f;
            isSitting = false;
            return;
        }

        if (!isGrounded)
        {
            idleTimer = 0f;
            isSitting = false;
            return;
        }

        idleTimer += Time.deltaTime;

        if (idleTimer >= sitDelay)
        {
            isSitting = true;
        }
    }


}
