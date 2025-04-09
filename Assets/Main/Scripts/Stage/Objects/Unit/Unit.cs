using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    protected Animator animator;
    protected AnimationSystem unitAnimationSystem;
    protected bool isAnimating = false;

    protected virtual IEnumerator Start()
    {
        yield return new WaitUntil(() => GameManager.Instance.UnitAnimationSystem != null);
        unitAnimationSystem = GameManager.Instance.UnitAnimationSystem;
        Initialize();
    }

    protected virtual void Initialize()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = gameObject.AddComponent<Animator>();
        }
        unitAnimationSystem.AddUnit(this);

        unitAnimationSystem.AttachAnimation(animator);
    }

    public virtual void SetAnimationClip(AnimationClip clip, string targetClipName)
    {
        AnimatorOverrideController overrideController =
            animator.runtimeAnimatorController as AnimatorOverrideController;
        if (overrideController == null)
        {
            overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
            animator.runtimeAnimatorController = overrideController;
        }
        overrideController[targetClipName] = clip;
    }

    public void OnAnimationComplete()
    {
        isAnimating = true;
    }

    protected void OnDisable()
    {
        unitAnimationSystem.RemoveUnit(this);
    }
}
