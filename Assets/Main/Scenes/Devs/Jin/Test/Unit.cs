using System.Collections;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;

public class Unit : MonoBehaviour
{
    private Animator animator;

    private void Start()
    {
        UnitManager.Instance.AddUnit(this);
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = gameObject.AddComponent<Animator>();
        }
        UnitManager.Instance.AttachAnimation(animator, this);
    }

    private void OnDisable()
    {
        UnitManager.Instance.RemoveUnit(this);
    }

    public AnimatorOverrideController GetAnimator()
    {
        return animator.runtimeAnimatorController as AnimatorOverrideController;
    }
}
