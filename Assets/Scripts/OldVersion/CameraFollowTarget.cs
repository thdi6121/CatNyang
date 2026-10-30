using UnityEngine;

public class CameraFollowTarget : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.5f, -3f);
    [SerializeField] private float followSmoothTime = 0.15f;
    [SerializeField] private float lookHeight = 1f;

    private Vector3 currentVelocity;

    private void Start()
    {
        if (target == null)
        {
            GameObject capsule = GameObject.Find("Capsule");
            if (capsule != null)
            {
                target = capsule.transform;
            }
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        //Vector3 desiredPosition = target.position + offset;
        Vector3 desiredPosition = target.TransformPoint(offset); //카메라가 캐릭터 뒤면만 보게 함
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref currentVelocity,
            followSmoothTime);

        Vector3 lookTarget = target.position + Vector3.up * lookHeight;
        transform.LookAt(lookTarget);
    }
}
