using System.Collections;
using System.Collections.Generic;
using VehicleBase.Vehicles.Blocks;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;

namespace VehicleBase.Vehicles.Blocks
{
    public class PerfectBlock : Block
    {
        public int BlockType { get; set; }

        public int materialType { get; }

    }
}