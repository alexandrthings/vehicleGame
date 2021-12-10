using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ASTankGame.Vehicles.BlockBehaviors
{
    public interface ILinkableBlock
    {
        List<ILinkableBlock> links { set; }

        void Connect(ILinkableBlock block);

        void Disconnect(ILinkableBlock block);
    }
}