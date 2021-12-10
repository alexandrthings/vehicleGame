using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using ASTankGame.Vehicles.BlockBehaviors;
using ASTankGame.Vehicles.Blocks;
using ASTankGame.Vehicles.Blocks.Xml;
using Unity.VisualScripting;
using UnityEngine;

namespace ASTankGame.Vehicles.Blocks.Management
{
    [ExecuteInEditMode]
    public class GlobalBlockManager : MonoBehaviour
    {
        public static GlobalBlockManager gBlockManager;

        public static List<PerfectBlock> armorBlocks = new List<PerfectBlock>();
        public static List<GOBlock> wheels = new List<GOBlock>();

        public static BlockContainer BlockList;

        public static Material standardMat;
        public static Material placeMat;

        public static Object defaultVehicle { get { return gBlockManager.DefaultVehicle; } }
        public Object DefaultVehicle;

        // Start is called before t
        // he first frame update
        void Awake()
        {
            if (gBlockManager != null && gBlockManager != this)
            {
                Debug.LogWarning("Duplicate global block managers, destroying for singleton");
                Destroy(this);
            }
            else
            {
                gBlockManager = this;
            }

            BlockList = BlockContainer.Load("blocks");
            standardMat = Resources.Load<Material>("StandardMat");
            placeMat = Resources.Load<Material>("PlaceMat");

            for (int i = 0; i < BlockList.blocks.Count; i++)
            {
                Debug.Log($"Loaded block {BlockList.blocks[i].Name} with type with health {BlockList.blocks[i].MaxHP} and armor {BlockList.blocks[i].Armor}");

                BlockList.blocks[i].blockID = i;

                if (BlockList.blocks[i].GetType() == typeof(GOBlock))
                {
                    GOBlock gb = BlockList.blocks[i] as GOBlock;
                    
                    Object block = Resources.Load(gb.ObjectName);
                    block.GetComponent<GOBehavior>().CalculateMirrorOffset();
                }

                if (BlockList.blocks[i].GetType() == typeof(PerfectBlock))
                {
                    armorBlocks.Add(BlockList.blocks[i] as PerfectBlock);
                }

                if (BlockList.blocks[i].GetType() == typeof(Wheel))
                {
                    wheels.Add(BlockList.blocks[i] as Wheel);
                }
            }
        }

        public static Block GetBlockByID(int id)
        {
            return BlockList.blocks[id];
        }

        public static Block GetBlockByName(string name)
        {
            for (int i = 0; i < BlockList.blocks.Count; i++)
            {
                if (BlockList.blocks[i].Name.ToLower() == name.ToLower())
                    return BlockList.blocks[i];
            }

            return null;
        }

    }
}