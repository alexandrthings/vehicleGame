using System;
using System.Collections;
using System.Collections.Generic;
using ASTankGame.Damage.Animation.Events;
using Unity.Mathematics;
using UnityEngine;

namespace ASTankGame.Characters.Animation.Events
{
    public class AnimationEventHandler : MonoBehaviour
    {
        public DamageTrigger[] damageTriggers;
        public ParticleSystem[] effects;

        // first name, then time
        public void EnableHitbox(string _name)
        {
            string name = "";
            for (int i = 0; i < _name.Length; i++)
            {
                if (_name[i] == '/')
                {
                    name = _name.Substring(0, i);

                    float time = 0;

                    if (float.TryParse(_name.Substring(i + 1), out time))
                    {
                        StartNamedTrigger(name, time);
                    }

                    break;
                }
            }
        }

        public void StartNamedTrigger(string _name, float duration)
        {
            for (int i = 0; i < damageTriggers.Length; i++)
            {
                if (damageTriggers[i].TriggerName == _name)
                {
                    damageTriggers[i].Enable(duration);
                }
            }
        }

        
    }
}