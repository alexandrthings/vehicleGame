using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ASTankGame.Vehicles.BlockBehaviors.Weapons
{
    /// <summary>
    /// interface for anything that you can aim and attach weapons to, or weapons themselves
    /// </summary>
    public interface ITargetable
    {
        bool isTurret { get; }
        void SetTarget(Vector3 position);
        float GetGuidanceMisalignment();
    }

    public interface IWeapon : ITargetable
    {
        bool controlTurrets { get; }

        bool Fire();
        void SelectAmmo(int type);
    }
}