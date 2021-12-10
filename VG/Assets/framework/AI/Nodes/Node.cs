using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace ASTankGame.Characters.AI.Nodes
{
    [System.Serializable]
    public abstract class Node
    {
        protected NodeState _nodeState;

        public NodeState nodeState
        {
            get { return _nodeState; }
        }

        public abstract NodeState Evaluate(AIBase AI);
        public int index;

        public virtual void BuildNodes(List<Node> newNodes)
        {
            ClearNodes();
        }

        public virtual void ClearNodes()
        {
        }
    }

    public enum NodeState
    {
        Running,
        Success,
        Failure
    }
}