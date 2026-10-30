using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class KeyboardCatMover : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 720f;
    [SerializeField] private float jumpForce = 5f;

    [Header("References")]
    [SerializeField] private Transform cameraTransform;

    private Rigidbody rb;

    // 실제 이동 방향
    private Vector3 moveDirection;

    // 캐릭터가 바라볼 방향
    private Vector3 lookDirection;

    private bool jumpRequested;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.constraints =
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationZ;

        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        float h = 0f;
        float v = 0f;

        if (keyboard.leftArrowKey.isPressed) h = -1;
        if (keyboard.rightArrowKey.isPressed) h = 1;

        if (keyboard.upArrowKey.isPressed) v = 1;
        if (keyboard.downArrowKey.isPressed) v = -1;

        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;

        camForward.y = 0;
        camRight.y = 0;

        camForward.Normalize();
        camRight.Normalize();

        moveDirection = (camForward * v + camRight * h).normalized;

        //--------------------------------------------------
        // 회전은 따로 결정
        //--------------------------------------------------

        if (moveDirection.sqrMagnitude > 0.001f)
        {
            // 후진 중이면 회전하지 않는다.
            if (v >= 0)
            {
                lookDirection = moveDirection;
            }
        }
        else
        {
            // 입력이 없으면 회전 중지
            lookDirection = Vector3.zero;
        }


        if (keyboard.spaceKey.wasPressedThisFrame)
            jumpRequested = true;
    }

    private void FixedUpdate()
    {
        Move();

        Rotate();

        Jump();
    }

    private void Move()
    {
        Vector3 velocity = moveDirection * moveSpeed;
        velocity.y = rb.linearVelocity.y;
        rb.linearVelocity = velocity;
    }

    private void Rotate()
    {
        if (lookDirection.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(lookDirection);

        rb.MoveRotation(
            Quaternion.RotateTowards(
                rb.rotation,
                targetRotation,
                rotationSpeed * Time.fixedDeltaTime));
    }

    private void Jump()
    {
        if (!jumpRequested)
            return;

        jumpRequested = false;

        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }
}