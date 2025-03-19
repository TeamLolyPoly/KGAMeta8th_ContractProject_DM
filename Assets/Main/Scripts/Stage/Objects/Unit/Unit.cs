using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    protected Animator animator;
    protected AnimationSystem unitAnimationSystem;
    protected Coroutine animChangeCoroutine;
    protected bool isAnimating = false;

    protected Queue<AnimationClip> animationQueue = new Queue<AnimationClip>();

    protected virtual IEnumerator Start()
    {
        yield return new WaitUntil(() => GameManager.Instance.IsInitialized);
        unitAnimationSystem = GameManager.Instance.UnitAnimationManager;
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
        // animationQueue.Enqueue(clip);

        // if (animChangeCoroutine != null)
        // {
        //     StopCoroutine(animChangeCoroutine);
        //     animChangeCoroutine = null;
        // }
        // animChangeCoroutine = StartCoroutine(AnimChangeRoutine(overrideController));
    }

    private IEnumerator AnimChangeRoutine(AnimatorOverrideController overrideController)
    {
        while (animationQueue.Count > 0)
        {
            print("애니메이션 클립체인지 기다리는중");
            yield return new WaitUntil(() => isAnimating == false);
            overrideController["Usual"] = animationQueue.Dequeue();
            animator.Play("Usual");
            print("애니메이션 클립체인지 성공");
        }
        animChangeCoroutine = null;
    }
    public void OnAnimation()
    {
        isAnimating = !isAnimating;
        print($"isAnimating: {isAnimating}");
    }

    protected void OnDisable()
    {
        unitAnimationSystem.RemoveUnit(this);
    }

}
