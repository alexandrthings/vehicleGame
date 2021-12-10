using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ASTankGame.Vehicles.Blocks.Planes
{
    public class FaceTable
    {
        #region triangulation
        public static readonly Vector3[] cubeVerts = new Vector3[8]
        {
            new Vector3(0, 0, 0), // 0
            new Vector3(1, 0, 0), // 1
            new Vector3(1, 1, 0), // 2
            new Vector3(0, 1, 0), // 3
            new Vector3(0, 0, 1), // 4
            new Vector3(1, 0, 1), // 5
            new Vector3(1, 1, 1), // 6
            new Vector3(0, 1, 1) // 7
        };

        // vertices following propagation order of (+x, -y) (-y, -x) (-x, +y) (+x, +y)
        public static readonly int[,] cubeFaces = new int[6, 4]
        {
            // z faces
            {1, 0, 3, 2},
            {5, 4, 7, 6},
            // y faces
            {1, 0, 4, 5},
            {2, 3, 7, 6},
            // x faces
            {4, 0, 3, 7},
            {5, 1, 2, 6}
        };

        public static readonly int[,] cubeTriangulation = new int[6, 6]
        {
            {2, 3, 0, 1, 2, 0},
            {0, 2, 1, 2, 0, 3},
            {0, 3, 2, 0, 2, 1},
            {1, 2, 0, 3, 0, 2},
            {0, 3, 1, 3, 2, 1},
            {1, 2, 0, 3, 0, 2}
        };
        #endregion

        public static readonly Vector3[] dirToVector3 = new Vector3[6]
        {
            // flat planes
            new Vector3(0, 0, 1),
            new Vector3(0, 0, 1),
            new Vector3(0, 1, 0),
            new Vector3(0, 1, 0),
            new Vector3(1, 0, 0),
            new Vector3(1, 0, 0)

            // sloped planes

        };

        public static readonly int[,] slopeDirToIndex = new int[6,6]
        {
            {-1, -1, 5, 6, 7, 8},
            {-1, -1, 9, 10, 11, 12},
            {5, 9, -1, -1, 13, 14},
            {6, 10, -1, -1, 15, 16},
            {7, 11, 14, 15, -1, -1},
            {8, 12, 13, 16, -1, -1}
        };

        #region plane coord operations
        public static Vector2Int GetFlatFacePlane(Vector3Int position, int dir)
        {
            switch (dir)
            {
                case 0:
                case 1:
                    return new Vector2Int(position.z, dir);
                case 2:
                case 3:
                    return new Vector2Int(position.y, dir);
                default:
                    return new Vector2Int(position.x, dir);
            }
        }

        public static Vector2Int GetFlatPlanePos(Vector3Int position, int dir)
        {
            switch (dir)
            {
                case 0:
                case 1:
                    return new Vector2Int(position.x, position.y);
                case 2:
                case 3:
                    return new Vector2Int(position.x, position.z);
                default:
                    return new Vector2Int(position.z, position.y);
            }
        }

        public static Vector2Int GetSlopePlane(Vector3Int position, int fwd, int up)
        {
            Vector2Int coords = new Vector2Int();

            coords.y = slopeDirToIndex[fwd, up];

            // maybe i can re-index these for easier access
            switch (coords.y)
            {
                // ZY axis slope
                case 5: case 6: case 9: case 10:
                    coords.x = position.z + position.y;
                    break;
                // YX axis slope
                case 13: case 14: case 15: case 16:
                    coords.x = position.x + position.y;
                    break;
                // XZ axis slope
                default:
                    coords.x = position.z + position.x;
                    break;
            }

            return coords;
        }

        public static Vector2Int GetSlopePlanePos(Vector3Int position, int slopeIndex)
        {
            // maybe i can re-index these for easier access
            switch (slopeIndex)
            {
                // ZY axis slope
                case 5: case 6: case 9: case 10:
                    return new Vector2Int(position.x, position.z);
                // YX axis slope
                case 13: case 14: case 15: case 16:
                    return new Vector2Int(position.x, position.z);
                // XZ axis slope
                default:
                    return new Vector2Int(position.y, position.z);
            }
        }

        public static Vector3Int PlaneToPhysPos(Vector2Int slopePos, Vector2Int position)
        {
            // maybe i can re-index these for easier access
            switch (slopePos.y)
            {
                //       ZY axis slope          |          and YX axis slope
                case 6: case 10: case 5: case 9: case 13: case 14: case 15: case 16:
                    return new Vector3Int(position.x, Mathf.Abs(slopePos.x - position.y), position.y);

                //return new Vector3Int(Mathf.Abs(slopePos.x - position.y), position.y, position.x);

                // XZ axis slope
                default:
                    return new Vector3Int(Mathf.Abs(slopePos.x - position.y), position.x, position.y);
            }
        }
        #endregion
    }
}
