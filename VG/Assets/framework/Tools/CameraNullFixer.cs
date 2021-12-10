using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace DuctTape
{
    [ExecuteAlways]
    public class CameraNullFixer : MonoBehaviour
    {
        void Update()
        {
            if (!EditorApplication.isPlaying)
                CinemachineBrain.SoloCamera = null;
        }
    }
}
#endif