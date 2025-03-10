using UnityEngine;

public class Unit : MonoBehaviour
{
    private Animator animator;

    public Bandtype bandtype;

    private void Start()
    {
        UnitAnimationManager.Instance.AddUnit(this);
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = gameObject.AddComponent<Animator>();
        }
        UnitAnimationManager.Instance.AttachAnimation(animator);
    }

    private void OnDisable()
    {
        UnitAnimationManager.Instance.RemoveUnit(this);
    }

    public AnimatorOverrideController GetAnimator()
    {
        AnimatorOverrideController overrideController =
            animator.runtimeAnimatorController as AnimatorOverrideController;
        if (overrideController == null)
        {
            overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
            animator.runtimeAnimatorController = overrideController;
        }
        return overrideController;
    }
}
