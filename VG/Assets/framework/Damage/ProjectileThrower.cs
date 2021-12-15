using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VehicleBase.Damage.Projectiles
{
    public class ProjectileThrower : MonoBehaviour
    {
        public Object projectilePrefab;
        public Transform firePoint;

        public int burstLength;
        public float burstDelay;

        public void Fire()
        {
            StartCoroutine(Shoot());
        }

        IEnumerator Shoot()
        {
            int burstCounter = 0;
            while (burstLength > burstCounter)
            {
                Object.Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

                burstCounter++;
                yield return new WaitForSeconds(burstDelay);
            }

            yield break;
        }
    }
}