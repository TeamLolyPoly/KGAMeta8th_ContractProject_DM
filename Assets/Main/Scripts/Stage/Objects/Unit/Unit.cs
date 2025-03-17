using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    protected Animator animator;
    protected AnimationSystem unitAnimationManager;

    protected virtual IEnumerator Start()
    {
        yield return new WaitUntil(() => GameManager.Instance.IsInitialized);
        unitAnimationManager = GameManager.Instance.UnitAnimationManager;
        Initialize();
    }
    protected virtual void Initialize()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = gameObject.AddComponent<Animator>();
        }
        unitAnimationManager.AddUnit(this);

        unitAnimationManager.AttachAnimation(animator);
    }

    public virtual void SetAnimationClip(AnimationClip animationClip)
    {
        AnimatorOverrideController overrideController =
            animator.runtimeAnimatorController as AnimatorOverrideController;
        if (overrideController == null)
        {
            overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
            animator.runtimeAnimatorController = overrideController;
        }
        overrideController["Usual"] = animationClip;
    }

    protected void OnDisable()
    {
        unitAnimationManager.RemoveUnit(this);
    }

}
