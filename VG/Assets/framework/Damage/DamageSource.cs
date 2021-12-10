using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ASTankGame.Damage
{
    public interface DamageSource
    {
        float Damage { get; set; }
        float APValue { get; set; }
        /// <summary>
        /// Shell radius in m
        /// </summary>
        float Radius { get; set; }

        Vector3 position { get; set; }
        Vector3 velocity { get; }

        DamageType DmgType { get;}
    }

    public enum DamageType
    {
        AP,
        HE,
        HEAT,
        HESH,
        THERMAL
    }

    public enum AmmoType
    {
        AP,
        APHE,
        APCR,
        APDS,
        APDSFS,
        SAPI,
        HE,
        HESH,
        HEAT,
        HEATFS,
        FRAG
    }
}