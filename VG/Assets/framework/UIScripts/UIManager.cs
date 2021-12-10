using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using ASTankGame.Characters;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.UI;

namespace ASTankGame.UI
{
    // i guess its a god object for all menus now
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private Menu[] menus;
        [SerializeField] private List<Menu> openMenus = new List<Menu>();

        private Menu activeMenu;

        public static UIManager ins; // singleton

        public void OnEnable()
        {
            ins = this;

            for (int i = 0; i < menus.Length; i++)
                menus[i].OnDeactivate();

            /*MonoBehaviour[] found = FindObjectsOfType<MonoBehaviour>();
 
            for (int i = 0; i < found.Length; i++) // yeah not very efficient
            {
                if (found.GetType() == typeof(IDisableInMenu))
                {
                    disableOnMenu.Add((IDisableInMenu)found);
                }
            }*/
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                DeactivateMenu(activeMenu);
            }
        }

        public event Action enableMenuToggles;
        public event Action disableMenuToggles;

        public void EnableMenuToggles(bool on)
        {
            if (on && enableMenuToggles != null)
            {
                enableMenuToggles();
            }
            else if (!on && disableMenuToggles != null)
            {
                disableMenuToggles();
            }
        }

        public static PlayerCharacter GetActiveCharacter()
        {
            return PlayerCharacter.pc;
        }

        public static void ToggleMenu(int index)
        {
            if (!IsMenuActive(0))
            {
                ActivateMenu(0);
            }
            else
            {
                DeactivateMenu(0);
            }
        }

        public static void ActivateMenu(string name)
        {
            for (int i = 0; i < ins.menus.Length; i++)
            {
                if (ins.menus[i].menuName.ToLower() == name.ToLower())
                {
                    ActivateMenu(i);
                }
            }
        }

        /// <summary>
        /// new vehicle = 0
        /// mirror = 1
        /// </summary>
        /// <param name="index">which menu to open</param>
        public static bool ActivateMenu(int index)
        {
            if (ins.menus.Length <= index)
                return false;

            if (ins.activeMenu != null)
            {
                if (ins.activeMenu.LockoutMenu || ins.activeMenu.SoloMenu)
                    return false;

                if (ins.menus[index].SoloMenu)
                    return false;

                if (ins.activeMenu.Transient)
                    DeactivateMenu(ins.activeMenu);

                ins.activeMenu.Selected = false;
            }

            ins.openMenus.Add(ins.menus[index]);
            ins.activeMenu = ins.menus[index];
            ins.activeMenu.Selected = true;
            ins.activeMenu.menuObject.SetActive(true);

            Cursor.lockState = CursorLockMode.Confined;

            ins.EnableMenuToggles(true);

            return true;
        }

        public static bool IsMenuActive(int index)
        {
            if (ins.menus.Length > index)
                return ins.menus[index].menuObject.activeSelf;

            else return false;
        }

        public static void DeactivateMenu(int index)
        {
            if (ins.menus.Length > index)
                DeactivateMenu(ins.menus[index]);
        }

        public static void DeactivateMenu(string name)
        {
            for (int i = 0; i < ins.menus.Length; i++)
            {
                if (ins.menus[i].menuName.ToLower() == name.ToLower())
                    DeactivateMenu(i);
            }
        }

        public static bool DeactivateMenu(Menu toDeactivate)
        {
            if (!ins.openMenus.Contains(toDeactivate))
                return false;

            toDeactivate.Selected = false;
            toDeactivate.menuObject.SetActive(false);
            toDeactivate.OnDeactivate();

            ins.openMenus.Remove(toDeactivate);

            if (ins.openMenus.Count <= 0)
                Cursor.lockState = CursorLockMode.Locked;

            if (toDeactivate == ins.activeMenu)
            {
                ins.activeMenu = null;

                if (ins.openMenus.Count > 0)
                {
                    ins.activeMenu = ins.openMenus[ins.openMenus.Count - 1];
                    ins.activeMenu.Selected = true;
                    ins.activeMenu.menuObject.SetActive(true);
                }
            }

            if (ins.openMenus.Count == 0)
                ins.EnableMenuToggles(false);

            return true;
        }
    }
}