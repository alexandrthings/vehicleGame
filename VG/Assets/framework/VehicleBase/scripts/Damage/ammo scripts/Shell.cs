using System.Collections;
using System.Collections.Generic;
using VehicleBase.Damage;
using VehicleBase.Utility;
using UnityEngine;
using UnityEngine.SubsystemsImplementation;

namespace VehicleBase.Vehicles.Ammo
{
    public class Shell : MonoBehaviour, DamageSource
    {
        public float Damage { get; set; }
        public float APValue { get; set; }
        public DamageType DmgType { get { return DamageType; } }
        public DamageType DamageType;
        public float Radius { get; set; }

        public bool IsLocalPlayer;

        public Vector3 position { get; set; }
        public Vector3 velocity { get { return rb.velocity; } }

        public LayerMask vehicleBoundMask;
        public LayerMask vehicleMask;

        public Object explosion;
        private Rigidbody rb;

        void Start()
        {
            Destroy(gameObject, 240); // max survival time 4 mins
        }

        private void FixedUpdate()
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, rb.velocity.normalized, out hit, rb.velocity.magnitude * Time.fixedDeltaTime, vehicleMask))
            {
                position = hit.point;
                CheckVehicleHits();
            }
        }

        public IEnumerator rotate()
        {
            while (true)
            {
                transform.LookAt(transform.position + rb.velocity);
                yield return new WaitForSeconds(0.3f);
            }
        }

        public void SetupShell(float rad, float speed, float AP)
        {
            Radius = rad;
            transform.localScale = Vector3.one * rad/1000;
            rb = transform.GetComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.velocity = transform.forward * speed;
            Damage = rad * speed;
            APValue = AP;
            StartCoroutine(rotate());
            enabled = true;
        }

        public void CheckVehicleHits()
        {
            RaycastHit[] hits = Physics.SphereCastAll(
                transform.position - rb.velocity * Time.fixedDeltaTime,
                Radius / 500,
                rb.velocity.normalized,
                rb.velocity.magnitude * Time.fixedDeltaTime * 2,
                vehicleBoundMask);

            if (hits.Length > 0)
            {
                Vehicle[] targets = new Vehicle[hits.Length];

                for (int i = 0; i < hits.Length; i++)
                {
                    targets[i] = hits[i].transform.GetComponent<Vehicle>();
                }

                PenetrationSolution pSol = DamageCalculator.CalculatePenetrationSolution(new DamageSource[] {this},
                    targets, rb.velocity.magnitude * 10 * Time.fixedDeltaTime);

                if (IsLocalPlayer)
                    DamageReplayer.QueueReplay(pSol);

                pSol.ApplyDamage();
            }

            Destroy(gameObject);
        }

        public void OnTriggerEnter(Collider col)
        {
            if (enabled)
                Destroy(gameObject,1);
        }

        void OnDestroy()
        {
            StopCoroutine(rotate());

            if (explosion != null)
                Destroy(Object.Instantiate(explosion, transform.position, Quaternion.identity), 5);
        }
    }
}