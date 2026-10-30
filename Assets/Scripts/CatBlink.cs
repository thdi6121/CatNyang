using UnityEngine;
using System.Collections;

public class RandomBlink : MonoBehaviour
{
    [SerializeField] private Animator animator;

    [SerializeField] private float minInterval = 2f;
    [SerializeField] private float maxInterval = 5f;

    private void Start()
    {
        StartCoroutine(BlinkRoutine());
    }

    IEnumerator BlinkRoutine()
    {
        while (true)
        {
            float wait = Random.Range(minInterval, maxInterval);

            yield return new WaitForSeconds(wait);

           //Debug.Log("±ôºýÀÓ" + wait);
            animator.SetTrigger("Blink");

            // 20% È®·ü·Î ÇÑ ¹ø ´õ ±ôºýÀÓ
            if (Random.value < 0.2f)
            {
                yield return new WaitForSeconds(0.15f);
                //Debug.Log("µÎ ¹ø ±ôºýÀÓ");
                animator.SetTrigger("Blink");
            }
        }
    }
}