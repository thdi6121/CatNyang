using UnityEngine;

public class MainCameraEdited : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private float lookAtUpOffset = 1.2f;

    [Header("Rotation")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float minPitch = -20f;
    [SerializeField] private float maxPitch = 60f;

    [Header("Zoom")]
    [SerializeField] private float distance = 5f;
    [SerializeField] private float minDistance = 1f;
    [SerializeField] private float maxDistance = 6f;
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float zoomSmoothSpeed = 10f;



    [Header("Smooth Settings")]
    public float positionSmoothSpeed = 10f; // 위치 이동 부드러움 정도
    public float rotationSmoothSpeed = 10f; // 회전 부드러움 정도


    private float yaw;
    private float pitch = 20f;

    // 실제 카메라 거리를 부드럽게 변경하기 위한 값
    private float currentDistance;

    private void Start()
    {
        currentDistance = distance;

        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        HandleRotation();
        HandleZoom();
        UpdateCameraPosition();
    }

    private void HandleRotation()
    {
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scroll) > 0.01f)
        {
            distance -= scroll * zoomSpeed;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }

        currentDistance = Mathf.Lerp(
            currentDistance,
            distance,
            1f - Mathf.Exp(-zoomSmoothSpeed * Time.deltaTime)
        );
    }

    /*private void UpdateCameraPosition()
    {
        // 카메라가 항상 바라볼 기준점
        Vector3 pivotPosition =
            target.position + Vector3.up * lookAtUpOffset;

        // 마우스로 결정한 고정된 카메라 각도
        Quaternion cameraRotation =
            Quaternion.Euler(pitch, yaw, 0f);

        // 같은 각도를 유지하면서 앞뒤로만 이동
        Vector3 cameraOffset =
            cameraRotation * Vector3.back * currentDistance;

        transform.position = pivotPosition + cameraOffset;
       
        transform.rotation = cameraRotation;
    }
}*/

    private void UpdateCameraPosition()
    {
        if (target == null) return;

        // 1. 목표 위치 계산
        Vector3 pivotPosition = target.position + Vector3.up * lookAtUpOffset;
        Quaternion targetRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 cameraOffset = targetRotation * Vector3.back * currentDistance;
        Vector3 targetPosition = pivotPosition + cameraOffset;

        // 2. 부드럽게 위치 이동 (Lerp)
        transform.position = Vector3.Lerp(transform.position, targetPosition, positionSmoothSpeed * Time.deltaTime);

        // 3. 부드럽게 회전 (Slerp)
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothSpeed * Time.deltaTime);
    }
}