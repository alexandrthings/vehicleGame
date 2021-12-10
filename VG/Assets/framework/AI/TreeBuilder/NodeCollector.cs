using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using ASTankGame.Characters.AI.Nodes;
using UnityEngine;

namespace ASTankGame.Characters.AI.TreeBuilder
{
    [ExecuteAlways]
    public class NodeCollector : MonoBehaviour
    {
        public static NodeCollector instance;

        public string[] LogicTypes;
        public string[] EvaluateTypes;
        public string[] ActionTypes;

        public void Awake()
        {
            instance = this;
        }

        public void Update()
        {
            if (instance == null)
                instance = this;
        }

        public List<Type> FindNodeTypes()
        {
            List<Type> returnList = new List<Type>();
            Type[] foundTypes = Assembly.GetAssembly(typeof(Node)).GetTypes();

            for (int i = 0; i < foundTypes.Length; i++)
            {
                if (foundTypes[i].IsSubclassOf(typeof(Node)))
                    returnList.Add(foundTypes[i]);
            }

            foreach (Type type in returnList)
            {
                Debug.Log(type.Name);
            }

            return returnList;
        }

        public void CollectTypeNames()
        {
            List<string> returnLogicList = new List<string>();
            Type[] foundTypes = Assembly.GetAssembly(typeof(Node)).GetTypes();

            for (int i = 0; i < foundTypes.Length; i++)
            {
                if (foundTypes[i].IsSubclassOf(typeof(LogicNode)))
                    returnLogicList.Add(foundTypes[i].Name);
            }

            LogicTypes = returnLogicList.ToArray();
        }

        public string[] NodeTypeTypes(int _type)
        {
            switch (_type)
            {
                case 0:
                    return LogicTypes;
                case 1:
                    return EvaluateTypes;
                default:
                    return ActionTypes;
            }
        }
    }
}