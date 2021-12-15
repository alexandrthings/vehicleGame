using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using VehicleBase.Characters;
using VehicleBase.Settings;
using VehicleBase.UI;
using VehicleBase.Utility;
using VehicleBase.Vehicles;
using VehicleBase.Vehicles.BlockBehaviors;
using VehicleBase.Vehicles.Blocks;
using VehicleBase.Vehicles.Blocks.Management;
using VehicleBase.Vehicles.UI;
using VehicleBase.Vehicles.XML;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Menu = VehicleBase.UI.Menu;

namespace VehicleBase.Vehicles.Building
{
    // this is a massive mess and needs to be redone
    // i mean its guaranteed to be on the player object so it may as well be a dependency hell
    public class Constructor : MonoBehaviour
    {
        #region variables n shit
        public static Constructor ins;

        public bool Active;

        public List<Transform> gizmoObjects = new List<Transform>();
        public List<Transform> snapObjects = new List<Transform>();

        [SerializeField] private GameObject mirrorPreviewObject;
        public GameObject xMirrorObject;
        public GameObject yMirrorObject;
        public GameObject zMirrorObject;
        public Vector3 mirrorPos;
        [SerializeField] private bool xMirror;
        [SerializeField] private bool yMirror;
        [SerializeField] private bool zMirror;
        private int placeMirror = -1;

        private Vector3Int[] positions;

        // fwd, up
        public Vector3Int posToPlace;
        public Vector2Int rotToPlace = new Vector2Int(1, 3);
        public List<Vector2Int> mirrorRotations = new List<Vector2Int>();
        private int spin = 0;
        private float scrollDelta = 0;
        [SerializeField] private float scrollSensitivity = 0.1f;

        [SerializeField] private LayerMask vehicleMask;
        [SerializeField] private LayerMask placeMask;

        private Block selectedBlock;
        [SerializeField] private GameObject BlockObject;

        [SerializeField] private Vehicle targetVehicle;

        private Stopwatch sw;

        public bool DebugMode = false;
        public string DebugVehicleName = "vehicle";

        public bool build = false;
        private bool menuDisable = false;

        public bool[] buildValid = new bool[8];
        public List<BuildTrigger> triggers = new List<BuildTrigger>();
        [SerializeField] private List<GOBehavior> previewBehaviors = new List<GOBehavior>();

        [SerializeField]
        private Material[] placeMat = new Material[8];
        #endregion

        void Start()
        {
            if (ins != null && ins != this)
                Destroy(this);
            else
                ins = this;

            for (int i = 0; i < 8; i++)
                placeMat[i] = new Material(GlobalBlockManager.placeMat);

            selectedBlock = GlobalBlockManager.GetBlockByID(0);
            BlockMenuGenerator.hider.SetActive(false);
            gizmoObjects[0].gameObject.SetActive(false);
            snapObjects[0].gameObject.SetActive(false);

            mirrorPreviewObject = Object.Instantiate(xMirrorObject);

            mirrorRotations.Add(rotToPlace);

            gizmoObjects[0].name = "DISABLE";

            UIManager.ins.enableMenuToggles += enableToggle; // rip this out
            UIManager.ins.disableMenuToggles += disableToggle;
        }

        void Update()
        {
            #region block debug select

            if (DebugMode && build)
            {
                if (Input.GetKeyDown(KeyCode.KeypadMinus) && targetVehicle != null)
                {
                    VehicleXMLManager.ins.SaveVehicle(targetVehicle, DebugVehicleName);
                }

                if (Input.GetKeyDown(KeyCode.KeypadPlus))
                {
                    VehicleXMLManager.ins.LoadVehicle(transform.position, transform.rotation, DebugVehicleName);
                }
            }

            #endregion

            // build mode
            if (Input.GetKeyDown(Keybinds.BuildMode))
            {
                build = !build;
                
                // change to build, or whatever teh player is doing
                ActiveUI.ChangeUITo(build ? UIState.Build : 
                    PlayerCharacter.pc.state == CharacterState.Sit ? UIState.Vehicle : UIState.Character);

                if (!build)
                {
                    BlockMenuGenerator.hider.SetActive(false);
                    UIManager.DeactivateMenu(BlockConfigUI.instance);
                    placeMirror = -1;
                    mirrorPreviewObject.SetActive(false);
                    xMirrorObject.SetActive(false);
                    yMirrorObject.SetActive(false);
                    zMirrorObject.SetActive(false);

                    for (int i = 0; i < gizmoObjects.Count; i++)
                    {
                        gizmoObjects[i].gameObject.SetActive(false);
                        snapObjects[i].gameObject.SetActive(false);
                    }
                }
            }

            if (!build)
                return;

            if (CameraTools.GBCurrentlyLookedAt(10, out GOBehavior block))
            {
                if (Input.GetKeyDown(Keybinds.ConfigMenu))
                {
                    BlockConfigUI.SetTarget(block);
                }
            }

            if (Input.GetKeyDown(Keybinds.NewVehicle))
            {
                UIManager.ToggleMenu(0);
            }

            MirrorFunctionality();

            if (Input.GetKeyDown(Keybinds.BlockMenu))
            {
                BlockMenuGenerator.hider.SetActive(!BlockMenuGenerator.hider.activeSelf);
                UnityEngine.Cursor.lockState = BlockMenuGenerator.hider.activeSelf
                    ? CursorLockMode.Confined
                    : CursorLockMode.Locked;
            }

            DisplayBuildGizmo();

            if (Input.GetButtonDown("Fire1"))
                PlaceBlock(selectedBlock);

            if (Input.GetButtonDown("Fire2"))
                RemoveBlock();

            if (Input.GetKeyDown(Keybinds.HoverVehicle))
                LevitateVehicle();

            //if (Input.GetKeyDown(KeyCode.E))
            //    PrintLocalPos();
        }

        private void MirrorFunctionality()
        {
            bool even;

            if (Input.GetKeyDown(Keybinds.MirrorMenu))
            {
                placeMirror++;

                if (placeMirror > 5)
                {
                    placeMirror = -1;
                    return;
                }

                Destroy(mirrorPreviewObject);

                // make preview thing
                switch (placeMirror)
                {
                    case 0:
                    case 1:
                        mirrorPreviewObject = Object.Instantiate(xMirrorObject);
                        break;
                    case 2:
                    case 3:
                        mirrorPreviewObject = Object.Instantiate(yMirrorObject);
                        break;
                    case 4:
                    case 5:
                        mirrorPreviewObject = Object.Instantiate(zMirrorObject);
                        break;
                }
            }

            if (Input.GetKeyDown(KeyCode.Escape))
                placeMirror = -1;

            even = placeMirror % 2 == 1;

            mirrorPreviewObject.SetActive(placeMirror != -1 && targetVehicle != null && build);

            if (targetVehicle == null)
                return;

            // adjust relevant axis
            switch (placeMirror)
            {
                case 0: case 1:
                    if (Input.GetButtonDown("Fire1")) // add or move symmetry plane
                        SetMirror(true, yMirror, zMirror, new Vector3(posToPlace.x + (even ? 0.5f : 0), mirrorPos.y, mirrorPos.z));

                    if (Input.GetButtonDown("Fire2")) // delete symmetry plane
                        SetMirror(false, yMirror, zMirror, mirrorPos);
                    break;
                case 2: case 3:

                    if (Input.GetButtonDown("Fire1")) 
                        SetMirror(xMirror, true, zMirror, new Vector3(mirrorPos.x, posToPlace.y + (even ? 0.5f : 0), mirrorPos.z));

                    if (Input.GetButtonDown("Fire2")) 
                        SetMirror(xMirror, false, zMirror, mirrorPos);
                    break;
                case 4: case 5:
                    if (Input.GetButtonDown("Fire1")) 
                        SetMirror(xMirror, yMirror, true, new Vector3(mirrorPos.x, mirrorPos.y, posToPlace.z + (even ? 0.5f : 0)));

                    if (Input.GetButtonDown("Fire2"))
                        SetMirror(xMirror, yMirror, false, mirrorPos);
                    break;

                default:
                    break;
            }

            if (targetVehicle != null)
                targetVehicle.SetMirrorData(xMirror, yMirror, zMirror, mirrorPos);

            if (placeMirror != -1)
            {
                mirrorPreviewObject.transform.position = targetVehicle.transform.TransformPoint(((Vector3)posToPlace + Vector3.one * (even ? 0.5f : 0)) * targetVehicle.Scale);
                mirrorPreviewObject.transform.rotation = targetVehicle.transform.rotation;
            }
        }

        #region building
        public void SelectBlockName(string name)
        {
            selectedBlock = GlobalBlockManager.GetBlockByName(name);
            triggers.Clear();
            previewBehaviors.Clear();

            if (BlockObject != null)
            {
                // delete previous blocks
                Destroy(BlockObject);
                BlockObject = null;
            }

            if (selectedBlock.GetType() == typeof(Wheel))
            {
                rotToPlace = new Vector2Int(1, 3);
            }

            if (selectedBlock.GetType() != typeof(Block) && selectedBlock.GetType() != typeof(PerfectBlock))
            {
                GOBlock gblock = (GOBlock)selectedBlock;
                BlockObject = (GameObject)Object.Instantiate(Resources.Load(gblock.ObjectName), gizmoObjects[0]);
                BlockObject.transform.localPosition = Vector3.zero;
                BlockObject.transform.localRotation = Quaternion.identity;

                Rigidbody rb = BlockObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;

                triggers.Add(BlockObject.AddComponent<BuildTrigger>());
                previewBehaviors.Add(BlockObject.GetComponent<GOBehavior>());

                StripPreview(BlockObject, true);
            }

            MirrorGizmosAndSnaps();
        }

        void MirrorGizmosAndSnaps()
        {
            while (gizmoObjects.Count > 1)
            {
                Destroy(gizmoObjects[1].gameObject);
                gizmoObjects.RemoveAt(1);
            }
            while (snapObjects.Count > 1)
            {
                Destroy(snapObjects[1].gameObject);
                snapObjects.RemoveAt(1);
            }

            int necessaryMirrors = (xMirror ? 2 : 1) * (yMirror ? 2 : 1) * (zMirror ? 2 : 1); // 2 for 1 mirror, 4 for 2 mirrors, 8 for 3 mirrors

            for (int i = 1; i < necessaryMirrors; i++)
            {
                gizmoObjects.Add(Object.Instantiate(gizmoObjects[0]));
                snapObjects.Add(Object.Instantiate(snapObjects[0]));

                for (int c = 0; c < gizmoObjects[i].childCount; c++) // hack for unfucking the copied objects
                {
                    Destroy(gizmoObjects[i].GetChild(c).gameObject);
                }

                // copy objects to gizmo copies
                if (BlockObject != null)
                {
                    GameObject spawnedPreview = (GameObject) Object.Instantiate(BlockObject, gizmoObjects[i]);
                    spawnedPreview.transform.localPosition = Vector3.zero;
                    spawnedPreview.transform.localRotation = Quaternion.identity;

                    triggers.Add(spawnedPreview.GetComponent<BuildTrigger>());
                    previewBehaviors.Add(spawnedPreview.GetComponent<GOBehavior>());
                }
            }

            if (triggers.Count > 0) // set first trigger as main
                triggers[0].MainTrigger = true;
        }

        // strip prefab of all function and set material to transparent
        public void StripPreview(GameObject preview, bool first)
        {
            preview.tag = "Untagged";
            preview.layer = LayerMask.NameToLayer("PreviewCollider");

            //Debug.Log($"Stripping {preview.name}");

            if (!first)
            {
                MonoBehaviour[] beh = preview.GetComponents<MonoBehaviour>();
                for (int i = 0; i < beh.Length; i++)
                {
                    Destroy(beh[i]);
                }
            }

            Collider[] col = preview.GetComponents<Collider>();
            for (int i = 0; i < col.Length; i++)
            {
                col[i].isTrigger = true;
                BoxCollider bCol = col[i] as BoxCollider;

                if (bCol != null)
                {
                    bCol.size = bCol.size * 0.9f;
                }
            }

            if (preview.TryGetComponent(out Renderer grenderer))
            {
                //grenderer.material = placeMat[0];
            }

            for (int i = 0; i < preview.transform.childCount; i++)
            {
                StripPreview(preview.transform.GetChild(i).gameObject, false);
            }
        }

        void DisplayBuildGizmo()
        {
            var ray = UnityEngine.Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
            RaycastHit hit;

            CheckRots();
            GetMirrorRotations();

            if (Physics.Raycast(ray, out hit, 1000, vehicleMask))
            {
                Vector3 position = hit.point + hit.normal * 0.2f;

                Vehicle target = null;

                if (hit.collider.transform.tag == "Subobject")
                {
                    SubObjectComponent subObject;
                    if (TransformTools.TryGetSubComponent(hit.collider.transform, out subObject))
                    {
                        target = subObject.subVehicle;
                    }
                }
                else
                {
                    target = TransformTools.GetParentVehicle(hit.collider.transform);
                }

                if (target == null)
                    return;

                int localDir =
                    BlockTable.DirFromVector(
                        Vector3Int.RoundToInt(target.transform.InverseTransformDirection(hit.normal)));

                if (localDir != rotToPlace.y && selectedBlock.GetType() != typeof(Wheel))
                    rotToPlace = new Vector2Int(BlockTable.axisSpin[localDir, spin], localDir);

                posToPlace = target.GetLocalPos(position);

                positions = GetMirroredPositions(posToPlace, previewBehaviors.Count > 0 ? previewBehaviors[0].mirrorOffset : Vector3Int.zero);

                // display all gizmos
                for (int i = 0; i < gizmoObjects.Count; i++)
                {
                    gizmoObjects[i].gameObject.SetActive(true);

                    gizmoObjects[i].parent = target.transform;
                    gizmoObjects[i].localPosition = (Vector3) positions[i] * target.Scale;
                    gizmoObjects[i].localRotation = Quaternion.LookRotation(
                        BlockTable.dirToVector[mirrorRotations[i].x], BlockTable.dirToVector[mirrorRotations[i].y]);
                    gizmoObjects[i].localScale = Vector3.one * target.Scale;

                    if (VerifyBlockPlacement(i))
                    {
                        buildValid[i] = true;
                        placeMat[i].color = Color.green;

                        if (triggers.Count > i)
                            foreach (Renderer ren in triggers[i].renderers)
                                ren.material = placeMat[i];
                    }
                    else
                    {
                        buildValid[i] = false;
                        placeMat[i].color = Color.red;

                        if (triggers.Count > i)
                            foreach (Renderer ren in triggers[i].renderers)
                                ren.material = placeMat[i];
                    }
                }

                PlaceSnapObject(target, hit);

                
                xMirrorObject.transform.position = target.transform.TransformPoint((Vector3)mirrorPos * target.Scale);
                xMirrorObject.transform.rotation = target.transform.rotation;

                yMirrorObject.transform.position = xMirrorObject.transform.position;
                yMirrorObject.transform.rotation = xMirrorObject.transform.rotation;

                zMirrorObject.transform.position = xMirrorObject.transform.position;
                zMirrorObject.transform.rotation = xMirrorObject.transform.rotation;

                xMirrorObject.SetActive(xMirror);
                yMirrorObject.SetActive(yMirror);
                zMirrorObject.SetActive(zMirror);
                
            }
            else
            {
                for (int i = 0; i < gizmoObjects.Count; i++)
                {
                    snapObjects[i].gameObject.SetActive(false);
                    gizmoObjects[i].gameObject.SetActive(false);
                }

                xMirrorObject.SetActive(false);
                yMirrorObject.SetActive(false);
                zMirrorObject.SetActive(false);
            }
        }

        public bool VerifyBlockPlacement(int index)
        {
            if (targetVehicle == null)
                return false;

            if (triggers.Count > index && triggers[index] != null)
                if (!triggers[index].BuildValid)
                    return false;

            Vector3Int position = targetVehicle.GetLocalPos(gizmoObjects[index].transform.position);

            bool connected = CheckForConnection(position);

            //Debug.Log($"pos {position} conn {connected}");

            // check all link points
            if (BlockObject != null)
                for (int i = 0; i < previewBehaviors[index].BlockLinks.Length; i++)
                {
                    if (CheckForConnection(position // if block is connected at the link position transformed
                                           + BlockTable.dirToVector[rotToPlace.x] * previewBehaviors[index].BlockLinks[i].z
                                           + BlockTable.dirToVector[rotToPlace.y] * previewBehaviors[index].BlockLinks[i].y
                                           + BlockTable.dirToVector[BlockTable.blockRight[rotToPlace.x, rotToPlace.y]] *
                                           previewBehaviors[index].BlockLinks[i].x))
                        return true;
                    
                }

            return connected;
        }

        public bool CheckForConnection(Vector3Int pos)
        {
            if (targetVehicle != null)
            {
                if (targetVehicle.GetBlockLocal(pos) != null) // check that nothing occupies this space
                    return false;

                //Debug.LogWarning("Not occupied");

                for (int i = 0; i < 6; i++)
                {
                    if (targetVehicle.GetBlockLocal(pos + BlockTable.dirToVector[i]) != null) // check for links
                        return true;

                    //Debug.LogError($"Not connected at {pos + BlockTable.dirToVector[i]}");
                }
            }

            return false;
        }

        void PrintLocalPos()
        {
            var ray = UnityEngine.Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 1000, vehicleMask))
            {
                Debug.LogWarning(TransformTools.GetParentVehicle(hit.transform).GetLocalPos(hit.point));
            }
        }

        void PlaceSnapObject(Vehicle target, RaycastHit hit)
        {
            Vector3 position = hit.point - hit.normal * 0.0001f;

            if (target == null)
                return;

            positions = GetMirroredPositions(target.GetLocalPos(position), Vector3Int.zero);

            for (int i = 0; i < snapObjects.Count; i++)
            {
                snapObjects[i].gameObject.SetActive(true);

                snapObjects[i].parent = target.transform;
                snapObjects[i].localPosition = (Vector3)positions[i] * target.Scale;
                snapObjects[i].localRotation = Quaternion.LookRotation(BlockTable.dirToVector[rotToPlace.x], BlockTable.dirToVector[rotToPlace.y]);
                snapObjects[i].localScale = Vector3.one * (target.Scale);
            }

            targetVehicle = target;
        }

        private bool PlaceBlock(Block toPlace)
        {
            if (menuDisable || placeMirror != -1)
                return false;

            var ray = UnityEngine.Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
            RaycastHit hit;

            // subobject placement support
            if (Physics.Raycast(ray, out hit, 1000, vehicleMask))
            {
                if (hit.collider.transform.tag == "Subobject")
                {
                    SubObjectComponent subObject;
                    if (TransformTools.TryGetSubComponent(hit.collider.transform, out subObject))
                    {
                        targetVehicle = subObject.subVehicle;

                        Vector3 pos = hit.point + hit.normal * 0.2f;

                        targetVehicle.AddBlock(toPlace, pos, (sbyte)rotToPlace.x, (sbyte)rotToPlace.y);
                        return true;
                    }
                }
            }

            if (!Physics.Raycast(ray, out hit, 1000, placeMask) || targetVehicle == null)
            {
                return false;
            }

            //Vector3 position = hit.point + hit.normal * 0.2f;

            for (int i = 0; i < gizmoObjects.Count; i++)
            {
                //Debug.Log($"position {gizmoObjects[i].position}, rot fwd {roto}");

                if (VerifyBlockPlacement(i))
                    targetVehicle.AddBlock(toPlace, gizmoObjects[i].position, (sbyte) mirrorRotations[i].x, (sbyte)mirrorRotations[i].y);
            }

            return true;
        }

        void LevitateVehicle()
        {
            if (menuDisable)
                return;

            var ray = UnityEngine.Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
            RaycastHit hit;

            if (!Physics.Raycast(ray, out hit, 1000, vehicleMask))
            {
                return;
            }

            hit.transform.GetComponent<Vehicle>().Levitate();
        }
        
        // removing is done, with mirror
        private bool RemoveBlock()
        {
            if (menuDisable || placeMirror != -1)
                return false;

            var ray = UnityEngine.Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
            RaycastHit hit;

            if (!Physics.Raycast(ray, out hit, 1000, vehicleMask))
            {
                return false;
            }

            Vector3 position = hit.point - hit.normal * 0.2f;

            for (int i = 0; i < snapObjects.Count; i++)
            {
                snapObjects[i].parent = null;
                gizmoObjects[i].parent = null;
            }

            if (hit.collider.transform.tag == "Subobject")
            {
                SubObjectComponent subObject;
                if (TransformTools.TryGetSubComponent(hit.collider.transform, out subObject))
                {
                    targetVehicle = subObject.vehicle;

                    targetVehicle.RemoveBlock(subObject.transform.position);
                    return true;
                }
            }

            if (hit.collider.transform.tag == "GObject")
            {
                GOBehavior component;
                if (TransformTools.TryGetGOComponent(hit.collider.transform, out component))
                {
                    position = component.transform.position;
                }
                else return false;

            }
            else
            {
                if (!Physics.Raycast(ray, out hit, 1000, placeMask) || targetVehicle == null)
                {
                    return false;
                }

                position = hit.point - hit.normal * 0.2f;
            }

            Vector3Int localPos = targetVehicle.GetLocalPos(position);

            foreach (Vector3Int pos in GetMirroredPositions(localPos, previewBehaviors.Count > 0 ? previewBehaviors[0].mirrorOffset : Vector3Int.zero))
                targetVehicle.RemoveBlockLocal(pos, true);

            return true;
        }

        Vector3Int[] GetMirroredPositions(Vector3Int firstPosition, Vector3Int precomputeOffset)
        {
            List<Vector3Int> positions = new List<Vector3Int>();

            positions.Add(firstPosition);

            if (xMirror)
            {
                positions.Add(Vector3Int.RoundToInt(new Vector3(mirrorPos.x - (positions[0].x - mirrorPos.x), positions[0].y, positions[0].z)) 
                              + precomputeOffset.x
                              * BlockTable.dirToVector[BlockTable.blockRight[mirrorRotations[0].x, mirrorRotations[0].y] ] ); // adding transformed offset for correct mirror
            }

            if (yMirror)
            {
                int count = positions.Count;
                for (int i = 0; i < count; i++)
                {
                    positions.Add(Vector3Int.RoundToInt(new Vector3(positions[i].x, mirrorPos.y - (positions[i].y - mirrorPos.y), positions[i].z))
                                                        + precomputeOffset.x
                                                        * BlockTable.dirToVector[BlockTable.blockRight[mirrorRotations[i].x, mirrorRotations[i].y]]);
                }
            }

            if (zMirror)
            {
                int count = positions.Count;
                for (int i = 0; i < count; i++)
                {
                    positions.Add(Vector3Int.RoundToInt(new Vector3(positions[i].x, positions[i].y, mirrorPos.z - (positions[i].z - mirrorPos.z)))
                                                        + precomputeOffset.x
                                                        * BlockTable.dirToVector[BlockTable.blockRight[mirrorRotations[i].x, mirrorRotations[i].y]]);
                }
            }

            return positions.ToArray();
        }

        void GetMirrorRotations()
        {
            mirrorRotations.Clear();
            mirrorRotations.Add(rotToPlace);

            if (xMirror)
            {
                Vector2Int mirrored = MirrorRotation(mirrorRotations[0], 4);
                mirrorRotations.Add(mirrored);
            }

            int count = mirrorRotations.Count;
            if (yMirror)
            {
                for (int i = 0; i < count; i++)
                {
                    Vector2Int mirrored = MirrorRotation(mirrorRotations[i], 2);
                    mirrorRotations.Add(mirrored);
                }
            }

            count = mirrorRotations.Count;
            if (zMirror)
            {
                for (int i = 0; i < count; i++)
                {
                    Vector2Int mirrored = MirrorRotation(mirrorRotations[i], 0);
                    mirrorRotations.Add(mirrored);
                }
            }
        }

        Vector2Int MirrorRotation(Vector2Int rotation, int axis)
        {
            Vector2Int newRot = new Vector2Int(rotation.x, rotation.y);

            if (BlockTable.MatchingAxis(newRot.x, axis))
                newRot.x = BlockTable.oppositeSide[newRot.x];
            else if (BlockTable.MatchingAxis(newRot.y, axis))
                newRot.y = BlockTable.oppositeSide[newRot.y];

            return newRot;
        }
        #endregion

        public void enableToggle()
        {
            menuDisable = true;
        }

        public void disableToggle()
        {
            menuDisable = false;
        }

        public static void SetMirror(bool x, bool y, bool z, Vector3 center)
        {
            if (ins == null)
                return;

            ins.xMirror = x;
            ins.yMirror = y;
            ins.zMirror = z;
            ins.mirrorPos = center;

            ins.SelectBlockName(ins.selectedBlock.Name);
        }

        #region rotate
        void CheckRots()
        {
            scrollDelta += Input.mouseScrollDelta.y;

            if (selectedBlock.GetType() == typeof(Wheel))
            {
                if (Mathf.Abs(scrollDelta) > scrollSensitivity) // flip only between left and right wheel configs
                    rotToPlace.x = rotToPlace.x == 0 ? 1 : 0;

                scrollDelta = 0;

                return;
            }

            if (scrollDelta > scrollSensitivity)
                YawRot(1);
            if (scrollDelta < -scrollSensitivity)
                YawRot(-1);

            /*
            if (Input.GetKeyDown(KeyCode.J))
                YawRot(1);
            if (Input.GetKeyDown(KeyCode.L))
                YawRot(-1);

            if (Input.GetKeyDown(KeyCode.U))
                RollRot(1);
            if (Input.GetKeyDown(KeyCode.O))
                RollRot(-1);

            if (Input.GetKeyDown(KeyCode.I))
                PitchRot(1);
            if (Input.GetKeyDown(KeyCode.K))
                PitchRot(-1);*/

        }

        void YawRot(int amt)
        {
            scrollDelta = 0;
            rotToPlace.x = BlockTable.FindRotationIndex(rotToPlace.x, amt, rotToPlace.y);
            spin += amt;
            if (spin < 0)
                spin = 3;
            if (spin > 3)
                spin = 0;
        }

        void RollRot(int amt)
        {
            rotToPlace.y = BlockTable.FindRotationIndex(rotToPlace.y, amt, rotToPlace.x);
        }

        void PitchRot(int amt)
        {
            Vector2Int prevRot = new Vector2Int(rotToPlace.x, rotToPlace.y);

            //Debug.LogWarning(BlockTable.blockRight[prevRot.x, prevRot.y]);

            rotToPlace.x = BlockTable.FindRotationIndex(prevRot.x, amt, BlockTable.blockLeft[prevRot.x, prevRot.y]);
            rotToPlace.y = BlockTable.FindRotationIndex(prevRot.y, amt, BlockTable.blockLeft[prevRot.x, prevRot.y]);
        }
        #endregion

#if UNITY_EDITOR
        public void OnDrawGizmos()
        {
            if (!EditorApplication.isPlaying)
                return;

            var ray = UnityEngine.Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
            RaycastHit hit;

            if (!Physics.Raycast(ray, out hit, 1000, vehicleMask))
            {
                return;
            }

            Vector3 position = hit.point - hit.normal * 0.2f;

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(position, 0.2f);

            position = hit.point + hit.normal * 0.2f;

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(position, 0.2f);
        }
#endif
    }
}