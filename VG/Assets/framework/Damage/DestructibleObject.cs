using System.Collections;
using System.Collections.Generic;
using ASTankGame.Damage;
using UnityEngine;

namespace ASTankGame.Damage.Objects
{
    public class DestructibleObject : MonoBehaviour, IDamageableObject
    {
        public float MaxHP { get; set; }
        public float Health { get; set; }

        public float Armor { get; set; }

        // Start is called before the first frame update
        void Start()
        {
            MaxHP = 1000;
            Health = 1000;
        }

        public void TakeDamage(float _amount, Vector3 _position, DamageType _type)
        {
            Health -= _amount;
            Debug.Log($"Took {_amount} damage");
        }
    }
}