using System.Collections;
using System.Collections.Generic;
using ASTankGame.Vehicles.Blocks;
using UnityEngine;

public static class BlockRotation
{
    public static Quaternion RotToQuat(Vector2Int rotation)
    {
        return Quaternion.LookRotation(BlockTable.dirToVector[rotation.x], BlockTable.dirToVector[rotation.y]);
    }

    public static Vector2Int RotateRotation(Vector2Int _rotation, int axis, int amount)
    {
        // if rolling block
        if (BlockTable.MatchingAxis(_rotation.x, axis))
            return new Vector2Int(_rotation.x, BlockTable.FindRotationIndex(_rotation.y, amount, axis));
        // if yawing block
        if (BlockTable.MatchingAxis(_rotation.y, axis))
            return new Vector2Int(BlockTable.FindRotationIndex(_rotation.x, amount, axis), _rotation.y);
        
        
        // this should return the rotated on z axis rotation
        return new Vector2Int(1, 3);
    }

    public static Vector3 RotateBlockVertex(Vector3 vertex, Matrix4x4 rotation)
    {
        return rotation.MultiplyPoint3x4(vertex);
    }
}
