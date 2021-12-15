using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VehicleBase.Vehicles.Building
{
    public class BuildTrigger : MonoBehaviour
    {
        public bool BuildValid;
        public bool Stayed;
        public bool MainTrigger; // this is the one that the player is looking at

        public List<Renderer> renderers = new List<Renderer>();

        void OnEnable()
        {
            GetAllRenderers(transform);
        }

        void GetAllRenderers(Transform target)
        {
            if (target.TryGetComponent(out Renderer ren))
            {
                renderers.Add(ren);
            }

            for (int i = 0; i < target.childCount; i++)
            {
                GetAllRenderers(target.GetChild(i));
            }
        }

        void FixedUpdate()
        {
            if (Stayed)
                Stayed = false;
            else
                BuildValid = true;
        }

        void OnTriggerStay(Collider other)
        {
            if (other.gameObject.layer == LayerMask.NameToLayer("PreviewCollider") && MainTrigger)
                return;

            BuildValid = false;
            Stayed = true;
        }

    }
}