using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ASTankGame.Characters.ASInput
{
    public class InputModule : MonoBehaviour
    {
        protected Character myCharacter;

        // Start is called before the first frame update
        public virtual void Start()
        {
            myCharacter = transform.GetComponent<Character>();
        }

        // Update is called once per frame
        public virtual void Update()
        {
            myCharacter.WASD = new Vector2(Random.Range(-1, 1), Random.Range(-1, 1));
        }
    }
}