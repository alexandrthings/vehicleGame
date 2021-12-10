using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ASTankGame.UI
{
    /// <summary>
    /// Methods to override are OnDeactivate and Deactivate
    /// </summary>
    public class Menu : MonoBehaviour
    {
        public string menuName = "Menu";
        public bool SoloMenu; // can only be opened when none are open
        public bool LockoutMenu; // does not allow others to open or operate while open
        public bool Selected; // currently active
        public bool Transient; // gets disabled when another menu opens

        public GameObject menuObject;

        /// <summary>
        /// disable menu
        /// </summary>
        public virtual void Deactivate()
        {
            UIManager.DeactivateMenu(this);
        }

        public virtual void OnDeactivate()
        {

        }
    }
}