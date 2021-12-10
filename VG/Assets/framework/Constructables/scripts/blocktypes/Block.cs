using System.Collections;
using System.Collections.Generic;
using ASTankGame.Damage;
using UnityEngine;
using System.Xml;
using System.Xml.Serialization;

namespace ASTankGame.Vehicles.Blocks
{
    public class Block : IDamageableObject, IBlock
    {
        [XmlAttribute] public string Name { get; set; }

        public int blockID { get; set; }

        public float MaxHP { get; set; }

        public float Mass { get; set; }

        public float Armor { get; set; }

        public int Material { get; set; }

        public Block()
        { }

        public Block(int id, float mass)
        {
            blockID = id;
            Mass = mass;
        }

        /*[XmlIgnore]
        public int forward { get;  set; }
        [XmlIgnore]
        public int up { get; set; }*/

        public Vector3Int[] LocalPositions { get; set; }

        public void TakeDamage(float damage, Vector3 position, DamageType type)
        {

        }

        public static Block SubobjectLink()
        {
            return new Block(-1, 9999999);
        }
    }
}
