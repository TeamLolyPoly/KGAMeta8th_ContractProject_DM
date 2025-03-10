using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectDM.UI
{
    public abstract class Panel : MonoBehaviour
    {
        private bool isOpen = false;
        public bool IsOpen => isOpen;
        public abstract PanelType PanelType { get; }

        public virtual void Open()
        {
            gameObject.SetActive(true);
            isOpen = true;
        }

        public virtual void Close()
        {
            gameObject.SetActive(false);
            isOpen = false;
        }
    }
}
