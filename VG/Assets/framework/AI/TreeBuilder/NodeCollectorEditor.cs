#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using ASTankGame.Characters.AI.TreeBuilder;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NodeCollector))]
public class NodeCollectorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Find Nodes"))
        {
            NodeCollector nc = (NodeCollector) target;

            nc.CollectTypeNames();
        }
    }
}
#endif