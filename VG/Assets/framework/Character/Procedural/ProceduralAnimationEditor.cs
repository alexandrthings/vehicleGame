#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using VehicleBase.Characters.Animation;
using VehicleBase.Characters.Animation.Generation;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ProceduralAnimationBuilder))]
public class ProceduralAnimationEditor : Editor
{
    public AvatarMask ToInterpolate;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        ProceduralAnimationBuilder anim = (ProceduralAnimationBuilder) target;


        if (GUILayout.Button("Prepare to Interpolate"))
        {
            anim.PrepareToInterpolate();
        }
    }
}
#endif