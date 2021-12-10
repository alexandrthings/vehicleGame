using System.Collections;
using System.Collections.Generic;
using ASTankGame.Vehicles.Blocks;
using ASTankGame.Vehicles.Blocks.Management;
using ASTankGame.Vehicles.Building;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace ASTankGame.Vehicles.UI
{
    public class BlockMenuGenerator : MonoBehaviour
    {
        [SerializeField] private Object button;

        public KeyCode OpenButton = KeyCode.E;

        [SerializeField] public static GameObject hider;

        [SerializeField] private float xDist;
        [SerializeField] private float yDist;

        private Constructor constructor;

        // Start is called before the first frame update
        void Start()
        {
            //LoadBlocks();
            hider = transform.GetChild(0).gameObject;
        }

        void Update()
        {
            /*
            if (Input.GetKeyDown(OpenButton))
            {
                hider.SetActive(!hider.activeSelf);

                UnityEngine.Cursor.lockState = hider.activeSelf ? CursorLockMode.Confined : CursorLockMode.Locked;
            }*/
        }

        public void LoadBlocks()
        {
            int count = 0;

            foreach (Block block in GlobalBlockManager.BlockList.blocks)
            {
                GameObject blockButton = (GameObject)Object.Instantiate(button, transform.GetChild(0));
                Button thisButton = blockButton.GetComponent<Button>();

                UnityAction action = delegate { ButtonPress(count); };

                thisButton.onClick.AddListener(action); 

                // wtf

                


                blockButton.transform.GetChild(0).GetComponent<Text>().text = block.Name;

                count++;
            }
        }

        public void ButtonPress(int block)
        {
            
        }
    }
}