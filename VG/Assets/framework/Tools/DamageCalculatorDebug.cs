#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using VehicleBase.Damage;
using UnityEngine;

public class DamageCalculatorDebug : MonoBehaviour
{
    public float radius = 1;

    void OnDrawGizmos()
    {
        foreach (DamageCalculator.AffectedBlock block in DamageCalculator.GetShellSlice(transform.forward, radius))
        {
            Gizmos.color = new Color(block.weight, 0, 0);
            Gizmos.DrawCube(transform.position + new Vector3(block.x, block.y, block.z), Vector3.one);
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 5);
    }
}
#endif