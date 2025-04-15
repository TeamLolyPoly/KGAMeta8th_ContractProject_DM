using Photon.Pun;
using System.Collections;
using UnityEngine;

public class Unit : MonoBehaviour
{
    protected Animator animator;
    protected AnimationSystem unitAnimationSystem;
    protected bool isAnimating = false;
    public bool isLeft;

    protected virtual IEnumerator Start()
    {
        yield return new WaitUntil(() => GameManager.Instance.UnitAnimationSystem != null);
        unitAnimationSystem = GameManager.Instance.UnitAnimationSystem;
        yield return new WaitUntil(() => unitAnimationSystem.IsInitialized == true);
        Initialize();
    }

    protected virtual void Initialize()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = gameObject.AddComponent<Animator>();
        }
        if (PhotonNetwork.InRoom)
        {
            if (GameManager.Instance.NetworkSystem.IsMasterClient())
            {
                if (isLeft)
                {
                    unitAnimationSystem.AddUnit(this);
                }
                else
                {
                    unitAnimationSystem.AddRemoteUnit(this);
                }
            }
            else
            {
                if (!isLeft)
                {
                    unitAnimationSystem.AddUnit(this);
                }
                else
                {
                    unitAnimationSystem.AddRemoteUnit(this);
                }
            }

        }
        else
        {
            unitAnimationSystem.AddUnit(this);
        }

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
        if (unitAnimationSystem != null)
        {
            unitAnimationSystem.RemoveUnit(this);
        }
    }
}
