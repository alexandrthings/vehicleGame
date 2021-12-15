#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VehicleBase.Characters.AI.TreeBuilder
{
    [CustomEditor( typeof(NodeRepresentation))]
    public class NodeRepresentationEditor : Editor
    {
        public string NodeName;

        public int index;
        public int index2;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            NodeRepresentation nr = (NodeRepresentation) target;

            nr.AINodeType = EditorGUILayout.Popup(nr.AINodeType, new string[] {"Logic", "Evaluation", "Action"});
            nr.AINodeIndex = EditorGUILayout.Popup(nr.AINodeIndex, NodeCollector.instance.NodeTypeTypes(nr.AINodeType));
        }
    }
}
#endif