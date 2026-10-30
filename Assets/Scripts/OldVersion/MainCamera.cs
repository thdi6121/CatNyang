using UnityEngine;

public class MainCamera : MonoBehaviour
{
    public Transform target;

    public float mouseSensitivity = 2f;
    public float distance = 5f;
    public float height = 2f;

    float yaw;
    float pitch = 20f;

    public float minPitch = -20f;
    public float maxPitch = 60f;

    //스크롤로 카메라 줌인, 줌아웃
    [Header("Zoom Parameters")]
    [SerializeField] private float minDistance = 1f;
    [SerializeField] private float maxDistance = 6f;

    [SerializeField] private float zoomSpeed = 5f;
   // [SerializeField] private float zoomSmoothSpeed = 10f;

    [SerializeField] private float minHeight = 0.8f;
    [SerializeField] private float maxHeight = 3.2f;

    //카메라 높이 조절
    [SerializeField] private float lookAtUpOffset = 1.2f;


    void Start()
    {
        UpdateHeightFromDistance();
    }

    void LateUpdate()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scroll) > 0.01f)
        {
            distance -= scroll * zoomSpeed;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }


        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);

        Vector3 offset = rotation * new Vector3(0, 0, -distance);

        transform.position = target.position + Vector3.up * height + offset;

        //transform.LookAt(target.position + Vector3.up * height);
        transform.LookAt(target.position + Vector3.up * (height + lookAtUpOffset));
    }

    private void UpdateHeightFromDistance()
    {
        float zoomRatio = Mathf.InverseLerp(
            minDistance,
            maxDistance,
            distance
        );

        height = Mathf.Lerp(
            minHeight,
            maxHeight,
            zoomRatio
        );
    }
}