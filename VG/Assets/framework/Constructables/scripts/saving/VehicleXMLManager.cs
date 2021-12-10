using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Xml.Serialization;
using System.Xml;
using ASTankGame.Vehicles.BlockBehaviors;
using ASTankGame.Vehicles.Blocks;
using ASTankGame.Vehicles.Blocks.Management;
using ASTankGame.Vehicles.Chunks;
using Unity.VisualScripting;

namespace ASTankGame.Vehicles.XML
{
    public class VehicleXMLManager : MonoBehaviour
    {
        public static VehicleXMLManager ins;

        public bool autoCenter = false;

        public Object InstantiatableVehicle
        {
            get { return defaultVehicle; }
        }
        [SerializeField] private Object defaultVehicle;

        private int subobjectReference = 0;

        void Awake()
        {
            if (ins != null && ins != this)
                Destroy(this);
            else
                ins = this;

            System.IO.Directory.CreateDirectory(Application.persistentDataPath + "/Saves/Vehicles/Local");
        }

        public void LoadVehicle(Vector3 position, Quaternion rotation, string name)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(SerializedVehicle));
            FileStream stream = new FileStream(Application.persistentDataPath + "/Saves/Vehicles/Local/" + name + ".xml", FileMode.Open);

            SerializedVehicle vehicle = (SerializedVehicle)serializer.Deserialize(stream);

            stream.Close();

            Vehicle veh = defaultVehicle.GetComponent<Vehicle>();

            veh.SetScale(vehicle.scale);

            GameObject vehicleObject = (GameObject)Object.Instantiate(defaultVehicle, position, rotation);

            // add blocks on parent vehicle then move on to subvehicles>
            LoadVehicleRecursive(vehicle.mainConstruct, vehicleObject.GetComponent<Vehicle>());

            vehicleObject.transform.name = "[VEHICLE] " + name;

            //constructs[0].UpdateAllChunks();
            //constructs[0].UpdateCOM();
        }

        public void LoadVehicleRecursive(ConstructData data, Vehicle target)
        {
            for (int i = 0; i < data.Blocks.Count; i++)
            {
                BlockData bd = data.Blocks[i];
                target.AddBlockLocal(GlobalBlockManager.GetBlockByName(bd.name), bd.pos, (sbyte)bd.fwd, (sbyte)bd.up, false);

                GOBehavior gb = target.GetGOBehaviorLocal(bd.pos);
                if (gb != null)
                {
                    if (bd.blockProperties != null)
                        for (int a = 0; a < bd.blockProperties.Length; a++)
                        {
                            GOBehavior.SetConfigurableData(gb, bd.blockProperties[a].name, bd.blockProperties[a].value);
                        }
                }
            }

            target.transform.name = "[SUBVEHICLE] " + data.subConstructID;

            target.UpdateAllChunks();

            //Debug.LogWarning($"expected {data.subConstructs.Count}, actual {target.Subvehicles.Count}");

            for (int i = 0; i < data.subConstructs.Count; i++)
            {
                if (target.Subvehicles.Count > i)
                    LoadVehicleRecursive(data.subConstructs[i], target.Subvehicles[i]);
            }
        }

        public bool SaveVehicle(Vehicle target, string name)
        {
            subobjectReference = 0;

            SerializedVehicle vehicle = new SerializedVehicle();
            
            // pulling data from target into object
            vehicle.name = name;
            vehicle.scale = target.Scale;

            //bool autoCenter = true; // add settings to this
            Vector3Int offset = Vector3Int.zero; // center vehicle in chunk for less MeshRenderers
            if (autoCenter)
            {
                offset = Vector3Int.RoundToInt(Vector3Int.one * target.ChunkSize / 2 - target.RB.centerOfMass);
            }

            ConstructData construct = AddCDataRecursive(target, 0, offset);

            vehicle.mainConstruct = construct;

            XmlSerializer serializer = new XmlSerializer(typeof(SerializedVehicle));
            FileStream stream = new FileStream(Application.persistentDataPath + "/Saves/Vehicles/Local/" + name + ".xml", FileMode.Create);

            serializer.Serialize(stream, vehicle);
            stream.Close();

            Debug.Log($"Saved vehicle {name} at {Application.persistentDataPath + "/Saves/Vehicles/Local/" + name + ".xml"} ");

            return true;
        }

        public ConstructData AddCDataRecursive(Vehicle target, int dataIndex, Vector3Int offset)
        {
            // add data from this construct, then go onto children
            ConstructData parent = GetBlockData(target, ref subobjectReference, offset);
            parent.subConstructID = dataIndex;

            for (int i = 0; i < target.Subvehicles.Count; i++)
            {
                ConstructData child = AddCDataRecursive(target.Subvehicles[i], dataIndex+1, Vector3Int.zero);
                parent.subConstructs.Add(child);
            }

            return parent;
        }

        public ConstructData GetBlockData(Vehicle target, ref int index, Vector3Int offset)
        {
            ConstructData bd = new ConstructData();

                foreach (KeyValuePair<Vector3Int, BlockRepresentation> block in target.Blocks)
                {
                    if (block.Value.blockID < 0)
                        continue;

                    BlockData curBlock = new BlockData();
                    curBlock.pos = block.Key + offset;
                    curBlock.name = block.Value.block.Name;
                    curBlock.pack = 0;
                    curBlock.fwd = block.Value.forward;
                    curBlock.up = block.Value.up;

                    if (target.GObjects.ContainsKey(block.Key))
                    {
                        GOBehavior.GetConfigurableData(target.GObjects[block.Key].GetComponent<GOBehavior>(),
                            out float[] data, out string[] nameData);

                        List<BlockProperty> bps = new List<BlockProperty>();

                        for (int i = 0; i < data.Length; i++)
                        {
                            bps.Add(new BlockProperty(nameData[i], data[i]));
                        }

                        curBlock.blockProperties = bps.ToArray();
                    }

                    bd.Blocks.Add(curBlock);
                }

                index++;

            return bd;
        }
    }

    [System.Serializable]
    public class SerializedVehicle
    {
        public ConstructData mainConstruct;
        public string name = "NewVehicle";
        public float scale = 1;
    }

    [System.Serializable]
    public class ConstructData
    {
        public int subConstructID;
        public List<ConstructData> subConstructs = new List<ConstructData>();
        public List<BlockData> Blocks = new List<BlockData>();
    }

    [System.Serializable]
    public class BlockData
    {
        public string name;
        public int pack, fwd, up;
        public Vector3Int pos;

        public BlockProperty[] blockProperties;
    }

    [System.Serializable]
    public class BlockProperty
    {
        public string name;
        public float value;

        public BlockProperty()
        {

        }

        public BlockProperty(string _name, float _value)
        {
            name = _name;
            value = _value;
        }
    }
}