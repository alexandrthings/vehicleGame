using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml.Serialization;
using System.IO;

namespace VehicleBase.Vehicles.Blocks.Xml
{ 
    [XmlRoot("BlockList")]
    public class BlockContainer
    {
        [XmlArray("Blocks")]
        [XmlArrayItem("Block", typeof(Block))]
        [XmlArrayItem("PerfectBlock", typeof(PerfectBlock))]
        [XmlArrayItem("GOBlock", typeof(GOBlock))]
        [XmlArrayItem("SubOBlock", typeof(SubOBlock))]
        [XmlArrayItem("Engine", typeof(Engine))]
        [XmlArrayItem("Wheel", typeof(Wheel))]
        public List<Block> blocks = new List<Block>();

        public Block GetBlockID(int id)
        {
            return blocks[id];
        }

        public static BlockContainer Load(string path)
        {
            TextAsset _xml = Resources.Load<TextAsset>(path);

            XmlSerializer serializer = new XmlSerializer(typeof(BlockContainer));

            StringReader reader = new StringReader(_xml.text);

            BlockContainer blockCont = serializer.Deserialize(reader) as BlockContainer;

            reader.Close();

            return blockCont;
        }
    }
}
