using UnityEngine;

public class CatSoundController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AudioSource audioSource;

    [Header("Sitting Sounds")]
    [SerializeField] private AudioClip purrClip;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            Debug.LogError(
                "CatSoundController: AudioSource가 없습니다.",
                gameObject
            );
        }
    }

    public void StartPurr()
    {
        if (audioSource == null || purrClip == null)
        {
            return;
        }

        // 이미 같은 소리가 재생 중이면 다시 시작하지 않음
        if (audioSource.isPlaying &&
            audioSource.clip == purrClip)
        {
            return;
        }

        audioSource.Stop();
        audioSource.clip = purrClip;
        audioSource.loop = true;
        audioSource.Play();
    }

    public void StopPurr()
    {
        if (audioSource == null)
        {
            return;
        }

        audioSource.Stop();
        audioSource.loop = false;
        audioSource.clip = null;
    }
}