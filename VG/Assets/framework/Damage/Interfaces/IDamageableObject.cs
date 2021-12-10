using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ASTankGame.Damage
{
    public interface IDamageableObject
    {
        float MaxHP { get; set; }

        float Armor { get; set; }

        void TakeDamage(float _amount, Vector3 _position, DamageType _type);
    }
}
