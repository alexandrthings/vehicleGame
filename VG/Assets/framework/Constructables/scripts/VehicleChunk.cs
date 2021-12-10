using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ASTankGame.Damage;
using ASTankGame.Vehicles.BlockBehaviors;
using ASTankGame.Vehicles.BlockBehaviors.Weapons;
using ASTankGame.Vehicles.Blocks;
using ASTankGame.Vehicles.Blocks.Management;
using Unity.VisualScripting;
using UnityEngine;
using Object = UnityEngine.Object;
using Debug = UnityEngine.Debug;

namespace ASTankGame.Vehicles.Chunks
{
    //[ExecuteInEditMode]
    /// <summary>
    /// Holds all block and damage data. Meshes, collides, and recalculates COM on update. Sends actuated block references to attached Vehicle.
    /// </summary>
    public class VehicleChunk : MonoBehaviour
    {
        #region block data
        public List<Collider> CubeColliders = new List<Collider>();
        public List<MeshCollider> MeshColliders = new List<MeshCollider>();

        public Vector3 COMPos = new Vector3();
        public float MassTotal = 0;
        #endregion

        #region other variables
        private Vehicle parentVehicle;
        public Vector3Int localPos { get; private set; }

        [SerializeField]
        private float Scaling = 1f;
        [SerializeField]
        private bool Simplify;

        public bool DebugMessages = false;

        public int chunkSize = 10;

        private List<Vector2> uvs = new List<Vector2>();

        public MeshFilter meshFilter;
        public MeshRenderer meshRenderer;
        #endregion

        void Awake()
        {
            if (transform.parent.parent.name.Contains("REPLAY"))
                return;

            Simplify = true;

            parentVehicle = transform.parent.parent.GetComponent<Vehicle>();
            Scaling = parentVehicle.Scale;

            localPos = Vector3Int.RoundToInt(transform.localPosition/Scaling);

            gameObject.layer = LayerMask.NameToLayer("Vehicle");

            meshFilter = gameObject.AddComponent<MeshFilter>();
            meshRenderer = gameObject.AddComponent<MeshRenderer>();

            meshRenderer.material = GlobalBlockManager.standardMat;

            chunkSize = parentVehicle.ChunkSize;

            //meshRenderer.material.set

            // temp
            if (!parentVehicle.IsSubvehicle && DebugMessages)

            if (transform.localPosition == Vector3.zero)
            {
                //AddBlock(GlobalBlockManager.GetBlockByID(0), Vector3Int.RoundToInt(new Vector3(0, 0, 0)), 1, 3);
                for (int x = 0; x < 1; x++)
                {
                    for (int y = 0; y < 1; y++)
                    {
                        for (int z = 0; z < 1; z++)
                        {
                            //PerfectBlocks.Add(new Vector3Int(x, y, z), (PerfectBlock) GlobalBlockManager.GetBlockID(0));
                            //AddBlock(GlobalBlockManager.GetBlockByID(0), Vector3Int.RoundToInt(new Vector3(x, y, z)), 1, 3, false);
                        }
                    }
                }
                
                Debug.Log("0,0,0 init");

                RemeshAndRecollide();
            }

        }

        #region mesh building
        public void RemeshAndRecollide() // best not tamper with this
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            int vertIndex = 0;

            int nCubeColliders = 0;
            int nMeshColliders = 0;

            COMPos = new Vector3();
            MassTotal = 0.00001f;

            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();

            Dictionary<Vector3Int, bool> doneBlocks = new Dictionary<Vector3Int, bool>();

            // constructing cubes
            for (int xPos = 0; xPos < chunkSize; xPos++)
            for (int yPos = 0; yPos < chunkSize; yPos++)
            for (int zPos = 0; zPos < chunkSize; zPos++)
            {
                Vector3Int absPos = new Vector3Int(xPos + localPos.x, yPos + localPos.y, zPos + localPos.z);

                if (!parentVehicle.Blocks.ContainsKey(absPos))
                    continue;

                KeyValuePair<Vector3Int, BlockRepresentation> pair =
                    new KeyValuePair<Vector3Int, BlockRepresentation>(absPos, parentVehicle.Blocks[absPos]);

                if (pair.Value.blockID < 0)
                    continue;

                COMPos += (Vector3)(pair.Key-localPos) * Scaling * pair.Value.block.Mass;
                MassTotal += pair.Value.block.Mass;

                bool destroyed = IsBlockDestroyed(pair.Key);

                // set GOBLOCK damage enabled
                if (pair.Value.GetType() == typeof(GOBlock))
                {
                    Debug.Log(destroyed);
                    parentVehicle.GObjects[pair.Key].SetActive(!destroyed);
                    Debug.Log(parentVehicle.GObjects[pair.Key]);
                    continue;
                }

                // if its done, not a perfect block, or destroyed, don't do the process
                if (destroyed || doneBlocks.ContainsKey(pair.Key) || pair.Value.block.GetType() != typeof(PerfectBlock))
                    continue;

                Vector2Int blockRot = new Vector2Int(pair.Value.forward, pair.Value.up);
                Matrix4x4 vertRotation = Matrix4x4.Rotate(BlockRotation.RotToQuat(blockRot));

                Vector3Int forward = BlockTable.dirToVector[pair.Value.forward];
                Vector3Int up = BlockTable.dirToVector[pair.Value.up];
                Vector3Int right =
                    BlockTable.dirToVector[
                        BlockTable.blockLeft[pair.Value.forward, pair.Value.up]]; // this is actually left

                PerfectBlock thisBlock = GlobalBlockManager.GetBlockByID(pair.Value.blockID) as PerfectBlock;

                // block type (is it a slope, block, tri, etc?)
                switch (thisBlock.BlockType)
                {
                    #region Building a Cube

                    case 0:
                        // propagate collider then use it as mesh
                        //int[] propagation = new int[6] {0,0,0,0,0,0};

                        Vector3Int LB = pair.Key;
                        Vector3Int UB = pair.Key;
                        bool[] blocked = new bool[6] {false, false, false, false, false, false};

                        // propagate all directions
                        while (!blocked[0] || !blocked[1] || !blocked[2] || !blocked[3] || !blocked[4] || !blocked[5])
                        {
                            if (!blocked[0])
                            {
                                if (CubePropagate(pair.Value.blockID, 0, LB, UB, doneBlocks))
                                    LB.z -= 1;
                                else
                                    blocked[0] = true;
                            }

                            if (!blocked[1])
                            {
                                if (CubePropagate(pair.Value.blockID, 1, LB, UB, doneBlocks))
                                    UB.z += 1;
                                else
                                    blocked[1] = true;
                            }

                            if (!blocked[2])
                            {
                                if (CubePropagate(pair.Value.blockID, 2, LB, UB, doneBlocks))
                                    LB.y -= 1;
                                else
                                    blocked[2] = true;
                            }

                            if (!blocked[3])
                            {
                                if (CubePropagate(pair.Value.blockID, 3, LB, UB, doneBlocks))
                                    UB.y += 1;
                                else
                                    blocked[3] = true;

                            }

                            if (!blocked[4])
                            {
                                if (CubePropagate(pair.Value.blockID, 4, LB, UB, doneBlocks))
                                    LB.x -= 1;
                                else
                                    blocked[4] = true;
                            }

                            if (!blocked[5])
                            {
                                if (CubePropagate(pair.Value.blockID, 5, LB, UB, doneBlocks))
                                    UB.x += 1;
                                else
                                    blocked[5] = true;
                            }
                        }

                        // declare used blocks
                        for (int x = LB.x; x <= UB.x; x++)
                        for (int y = LB.y; y <= UB.y; y++)
                        for (int z = LB.z; z <= UB.z; z++)
                            doneBlocks.Add(new Vector3Int(x, y, z), true);

                        // build block
                        Vector3[] verts = new Vector3[8];
                        Array.Copy(BlockTable.cubeVerts, verts, 8);

                        // transform vertices
                        LB = LB - localPos;
                        UB = UB - localPos;

                        verts[0] = (verts[0] + LB) * Scaling;
                        verts[1] = (verts[1] + new Vector3(UB.x, LB.y, LB.z)) * Scaling;
                        verts[2] = (verts[2] + new Vector3(UB.x, UB.y, LB.z)) * Scaling;
                        verts[3] = (verts[3] + new Vector3(LB.x, UB.y, LB.z)) * Scaling;
                        verts[4] = (verts[4] + new Vector3(LB.x, LB.y, UB.z)) * Scaling;
                        verts[5] = (verts[5] + new Vector3(UB.x, LB.y, UB.z)) * Scaling;
                        verts[6] = (verts[6] + UB) * Scaling;
                        verts[7] = (verts[7] + new Vector3(LB.x, UB.y, UB.z)) * Scaling;

                        // add tris
                        for (int p = 0; p < 6; p++)
                        {
                            for (int t = 0; t < 6; t++)
                            {
                                vertices.Add(verts[BlockTable.cubeTriangulation[p, t]]);

                                triangles.Add(vertIndex);

                                vertIndex++;
                            }
                        }

                        BoxCollider BoxCol;

                        // if too many new colliders, start adding
                        if (nCubeColliders >= CubeColliders.Count)
                        {
                            BoxCol = transform.AddComponent<BoxCollider>();

                            BoxCol.center = (LB + ((Vector3) (UB - LB) / 2)) * Scaling;
                            BoxCol.size = (Vector3.one + UB - LB) * Scaling;

                            CubeColliders.Add(BoxCol);
                        }
                        else // else reuse
                        {
                            BoxCol = CubeColliders[nCubeColliders] as BoxCollider;

                            BoxCol.center = (LB + ((Vector3) (UB - LB) / 2)) * Scaling;
                            BoxCol.size = (Vector3.one + UB - LB) * Scaling;
                        }

                        nCubeColliders++;
                        break;

                    #endregion

                    #region Building a Slope
                    case 1:
                        // L  R  U  D
                        int[] extents = new int[4] {0, 0, 0, 0};

                        bool[] blocked2 = new bool[2] {false, false};

                        doneBlocks.Add(pair.Key, true);
                        //Debug.Log($"position {pair.Key}");

                        // might newed to delete this later
                        int failCatch = 0;

                        // get width of slope
                        while (!blocked2[0] || !blocked2[1])
                        {
                            // propagate left and right
                            BlockRepresentation checkedBlock;

                            if (!blocked2[0])
                            {
                                Vector3Int checkedPos = pair.Key - right * (extents[0] + 1);
                                checkedBlock = parentVehicle.GetBlockLocal(checkedPos);

                                if (PBlockExistsInChunk(checkedPos) &&
                                    !IsBlockDestroyed(checkedPos) &&
                                    CheckSlopePropagation(pair.Value, checkedBlock) &&
                                    !doneBlocks.ContainsKey(checkedPos))
                                {

                                    extents[0] += 1;
                                    doneBlocks.Add(checkedPos, true);

                                }
                                else
                                {
                                    blocked2[0] = true;
                                }
                            }

                            if (!blocked2[1])
                            {
                                Vector3Int checkedPos = pair.Key + right * (extents[1] + 1);
                                checkedBlock = parentVehicle.GetBlockLocal(checkedPos);

                                if (PBlockExistsInChunk(checkedPos) && 
                                    !IsBlockDestroyed(checkedPos) &&
                                    CheckSlopePropagation(pair.Value, checkedBlock) &&
                                    !doneBlocks.ContainsKey(checkedPos))
                                {

                                    extents[1] += 1;
                                    doneBlocks.Add(checkedPos, true);

                                }
                                else
                                {
                                    blocked2[1] = true;
                                }
                            }

                            failCatch++;

                            if (failCatch > 1000)
                            {
                                Debug.LogWarning($"tri failed, {blocked2[0]}, {blocked2[1]}");
                                break;
                            }
                        }

                        // reset flags for vertical
                        blocked2[0] = false;
                        blocked2[1] = false;

                        failCatch = 0;

                        // get steps of slope
                        while (!blocked2[0] || !blocked2[1])
                        {
                            List<KeyValuePair<Vector3Int, bool>> checkedList =
                                new List<KeyValuePair<Vector3Int, bool>>();

                            // check length of it above
                            if (!blocked2[0])
                            {
                                for (int i = -extents[0]; i <= extents[1]; i++)
                                {
                                    Vector3Int checkedPos = pair.Key + right * i +
                                                            (-BlockTable.dirToVector[pair.Value.forward] +
                                                             BlockTable.dirToVector[pair.Value.up]) * (extents[2] + 1);

                                    BlockRepresentation checkedBlock = parentVehicle.GetBlockLocal(checkedPos);

                                    if (!PBlockExistsInChunk(checkedPos) || 
                                        IsBlockDestroyed(checkedPos) ||
                                        doneBlocks.ContainsKey(checkedPos) ||
                                        !CheckSlopePropagation(pair.Value, checkedBlock))
                                    {
                                        blocked2[0] = true;
                                        checkedList.Clear();
                                        goto UPFAIL;
                                    }

                                    checkedList.Add(new KeyValuePair<Vector3Int, bool>(checkedPos, true));
                                }

                                extents[2] += 1;
                            }

                            UPFAIL:

                            doneBlocks.AddRange(checkedList.ToArray());
                            checkedList.Clear();
                            // check length of it below
                            if (!blocked2[1])
                            {
                                for (int i = -extents[0]; i <= extents[1]; i++)
                                {
                                    Vector3Int checkedPos = pair.Key + right * i +
                                                            ((BlockTable.dirToVector[pair.Value.forward] -
                                                              BlockTable.dirToVector[pair.Value.up]) *
                                                             (extents[3] + 1));

                                    //Debug.LogWarning($"pos {checkedPos}, extents[0] = {-extents[0]}, extents[1] = {extents[1]}, i = {i}");

                                    BlockRepresentation checkedBlock = parentVehicle.GetBlockLocal(checkedPos);

                                    if (!PBlockExistsInChunk(checkedPos) || 
                                        IsBlockDestroyed(checkedPos) || 
                                        doneBlocks.ContainsKey(checkedPos) ||
                                        !CheckSlopePropagation(pair.Value, checkedBlock))
                                    {
                                        //Debug.LogError($"Stopped at {checkedPos}, iteration {i}");
                                        blocked2[1] = true;
                                        checkedList.Clear();
                                        goto DOWNFAIL;
                                    }

                                    checkedList.Add(new KeyValuePair<Vector3Int, bool>(checkedPos, true));
                                }

                                extents[3] += 1;
                            }

                            DOWNFAIL:

                            doneBlocks.AddRange(checkedList.ToArray());
                            checkedList.Clear();

                            failCatch++;

                            if (failCatch > 1000)
                            {
                                Debug.LogWarning("failed");
                                break;
                            }
                        }

                        // log it for now
                        //Debug.Log($"Slope attempt results: Extent 0 {extents[0]}, Extent 1 {extents[1]}, Extent 2 {extents[2]}, Extent 3 {extents[3]}");

                        // draw main slope          
                        Vector3[] vertsSlope = new Vector3[8]
                        {
                            BlockTable.slopeVerts[2], BlockTable.slopeVerts[3], BlockTable.slopeVerts[4],
                            BlockTable.slopeVerts[5], BlockTable.slopeVerts[0], BlockTable.slopeVerts[1],
                            BlockTable.slopeVerts[0], BlockTable.slopeVerts[1]
                        };
                        // 0 is top right, 1 is top left, 2 is bottom left, 3 is bototm right
                        vertsSlope[0] = (pair.Key - localPos + vertRotation.MultiplyPoint3x4(vertsSlope[0]) + right * extents[1] -
                            forward * extents[2] + up * extents[2]) * Scaling;
                        vertsSlope[1] = (pair.Key - localPos + vertRotation.MultiplyPoint3x4(vertsSlope[1]) - right * extents[0] -
                            forward * extents[2] + up * extents[2]) * Scaling;

                        vertsSlope[2] = (pair.Key - localPos + vertRotation.MultiplyPoint3x4(vertsSlope[2]) - right * extents[0] +
                            forward * extents[3] - up * extents[3]) * Scaling;
                        vertsSlope[3] = (pair.Key - localPos + vertRotation.MultiplyPoint3x4(vertsSlope[3]) + right * extents[1] +
                            forward * extents[3] - up * extents[3]) * Scaling;

                        // edit collider vertices
                        vertsSlope[4] = (pair.Key - localPos + vertRotation.MultiplyPoint3x4(vertsSlope[4]) - right * extents[0] -
                            forward * extents[2] + up * extents[2]) * Scaling;
                        vertsSlope[5] = (pair.Key - localPos + vertRotation.MultiplyPoint3x4(vertsSlope[5]) + right * extents[1] -
                            forward * extents[2] + up * extents[2]) * Scaling;

                        vertsSlope[6] = (pair.Key - localPos + vertRotation.MultiplyPoint3x4(vertsSlope[6]) - right * extents[0] +
                            forward * extents[3] - up * extents[3]) * Scaling;
                        vertsSlope[7] = (pair.Key - localPos + vertRotation.MultiplyPoint3x4(vertsSlope[7]) + right * extents[1] +
                            forward * extents[3] - up * extents[3]) * Scaling;

                        vertices.Add(vertsSlope[0]);
                        vertices.Add(vertsSlope[1]);
                        vertices.Add(vertsSlope[2]);
                        vertices.Add(vertsSlope[3]);
                        triangles.AddRange(new int[]
                            {vertIndex + 1, vertIndex + 3, vertIndex, vertIndex + 1, vertIndex + 2, vertIndex + 3});
                        vertIndex += 4;

                        MeshCollider SlopeCol;
                        // add collider
                        // if too many new colliders, start adding
                        if (nMeshColliders >= MeshColliders.Count)
                        {
                            GameObject colObject = new GameObject($"MeshCol {nMeshColliders}");
                            colObject.transform.parent = transform;
                            colObject.transform.localPosition = Vector3.zero;
                            colObject.transform.localRotation = Quaternion.identity;
                            colObject.layer = LayerMask.NameToLayer("Vehicle");

                            SlopeCol = colObject.AddComponent<MeshCollider>();
                            SlopeCol.convex = true;

                            MeshColliders.Add(SlopeCol);
                        }
                        else // reuse
                        {
                            SlopeCol = MeshColliders[nMeshColliders] as MeshCollider;
                        }

                        Mesh colMesh = new Mesh();
                        colMesh.vertices = vertsSlope; // slope side
                        colMesh.triangles = new int[] {1, 3, 0, 1, 2, 3, 5, 4, 6, 5, 7, 4};

                        SlopeCol.sharedMesh = colMesh;

                        nMeshColliders++;

                        // check and draw steps
                        for (int i = -extents[3]; i <= extents[2]; i++)
                        {
                            Vector3Int checkedPos;

                            /*for (int w = -extents[0]; w <= extents[1]; w++)
                            {
                                checkedPos = pair.Key + right * w + (-BlockTable.dirToVector[pair.Value.forward] + BlockTable.dirToVector[pair.Value.up]) * i;
                                // check down

                            }*/

                            // fuck it i dont care about hiding the steps
                            vertices.AddRange
                            (new Vector3[]
                                {
                                    // bottom face
                                    (pair.Key - localPos + vertRotation.MultiplyPoint3x4(BlockTable.slopeVerts[0]) -
                                        right * extents[0] + (up - forward) * i) * Scaling,
                                    (pair.Key - localPos + vertRotation.MultiplyPoint3x4(BlockTable.slopeVerts[1]) +
                                     right * extents[1] + (up - forward) * i) * Scaling,
                                    (pair.Key - localPos + vertRotation.MultiplyPoint3x4(BlockTable.slopeVerts[4]) -
                                        right * extents[0] + (up - forward) * i) * Scaling,
                                    (pair.Key - localPos + vertRotation.MultiplyPoint3x4(BlockTable.slopeVerts[5]) +
                                     right * extents[1] + (up - forward) * i) * Scaling,
                                    // back face
                                    (pair.Key - localPos + vertRotation.MultiplyPoint3x4(BlockTable.slopeVerts[0]) -
                                        right * extents[0] + (up - forward) * i) * Scaling,
                                    (pair.Key - localPos + vertRotation.MultiplyPoint3x4(BlockTable.slopeVerts[1]) +
                                     right * extents[1] + (up - forward) * i) * Scaling,
                                    (pair.Key - localPos + vertRotation.MultiplyPoint3x4(BlockTable.slopeVerts[3]) -
                                        right * extents[0] + (up - forward) * i) * Scaling,
                                    (pair.Key - localPos + vertRotation.MultiplyPoint3x4(BlockTable.slopeVerts[2]) +
                                     right * extents[1] + (up - forward) * i) * Scaling
                                }
                            );

                            triangles.AddRange(new int[]
                            {
                                vertIndex, vertIndex + 1, vertIndex + 2, vertIndex + 1, vertIndex + 3, vertIndex + 2,
                                vertIndex + 4, vertIndex + 6, vertIndex + 5, vertIndex + 5, vertIndex + 6,
                                vertIndex + 7,
                            });

                            vertIndex += 8;

                            #region endings

                            // check left end
                            checkedPos = pair.Key - right * extents[0] + (-BlockTable.dirToVector[pair.Value.forward] +
                                                                          BlockTable.dirToVector[pair.Value.up]) * i;

                            BlockRepresentation checkedBlock1 = parentVehicle.GetBlock(checkedPos);
                            BlockRepresentation checkedBlock2 = parentVehicle.GetBlock(checkedPos - right);

                            // if unobstructed draw it
                            if (!CheckBlockFaceKnown(checkedBlock1, 1, 4, checkedBlock2))
                            {
                                vertices.Add((checkedPos - localPos + vertRotation.MultiplyPoint3x4(BlockTable.slopeVerts[0])) *
                                             Scaling);
                                vertices.Add((checkedPos - localPos + vertRotation.MultiplyPoint3x4(BlockTable.slopeVerts[3])) *
                                             Scaling);
                                vertices.Add((checkedPos - localPos + vertRotation.MultiplyPoint3x4(BlockTable.slopeVerts[4])) *
                                             Scaling);
                                triangles.AddRange(new int[] {vertIndex, vertIndex + 2, vertIndex + 1});
                                vertIndex += 3;
                            }

                            // check right end
                            checkedPos += right * (extents[0] + extents[1]);
                            checkedBlock1 = parentVehicle.GetBlock(checkedPos);
                            checkedBlock2 = parentVehicle.GetBlock(checkedPos + right);

                            // if unobstructed draw it
                            if (!CheckBlockFaceKnown(checkedBlock1, 1, 5, checkedBlock2))
                            {
                                vertices.Add((checkedPos - localPos + vertRotation.MultiplyPoint3x4(BlockTable.slopeVerts[1])) *
                                             Scaling);
                                vertices.Add((checkedPos - localPos + vertRotation.MultiplyPoint3x4(BlockTable.slopeVerts[2])) *
                                             Scaling);
                                vertices.Add((checkedPos - localPos + vertRotation.MultiplyPoint3x4(BlockTable.slopeVerts[5])) *
                                             Scaling);
                                triangles.AddRange(new int[] {vertIndex, vertIndex + 1, vertIndex + 2});
                                vertIndex += 3;
                            }

                            #endregion

                        }

                        break;

                    #endregion

                    //  !!!!! leaves edge case when in orientation 1,3,5 that makes suboptimal meshes
                    #region building tri
                    case 2:
                        List<KeyValuePair<Vector3Int, bool>> triDoneBlocks = new List<KeyValuePair<Vector3Int, bool>>();

                        Vector3Int upLimit = pair.Key;
                        Vector3Int rightLimit = pair.Key;
                        Vector3Int fwdLimit = pair.Key;

                        doneBlocks.Add(pair.Key, true);
                        int failsafe = 0;

                        // expand LeftForwardUp
                        while (true)
                        {
                            // check everything
                            Vector3Int newUpLimit = upLimit - right + up;
                            Vector3Int newFwdLimit = fwdLimit - right + forward;
                            Vector3Int checkedPos = newUpLimit;

                            // sweep from up limit to down limit
                            bool inv = false;
                            while (checkedPos != newFwdLimit - up)
                            {
                                if (!PBlockExistsInChunk(checkedPos) || IsBlockDestroyed(checkedPos) ||
                                    doneBlocks.ContainsKey(checkedPos))
                                    goto UpForwardFail;

                                BlockRepresentation checkedBlock = parentVehicle.GetBlockLocal(checkedPos);

                                if (inv)
                                {
                                    // not an inv. triangle when it should be one
                                    if (!CheckTriPropagation(pair.Value, checkedBlock, inv))
                                        goto UpForwardFail;

                                    // step forward to where triangle should be
                                    triDoneBlocks.Add(new KeyValuePair<Vector3Int, bool>(checkedPos, false));
                                    checkedPos += forward;
                                }
                                else
                                {
                                    // not a triangle when it should be one
                                    if (!CheckTriPropagation(pair.Value, checkedBlock, inv))
                                        goto UpForwardFail;

                                    // step down where inv. triangle should be
                                    triDoneBlocks.Add(new KeyValuePair<Vector3Int, bool>(checkedPos, false));
                                    checkedPos -= up;
                                }

                                inv = !inv;
                            }

                            upLimit = newUpLimit;
                            fwdLimit = newFwdLimit;

                            doneBlocks.AddRange(triDoneBlocks.ToArray());
                            triDoneBlocks.Clear();

                            failsafe++;
                            if (failsafe > 10000)
                                return;
                        }

                        UpForwardFail:
                        triDoneBlocks.Clear();

                        failsafe = 0;

                        // expand BackRightUp
                        while (true)
                        {
                            // check everything
                            Vector3Int newUpLimit = upLimit - forward + up;
                            Vector3Int newRightLimit = rightLimit + right - forward;
                            Vector3Int checkedPos = newUpLimit;

                            // sweep from up limit to down limit
                            bool inv = false;
                            while (checkedPos != newRightLimit - up)
                            {
                                if (!PBlockExistsInChunk(checkedPos) || IsBlockDestroyed(checkedPos) ||
                                    doneBlocks.ContainsKey(checkedPos))
                                    goto UpRightFail;

                                BlockRepresentation checkedBlock = parentVehicle.GetBlockLocal(checkedPos);

                                if (inv)
                                {
                                    // not an inv. triangle when it should be one
                                    if (!CheckTriPropagation(pair.Value, checkedBlock, inv))
                                        goto UpRightFail;

                                    // step forward to where triangle should be
                                    triDoneBlocks.Add(new KeyValuePair<Vector3Int, bool>(checkedPos, false));
                                    checkedPos += right;
                                }
                                else
                                {
                                    // not a triangle when it should be one
                                    if (!CheckTriPropagation(pair.Value, checkedBlock, inv))
                                        goto UpRightFail;

                                    // step down where inv. triangle should be
                                    triDoneBlocks.Add(new KeyValuePair<Vector3Int, bool>(checkedPos, false));
                                    checkedPos -= up;
                                }

                                inv = !inv;
                            }

                            upLimit = newUpLimit;
                            rightLimit = newRightLimit;

                            doneBlocks.AddRange(triDoneBlocks.ToArray());
                            triDoneBlocks.Clear();

                            failsafe++;
                            if (failsafe > 10000)
                                return;
                        }

                        UpRightFail:
                        triDoneBlocks.Clear();

                        failsafe = 0;

                        // expand DownRightForward
                        while (true)
                        {
                            // check everything
                            Vector3Int newForwardLimit = upLimit + forward - up;
                            Vector3Int newRightLimit = rightLimit + right - up;
                            Vector3Int checkedPos = newRightLimit;

                            // sweep from up limit to down limit
                            bool inv = false;
                            while (checkedPos != newForwardLimit - right)
                            {
                                if (!PBlockExistsInChunk(checkedPos) || IsBlockDestroyed(checkedPos) ||
                                    doneBlocks.ContainsKey(checkedPos))
                                    goto ForwardRightFail;

                                BlockRepresentation checkedBlock = parentVehicle.GetBlockLocal(checkedPos);

                                if (inv)
                                {
                                    // not an inv. triangle when it should be one
                                    if (!CheckTriPropagation(pair.Value, checkedBlock, inv))
                                        goto ForwardRightFail;

                                    // step forward to where triangle should be
                                    triDoneBlocks.Add(new KeyValuePair<Vector3Int, bool>(checkedPos, false));
                                    checkedPos += forward;
                                }
                                else
                                {
                                    // not a triangle when it should be one
                                    if (!CheckTriPropagation(pair.Value, checkedBlock, inv))
                                        goto ForwardRightFail;

                                    // step down where inv. triangle should be
                                    triDoneBlocks.Add(new KeyValuePair<Vector3Int, bool>(checkedPos, false));
                                    checkedPos -= right;
                                }

                                inv = !inv;
                            }

                            doneBlocks.AddRange(triDoneBlocks.ToArray());
                            triDoneBlocks.Clear();

                            fwdLimit = newForwardLimit;
                            rightLimit = newRightLimit;

                            failsafe++;
                            if (failsafe > 10000)
                                return;
                        }

                        ForwardRightFail:
                        triDoneBlocks.Clear();

                        // mesh and collide
                        Vector3[] triVerts = new Vector3[6]
                        {
                            (vertRotation.MultiplyPoint3x4(BlockTable.triVerts[1]) - localPos + rightLimit) * Scaling,
                            (vertRotation.MultiplyPoint3x4(BlockTable.triVerts[2]) - localPos + upLimit) * Scaling,
                            (vertRotation.MultiplyPoint3x4(BlockTable.triVerts[3]) - localPos + fwdLimit) * Scaling,
                            (vertRotation.MultiplyPoint3x4(BlockTable.triVerts[0]) - localPos + rightLimit) * Scaling,
                            (vertRotation.MultiplyPoint3x4(BlockTable.triVerts[0]) - localPos + upLimit) * Scaling,
                            (vertRotation.MultiplyPoint3x4(BlockTable.triVerts[0]) - localPos + fwdLimit) * Scaling,
                        };

                        MeshCollider TriCol;
                        // add collider
                        // if too many new colliders, start adding
                        if (nMeshColliders >= MeshColliders.Count)
                        {
                            GameObject colObject = new GameObject($"MeshCol {nMeshColliders}");
                            colObject.transform.parent = transform;
                            colObject.transform.localPosition = Vector3.zero;
                            colObject.transform.localRotation = Quaternion.identity;
                            colObject.layer = LayerMask.NameToLayer("Vehicle");

                            TriCol = colObject.AddComponent<MeshCollider>();
                            TriCol.convex = true;

                            MeshColliders.Add(TriCol);
                        }
                        else // reuse
                        {
                            TriCol = MeshColliders[nMeshColliders];
                        }

                        nMeshColliders++;

                        Mesh tcolMesh = new Mesh();
                        tcolMesh.vertices = triVerts; // slope side
                        tcolMesh.triangles = new int[]
                            {0, 1, 2, 3, 5, 4, 0, 3, 1, 1, 3, 4, 0, 2, 5, 0, 5, 3, 1, 4, 2, 4, 5, 2};

                        TriCol.sharedMesh = tcolMesh;

                        // finish meshing
                        if (Simplify)
                        {
                            for (int i = 0; i < tcolMesh.triangles.Length; i += 3)
                            {
                                vertices.Add(triVerts[tcolMesh.triangles[i]]);
                                vertices.Add(triVerts[tcolMesh.triangles[i + 1]]);
                                vertices.Add(triVerts[tcolMesh.triangles[i + 2]]);

                                triangles.AddRange(new int[] {vertIndex, vertIndex + 1, vertIndex + 2});

                                vertIndex += 3;
                            }
                        }
                        else // not simplified drawing all steps
                        {
                            // slopes 6
                            vertices.Add(triVerts[0]);
                            vertices.Add(triVerts[1]);
                            vertices.Add(triVerts[2]);
                            vertices.Add(triVerts[3]);
                            vertices.Add(triVerts[4]);
                            vertices.Add(triVerts[5]);

                            triangles.AddRange(new int[]
                                {vertIndex, vertIndex + 1, vertIndex + 2, vertIndex + 3, vertIndex + 5, vertIndex + 4});
                            vertIndex += 6;

                            // do steps
                            Vector3Int checkedPos = upLimit;
                            Vector3Int rightCheck = upLimit;
                            Vector3Int fwdCheck = upLimit;


                        }

                        break;

                    #endregion

                    #region Building Inverted Tri
                    case 3:
                        List<KeyValuePair<Vector3Int, bool>> invTriDoneBlocks =
                            new List<KeyValuePair<Vector3Int, bool>>();

                        Vector3Int UpLeftLimit = pair.Key;
                        Vector3Int UpBackLimit = pair.Key;
                        Vector3Int BottomLimit = pair.Key;

                        doneBlocks.Add(pair.Key, true);
                        int invfailsafe = 0;

                        // expand UpBackLeft
                        while (true)
                        {
                            // check everything
                            Vector3Int newBackLimit = UpBackLimit - forward + up;
                            Vector3Int newLeftLimit = UpLeftLimit - right + up;
                            Vector3Int checkedPos = newLeftLimit;

                            // sweep from up limit to down limit
                            bool inv = true;
                            while (checkedPos != newBackLimit + right)
                            {
                                if (!PBlockExistsInChunk(checkedPos) || IsBlockDestroyed(checkedPos) ||
                                    doneBlocks.ContainsKey(checkedPos))
                                    goto InvUpForwardFail;

                                BlockRepresentation checkedBlock = parentVehicle.GetBlockLocal(checkedPos);

                                if (inv)
                                {
                                    // not an inv. triangle when it should be one
                                    if (!CheckInvPropagation(pair.Value, checkedBlock, inv))
                                        goto InvUpForwardFail;

                                    // step forward to where triangle should be
                                    invTriDoneBlocks.Add(new KeyValuePair<Vector3Int, bool>(checkedPos, false));
                                    checkedPos += right;
                                }
                                else
                                {
                                    // not a triangle when it should be one
                                    if (!CheckInvPropagation(pair.Value, checkedBlock, inv))
                                        goto InvUpForwardFail;

                                    // step down where inv. triangle should be
                                    invTriDoneBlocks.Add(new KeyValuePair<Vector3Int, bool>(checkedPos, false));
                                    checkedPos -= forward;
                                }

                                inv = !inv;
                            }

                            UpLeftLimit = newLeftLimit;
                            UpBackLimit = newBackLimit;

                            doneBlocks.AddRange(invTriDoneBlocks.ToArray());
                            invTriDoneBlocks.Clear();

                            invfailsafe++;
                            if (invfailsafe > 10000)
                                return;
                        }

                        InvUpForwardFail:
                        invTriDoneBlocks.Clear();

                        invfailsafe = 0;

                        // expand ForwardLeftDown
                        while (true)
                        {
                            // check everything
                            Vector3Int newLeftLimit = UpLeftLimit + forward - right;
                            Vector3Int newDownLimit = BottomLimit + forward - up;
                            Vector3Int checkedPos = newLeftLimit;

                            // sweep from up limit to down limit
                            bool inv = true;
                            while (checkedPos != newDownLimit + right)
                            {
                                if (!PBlockExistsInChunk(checkedPos) || IsBlockDestroyed(checkedPos) ||
                                    doneBlocks.ContainsKey(checkedPos))
                                    goto InvUpRightFail;

                                BlockRepresentation checkedBlock = parentVehicle.GetBlockLocal(checkedPos);

                                if (inv)
                                {
                                    // not an inv. triangle when it should be one
                                    if (!CheckInvPropagation(pair.Value, checkedBlock, inv))
                                        goto InvUpRightFail;

                                    // step forward to where triangle should be
                                    invTriDoneBlocks.Add(new KeyValuePair<Vector3Int, bool>(checkedPos, false));
                                    checkedPos += right;
                                }
                                else
                                {
                                    // not a triangle when it should be one
                                    if (!CheckInvPropagation(pair.Value, checkedBlock, inv))
                                        goto InvUpRightFail;

                                    // step down where inv. triangle should be
                                    invTriDoneBlocks.Add(new KeyValuePair<Vector3Int, bool>(checkedPos, false));
                                    checkedPos -= up;
                                }

                                inv = !inv;
                            }

                            UpLeftLimit = newLeftLimit;
                            BottomLimit = newDownLimit;

                            doneBlocks.AddRange(invTriDoneBlocks.ToArray());
                            invTriDoneBlocks.Clear(); // idk why tris dont need this, this thing has a mind of its own

                            invfailsafe++;
                            if (invfailsafe > 10000)
                                return;
                        }

                        InvUpRightFail:
                        invTriDoneBlocks.Clear();

                        invfailsafe = 0;

                        // expand RightDownBack
                        while (true)
                        {
                            // check everything
                            Vector3Int newBottomLimit = BottomLimit + right - up;
                            Vector3Int newBackLimit = UpBackLimit + right - forward;
                            Vector3Int checkedPos = newBackLimit;

                            // sweep from up limit to down limit
                            bool inv = true;
                            while (checkedPos != newBottomLimit + forward)
                            {
                                if (PBlockExistsInChunk(checkedPos) || IsBlockDestroyed(checkedPos) ||
                                    doneBlocks.ContainsKey(checkedPos))
                                    goto InvForwardRightFail;

                                BlockRepresentation checkedBlock = parentVehicle.GetBlockLocal(checkedPos);

                                if (inv)
                                {
                                    // not an inv. triangle when it should be one
                                    if (!CheckInvPropagation(pair.Value, checkedBlock, inv))
                                        goto InvForwardRightFail;

                                    // step forward to where triangle should be
                                    invTriDoneBlocks.Add(new KeyValuePair<Vector3Int, bool>(checkedPos, false));
                                    checkedPos += forward;
                                }
                                else
                                {
                                    // not a triangle when it should be one
                                    if (!CheckInvPropagation(pair.Value, checkedBlock, inv))
                                        goto InvForwardRightFail;

                                    // step down where inv. triangle should be
                                    invTriDoneBlocks.Add(new KeyValuePair<Vector3Int, bool>(checkedPos, false));
                                    checkedPos -= up;
                                }

                                inv = !inv;
                            }

                            UpBackLimit = newBackLimit;
                            BottomLimit = newBottomLimit;

                            doneBlocks.AddRange(invTriDoneBlocks.ToArray());
                            invTriDoneBlocks.Clear();

                            invfailsafe++;
                            if (invfailsafe > 10000)
                                return;
                        }

                        InvForwardRightFail:
                        invTriDoneBlocks.Clear();

                        // mesh and collide
                        Vector3[] invVerts = new Vector3[9]
                        {
                            (vertRotation.MultiplyPoint3x4(BlockTable.invTriVerts[2]) - localPos + UpBackLimit) * Scaling,
                            (vertRotation.MultiplyPoint3x4(BlockTable.invTriVerts[6]) - localPos + UpLeftLimit) * Scaling,
                            (vertRotation.MultiplyPoint3x4(BlockTable.invTriVerts[5]) - localPos + BottomLimit) * Scaling,
                            // 3 and 4
                            (vertRotation.MultiplyPoint3x4(BlockTable.invTriVerts[1]) - localPos + UpBackLimit) * Scaling,
                            (vertRotation.MultiplyPoint3x4(BlockTable.invTriVerts[3]) - localPos + UpBackLimit) * Scaling,
                            // 5 and 6
                            (vertRotation.MultiplyPoint3x4(BlockTable.invTriVerts[3]) - localPos + UpLeftLimit) * Scaling,
                            (vertRotation.MultiplyPoint3x4(BlockTable.invTriVerts[4]) - localPos + UpLeftLimit) * Scaling,
                            // 7 and 8
                            (vertRotation.MultiplyPoint3x4(BlockTable.invTriVerts[4]) - localPos + BottomLimit) * Scaling,
                            (vertRotation.MultiplyPoint3x4(BlockTable.invTriVerts[1]) - localPos + BottomLimit) * Scaling,
                        };

                        Mesh invcolMesh = new Mesh();
                        invcolMesh.vertices = invVerts; // slope side
                        invcolMesh.triangles = new int[]
                        {
                            0, 1, 2, 0, 4, 5, 0, 5, 1, 1, 6, 7, 1, 7, 2, 2, 8, 3, 2, 3, 0, 0, 3, 4, 6, 1, 5, 2, 7, 8, 5,
                            4, 3, 5, 3, 6, 6, 3, 8, 6, 8, 7
                        };

                        MeshCollider InvCol;
                        // add collider
                        // if too many new colliders, start adding
                        if (nMeshColliders >= MeshColliders.Count)
                        {
                            GameObject colObject = new GameObject($"MeshCol {nMeshColliders}");
                            colObject.transform.parent = transform;
                            colObject.transform.localPosition = Vector3.zero;
                            colObject.transform.localRotation = Quaternion.identity;
                            colObject.layer = LayerMask.NameToLayer("Vehicle");

                            InvCol = colObject.AddComponent<MeshCollider>();
                            InvCol.convex = true;

                            MeshColliders.Add(InvCol);
                        }
                        else // reuse
                        {
                            InvCol = MeshColliders[nMeshColliders];
                        }

                        nMeshColliders++;

                        InvCol.sharedMesh = invcolMesh;

                        // finish meshing
                        if (Simplify)
                        {
                            for (int i = 0; i < invcolMesh.triangles.Length; i += 3)
                            {
                                vertices.Add(invVerts[invcolMesh.triangles[i]]);
                                vertices.Add(invVerts[invcolMesh.triangles[i + 1]]);
                                vertices.Add(invVerts[invcolMesh.triangles[i + 2]]);

                                triangles.AddRange(new int[] {vertIndex, vertIndex + 1, vertIndex + 2});

                                vertIndex += 3;
                            }

                            /* main slope
                            vertices.Add(invVerts[0]);
                            vertices.Add(invVerts[1]);
                            vertices.Add(invVerts[2]);

                            triangles.AddRange(new int[] { vertIndex, vertIndex + 1, vertIndex + 2 });
                            vertIndex += 3;
                            */
                        }
                        else
                        {
                            vertices.Add(invVerts[0]);
                            vertices.Add(invVerts[1]);
                            vertices.Add(invVerts[2]);

                            triangles.AddRange(new int[] {vertIndex, vertIndex + 1, vertIndex + 2});
                            vertIndex += 3;

                            // do steps

                        }

                        break;

                    #endregion
                }
            }

            // delete excess colliders
            while (nCubeColliders < CubeColliders.Count)
            {
                Destroy(CubeColliders[nCubeColliders]);
                CubeColliders.RemoveAt(nCubeColliders);
            }

            while (nMeshColliders < MeshColliders.Count)
            {
                Destroy(MeshColliders[nMeshColliders].gameObject);
                MeshColliders.RemoveAt(nMeshColliders);
            }

            COMPos = COMPos / MassTotal;
            MassTotal = MassTotal * Scaling;

            Mesh mesh = new Mesh();
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();

            meshFilter.mesh = mesh;

            vertices.Clear();
            triangles.Clear();

            mesh.name = gameObject.name;

            parentVehicle.UpdateCOM();

            if (DebugMessages)
                Debug.Log($"Update took {sw.ElapsedMilliseconds} ms.");

            sw.Stop();
        }

        public bool CubePropagate(int blockID, int side, Vector3Int lowBounds, Vector3Int highBounds, Dictionary<Vector3Int, bool> done)
        {
            switch (side)
            {
                case 0:  // sweep back
                    for (int x = lowBounds.x; x <= highBounds.x; x++)
                    {
                        for (int y = lowBounds.y; y <= highBounds.y; y++)
                        {
                            Vector3Int checkedBlock = new Vector3Int(x, y, lowBounds.z - 1);

                            if (checkedBlock.z < localPos.z)
                                return false;

                            if (parentVehicle.PerfectBlocks.ContainsKey(checkedBlock))
                            {
                                if (parentVehicle.PerfectBlocks[checkedBlock].blockID == blockID && !done.ContainsKey(checkedBlock) && !IsBlockDestroyed(checkedBlock))
                                    continue;
                            }

                            return false;
                        }
                    }
                    break;
                case 1: // sweep forward
                    for (int x = lowBounds.x; x <= highBounds.x; x++)
                    {
                        for (int y = lowBounds.y; y <= highBounds.y; y++)
                        {
                            Vector3Int checkedBlock = new Vector3Int(x, y, highBounds.z + 1);

                            if (checkedBlock.z > localPos.z + chunkSize-1)
                                return false;

                            if (parentVehicle.PerfectBlocks.ContainsKey(checkedBlock))
                            {
                                if (parentVehicle.PerfectBlocks[checkedBlock].blockID == blockID && !done.ContainsKey(checkedBlock) && !IsBlockDestroyed(checkedBlock))
                                    continue;
                            }

                            return false;
                        }
                    }
                    break;
                case 2: // sweep down
                    for (int x = lowBounds.x; x <= highBounds.x; x++)
                    {
                        for (int z = lowBounds.z; z <= highBounds.z; z++)
                        {
                            Vector3Int checkedBlock = new Vector3Int(x, lowBounds.y - 1, z);

                            if (checkedBlock.y < localPos.y)
                                return false;

                            if (parentVehicle.PerfectBlocks.ContainsKey(checkedBlock))
                            {
                                if (parentVehicle.PerfectBlocks[checkedBlock].blockID == blockID && !done.ContainsKey(checkedBlock) && !IsBlockDestroyed(checkedBlock))
                                    continue;
                            }

                            return false;
                        }
                    }
                    break;
                case 3: // sweep up
                    for (int x = lowBounds.x; x <= highBounds.x; x++)
                    {
                        for (int z = lowBounds.z; z <= highBounds.z; z++)
                        {
                            Vector3Int checkedBlock = new Vector3Int(x, highBounds.y + 1, z);

                            if (checkedBlock.y > localPos.y + chunkSize - 1)
                                return false;

                            if (parentVehicle.PerfectBlocks.ContainsKey(checkedBlock))
                            {
                                if (parentVehicle.PerfectBlocks[checkedBlock].blockID == blockID && !done.ContainsKey(checkedBlock) && !IsBlockDestroyed(checkedBlock))
                                    continue;
                            }

                            return false;
                        }
                    }
                    break;
                case 4: // sweep left? -x
                    for (int y = lowBounds.y; y <= highBounds.y; y++)
                    {
                        for (int z = lowBounds.z; z <= highBounds.z; z++)
                        {
                            Vector3Int checkedBlock = new Vector3Int(lowBounds.x - 1, y, z);

                            if (checkedBlock.x < localPos.x)
                                return false;

                            if (parentVehicle.PerfectBlocks.ContainsKey(checkedBlock))
                            {
                                if (parentVehicle.PerfectBlocks[checkedBlock].blockID == blockID && !done.ContainsKey(checkedBlock) && !IsBlockDestroyed(checkedBlock))
                                    continue;
                            }

                            return false;
                        }
                    }
                    break;
                default: // sweep right
                    for (int y = lowBounds.y; y <= highBounds.y; y++)
                    {
                        for (int z = lowBounds.z; z <= highBounds.z; z++)
                        {
                            Vector3Int checkedBlock = new Vector3Int(highBounds.x + 1, y, z);

                            if (checkedBlock.x > localPos.x + chunkSize - 1)
                                return false;

                            if (parentVehicle.PerfectBlocks.ContainsKey(checkedBlock))
                            {
                                if (parentVehicle.PerfectBlocks[checkedBlock].blockID == blockID && !done.ContainsKey(checkedBlock) && !IsBlockDestroyed(checkedBlock))
                                    continue;
                            }

                            return false;
                        }
                    }
                    break;
            }

            return true;
        }

        /// <summary>
        /// returns true on success
        /// </summary>
        public bool CheckSlopePropagation(BlockRepresentation ours, BlockRepresentation other)
        {
            if (other == null)
                return false;

            if (ours.blockID == other.blockID && BlockTable.FUMultMatch(new Vector2Int(ours.forward, ours.up), new Vector2Int(other.forward, other.up)))
                return true;

            return false;
        }

        public bool CheckTriPropagation(BlockRepresentation ours, BlockRepresentation theirs, bool inv)
        {
            return ( // has both same rotation
                BlockTable.FURMultMatch(
                new Vector3Int(ours.forward, ours.up, BlockTable.blockRight[ours.forward, ours.up]),
                new Vector3Int(ours.forward, ours.up, BlockTable.blockRight[ours.forward, ours.up])
                )
                && // and correct blockID
                inv ? ours.blockID + 1 == theirs.blockID : ours.blockID == theirs.blockID
                );
        }

        public bool CheckInvPropagation(BlockRepresentation ours, BlockRepresentation theirs, bool inv)
        {
            if (ours == null || theirs == null)
                return false;

            return ( // has both same rotation
                    BlockTable.FURMultMatch(
                        new Vector3Int(ours.forward, ours.up, BlockTable.blockRight[ours.forward, ours.up]),
                        new Vector3Int(ours.forward, ours.up, BlockTable.blockRight[ours.forward, ours.up])
                    )
                    && // and correct blockID
                    !inv ? ours.blockID - 1 == theirs.blockID : ours.blockID == theirs.blockID
                );
        }

        bool PBlockExistsInChunk(Vector3Int pos) // kinda inefficient
        {
            Bounds cbounds = new Bounds(localPos + Vector3.one * chunkSize/2 - Vector3.one / 2, Vector3.one * chunkSize - Vector3.one);

            return (parentVehicle.GetBlockLocal(pos) != null && cbounds.Contains(pos));
        }

        bool CheckBlockFaceKnown(BlockRepresentation ourBlock, int ourObstruction, int _dir, BlockRepresentation target)
        {
            if (target == null || ourBlock == null)
                return false;

            // if there's no block in that direction it's not obstructed (or its not a pblock)
            if (target.blockType == -1)
                return false;

            if (target.blockType == 0)
                return true;

            int theirSide = BlockSideFacingThisBlock(_dir, target.forward, target.up);

            int theirObstruction = BlockTable.obstructions[target.blockType, theirSide];

            //Debug.Log($"our Side: {ourSide}, their Side: {theirSide}");
            //Debug.Log($"ourObstruction: {ourObstruction} theirObstruction: {theirObstruction}");

            if (theirObstruction == 0)
                return true;

            if (ourObstruction > theirObstruction)
                return true;

            // if we're matching tris they need to be the same orientation to occlude
            if (ourObstruction == theirObstruction)
            {
                //Debug.Log($"{ourBlock} + {target}");
                return BlockTable.MatchingTriSide(ourBlock.blockType, target.blockType, ourBlock.forward, ourBlock.up, _dir, theirSide, target);
            }

            return false;
        }

        public byte BlockSideFacingThisBlock(int _side, sbyte _front, sbyte _top)
        { 
            if (_front == _side) // If the front is facing away from our block then the side we're facing is 0 (-Z)
                return 0;
            if (BlockTable.oppositeSide[_front] == _side) // If the front face is facing toward our block then the side we're facing is 1 (+Z)
                return 1;
            if (_top == _side) // If the top is facing the away from our block then the side we're facing is 2 (-Y)
                return 2;
            if (BlockTable.oppositeSide[_top] == _side) // If the top face is facing toward our block then the side we're facing is 3 (+Y)
                return 3;
            if (BlockTable.blockRight[_front, _top] == _side) // if the right face is facing away from our block, we're facing the left 4 (-X)
                return 4;

            // We went through all the other sides, then it's right (5 or +X).
            return 5;
        }
        #endregion

        #region damage
        public bool IsBlockDestroyed(Vector3Int pos)
        {
            if (!parentVehicle.damage.ContainsKey(pos))
                return false;

            BlockRepresentation block = parentVehicle.GetBlockLocal(pos);

            if (block == null || block.block.MaxHP <= parentVehicle.damage[pos])
                return true;

            return false;
        }
        #endregion

        #region editing and utility
        public void CheckForUpdateBorderChunk(Vector3Int position)
        {
            // this is horrible, and has the nearby mesh rebuild once for every nearby block. need to make this only run once.
            int onEdge = IsOnEdge(position);
            if (onEdge != -1)
            {
                Vector3Int chunkPos = Vector3Int.RoundToInt((Vector3) BlockTable.dirToVector[onEdge] * chunkSize * Scaling);

                //if (parentVehicle.Chunks.ContainsKey(chunkPos))
                //    parentVehicle.Chunks[chunkPos].RemeshAndRecollide();
            }
        }

        public void MassRemoveBlock(Vector3Int[] positions)
        {
            for (int i = 0; i < positions.Length; i++)
            {
                if (parentVehicle.PerfectBlocks.ContainsKey(positions[i]))
                {
                    parentVehicle.PerfectBlocks.Remove(positions[i]);
                    parentVehicle.Blocks.Remove(positions[i]);
                }
                else if (parentVehicle.Blocks.ContainsKey(positions[i]))
                {
                    parentVehicle.Blocks.Remove(positions[i]);

                    if (parentVehicle.GObjects.ContainsKey(positions[i]))
                    {
                        Destroy(parentVehicle.GObjects[positions[i]]);
                        parentVehicle.GObjects.Remove(positions[i]);
                    }
                }
                else
                {
                    Debug.Log($"Nothing at position {positions[i]}");
                    return;
                }
            }

            if (parentVehicle.Blocks.Count == 0)
            {
                parentVehicle.DestroyChunk(Vector3Int.RoundToInt(transform.localPosition / Scaling));
                return;
            }

            RemeshAndRecollide();
        }

        public int IsOnEdge(Vector3Int position)
        {
            if (position.x == 0)
                return 0;
            if (position.x == chunkSize)
                return 1;

            if (position.y == 0)
                return 2;
            if (position.y == chunkSize)
                return 3;

            if (position.z == 0)
                return 4;
            if (position.z == chunkSize)
                return 5;

            return -1;
        }

        public Vector3 OffsetAndScale(Vector3 position)
        {
            return (position - Vector3.one / 2) * Scaling;
        }
        #endregion

#if UNITY_EDITOR
        public void OnDrawGizmosSelected()
        {
            Vector3 cpos = localPos + Vector3.one * chunkSize;

            //Gizmos.color = Color.red;
            //Gizmos.DrawSphere(localPos, 0.2f);
            //Gizmos.color = Color.cyan;
            //Gizmos.DrawCube(cpos, Vector3.one * chunkSize*2);

            if (MeshColliders.Count > 0)
            {
                MeshCollider mCol = MeshColliders[0] as MeshCollider;

                foreach (Vector3 pos in mCol.sharedMesh.vertices)
                {
                    Gizmos.DrawWireSphere(transform.position + pos, 0.025f);
                }
            }

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.TransformPoint(COMPos), 0.2f);
        }
#endif
        #region backup
        /* Old meshing algorithm
        public void OldMeshing()
        {
            int vertIndex = 0;
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();

            // constructing cubes
            foreach (KeyValuePair<Vector3Int, BlockRepresentation> pair in blockDictionary.PerfectBlocks)
            {
                Vector2Int blockRot = new Vector2Int(pair.Value.forward, pair.Value.up);
                Matrix4x4 vertRotation = Matrix4x4.Rotate(BlockRotation.RotToQuat(blockRot));

                PerfectBlock thisBlock = GlobalBlockManager.GetBlockByID(pair.Value.blockID) as PerfectBlock;

                // block type (is it a slope, block, tri, etc?)
                switch (thisBlock.BlockType)
                {
                    #region Building a Cube
                    case 0:
                        for (int p = 0; p < 6; p++)
                        {
                            // yep, this cube is obstructed from that side, don't draw it
                            if (CheckBlockFace(pair.Key, vertRotation, p, thisBlock.BlockType, pair.Value.forward, pair.Value.up))
                                continue;

                            // not obstructed, draw it
                            for (int i = 0; i < 6; i++)
                            {
                                int triangleIndex = BlockTable.cubeTriangulation[p, i]; // what this really means is which vertices of the 8 to add

                                vertices.Add(((Vector3)pair.Key + BlockRotation.RotateBlockVertex(-Vector3.one / 2 + BlockTable.cubeVerts[triangleIndex], vertRotation)) * Scaling);
                                triangles.Add(vertIndex);

                                vertIndex++;
                            }
                        }

                        break;
                    #endregion

                    #region Building a Slope
                    case 1:
                        for (int p = 0; p < 6; p++)
                        {
                            // top face doesnt exist
                            if (p == 3)
                                continue;
                            // never obstruct slope face
                            if (p != 1)
                            {
                                if (CheckBlockFace(pair.Key, vertRotation, p, thisBlock.BlockType, pair.Value.forward, pair.Value.up))
                                    continue;
                            }

                            // not obstructed, draw it
                            for (int i = 0; i < 6; i++)
                            {
                                int triangleIndex = BlockTable.slopeTriangulation[p, i]; // what this really means is which vertices of the 8 to add

                                if (triangleIndex == -1)
                                    break;

                                vertices.Add(((Vector3)pair.Key + BlockRotation.RotateBlockVertex(-Vector3.one / 2 + BlockTable.slopeVerts[triangleIndex], vertRotation)) * Scaling);
                                triangles.Add(vertIndex);

                                vertIndex++;
                            }
                        }

                        break;

                    #endregion

                    #region building tri
                    case 2:
                        for (int p = 0; p < 5; p++)
                        {
                            // top face doesnt exist
                            if (p == 3)
                                continue;
                            // never obstruct slope face
                            if (p != 1)
                            {
                                if (CheckBlockFace(pair.Key, vertRotation, p, thisBlock.BlockType, pair.Value.forward, pair.Value.up))
                                    continue;
                            }

                            // not obstructed, draw it
                            for (int i = 0; i < 3; i++)
                            {
                                int triangleIndex = BlockTable.triTriangulation[p, i]; // what this really means is which vertices of the 8 to add

                                vertices.Add(((Vector3)pair.Key + BlockRotation.RotateBlockVertex(-Vector3.one / 2 + BlockTable.triVerts[triangleIndex], vertRotation)) * Scaling);
                                triangles.Add(vertIndex);

                                vertIndex++;
                            }
                        }

                        break;
                    #endregion

                    #region Building Inverted Tri
                    case 3:
                        for (int p = 0; p < 7; p++)
                        {
                            if (p != 6)
                                if (CheckBlockFace(pair.Key, vertRotation, p, thisBlock.BlockType, pair.Value.forward, pair.Value.up))
                                    continue;

                            // not obstructed, draw it
                            for (int i = 0; i < 6; i++)
                            {
                                int triangleIndex = BlockTable.invTriTriangulation[p, i]; // what this really means is which vertices of the 8 to add

                                if (triangleIndex == -1)
                                    break;

                                vertices.Add(((Vector3)pair.Key + BlockRotation.RotateBlockVertex(-Vector3.one / 2 + BlockTable.invTriVerts[triangleIndex], vertRotation)) * Scaling);
                                triangles.Add(vertIndex);

                                vertIndex++;
                            }
                        }

                        break;
                        #endregion
                }
            }

            Mesh mesh = new Mesh();
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();

            mesh.name = gameObject.name;

            meshFilter.mesh = mesh;
            meshCol.sharedMesh = mesh;
        }
        */
        #endregion
    }
}
