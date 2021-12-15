using System.Collections;
using System.Collections.Generic;
using VehicleBase.Damage;
using VehicleBase.Utility;
using UnityEngine;

namespace VehicleBase.Damage.Animation.Events
{
    public class DamageTrigger : MonoBehaviour
    {
        public string TriggerName;

        [Header("Damage")]
        public DamageType type;
        
        public float enterDamage;

        public bool DoT;
        public float ContinuousDPS;

        [Header("Duration")]
        public float Duration;
        private float timer;

        public ParticleSystem PlayOnContact;

        private List<IDamageableObject> damageableObjects = new List<IDamageableObject>();
        private Collider myCollider;

        public void Start()
        {
            myCollider = transform.GetComponent<Collider>();
        }

        public void Enable(float duration)
        {
            myCollider.enabled = true;
            Duration = duration;
            timer = 0;
        }

        public void OnTriggerEnter(Collider other)
        {
            IDamageableObject newObject = TransformTools.GetHeirarchyParent(other.transform).GetComponent<IDamageableObject>();

            newObject.TakeDamage(enterDamage, transform.position, type);

            damageableObjects.Add(newObject);

            if (PlayOnContact != null)
                if (!PlayOnContact.isPlaying)
                    PlayOnContact.Play();
        }

        public void Update()
        {
            if (!myCollider.enabled)
                return;

            if (timer > Duration)
                myCollider.enabled = false;

            timer += Time.deltaTime;

            if (!DoT)
                return;

            for (int i = 0; i < damageableObjects.Count; i++)
            {
                damageableObjects[i].TakeDamage(ContinuousDPS * Time.deltaTime, transform.position, type);
            }
        }

        public void OnTriggerExit(Collider other)
        {
            if (!DoT)
                return;

            IDamageableObject existingObject = TransformTools.GetHeirarchyParent(other.transform).GetComponent<IDamageableObject>();

            damageableObjects.Remove(existingObject);

            if (PlayOnContact != null && damageableObjects.Count == 0)
                PlayOnContact.Stop();
        }
    }
}