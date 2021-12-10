using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ASTankGame.Characters.Movement
{
    public class Dodge : MoveType
    {
        public float maxSpeed;
        public float duration;
            /*[HideInInspector]*/ public Vector2 entryInput;
        [SerializeField] DodgeType currentType;

        private bool triggered;

        public LayerMask HaltMask;

        [Tooltip("first is standard dodge \n second is slide \n third is blink \n 4th is yeet")]
        public ParticleSystem[] particleEffects;

        public override void Begin()
        {
            base.Begin();

            StopAllCoroutines();

            if (entryInput.magnitude < 0.1f)
            {
                entryInput = transform.forward;
            }

            triggered = false;

            switch (currentType)
            {
                case DodgeType.Dodge:
                    rb.AddForce((character.TargetForward * entryInput.y + character.TargetRight * entryInput.x) * MoveForce/2);
                    StartCoroutine(DodgeMove());
                    break;
                case DodgeType.Slide:

                    if (!character.Grounded)
                    {
                        character.StopAnimation();
                        return;
                    }

                    StartCoroutine(SlideMove());
                    break;
                case DodgeType.Blink:
                    StartCoroutine(BlinkMove());
                    break;
                case DodgeType.Yeet:
                    StartCoroutine(YeetMove());
                    break;
            }
        }

        public override void Start()
        {
            base.Start();

        }

        public override void Update()
        {
            base.Update();

            if (!Run)
                return;

            Quaternion targetRot = Quaternion.LookRotation(character.TargetRight * entryInput.x + character.TargetForward * entryInput.y, Vector3.up);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, 0.2f);
        }

        public override void FixedUpdate()
        {
            if (!Run)
                return;
        }

        IEnumerator DodgeMove()
        {
            StartParticles();

            float t = 0;
            StartCoroutine(AccelerateForTime((character.TargetForward * entryInput.y + character.TargetRight * entryInput.x) * maxSpeed, duration));

            while (t < duration)
            {
                t += Time.deltaTime;

                yield return new WaitForEndOfFrame();
            }
           
            StopAllCoroutines();
        }

        IEnumerator SlideMove()
        {
            yield return new WaitForFixedUpdate();
        }

        IEnumerator BlinkMove()
        {
            float t = 0;
            while (t < duration)
            {
                if (t > 0.2f && !triggered)
                {
                    StartParticles();
                }

                t += Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }

            yield return new WaitForFixedUpdate();
        }

        IEnumerator YeetMove()
        {
            yield return new WaitForFixedUpdate();
        }

        void StartParticles()
        {
            triggered = true;
            foreach (ParticleSystem effect in particleEffects)
            {
                effect.Play();
            }
        }

        enum DodgeType
        {
            Dodge,
            Slide,
            Blink,
            Yeet
        }
    }
}