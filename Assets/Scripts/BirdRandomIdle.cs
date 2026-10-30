using UnityEngine;

public class BirdIdleController : MonoBehaviour
{
    [SerializeField] private Animator animator;

    [Header("Blink")]
    [SerializeField] private float minBlinkInterval = 2f;
    [SerializeField] private float maxBlinkInterval = 6f;


    [Header("Beak")]
    [SerializeField] private float minBeakInterval = 2f;
    [SerializeField] private float maxBeakInterval = 6f;

    [Header("Look")]
    [SerializeField] private float minLookInterval = 3f;
    [SerializeField] private float maxLookInterval = 7f;

    private static readonly int BlinkHash =
        Animator.StringToHash("Blink");

    private static readonly int LookLeftHash =
        Animator.StringToHash("LookLeft");

    private static readonly int LookRightHash =
        Animator.StringToHash("LookRight");

    private static readonly int LookUpHash =
        Animator.StringToHash("LookUp");

    private static readonly int LookDownHash =
        Animator.StringToHash("LookDown");
    private static readonly int BeakHash =
        Animator.StringToHash("Beak");

    private float nextBlinkTime;
    private float nextLookTime;
    private float nextBeakTime;


    private void Start()
    {
        ScheduleNextBlink();
        ScheduleNextLook();
        ScheduleNextBeak();

    }

    private void Update()
    {
        //if (!canIdleAction)
           // return;

        if (Time.time >= nextBlinkTime)
        {
            animator.SetTrigger(BlinkHash);
            ScheduleNextBlink();
        }

        if (Time.time >= nextLookTime)
        {
            PlayRandomLook();
            ScheduleNextLook();
        }

        if (Time.time >= nextBeakTime)
        {
            animator.SetTrigger(BeakHash);
            ScheduleNextBeak();
        }
    }

    private void ScheduleNextBlink()
    {
        nextBlinkTime =
            Time.time + Random.Range(minBlinkInterval, maxBlinkInterval);
    }

    private void ScheduleNextLook()
    {
        nextLookTime =
            Time.time + Random.Range(minLookInterval, maxLookInterval);
    }

    private void ScheduleNextBeak()
    {
        nextBeakTime =
            Time.time + Random.Range(minBeakInterval, maxBeakInterval);
    }

    private void PlayRandomLook()
    {
        switch (Random.Range(0, 4))
        {
            case 0:
                animator.SetTrigger(LookLeftHash);
                break;

            case 1:
                animator.SetTrigger(LookRightHash);
                break;

            case 2:
                animator.SetTrigger(LookUpHash);
                break;

            case 3:
                animator.SetTrigger(LookDownHash);
                break;
        }
    }

}