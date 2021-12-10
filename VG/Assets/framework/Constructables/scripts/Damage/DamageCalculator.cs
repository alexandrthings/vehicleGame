using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using ASTankGame.Vehicles;
using ASTankGame.Vehicles.Blocks;
using Cinemachine;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ASTankGame.Damage
{
    public static class DamageCalculator
    {
        /// <summary>
        /// Calculate damage done
        /// </summary>
        /// <param name="target">Target vehicle</param>
        /// <param name="sources">Damage done in order</param>
        /// <returns></returns>
        public static PenetrationSolution CalculatePenetrationSolution(DamageSource[] sources, Vehicle[] targets, float checkDepth)
        {
            if (sources == null || targets == null || targets.Length == 0 || sources.Length == 0)
                return null;

            PenetrationSolution sol = new PenetrationSolution();

            sol.Targets = targets;

            float duration = 0;

            // iterate over every damage sequence
            for (int i = 0; i < sources.Length; i++)
            {
                switch (sources[i].DmgType)
                {
                    case DamageType.HEAT:
                    case DamageType.AP:
                        sol.DamageSequences.Add(CalculateAPSequence(targets, sources[i], checkDepth, duration));
                        break;
                    case DamageType.HE:
                        sol.DamageSequences.Add(CalculateHESequence(targets.ToArray(), sources[i]));
                        break;
                    case DamageType.HESH:
                        sol.DamageSequences.Add(CalculateShatterSequence(targets.ToArray(), sources[i]));
                        break;
                }
            }

            duration += sol.DamageSequences.Last().length;
            sol.shellStartPoint = targets[0].transform.InverseTransformPoint(sources[0].position) / targets[0].Scale;

            // normalize time
            sol.NormalizeTime(duration);

            return sol;
        }

        #region AP Calculations
        private static DamageSequence CalculateAPSequence(Vehicle[] Targets, DamageSource source, float checkDepth, float time)
        {
            Debug.LogWarning($"AP {source.APValue}, DMG {source.Damage}, Radius {source.Radius}, velocity {source.velocity}, cdepth {checkDepth}");
            DamageSequence DSeq = new DamageSequence();
            List<AffectedBlock[]> DamageSlices = new List<AffectedBlock[]>();
            Vector3[] positions = new Vector3[Targets.Length];
            Vector3[] directions = new Vector3[Targets.Length];
            float[] times = new float[Targets.Length];

            // for picking which slice to do next
            float[] advPerSlice = new float[Targets.Length];
            float[] penDistances = new float[Targets.Length];

            float energyLeft = source.Damage;

            int failsafe = 0;

            // figure out local coordinate specifics for each possible hit
            for (int i = 0; i < Targets.Length; i++)
            {
                if (Targets[i] == null)
                    return DSeq;

                positions[i] = Targets[i].transform.InverseTransformPoint(source.position) / Targets[i].Scale;
                directions[i] = Targets[i].transform.InverseTransformDirection(source.velocity).normalized;

                DamageSlices.Add(GetShellSlice(directions[i], source.Radius / 1000 / Targets[i].Scale));
                penDistances[i] = 0;
                advPerSlice[i] = GetDistToNextSlice(GetSliceNormal(directions[i]), directions[i]);

                times[i] = time;
            }

            while (energyLeft > 0)
            {
                int I = 0;

                float smallest = Mathf.Infinity;

                // find next slice
                for (int i = 0; i < Targets.Length; i++)
                {
                    float dist = penDistances[i];

                    if (dist < smallest)
                    {
                        smallest = dist;
                        I = i;
                    }
                }

                //Debug.LogWarning($"Smallest {I} at length {smallest}");

                if (smallest > checkDepth) // stop if we're too deep
                    goto RETURN;
                
                // advance relative positions to next layer
                TimestampedDamage layer = CalculateAPLayer(Targets[I], I, DamageSlices[I],
                    positions[I] + directions[I] * penDistances[I], energyLeft, source.APValue, times[I]);
                DSeq.Add(layer);
                penDistances[I] += advPerSlice[I];
                energyLeft -= layer.totalDamageDealt;
                times[I] += advPerSlice[I] / source.velocity.magnitude;

                failsafe++;
                if (failsafe > 10000)
                {
                    Debug.LogError("AP Calculation got stuck");

                    for (int i = 0; i < Targets.Length; i++)
                    {
                        Debug.LogError($"Stuck depth {i} is {penDistances[i]}, slice adv {advPerSlice[i]}");
                    }

                    goto RETURN;
                }
            }

            RETURN:
            DSeq.length = DSeq.DamageTimestamps.Last().timestamp;

            return DSeq;
        }

        // bullet pos in local space
        private static TimestampedDamage CalculateAPLayer(Vehicle target, int tIndex, AffectedBlock[] slice, Vector3 bulletPos, float damage, float APValue, float tstamp)
        {
            TimestampedDamage damageTotal = new TimestampedDamage();
            damageTotal.DamageDealt = new List<DmgAndPos>();

            if (target == null || slice.Length == 0)
                return damageTotal;

            float dmgRemaining = damage;
            damageTotal.totalDamageDealt = 0;
            damageTotal.target = tIndex;
            
            Vector3Int bActualPos = Vector3Int.RoundToInt(bulletPos);

            for (int i = 0; i < slice.Length; i++)
            {
                if (dmgRemaining <= 0)
                    break;

                Vector3Int pos = bActualPos + new Vector3Int(slice[i].x, slice[i].y, slice[i].z);
                BlockRepresentation blockR = target.GetBlockLocalWithDamage(pos, out float DMG);

                // damage math
                if (blockR != null)
                {
                    float APMultiplier = Mathf.Clamp(blockR.block.Armor / (APValue + 0.0001f), 0.5f, 100f);
                    float damageUsed = (blockR.block.MaxHP - DMG) * (APMultiplier) * slice[i].weight;

                    damageTotal.DamageDealt.Add(new DmgAndPos(pos, Mathf.Clamp(blockR.block.MaxHP - DMG + 0.1f, 0, blockR.block.MaxHP) * slice[i].weight));
                    damageTotal.totalDamageDealt += blockR.block.MaxHP - DMG;

                    //Debug.LogWarning($"AP Mult {APMultiplier}, DUsed {damageUsed}");

                    dmgRemaining -= damageUsed;
                }
            }

            damageTotal.timestamp = tstamp;
            damageTotal.projectilePos = bulletPos;

            //Debug.Log(damageTotal.totalDamageDealt);

            // figure out shell cross section
            return damageTotal;
        }

        /// <summary>
        /// Find blocks to damage.
        /// </summary>
        /// <param name="direction">Direction of bullet. MUST BE LOCAL DIRECTION</param>
        /// <param name="radiusInBlocks">Radius in blocks of bullet.</param>
        /// <returns></returns>
        public static AffectedBlock[] GetShellSlice(Vector3 direction, float radiusInBlocks)
        {
            if (direction == Vector3.zero)
                return new AffectedBlock[0];

            Vector3Int normal = GetSliceNormal(direction);

            float incidentDot = Mathf.Abs(Vector3.Dot(direction.normalized, normal));
            // range of atan2 is [-pi, pi]
            float ellipseAngle = GetEllipseAngle(direction, normal) + Mathf.PI;

            //Debug.Log("Ellipse Angle " + ellipseAngle);

            List<AffectedBlock> slice = new List<AffectedBlock>();

            float checkRadius = radiusInBlocks*(1 / incidentDot) + 1;

            for (int x = -Mathf.FloorToInt(checkRadius); x <= checkRadius; x++) {
                for (int y = -Mathf.FloorToInt(checkRadius); y <= checkRadius; y++)
                {
                    float bAngle = AngleConv.Conv360to180(ellipseAngle + Mathf.Atan2(y, x) + Mathf.PI);

                    float weight = GetEllipseRadius(radiusInBlocks * (1 / incidentDot), radiusInBlocks, bAngle) - Mathf.Sqrt(Mathf.Pow(x, 2) + Mathf.Pow(y, 2));

                    if (weight > 0)
                    {
                        Vector3Int tPos = TransformSlicePoint(x, y, normal);
                        slice.Add(new AffectedBlock(tPos.x, tPos.y, tPos.z, weight));
                    }
                }
            }

            return slice.ToArray();
        }

        public static Vector3Int GetSliceNormal(Vector3 direction)
        {
            float xDot = Mathf.Abs(Vector3.Dot(direction, Vector3.right));
            float yDot = Mathf.Abs(Vector3.Dot(direction, Vector3.up));
            float zDot = Mathf.Abs(Vector3.Dot(direction, Vector3.forward));

            if (zDot > xDot && zDot > yDot)
                return Vector3Int.forward;
            else if (xDot > yDot)
                return Vector3Int.right;
            else
                return Vector3Int.up;
        }

        private static Vector3Int TransformSlicePoint(int x, int y, Vector3Int normal)
        {
            if (normal == Vector3Int.forward)
            {
                return new Vector3Int(x, y, 0);
            }
            else if (normal == Vector3Int.up)
            {
                return new Vector3Int(x, 0, y);
            }

            return new Vector3Int(0, y, x);
        }

        private static float GetEllipseAngle(Vector3 dir, Vector3Int normal)
        {
            if (normal == Vector3Int.forward)
            {
                return Mathf.Atan2(dir.y, -dir.x);
            }
            else if (normal == Vector3Int.up)
            {
                return Mathf.Atan2(-dir.z, dir.x);
            }

            return Mathf.Atan2(dir.y, -dir.z);
        }

        private static float GetEllipseRadius(float a, float b, float theta)
        {
            return (a*b)
                   /
                   Mathf.Sqrt( Mathf.Pow(b * Mathf.Cos(theta), 2) + Mathf.Pow(a * Mathf.Sin(theta), 2) );
        }

        public static float GetDistToNextSlice(Vector3Int direction, Vector3 forward)
        {
            Vector3 projected = Vector3.ProjectOnPlane(forward.normalized, direction);

            float distance = Mathf.Sqrt(Mathf.Pow(projected.magnitude, 2) + 1);

            return distance;
        }

        public struct AffectedBlock
        {
            public int x { get; private set; }
            public int y { get; private set; }
            public int z { get; private set; }
            public float weight { get; private set; }

            public AffectedBlock(int X, int Y, int Z, float W)
            {
                x = X;
                y = Y;
                z = Z;
                weight = W;
            }
        }
        #endregion

        private static DamageSequence CalculateHESequence(Vehicle[] Target, DamageSource source)
        {
            return null;
        }

        private static DamageSequence CalculateShatterSequence(Vehicle[] Target, DamageSource source)
        {
            return null;
        }
    }

    /// <summary>
    /// Struct for saving the damage dealt in a single attack.
    /// </summary>
    public class PenetrationSolution
    {
        public int ShellType = 0;
        public Vector3 shellStartPoint = Vector3.zero; // for replay
        public Vehicle[] Targets;

        public List<DamageSequence> DamageSequences = new List<DamageSequence>();

        public void NormalizeTime(float _length)
        {
            for (int i = 0; i < DamageSequences.Count; i++)
            {
                DamageSequences[i].NormalizeTime(_length);
            }
        }

        public void ApplyDamage()
        {
            List<Vehicle> affectedVehicles = new List<Vehicle>();

            foreach (DamageSequence dseq in DamageSequences)
            {
                foreach (TimestampedDamage tdam in dseq.DamageTimestamps)
                {
                    if (tdam.totalDamageDealt > 0)
                        Targets[tdam.target].TakeDamage(tdam.DamageDealt.ToArray(), false);

                    if (!affectedVehicles.Contains(Targets[tdam.target]))
                        affectedVehicles.Add(Targets[tdam.target]);
                }
            }

            for (int i = 0; i < affectedVehicles.Count; i++)
            {
                affectedVehicles[i].UpdateBufferedChunks();
            }
        }
    }

    // Damage as done in order
    public class DamageSequence
    {
        public float length;
        public List<TimestampedDamage> DamageTimestamps = new List<TimestampedDamage>();

        public void Add(TimestampedDamage dmg)
        {
            DamageTimestamps.Add(dmg);
        }

        public void NormalizeTime(float _length)
        {
            length = length / _length;

            for (int i = 0; i < DamageTimestamps.Count; i++)
            {
                DamageTimestamps[i].SetTimestamp(DamageTimestamps[i].timestamp / _length);
            }
        }

        public Vector3 GetShellSpeedModifier()
        {
            Vector3 modifier = Vector3.one;

            for (int i = 0; i < DamageTimestamps.Count; i++)
            {
                modifier = Vector3.Scale(modifier, DamageTimestamps[i].velocityModifier);
            }

            return modifier;
        }
    }
    
    // Specifics of what damage and when, also shell bouncing
    public class TimestampedDamage
    {
        public int target;
        public float timestamp;
        public List<DmgAndPos> DamageDealt;
        public float totalDamageDealt;
        public Vector3 velocityModifier;
        public Vector3 projectilePos;

        public void SetTimestamp(float ts)
        {
            timestamp = ts;
        }
    }

    public struct DmgAndPos
    {
        public Vector3Int pos;
        public float dmg;

        public DmgAndPos(Vector3Int _pos, float _dmg)
        {
            pos = _pos;
            dmg = _dmg;
        }
    }
}