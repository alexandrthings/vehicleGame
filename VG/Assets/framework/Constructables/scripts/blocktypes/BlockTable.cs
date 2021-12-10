using System.Collections;
using System.Collections.Generic;
using ASTankGame.Vehicles.Blocks;
using UnityEngine;
using UnityEngine.Windows.WebCam;

namespace ASTankGame.Vehicles.Blocks
{
    public static class BlockTable
    {
        #region BlockVertexBuild

        public static readonly Vector3[] cubeVerts = new Vector3[8]
        {
            new Vector3(0, 0, 0) - Vector3.one / 2, // 0
            new Vector3(1, 0, 0) - Vector3.one / 2, // 1
            new Vector3(1, 1, 0) - Vector3.one / 2, // 2
            new Vector3(0, 1, 0) - Vector3.one / 2, // 3
            new Vector3(0, 0, 1) - Vector3.one / 2, // 4
            new Vector3(1, 0, 1) - Vector3.one / 2, // 5
            new Vector3(1, 1, 1) - Vector3.one / 2, // 6
            new Vector3(0, 1, 1) - Vector3.one / 2 // 7
        };

        public static readonly Vector3[] slopeVerts = new Vector3[6]
        {
            new Vector3(0, 0, 0) - Vector3.one / 2, // bl 0
            new Vector3(1, 0, 0) - Vector3.one / 2, // br 1
            new Vector3(1, 1, 0) - Vector3.one / 2, // 2
            new Vector3(0, 1, 0) - Vector3.one / 2, // 3
            new Vector3(0, 0, 1) - Vector3.one / 2, // 4
            new Vector3(1, 0, 1) - Vector3.one / 2 // 5
        };

        public static readonly Vector3[] triVerts = new Vector3[4]
        {
            new Vector3(0, 0, 0) - Vector3.one / 2,
            new Vector3(1, 0, 0) - Vector3.one / 2,
            new Vector3(0, 1, 0) - Vector3.one / 2,
            new Vector3(0, 0, 1) - Vector3.one / 2
        };

        public static readonly Vector3[] invTriVerts = new Vector3[7]
        {
            new Vector3(0, 0, 0) - Vector3.one / 2, // 0
            new Vector3(1, 0, 0) - Vector3.one / 2, // 1
            new Vector3(1, 1, 0) - Vector3.one / 2, // 2
            new Vector3(0, 1, 0) - Vector3.one / 2, // 3
            new Vector3(0, 0, 1) - Vector3.one / 2, // 4
            new Vector3(1, 0, 1) - Vector3.one / 2, // 5
            new Vector3(0, 1, 1) - Vector3.one / 2 // 6
        };

        #endregion

        #region utility
        public static int DirFromVector(Vector3Int vector)
        {
            if (vector == Vector3Int.forward)
                return 1;
            if (vector == -Vector3Int.forward)
                return 0;

            if (vector == -Vector3Int.up)
                return 2;
            if (vector == Vector3Int.up)
                return 3;

            if (vector == -Vector3Int.right)
                return 4;
            
            return 5;
        }

        public static readonly int[,] axisSpin = new int[6, 4]
        {
            {3, 5, 2, 4},
            {3, 4, 2, 5},
            {1, 4, 0, 5},
            {1, 5, 0, 4},
            {3, 0, 2, 1},
            {3, 1, 2, 0}
        };

        public static readonly int[,] blockRight = new int[6, 6]
        {  // 0,  1, 2, 3, 4, 5
            {-1, -1, 5, 4, 3, 2}, // 0
            {-1, -1, 4, 5, 2, 3}, // 1
            {4, 5, -1, -1, 0, 1}, // 2
            {5, 4, -1, -1, 1, 0}, // 3
            {2, 3, 1, 0, -1, -1}, // 4
            {3, 2, 0, 1, -1, -1}  // 5
        };

        public static readonly int[,] blockLeft = new int[6, 6]
        {
            {-1, -1, 5, 4, 2, 3}, // 0
            {-1, -1, 4, 5, 3, 2}, // 1
            {4, 5, -1, -1, 1, 0}, // 2
            {5, 4, -1, -1, 0, 1}, // 3
            {3, 2, 0, 1, -1, -1}, // 4
            {2, 3, 1, 0, -1, -1}  // 5
        };

        public static readonly byte[] oppositeSide = new byte[6]
        {
            1, 0, 3, 2, 5, 4
        };

        public static readonly int[,] rollOnAxis = new int[6, 4]
        {
            {3, 4, 2, 5},
            {3, 5, 2, 4},
            {1, 5, 0, 4},
            {1, 4, 0, 5},
            {3, 1, 2, 0},
            {3, 0, 2, 1}
        };

        // find where in rotation it is
        public static int FindRotationIndex(int current, int add, int axis)
        {
            int rollIndex = 0;

            for (int i = 0; i < 4; i++)
            {
                if (rollOnAxis[axis, i] == current)
                {
                    rollIndex = i;
                    break;
                }
            }

            rollIndex += add;

            if (rollIndex < 0)
            {
                rollIndex = 4 + (rollIndex % 4);
            }
            else
            {
                rollIndex = rollIndex % 4;
            }

            //Debug.Log(rollIndex);

            return rollOnAxis[axis, rollIndex];
        }

        public static bool MatchingAxis(int dir1, int dir2)
        {
            if (dir1 == dir2)
                return true;
            if (dir1 == oppositeSide[dir2])
                return true;
            return false;
        }
        #endregion

        #region check directions

        public static Vector3Int[] dirToVector = new Vector3Int[6]
        {
            new Vector3Int(0, 0, -1),
            new Vector3Int(0, 0, 1),
            new Vector3Int(0, -1, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(1, 0, 0)
        };

        public static Vector3Int[] slopeFaceChecks = new Vector3Int[4]
        {
            new Vector3Int(0, 0, -1),
            new Vector3Int(0, -1, 0),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(1, 0, 0)
        };

        #endregion

        #region block type obstruction maps

        // this says which side of the unusual blocks obstructs
        public static readonly int[,] obstructions = new int[4, 6]
        {
            {0, 0, 0, 0, 0, 0},
            {0, 10, 0, 10, 1, 1},
            {1, 10, 1, 10, 1, 10},
            {0, 1, 0, 1, 0, 1} // back, front, bottom, top, left, right
        };

        /// <summary>
        /// Check if the triangle side is properly aligned to hide it
        /// </summary>
        /// <returns></returns>
        public static bool MatchingTriSide(int ourType, int theirType, int ourFwd, int ourUp, int ourSide, int theirSide, BlockRepresentation block2)
        {
            switch (ourType)
            {
                case 1: // slope
                    if (theirType == 1)
                        return FUMultMatch(new Vector2Int(ourFwd, ourUp), new Vector2Int(ourFwd, ourUp));
                    if (theirType == 2 || theirType == 3)
                        return FURExclusionMatch(new Vector2Int(ourUp, ourFwd),
                            new Vector3Int(block2.forward, block2.up, blockRight[block2.forward, block2.up]));
                    break;

                case 2: // tri or invtri
                case 3:
                    if (theirType == 1)
                        return FURExclusionMatch(new Vector2Int(ourUp, ourFwd),
                            new Vector3Int(block2.forward, block2.up, blockRight[block2.forward, block2.up]));
                    if (theirType == 2 || theirType == 3)
                        return FURMultMatch(new Vector3Int(ourUp, ourFwd, blockRight[ourFwd, ourUp]),
                            new Vector3Int(block2.forward, block2.up, blockRight[block2.forward, block2.up]));
                    break;
            }

            //if (block1.BlockType == 2 || block1.BlockType == 3)

            return false;
        }

        public static bool FURExclusionMatch(Vector2Int Rot1, Vector3Int Rot2)
        {
            if (FUMultMatch(Rot1, new Vector2Int(Rot2.x, Rot2.y)))
                return true;
            if (FUMultMatch(Rot1, new Vector2Int(Rot2.y, Rot2.z)))
                return true;
            if (FUMultMatch(Rot1, new Vector2Int(Rot2.x, Rot2.z)))
                return true;
            return false;
        }

        /// <summary>
        /// Full rotation multiply matching test
        /// </summary>
        /// <param name="Rot1">Forward, Up, Right</param>
        /// <param name="Rot2">Forward, Up, Right</param>
        /// <returns></returns>
        public static bool FURMultMatch(Vector3Int Rot1, Vector3Int Rot2)
        {
            return (Rot1.x + 1) * (Rot1.y + 1) * (Rot1.z + 1) ==
                   (Rot2.x + 1) * (Rot2.y + 1) * (Rot2.z + 1); // adding 1 so the zero doesnt fuck everything up
        }

        /// <summary>
        /// Forward/Upward rotation matching test
        /// </summary>
        /// <param name="Rot1">Forward, Up</param>
        /// <param name="Rot2">Forward, Up</param>
        /// <returns></returns>
        public static bool FUMultMatch(Vector2Int Rot1, Vector2Int Rot2)
        {
            return (Rot1.x + 1) * (Rot1.y + 1) ==
                   (Rot2.x + 1) * (Rot2.y + 1); // adding 1 so the zero doesnt fuck everything up
        }

        #endregion

        #region triangulation

        // These are the indices that need to be created for the triangle to exist in that particular face.
        public static readonly int[,] cubeTriangulation = new int[6, 6]
        {
            {0, 3, 1, 1, 3, 2}, // back face
            {5, 6, 4, 4, 6, 7}, // front face
            {1, 5, 0, 0, 5, 4}, // bottom face
            {3, 7, 2, 2, 7, 6}, // top face
            {4, 7, 0, 0, 7, 3}, // left face
            {1, 2, 5, 5, 2, 6}  // right face
        };

        public static readonly int[,] slopeTriangulation = new int [6, 6]
        {
            {0, 3, 1, 1, 3, 2}, // back face
            {3, 4, 2, 2, 4, 5}, // slope face
            {1, 5, 0, 0, 5, 4}, // bottom face
            {-1, -1, -1, -1, -1, -1}, // top "face"
            {4, 3, 0, -1, -1, -1}, // left face
            {1, 2, 5, -1, -1, -1} // right face
        };

        public static readonly int[,] triTriangulation = new int[5, 3]
        {
            {0, 2, 1}, // back face
            {3, 1, 2}, // slope face
            {3, 0, 1}, // bottom face
            {-1, -1, -1}, // top ghost face
            {2, 0, 3} // left face
        };

        public static readonly int[,] invTriTriangulation = new int[7, 6]
        {
            {0, 3, 1, 1, 3, 2}, // back face
            {5, 6, 4, -1, -1, -1}, // front face
            {1, 5, 0, 0, 5, 4}, // bottom face
            {2, 3, 6, -1, -1, -1}, // top
            {4, 6, 0, 0, 6, 3}, // left face
            {1, 2, 5, -1, -1, -1}, // right face
            {2, 6, 5, -1, -1, -1}
        };

        #endregion
    }
}