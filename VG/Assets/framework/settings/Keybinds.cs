using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ASTankGame.Settings
{
    public class Keybinds : MonoBehaviour
    {
        // build mode keybinds
        public static KeyCode BuildMode;
        public static KeyCode Menu;
        public static KeyCode BlockMenu;
        public static KeyCode HoverVehicle;
        public static KeyCode NewVehicle;
        public static KeyCode MirrorMenu;
        public static KeyCode ConfigMenu;

        void Start()
        {
            BuildMode = KeyCode.B;
            Menu = KeyCode.Escape;
            BlockMenu = KeyCode.E;
            HoverVehicle = KeyCode.V;
            NewVehicle = KeyCode.N;
            MirrorMenu = KeyCode.M;
            ConfigMenu = KeyCode.Q;
        }

    }
}