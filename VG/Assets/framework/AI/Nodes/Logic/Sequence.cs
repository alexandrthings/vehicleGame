using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ASTankGame.Characters.AI.Nodes
{
    public class Sequence : Node
    {
        public override NodeState Evaluate(AIBase AI)
        {
            return NodeState.Failure;
        }
    }
}