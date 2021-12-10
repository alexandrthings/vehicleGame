using System.Collections;
using System.Collections.Generic;
using ASTankGame.Vehicles.Blocks.Management;
using Unity.VisualScripting;
using UnityEngine;

namespace ASTankGame.Vehicles.Blocks.Planes
{
    public class FacePlane
    {
        public Vector2Int planePos;
        public Vector3 planeOrigin;

        public float scale;

        public Dictionary<Vector2Int, byte> tiles = new Dictionary<Vector2Int, byte>();

        public MeshFilter meshFilter;
        public MeshCollider collider;

        public FacePlane(GameObject parent, Vector2Int _planePos, float _scale)
        {
            planePos = _planePos;
            scale = _scale;

            // make an object for this faceplane
            GameObject meshObject = new GameObject($"FPMesh {_planePos}");
            meshObject.transform.parent = parent.transform;

            meshFilter = meshObject.AddComponent<MeshFilter>();
            MeshRenderer render = meshObject.AddComponent<MeshRenderer>();
            collider = meshObject.AddComponent<MeshCollider>();

            collider.convex = true;

            render.material = GlobalBlockManager.standardMat;

            meshObject.transform.localPosition = FaceTable.dirToVector3[_planePos.y] * _planePos.x;
            planeOrigin = meshObject.transform.localPosition;

            meshObject.transform.localPosition = planeOrigin * scale;
        }

        public void Update()
        {
            int vertIndex = 0;
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();

            Dictionary<Vector2Int, byte> toDoTiles = new Dictionary<Vector2Int, byte>(tiles);

            foreach (KeyValuePair<Vector2Int, byte> tile in tiles)
            {
                if (!toDoTiles.ContainsKey(tile.Key))
                {
                    Debug.Log($"{tile.Key} key failed");
                    continue;
                }

                switch (tile.Value)
                {
                    case 0:
                        #region propagate and draw square
                        // propagation distance in directions +x, -y, -x, +y, in that order
                        int[] propagation = new int[4] {0, 0, 0, 0};
                        bool[] pFailure = new bool[4] {false, false, false, false};

                        int failcatch = 0;

                        // propagate until all directions fail
                        while (!pFailure[0] || !pFailure[1] || !pFailure[2] || !pFailure[3])
                        {
                            // propagate +X
                            if (!pFailure[0]) // sweep from lowest extent to highest extent on Y axis to see that rectangle side is whole to merge
                            {
                                for (int i = propagation[1]; i <= propagation[3]; i++)
                                {
                                    Vector2Int checkedTile = tile.Key + new Vector2Int(propagation[0]+1, i);

                                    Debug.Log($"Tile {tile.Key} Sweep coordinate {i}, limit {propagation[3]}, Checked tile {checkedTile}");

                                    if (!toDoTiles.ContainsKey(checkedTile)) // there is a missing key, or a used one, fail propagation
                                    {
                                        pFailure[0] = true;
                                        Debug.Log("End R");
                                        goto PLUSXFAILURE;
                                    }
                                }
                                
                                propagation[0] += 1;
                            }
                            
                            PLUSXFAILURE:
                            
                            // propagate -X
                            if (!pFailure[2]) // sweep from lowest extent to highest extent on Y axis to see that rectangle side is whole to merge
                            {
                                for (int i = propagation[1]; i <= propagation[3]; i++)
                                {
                                    Vector2Int checkedTile = tile.Key + new Vector2Int(-propagation[2] - 1, i);

                                    Debug.Log($"Tile {tile.Key} Sweep coordinate {i}, limit {propagation[3]}, Checked tile {checkedTile}");

                                    if (!toDoTiles.ContainsKey(checkedTile)) // there is a missing key, or a used one, fail propagation
                                    {
                                        pFailure[2] = true;
                                        Debug.Log("End L");
                                        goto MINUSXFAILURE;
                                    }
                                }

                                propagation[2] += 1;
                            }

                            MINUSXFAILURE:

                            //propagate +Y
                            if (!pFailure[3]) // sweep from leftmost extent to rightmost extent on X axis to see that rectangle side is whole to merge
                            {
                                for (int i = tile.Key.x - propagation[2]; i <= propagation[0] + tile.Key.y; i++)
                                {
                                    Vector2Int checkedTile = tile.Key + new Vector2Int(i, propagation[3] + 1);

                                    if (!toDoTiles.ContainsKey(checkedTile)) // there is a missing key, or a used one, fail propagation
                                    {
                                        pFailure[3] = true;
                                        goto PLUSYFAILURE;
                                    }
                                }

                                propagation[3] += 1;
                            }

                            PLUSYFAILURE:

                            // propagate -Y
                            if (!pFailure[1]) // sweep from leftmost extent to rightmost extent on X axis to see that rectangle side is whole to merge
                            {
                                for (int i = tile.Key.x - propagation[2]; i <= propagation[0] + tile.Key.y; i++)
                                {
                                    Vector2Int checkedTile = tile.Key + new Vector2Int(i, -propagation[1] - 1);

                                    if (!toDoTiles.ContainsKey(checkedTile)) // there is a missing key, or a used one, fail propagation
                                    {
                                        pFailure[1] = true;
                                        goto MINUSYFAILURE;
                                    }
                                }

                                propagation[1] += 1;
                            }

                            MINUSYFAILURE:;
                            
                            failcatch++;

                            if (failcatch > 1000)
                            {
                                Debug.LogError("Critical propagation failure, ejecting");
                                break;
                            }
                        }

                        // remove used tiles
                        for (int x = -propagation[2]; x <= propagation[0]; x++)
                        {
                            for (int y = -propagation[1]; y <= propagation[3]; y++)
                            {
                                toDoTiles.Remove(tile.Key + new Vector2Int(x, y));
                            }
                        }

                        // add stretched square to mesh and uvs
                        Vector3[] verts = new Vector3[4]
                        {
                            (AddTilePosToOrigin(tile.Key + new Vector2Int(propagation[0], propagation[1]), planePos.y) + FaceTable.cubeVerts[FaceTable.cubeFaces[planePos.y, 0]]) * scale,
                            (AddTilePosToOrigin(tile.Key + new Vector2Int(propagation[1], propagation[2]), planePos.y) + FaceTable.cubeVerts[FaceTable.cubeFaces[planePos.y, 1]]) * scale,
                            (AddTilePosToOrigin(tile.Key + new Vector2Int(propagation[2], propagation[3]), planePos.y) + FaceTable.cubeVerts[FaceTable.cubeFaces[planePos.y, 2]]) * scale,
                            (AddTilePosToOrigin(tile.Key + new Vector2Int(propagation[0], propagation[3]), planePos.y) + FaceTable.cubeVerts[FaceTable.cubeFaces[planePos.y, 3]]) * scale
                        };

                        vertices.AddRange(verts);

                        for (int i = 0; i < 6; i++)
                        {
                            triangles.Add(vertIndex + FaceTable.cubeTriangulation[planePos.y, i]);
                        }

                        vertIndex += 4;

                        break;
                    #endregion
                }
            }

            Mesh mesh = new Mesh();
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();

            mesh.RecalculateNormals();

            meshFilter.mesh = mesh;
            collider.sharedMesh = mesh;
        }

        // adding is done manually in chunk
        public void RemoveTile(Vector2Int position)
        {
            if (tiles.ContainsKey(position))
                tiles.Remove(position);
        }

        public Vector3 AddTilePosToOrigin(Vector2Int tile, int dir)
        {
            switch (dir)
            {
                case 0:
                case 1:
                    return new Vector3(tile.x, tile.y, 0);
                case 2:
                case 3:
                    return new Vector3(tile.x, 0, tile.y);
                default:
                    return new Vector3(0, tile.y, tile.x);
            }
        }

        public static readonly Vector2Int[] indexToDir = new Vector2Int[4]
        {
            new Vector2Int(1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1)
        };
    }
}