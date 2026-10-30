using UnityEngine;

public class SitIdleSoundBehaviour : StateMachineBehaviour
{
    public override void OnStateEnter(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex)
    {
        CatSoundController soundController =
            animator.GetComponent<CatSoundController>();

        if (soundController != null)
        {
            soundController.StartPurr();
        }
    }

    public override void OnStateExit(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex)
    {
        CatSoundController soundController =
            animator.GetComponent<CatSoundController>();

        if (soundController != null)
        {
            soundController.StopPurr();
        }
    }
}