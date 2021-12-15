using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using VehicleBase.Damage;
using VehicleBase.Utility;
using VehicleBase.Vehicles.BlockBehaviors;
using VehicleBase.Vehicles.BlockBehaviors.Weapons;
using VehicleBase.Vehicles.Blocks;
using VehicleBase.Vehicles.Blocks.Management;
using VehicleBase.Vehicles.Chunks;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using Debug = UnityEngine.Debug;

namespace VehicleBase.Vehicles
{
    //[ExecuteInEditMode]
    /// <summary>
    /// MonoBehavior that holds all vehicle functions
    /// </summary>
    public class Vehicle : MonoBehaviour
    {
        #region stats
        private float enginePower;
        private float batteryPower;

        public string vehicleName = "NewVehicle";

        public VehiclePlayerData vpData = new VehiclePlayerData();

        public int SubvDepth { get { return subvDepth; } }
        private int subvDepth;

        public bool IsDebris;

        // for turret
        public bool IsTurret = false;

        private BoxCollider boundsCollider;
        public Vector3 PositiveBounds { get { return posBounds; } }
        public Vector3 NegativeBounds { get { return negBounds; } }
        [SerializeField] private Vector3 posBounds;
        [SerializeField] private Vector3 negBounds;

        public VehicleResourceMgr VResources { get { return vResources; } }
        private VehicleResourceMgr vResources;

        public Dictionary<Vector3Int, BlockRepresentation> Blocks = new Dictionary<Vector3Int, BlockRepresentation>();
        public Dictionary<Vector3Int, BlockRepresentation> PerfectBlocks = new Dictionary<Vector3Int, BlockRepresentation>();
        public Dictionary<Vector3Int, GameObject> GObjects = new Dictionary<Vector3Int, GameObject>();

        public Dictionary<Vector3Int, float> damage = new Dictionary<Vector3Int, float>();

        public List<IUpdateOnChange> updatingBlocks = new List<IUpdateOnChange>();
        //public List<IConfigurable> ConfigurablesData = new List<IConfigurable>();
        #endregion

        #region mobility
        private List<VehicleWheel> wheels = new List<VehicleWheel>();

        #endregion

        #region control
        private VehicleControlMode controlMode = VehicleControlMode.Helicopter;

        private IControlInput activeInput;
        // change this to accomodate AI
        public bool Active { get { return activeInput != null; } }

        public Vector3 TargetForward { get { return targetFwd; } }
        [SerializeField] private Vector3 targetFwd;
        [SerializeField] private Vector3 targetRot;
        [SerializeField] private Vector3 target;

        public Vector3 Input { get { return input; } }
        private Vector3 input;

        public float Throttle { get { return throttle; } }
        [SerializeField] private float throttle;

        public float rollPID { get { return PIDr.currentValue; } }
        public float pitchPID { get { return PIDp.currentValue; } }
        public float yawPID { get { return PIDy.currentValue; } }
        [SerializeField] protected PID.PID PIDr;
        [SerializeField] protected PID.PID PIDp;
        [SerializeField] protected PID.PID PIDy;

        [SerializeField]
        private bool levitating;
        #endregion

        #region armaments
        public List<FireGroup> FGs
        {
            get { return fireGroups; }
        }

        [SerializeField] private List<FireGroup> fireGroups = new List<FireGroup>();
        #endregion

        #region meshing
        public Vehicle masterVehicle;
        public Vehicle parentVehicle;
        public List<Vehicle> Subvehicles = new List<Vehicle>();

        private Dictionary<Vector3Int, VehicleChunk> Chunks = new Dictionary<Vector3Int, VehicleChunk>();
        private List<Vector3Int> chunkUpdateBuffer = new List<Vector3Int>();

        // turret and spin block functionality
        public bool IsSubvehicle = false;
        public List<SubObjectComponent> attachmentPoints = new List<SubObjectComponent>();

        [SerializeField] private Transform chunkParent;

        public Rigidbody RB
        {
            get { return rb; }
        }
        [SerializeField] private Rigidbody rb;

        private Vector3 com;
        private float mass;
        public float Mass
        {
            get { return mass; }
        }

        public float Scale {
            get { return vehicleScale; }
        }
        public int ChunkSize
        {
            get { return chunkSize; }
        }

        [SerializeField] private float vehicleScale = 0.5f;
        [SerializeField] private int chunkSize = 10;
        #endregion

        // for some reason this has to be OnEnable for vehicle loading to work
        void OnEnable()
        {
            if (transform.name.Contains("REPLAY"))
            {
                this.enabled = false;
                vResources.enabled = false;



                return;
            }

            gameObject.layer = LayerMask.NameToLayer("VehicleBounds");
            boundsCollider = transform.AddComponent<BoxCollider>();

            if (attachmentPoints.Count > 0)
            {
                if (attachmentPoints[0].GetType() == typeof(TurretRing))
                    IsTurret = true;
                if (attachmentPoints[0] as IUpdateOnChange != null)
                    updatingBlocks.Add(attachmentPoints[0] as IUpdateOnChange);
            }

            subvDepth = 0;

            masterVehicle = this;
            parentVehicle = TransformTools.GetParentVehicleECT(transform);

            if (parentVehicle != null)
                IsSubvehicle = true;

            if (IsSubvehicle)
            {
                //Destroy(transform.GetComponent<Rigidbody>());

                masterVehicle = TransformTools.GetMasterVehicle(this);
                
                /*GameObject rbOb = new GameObject("MassObject");
                rbOb.transform.parent = transform;
                rbOb.transform.localPosition = Vector3.zero;
                rbOb.transform.localRotation = Quaternion.identity;
                */
                rb = transform.GetComponent<Rigidbody>();
                //rb.isKinematic = true;
                
                transform.parent = null;

                vehicleScale = parentVehicle.vehicleScale;

                // (!parentVehicle.IsSubvehicle)
                transform.localScale = Vector3.one;

                fireGroups = masterVehicle.FGs;

                vResources = transform.GetComponent<VehicleResourceMgr>();

                subvDepth = parentVehicle.SubvDepth + 1;
            }
            else
            {
                for (int i = 0; i < 10; i++)
                {
                    fireGroups.Add(new FireGroup());
                }

                vResources = transform.GetComponent<VehicleResourceMgr>();
            }

            chunkParent = new GameObject("Chunk Parent").transform;
            chunkParent.parent = transform;
            chunkParent.gameObject.layer = LayerMask.NameToLayer("Vehicle");
            chunkParent.transform.localPosition = Vector3.zero;
            chunkParent.transform.localRotation = Quaternion.identity;

            //HASPARENT:
            if (!Chunks.ContainsKey(Vector3Int.zero))
            {
                GameObject firstChunk = new GameObject("0,0,0");
                firstChunk.transform.parent = chunkParent;
                firstChunk.transform.localPosition = Vector3.zero;
                firstChunk.transform.localRotation = Quaternion.identity;

                Chunks.Add(Vector3Int.zero, firstChunk.AddComponent<VehicleChunk>());
            }

            Chunks[Vector3Int.zero].transform.parent = chunkParent;

            UpdateCOM();

            StartCoroutine(finishStart());
        }

        IEnumerator finishStart()
        {
            yield return new WaitForSeconds(0.5f);

            if (!levitating && transform.name != "REPLAY")
                rb.isKinematic = false;

            if (transform.name == "REPLAY")
            {
                this.enabled = false;
                vResources.enabled = false;

            }

            UpdateCOM();
        }

        private void Update()
        {
            if (activeInput != null)
            {
                target = activeInput.target;
                input = new Vector3(activeInput.ad, activeInput.ws, activeInput.qe);
                //throttle = activeInput.rf;

                fireGroups[0].SetTarget(target);

                targetFwd = (target - rb.centerOfMass).normalized;
                targetRot = Quaternion.LookRotation(target - rb.worldCenterOfMass, Vector3.up).eulerAngles;

                InterpretControls();
            }

            if (levitating && !IsSubvehicle)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, Vector3.up), Vector3.up), 0.1f);

                for (int i = 0; i < fireGroups.Count; i++)
                {
                    fireGroups[0].SetTarget(Vector3.zero);
                }
            }
        }

        private void FixedUpdate()
        {
            //rb.transform.position = transform.position;
            //rb.transform.rotation = transform.rotation;
        }

        private void LateUpdate()
        {
            UpdateBufferedChunks();
        }

        private void InterpretControls()
        {
            switch (controlMode)
            {
                case VehicleControlMode.Helicopter:
                    PIDr.Evaluate(AngleConv.Conv360to180((360 - transform.localEulerAngles.z) - input.x * 10), 0);
                    PIDp.Evaluate(AngleConv.Conv360to180((360 - transform.localEulerAngles.x) + targetRot.x), 0);
                    PIDy.Evaluate(AngleConv.Conv360to180((360 - transform.localEulerAngles.y) + targetRot.y), 0);

                    throttle = Mathf.Clamp01(throttle + activeInput.rf * Time.deltaTime);
                    break;
                case VehicleControlMode.Plane:

                    break;
                case VehicleControlMode.Ship:

                    break;

                default:
                    
                    break;
            }
        }

        #region vehicle functions

        #endregion

        // everything below this is for building and making the vehicle exist =======================
        #region interaction
        public void ActivateInput(IControlInput seat)
        {
            if (activeInput == null)
                activeInput = seat;
        }

        public void DeactivateInput(IControlInput seat)
        {
            if (activeInput == seat)
                activeInput = null;
        }
        #endregion

        #region building
        public void UpdateBoundBox()
        {
            boundsCollider.center = negBounds + (posBounds - negBounds) / 2;
            boundsCollider.size = (posBounds - negBounds);
        }

        public Vector3Int GetLocalPos(Vector3 _position)
        {
            return Vector3Int.RoundToInt(transform.InverseTransformPoint(_position) / vehicleScale);
        }

        public void AddBlock(Block _block, Vector3 _position, sbyte fwd, sbyte up)
        {
            Vector3Int position = Vector3Int.RoundToInt(transform.InverseTransformPoint(_position)/vehicleScale);
            AddBlockLocal(_block, position, fwd, up, true);
        }

        public bool AddBlockLocal(Block _block, Vector3Int position, sbyte forward, sbyte up, bool update)
        {
            Vector3Int chunkPos = Vector3Int.FloorToInt((Vector3)position / chunkSize) * chunkSize;

            if (_block == null || parentVehicle != null && parentVehicle.IsDebris)
                return false;

            if (!Chunks.ContainsKey(chunkPos))
            {
                GameObject newChunk = new GameObject($"{chunkPos}");
                newChunk.transform.parent = chunkParent;
                newChunk.transform.localPosition = (Vector3)chunkPos * vehicleScale;
                newChunk.transform.localRotation = Quaternion.identity;
                newChunk.transform.localScale = Vector3.one;

                newChunk.layer = LayerMask.NameToLayer("Vehicle");

                Chunks.Add(chunkPos, newChunk.AddComponent<VehicleChunk>());
            }

            if (Blocks.ContainsKey(position))
            {
                Debug.LogError($"BLOCK ALREADY EXISTS {position}, ROUNDED {Vector3Int.FloorToInt(position)} ON VEHICLE {parentVehicle.name}, CHUNK {gameObject.name}");
                return false;
            }

            BlockRepresentation orientation = new BlockRepresentation(forward, up, _block.blockID, -1, _block);

            //need to add iweapon to firegroup

            // check if its a perfect block
            PerfectBlock pblock = _block as PerfectBlock;
            if (pblock != null)
            {
                PerfectBlocks.Add(position, orientation);

                orientation.blockType = pblock.BlockType;
            }

            // check if its a GOBlock
            GOBlock goBlock = _block as GOBlock;
            if (goBlock != null)
            {
                GameObject thisBlock = (GameObject)Object.Instantiate(
                    Resources.Load(goBlock.ObjectName), 
                    (Vector3)position * vehicleScale, 
                    Quaternion.LookRotation(BlockTable.dirToVector[forward], BlockTable.dirToVector[up]), transform
                    );

                thisBlock.transform.localPosition = (Vector3)position * vehicleScale;

                if (goBlock.Scaleable)
                    thisBlock.transform.localScale = Vector3.one * vehicleScale;

                thisBlock.transform.localRotation = Quaternion.LookRotation(BlockTable.dirToVector[forward], BlockTable.dirToVector[up]);

                GObjects.Add(position, thisBlock);

                // fetch attached block script and distribute to needed storages
                GOBehavior attachedScript = thisBlock.GetComponent<GOBehavior>();

                attachedScript.SetOrientation(position, forward, up);

                // spawn phantom pointers for this multiblock
                foreach (Vector3Int pos in attachedScript.BlockLinks)
                {
                    Vector3Int transformedPos = pos.x * BlockTable.dirToVector[BlockTable.blockLeft[forward, up]] +
                                                pos.y * BlockTable.dirToVector[up] +
                                                pos.z * BlockTable.dirToVector[forward];

                    //Debug.LogWarning($"position {position + transformedPos + localPos} offset x: {-transformedPos.x}, y: {-transformedPos.y}, z: {-transformedPos.z}");

                    BlockRepresentation phantom = new BlockRepresentation((sbyte)(-transformedPos.z), (sbyte)(-transformedPos.y), -2, (-transformedPos.x), null);
                    
                    AddBlockLink(phantom, transformedPos + position);
                }
            }

            Blocks.Add(position, orientation);

            if (update)
                Chunks[chunkPos].RemeshAndRecollide();

            return true;
        }

        public void AddBlockLink(BlockRepresentation br, Vector3Int pos)
        {
            Vector3Int chunkPos = Vector3Int.FloorToInt((Vector3)pos / chunkSize) * chunkSize;

            if (!Chunks.ContainsKey(chunkPos))
            {
                GameObject newChunk = new GameObject($"{chunkPos}");
                newChunk.transform.parent = chunkParent;
                newChunk.transform.localPosition = (Vector3)chunkPos * vehicleScale;
                newChunk.transform.localRotation = Quaternion.identity;
                newChunk.transform.localScale = Vector3.one;

                newChunk.layer = LayerMask.NameToLayer("Vehicle");

                Chunks.Add(chunkPos, newChunk.AddComponent<VehicleChunk>());
            }

            if (Blocks.ContainsKey(pos - chunkPos))
            {
                Debug.LogWarning($"Link space occupied {chunkPos}, {pos-chunkPos}");
                return;
            }
            
            Blocks.Add(pos, br);
        }

        // pos is relative (i.e. chunk {cSize, cSize, cSize} would be {1, 1, 1} )
        public void UpdateChunk(Vector3Int pos)
        {
            pos = pos * chunkSize;

            if (Chunks.ContainsKey(pos))
                Chunks[pos].RemeshAndRecollide();
        }

        public void UpdateBufferedChunks() // for buffering damage
        {
            if (chunkUpdateBuffer.Count > 0)
            {
                CheckConnections(true);

                for (int i = 0; i < chunkUpdateBuffer.Count; i++)
                {
                    UpdateChunk(chunkUpdateBuffer[i]);
                }

                chunkUpdateBuffer.Clear();
            }
        }

        public void UpdateAllChunks()
        {
            foreach (KeyValuePair<Vector3Int, VehicleChunk> chunk in Chunks)
            {
                chunk.Value.RemeshAndRecollide();
            }
        }

        // this is really scuffed
        public KeyValuePair<Vector3Int, float>[] GetBlocks()
        {
            // get every position from every chunk
            List<KeyValuePair<Vector3Int, float>> positions = new List<KeyValuePair<Vector3Int, float>>();

                foreach (Vector3Int key in Blocks.Keys)
                {
                    positions.Add(new KeyValuePair<Vector3Int, float>(key, Blocks[key].block.Mass));
                }

                /* weight vehicle connection to require being connected to main object
                for (int i = 0; i < attachmentPoints.Count; i++)
                {
                    for (int c = 0; c < attachmentPoints[i].connectionPos.Length; c++)
                    {
                        positions.Add(new KeyValuePair<Vector3Int, float>(attachmentPoints[i].connectionPos[c], 9999999));
                    }
                }*/

            return positions.ToArray();
        }

        public void RemoveBlock(Vector3 _position)
        {
            Vector3Int position = Vector3Int.RoundToInt(transform.InverseTransformPoint(_position)/vehicleScale);

            RemoveBlockLocal(position, true);
        }

        public void RemoveBlockLocal(Vector3Int position, bool update)
        {
            Vector3Int chunkPos = Vector3Int.FloorToInt((Vector3)position / chunkSize) * chunkSize;

            if (masterVehicle.IsDebris)
                return;

            // dont remove subobject phantoms
            if (Blocks.ContainsKey(position) && Blocks[position].blockID < 0)
                return;

            if (PerfectBlocks.ContainsKey(position))
            {
                PerfectBlocks.Remove(position);
                Blocks.Remove(position);
            }
            else if (Blocks.ContainsKey(position))
            {
                if (GObjects.ContainsKey(position))
                {
                    // fetch attached block script and distribute to needed storages
                    GOBehavior attachedScript = GObjects[position].GetComponent<GOBehavior>();

                    // remove phantom pointers for this multiblock
                    foreach (Vector3Int pos in attachedScript.BlockLinks)
                    {
                        Vector3Int transformedPos = pos.x * BlockTable.dirToVector[BlockTable.blockLeft[attachedScript.fwd, attachedScript.up]] +
                                                    pos.y * BlockTable.dirToVector[attachedScript.up] +
                                                    pos.z * BlockTable.dirToVector[attachedScript.fwd];

                        //Debug.Log($"x: {position.x + transformedPos.x}, y: {position.y + transformedPos.y}, z: {position.z + transformedPos.z}");

                        Blocks.Remove(position + transformedPos);
                    }

                    Destroy(GObjects[position]);

                    GObjects.Remove(position);
                }

                Blocks.Remove(position);
            }
            else
            {
                Debug.Log($"Nothing at position {position}");
                return;
            }

            if (Blocks.Count == 0)
            {
                Destroy(gameObject);
                return;
            }

            if (update)
            {
                Chunks[chunkPos].RemeshAndRecollide();
                CheckConnections(false);
            }
        }

        public void MassRemoveBlock(Vector3Int[] positions, bool damaged)
        {
            List<Vector3Int> updatedChunks = new List<Vector3Int>();

            // remove blocks from chunks
            for (int i = 0; i < positions.Length; i++)
            {
                Vector3Int chunkPos = Vector3Int.FloorToInt((Vector3)positions[i] / chunkSize);

                if (!updatedChunks.Contains(chunkPos))
                    updatedChunks.Add(chunkPos);

                if (damaged && GetBlockLocal(positions[i]).block != null)
                {
                    if (damage.ContainsKey(positions[i]))
                        damage[positions[i]] = GetBlockLocal(positions[i]).block.MaxHP + 1;
                    else
                        damage.Add(positions[i], GetBlockLocal(positions[i]).block.MaxHP + 1);
                }
                else
                    RemoveBlockLocal(positions[i], false);
            }

            // update chunks that were affected
            for (int i = 0; i < updatedChunks.Count; i++)
            {
                if (!chunkUpdateBuffer.Contains(updatedChunks[i]))
                    chunkUpdateBuffer.Add(updatedChunks[i]);
            }
        }

        public void Levitate()
        {
            masterVehicle.levitating = !masterVehicle.levitating;

            masterVehicle.LevitateSubvehicles();
            masterVehicle.rb.isKinematic = masterVehicle.levitating;
        }

        public void LevitateSubvehicles()
        {
            levitating = masterVehicle.levitating;

            //rb.isKinematic = levitating;

            foreach (Vehicle sub in Subvehicles)
            {
                sub.LevitateSubvehicles();
            }
        }

        public BlockRepresentation GetBlock(Vector3 _position)
        {
            Vector3Int position = Vector3Int.RoundToInt(transform.InverseTransformPoint(_position) / vehicleScale);

            if (Blocks.ContainsKey(position))
                return Blocks[position];

            return null;
        }

        public void DestroyChunk(Vector3Int pos)
        {
            Destroy(Chunks[pos].gameObject);
            Chunks.Remove(pos);

            if (Chunks.Count == 0)
                Destroy(gameObject);
        }
        #endregion

        #region special building
        public void SetScale(float scale) // to be used on spawning
        {
            vehicleScale = scale;
        }
        // jic fire group doesnt exist yet
        public void AddWeaponToFireGroup(IWeapon weapon, int fg)
        {
            fireGroups[fg].AddWeapon(weapon);
        }

        public void RemoveWeaponFromFireGroup(IWeapon weapon, int fg)
        {
            fireGroups[fg].RemoveWeapon(weapon);
        }

        public void RemoveFromAllFireGroups(IWeapon weapon)
        {
            for (int i = 0; i < fireGroups.Count; i++)
            {
                fireGroups[i].RemoveWeapon(weapon);
            }
        }

        // for vehicle functionality, NOT For resources
        public void ReadInterfacedBehavior(MonoBehaviour component, bool added)
        {
            masterVehicle.vResources.ReadInterfacedBehavior(component, added);

            IUpdateOnChange updateable = component as IUpdateOnChange;
            IWeapon weapon = component as IWeapon;

            if (added)
            {
                if (weapon != null)
                    masterVehicle.AddWeaponToFireGroup(weapon, 0);
                if (updateable != null)
                    updatingBlocks.Add(updateable);
            }
            else
            {
                if (weapon != null)
                    masterVehicle.RemoveFromAllFireGroups(weapon);
                if (updateable != null)
                    updatingBlocks.Remove(updateable);
            }
        }

        public void UpdateUpdateables()
        {
            for (int i = 0; i < updatingBlocks.Count; i++)
            {
                updatingBlocks[i].Updated();
            }
        }
        #endregion

        #region fire control
        public void SetFireGroupTarget(int fg, Vector3 target)
        {
            fireGroups[fg].SetTarget(target);
        }

        public void FireFireGroup(int fg)
        {
            fireGroups[fg].Fire();
        }
        #endregion

        #region data acquisition
        public void SetMirrorData(bool x, bool y, bool z, Vector3 cent)
        {
            vpData.xMirror = x;
            vpData.yMirror = y;
            vpData.zMirror = z;
            vpData.mirrorPos = cent;
        }

        public void SetCameraData(Vector3 focus, float separation)
        {
            vpData.cameraOffset = focus;
            vpData.cameraDistance = separation;
        }

        public BlockRepresentation GetBlockLocal(Vector3Int pos)
        {
            if (Blocks.ContainsKey(pos))
                return Blocks[pos];

            return null;
        }

        public BlockRepresentation GetBlockLocalWithDamage(Vector3Int pos, out float dmg)
        {
            Vector3Int damageGetPos = pos;
            dmg = 0;

            if (!Blocks.ContainsKey(damageGetPos))
                return null;

            if (Blocks[damageGetPos].blockID == -2)
                damageGetPos = GetLinkBlockActual(pos, Blocks[pos]);

            if (damage.ContainsKey(damageGetPos))
                dmg = damage[damageGetPos];

            return Blocks[damageGetPos];
        }

        public GOBehavior GetGOBehaviorLocal(Vector3Int pos)
        {
            if (GObjects.ContainsKey(pos))
                return GObjects[pos].GetComponent<GOBehavior>();

            return null;
        }

        public static Vector3Int LocalToVehicleGridOffset(Vector3Int offset, int fwd, int up)
        {
            return offset.z * BlockTable.dirToVector[fwd] + 
                   offset.y * BlockTable.dirToVector[up] +
                   offset.x * BlockTable.dirToVector[BlockTable.blockRight[fwd, up]];
        }

        public Vector3Int GetLinkBlockActual(Vector3Int pos, BlockRepresentation block)
        {
            if (block == null)
            {
                Debug.LogError($"{pos} block reference is null!");
                return Vector3Int.zero;
            }

            return new Vector3Int(block.blockType + pos.x, block.up + pos.y, block.forward + pos.z);
        }
        #endregion

        #region connection checks
        // this is horribly done
        public void CheckConnections(bool damaged)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            List<List<Vector3Int>> floodAreas = new List<List<Vector3Int>>();
            List<float> areaMasses = new List<float>();
            Queue blockQueue = new Queue();

            // add every single block pos to a list
            Dictionary<Vector3Int, FloodFillBlock> blockPositions = new Dictionary<Vector3Int, FloodFillBlock>();
            Dictionary<Vector3Int, List<Vector3Int>> blockLinks = new Dictionary<Vector3Int, List<Vector3Int>>();

            foreach (Vector3Int key in Blocks.Keys)
            {
                // add complex block connections
                if (Blocks[key].blockID == -2)
                {
                    Vector3Int linkPos = GetLinkBlockActual(key, Blocks[key]);

                    if (blockLinks.ContainsKey(linkPos))
                    {
                        blockLinks[linkPos].Add(key);
                    }
                    else
                    {
                        blockLinks.Add(linkPos, new List<Vector3Int>());
                        blockLinks[linkPos].Add(key);
                    }

                    blockPositions.Add(key, new FloodFillBlock(true, 0));
                }
                else
                {
                    blockPositions.Add(key, new FloodFillBlock(false, Blocks[key].block.Mass));
                }
            }

            int area = 0;

            int failsafe = 0;

            while (blockPositions.Count > 0)
            {
                // start reading
                Vector3Int first = blockPositions.First().Key;

                blockQueue.Enqueue(first);
                floodAreas.Add(new List<Vector3Int>());
                areaMasses.Add(0);

                floodAreas[area].Add(first);
                areaMasses[area] += blockPositions[first].mass;
                blockPositions.Remove(first);

                // flood current area
                while (blockQueue.Count > 0)
                {
                    Vector3Int pos = (Vector3Int) blockQueue.Dequeue();

                    // get links, delete the entry once its been linked 

                    // check all 6 directions
                    for (int i = 0; i < 6; i++)
                    {
                        Vector3Int checkPos = pos + BlockTable.dirToVector[i];

                        // flood fill
                        if (damaged)
                        {
                            if (blockPositions.ContainsKey(checkPos))
                            {
                                if (blockPositions[checkPos].linked) // is a link?
                                {
                                    Vector3Int actual = GetLinkBlockActual(checkPos, GetBlockLocal(checkPos));

                                    if (blockLinks.ContainsKey(actual) && !IsBlockDestroyed(actual))
                                    {
                                        foreach (Vector3Int bl in blockLinks[actual]) // yeah this nesting is kinda nasty but it will do for now
                                        {
                                            blockQueue.Enqueue(bl);
                                            floodAreas[area].Add(bl);
                                            blockPositions.Remove(bl);
                                        }

                                        blockQueue.Enqueue(actual);
                                        floodAreas[area].Add(actual);
                                        areaMasses[area] += blockPositions[actual].mass;
                                        blockLinks.Remove(actual);
                                    }
                                }
                                else if (!IsBlockDestroyed(checkPos))
                                {
                                    if (blockLinks.ContainsKey(checkPos))
                                    {
                                        foreach (Vector3Int bl in blockLinks[checkPos])
                                        {
                                            blockQueue.Enqueue(bl);
                                            floodAreas[area].Add(bl);
                                            blockPositions.Remove(bl);
                                        }
                                    }

                                    floodAreas[area].Add(checkPos);
                                    areaMasses[area] += blockPositions[checkPos].mass;
                                    blockPositions.Remove(checkPos);
                                    blockQueue.Enqueue(checkPos);
                                }
                            }
                        }
                        else
                        {
                            if (blockPositions.ContainsKey(checkPos))
                            {
                                if (blockPositions[checkPos].linked) // is a link?
                                {
                                    Vector3Int actual = GetLinkBlockActual(checkPos, GetBlockLocal(checkPos));

                                    if (blockLinks.ContainsKey(actual))
                                    {
                                        foreach (Vector3Int bl in blockLinks[actual]) // yeah this nesting is kinda nasty but it will do for now
                                        {
                                            blockQueue.Enqueue(bl);
                                            floodAreas[area].Add(bl);
                                            blockPositions.Remove(bl);
                                        }

                                        blockQueue.Enqueue(actual);
                                        floodAreas[area].Add(actual);
                                        areaMasses[area] += blockPositions[actual].mass;
                                        blockLinks.Remove(actual);
                                    }
                                }
                                else 
                                {
                                    if (blockLinks.ContainsKey(checkPos)) // is the origin of the link?
                                        foreach (Vector3Int bl in blockLinks[checkPos])
                                        {
                                            blockQueue.Enqueue(bl);
                                            floodAreas[area].Add(bl);
                                            blockPositions.Remove(bl);
                                        }

                                    floodAreas[area].Add(checkPos);
                                    areaMasses[area] += blockPositions[checkPos].mass;
                                    blockPositions.Remove(checkPos);
                                    blockQueue.Enqueue(checkPos);
                                }
                            }
                        }
                    }

                    if (failsafe > 100000)
                    {
                        Debug.LogError("ERROR FLOOD FILLING AREA: VOLUME TIMEOUT");
                        return;
                    }

                    //blockPositions.Remove(pos);

                    failsafe++;
                }
                area++;
            }

            // compare areas
            int highestIndex = 0;
            for (int i = 0; i < floodAreas.Count; i++)
            {
                if (areaMasses[i] > areaMasses[highestIndex])
                {
                    highestIndex = i;
                }
            }

            // delete smaller unconnected areas
            for (int i = 0; i < floodAreas.Count; i++)
            {
                // if area connected to main vehicle keep it
                if (areaMasses[i] > 999999)
                    continue;

                if (i == highestIndex)
                    continue;

                MassRemoveBlock(floodAreas[i].ToArray(), damaged);
            }

            floodAreas.Clear();
            areaMasses.Clear();

            Debug.Log($"Flood filling {name} finished in {sw.ElapsedMilliseconds} ms.");
            sw.Stop();
        }

        private struct FloodFillBlock
        {
            public bool linked { get; private set; }
            public float mass { get; private set; }

            public FloodFillBlock(bool _link, float _mass)
            {
                linked = _link;
                mass = _mass;
            }
        }
        
        /// <summary>
        /// made for either deleting or turning component into debris upon disconnect
        /// </summary>
        /// <param name="Destroyed">whether the component was disabled via damage or deleted through build mode</param>
        public void MainObjectDisconnect(bool Destroyed)
        {
            if (!IsSubvehicle) // this shouldnt happen but jic
            { Debug.LogError($"Something is calling MODisconnect on {vehicleName} when it shouldn't be."); return; }

            if (Destroyed)
            {
                TurnVehicleToDerelict();
            }
            else
            {
                if (attachmentPoints.Count == 0)
                {
                    if (this != null) // editor for some reason throws errors if this doesnt exist
                        Destroy(gameObject);
                }
            }
        }
        #endregion

        #region Destruction and Damage
        public void TakeDamage(DmgAndPos[] dmg, bool update)
        {
            if (dmg == null)
                return;

            for (int i = 0; i < dmg.Length; i++)
            {
                if (damage.ContainsKey(dmg[i].pos))
                    damage[dmg[i].pos] += dmg[i].dmg;
                else
                    damage.Add(dmg[i].pos, dmg[i].dmg);

                if (Blocks.ContainsKey(dmg[i].pos) && Blocks[dmg[i].pos].block != null)
                    if (damage[dmg[i].pos] >= Blocks[dmg[i].pos].block.MaxHP) // log chunk update
                    {
                        Vector3Int cPos = Vector3Int.FloorToInt(dmg[i].pos / chunkSize);

                        if (update)
                            UpdateChunk(cPos);
                        else if (!chunkUpdateBuffer.Contains(cPos))
                            chunkUpdateBuffer.Add(cPos);
                    }
            }
        }

        public bool IsBlockDestroyed(Vector3Int pos)
        {
            if (!damage.ContainsKey(pos))
                return false;

            if (Blocks[pos] == null || Blocks[pos].block.MaxHP < damage[pos])
                return true;

            return false;
        }

        // disconnected sections are turned into debris. I'll need to set the materials to the weathered variant too
        public void TurnSectionToDebris(List<Vector3Int> section)
        {

        }

        // completely destroy vehicle
        public void TurnVehicleToDerelict()
        {
            foreach (KeyValuePair<Vector3Int, BlockRepresentation> block in Blocks)
            {
                if (damage.ContainsKey(block.Key))
                    damage[block.Key] = block.Value.block.MaxHP + 1;
                else
                    damage.Add(block.Key, block.Value.block.MaxHP + 1);
            }

            UpdateAllChunks();
        }
        #endregion

        public void UpdateCOM()
        {
            Vector3 runningPos = Vector3.zero;
            mass = 1;

            negBounds = Chunks.Keys.First();
            posBounds = Chunks.Keys.First() + Vector3.one * chunkSize;

            foreach (KeyValuePair<Vector3Int, VehicleChunk> Chunk in Chunks)
            {
                //Debug.Log((Chunk.Value.COMPos + Chunk.Value.transform.localPosition));
                runningPos += (Chunk.Value.COMPos + Chunk.Value.transform.localPosition) * Chunk.Value.MassTotal;
                mass += Chunk.Value.MassTotal;

                if (Chunk.Key.x > posBounds.x) // probably a better way to do this....
                    posBounds.x = Chunk.Key.x + chunkSize;
                else if (Chunk.Key.x < negBounds.x) 
                    negBounds.x = Chunk.Key.x;

                if (Chunk.Key.y > posBounds.y)
                    posBounds.y = Chunk.Key.y + chunkSize;
                else if (Chunk.Key.y < negBounds.y)
                    negBounds.y = Chunk.Key.y;

                if (Chunk.Key.z > posBounds.z)
                    posBounds.z = Chunk.Key.z + chunkSize;
                else if (Chunk.Key.z < negBounds.z)
                    negBounds.z = Chunk.Key.z;
            }

            negBounds = negBounds * Scale;
            posBounds = posBounds * Scale;

            UpdateBoundBox();

            runningPos = runningPos / mass;

            com = runningPos;

            rb.mass = mass;
            rb.centerOfMass = com;

            UpdateUpdateables();
        }

#if UNITY_EDITOR
        public void OnDrawGizmosSelected()
        {
            
            foreach (KeyValuePair<Vector3Int, float> dmg in damage)
            {
                Gizmos.color = new Color(dmg.Value, 0, 0);
                Gizmos.DrawCube(transform.position + (Vector3)dmg.Key*Scale, Vector3.one * Scale);
            }
            
            Gizmos.color = Color.yellow;

            if (rb != null)
                Gizmos.DrawSphere(transform.TransformPoint(IsSubvehicle ? com : rb.centerOfMass), 0.4f);
        }
#endif
    }

    [System.Serializable]
    public class VehiclePlayerData
    {
        public Vector3 cameraOffset;
        public float cameraDistance;
        public bool xMirror;
        public bool yMirror;
        public bool zMirror;
        public Vector3 mirrorPos;
    }

    public enum VehicleControlMode
    {
        CarOrTank,
        Plane,
        Helicopter,
        VTOL,
        Ship
    }
}
