using System.Collections;
using UnityEngine;

public class Unit : MonoBehaviour
{
    private Animator animator;
    private AnimationSystem unitAnimationManager;
    public Bandtype bandtype;

    private IEnumerator Start()
    {
        yield return new WaitUntil(() => GameManager.Instance.IsInitialized);
        unitAnimationManager = GameManager.Instance.UnitAnimationManager;
        Initialize();
    }

    private void Initialize()
    {
        unitAnimationManager.AddUnit(this);

        animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = gameObject.AddComponent<Animator>();
        }

        unitAnimationManager.AttachAnimation(animator);
    }

    private void OnDisable()
    {
        unitAnimationManager.RemoveUnit(this);
    }

    public void SetAnimationClip(AnimationClip animationClip)
    {
        AnimatorOverrideController overrideController =
            animator.runtimeAnimatorController as AnimatorOverrideController;
        if (overrideController == null)
        {
            overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
            animator.runtimeAnimatorController = overrideController;
        }
        overrideController["Action"] = animationClip;
    }
}
