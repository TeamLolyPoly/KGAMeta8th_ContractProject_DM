using UnityEngine;

namespace ProjectDM.UI
{
    public abstract class Panel : MonoBehaviour
    {
        private bool isOpen = false;
        public bool IsOpen => isOpen;
        public abstract PanelType PanelType { get; }
        public Animator animator;

        public virtual void Open()
        {
            gameObject.SetActive(true);
            isOpen = true;
            if (animator != null)
            {
                animator.SetBool("isOpen", isOpen);
            }
        }

        public virtual void Close(bool objActive = true)
        {
            isOpen = false;
            if (!objActive)
            {
                gameObject.SetActive(false);
            }
            if (animator != null)
            {
                animator.SetBool("isOpen", isOpen);
            }
        }
    }
}
