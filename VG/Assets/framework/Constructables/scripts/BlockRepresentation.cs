using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ASTankGame.Vehicles.Blocks
{
    /// <summary>
    /// A simplified block class that serves to save memory by pointing to the block's parameters rather than copying them for every existing block.
    /// </summary>
    public class BlockRepresentation
    {
        public sbyte forward;
        public sbyte up;

        public int blockID;
        public int blockType;

        public Block block;

        public BlockRepresentation(sbyte _forward, sbyte _up, int _blockID, int _blockType, Block _block)
        {
            forward = _forward;
            up = _up;
            blockID = _blockID;
            blockType = _blockType;
            block = _block;
        }

    }
}