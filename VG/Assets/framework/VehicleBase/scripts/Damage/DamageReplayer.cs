using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using VehicleBase.Vehicles;
using VehicleBase.Vehicles.BlockBehaviors;
using VehicleBase.Vehicles.Blocks;
using VehicleBase.Vehicles.Blocks.Management;
using VehicleBase.Vehicles.Chunks;
using Cinemachine;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

namespace VehicleBase.Damage
{
    public class DamageReplayer : MonoBehaviour
    {
        public static DamageReplayer instance;

        public UnityEngine.Camera replayCamera;
        public Object destroyedBlockPrefab;
        public int defaultBlockCount = 100;
        public LayerMask vm;

        private Queue<GameObject> vehiclePool = new Queue<GameObject>();
        private Queue<GameObject> blockPool = new Queue<GameObject>();

        private List<GameObject> targets = new List<GameObject>();
        private GameObject shellObject;

        private bool Playing = false;

        public void OnEnable()
        {
            instance = this;

            InstantiatePools();
        }

        public static void QueueReplay(PenetrationSolution solution)
        {
            instance.Replay(solution);
        }

        public void Replay(PenetrationSolution solution)
        {
            if (Playing)
                return;

            Playing = true;
            replayCamera.gameObject.SetActive(true);

            InstantiateReplayObject(solution);

            replayCamera.gameObject.SetActive(true);

            IEnumerator damagePlayer = PlayDamage(solution);

            StartCoroutine(damagePlayer);
        }

        private void InstantiateReplayObject(PenetrationSolution solution)
        {
            Vector3 relativeStartPos = solution.Targets[0].transform.position;

            for (int i = 0; i < solution.Targets.Length; i++)
            {
                GameObject tgtGO = PullVehicleObject();

                Debug.Log($"{i}, vehicle pos {tgtGO.transform.position}");

                CopyObject(relativeStartPos, solution.Targets[i].gameObject, tgtGO, 1);

                tgtGO.transform.position = solution.Targets[i].transform.position - relativeStartPos;
                tgtGO.transform.rotation = solution.Targets[i].transform.rotation;

                targets.Insert(i, tgtGO);
            }
        }

        private void CopyObject(Vector3 offset, GameObject target, GameObject output, float prevScale)
        {
            if (LayerMask.LayerToName(target.layer) != "Vehicle" && LayerMask.LayerToName(target.layer) != "VehicleBounds")
                return;

            float newScale = prevScale * target.transform.localScale.x;

            if (target.TryGetComponent(out MeshRenderer mr))
            {
                MeshFilter mf = target.GetComponent<MeshFilter>();

                MeshFilter nmf = output.GetComponent<MeshFilter>();
                nmf.sharedMesh = mf.sharedMesh;

                MeshRenderer nmr = output.GetComponent<MeshRenderer>();
                nmr.materials = mr.materials;

                output.transform.localPosition = target.transform.position - offset;
                output.transform.rotation = target.transform.rotation;
                output.transform.localScale = target.transform.localScale * prevScale;

                output.SetActive(true);
            }

            for (int c = 0; c < target.transform.childCount; c++)
            {
                GameObject newChild = PullVehicleObject();
                targets.Add(newChild);
                CopyObject(offset, target.transform.GetChild(c).gameObject, newChild, newScale);
            }
        }

        private IEnumerator PlayDamage(PenetrationSolution solution)
        {
            Vector3 relativeStartPos = solution.Targets[0].transform.position;

            GameObject shell = (GameObject)Object.Instantiate(
                GlobalAmmoManager.GetAmmoPrefab(solution.ShellType), 
                solution.Targets[0].transform.position - solution.shellStartPoint, 
                transform.rotation, transform);

            shell.transform.localPosition = solution.shellStartPoint * solution.Targets[0].Scale;

            replayCamera.transform.parent = shell.transform;

            replayCamera.transform.localPosition = new Vector3(0, 0.2f, -1);
            replayCamera.transform.localRotation = Quaternion.identity;

            List<TimestampedDamage> damageTimestamps = new List<TimestampedDamage>();

            for (int i = 0; i < solution.DamageSequences.Count; i++)
                damageTimestamps.AddRange(solution.DamageSequences[i].DamageTimestamps);

            for (int i = 0; i < damageTimestamps.Count; i++)
                Debug.LogWarning(ShellPosToGlobal(targets[damageTimestamps[i].target].transform, solution.Targets[damageTimestamps[i].target], damageTimestamps[i]));

            float interp = 0f;
            float nextTimestamp = damageTimestamps[0].timestamp + 0.25f;
            int index = -1; // index of current interpolation pair

            Vector3[] positions = new Vector3[2]
            {
                shell.transform.localPosition, 
                ShellPosToGlobal(targets[damageTimestamps[0].target].transform, solution.Targets[damageTimestamps[0].target], damageTimestamps[0])
            };

            while (interp <= 1.25f)
            {
                shell.transform.localPosition = Vector3.Lerp(positions[0], positions[1], interp / (nextTimestamp + 0.0001f));

                shell.transform.rotation = Quaternion.LookRotation(positions[1] - positions[0]);

                if (interp > damageTimestamps[index + 1].timestamp)
                {
                    index += 1;

                    if (index + 1 >= damageTimestamps.Count)
                        goto PLAYINGDONE;

                    int vehIndex = damageTimestamps[index].target;

                    positions[0] = ShellPosToGlobal(targets[vehIndex].transform, solution.Targets[vehIndex], damageTimestamps[index]);
                    positions[1] = ShellPosToGlobal(targets[vehIndex].transform, solution.Targets[vehIndex], damageTimestamps[index+1]);

                    nextTimestamp = damageTimestamps[index+1].timestamp + 0.25f;

                    Debug.Log($"index {index}, p0 {positions[0]} p1 {positions[1]}, interp {interp / (nextTimestamp + 0.0001f)}");
                }

                interp += Time.fixedDeltaTime * 0.1f;
                yield return new WaitForFixedUpdate();
            }

            PLAYINGDONE:

            yield return new WaitForSeconds(1);

            for (int i = 0; i < targets.Count; i++)
                targets[i].SetActive(false);

            targets.Clear();
            replayCamera.transform.parent = transform;
            Destroy(shell);

            replayCamera.gameObject.SetActive(false);

            Playing = false;
        }

        public Vector3 ShellPosToGlobal(Transform newTarget, Vehicle target, TimestampedDamage damage)
        {
            //Debug.Log($"dpos {damage.projectilePos}");
            return newTarget.TransformPoint(damage.projectilePos * target.Scale);
        }

        private void InstantiatePools()
        {
            for (int i = 0; i < defaultBlockCount; i++)
            {
                GameObject spawned = (GameObject)Object.Instantiate(destroyedBlockPrefab, transform.position, transform.rotation, transform);
                blockPool.Enqueue(spawned);
                spawned.SetActive(false);
            }

            for (int i = 0; i < 500; i++)
            {
                GameObject spawned = new GameObject("VEHICLEPREFAB");
                spawned.AddComponent<MeshFilter>();
                spawned.AddComponent<MeshRenderer>();
                spawned.transform.parent = transform;
                vehiclePool.Enqueue(spawned);
                spawned.SetActive(false);
            }
        }

        private GameObject PullVehicleObject()
        {
            GameObject pulled = vehiclePool.Dequeue();
            vehiclePool.Enqueue(pulled);
            return pulled;
        }

#if UNITY_EDITOR
        public void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;

        }
        #endif
    }
}