using System.Collections;
using System.Collections.Generic;
using VehicleBase.Vehicles.Building;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VehicleBase.UI
{
    public class MirrorUI : Menu
    {
        [SerializeField] private GameObject xMirror;
        [SerializeField] private GameObject yMirror;
        [SerializeField] private GameObject zMirror;

        [SerializeField] private TMP_InputField xPos;
        [SerializeField] private TMP_InputField yPos;
        [SerializeField] private TMP_InputField zPos;

        public void ToglX()
        {
            xMirror.SetActive(!xMirror.activeSelf);
            MirrorUpdate();
        }
        public void ToglY()
        {
            yMirror.SetActive(!yMirror.activeSelf);
            MirrorUpdate();
        }
        public void ToglZ()
        {
            zMirror.SetActive(!zMirror.activeSelf);
            MirrorUpdate();
        }

        public void MirrorUpdate()
        {
            if (!int.TryParse(xPos.text, out int x))
                x = 0;
            if (!int.TryParse(yPos.text, out int y))
                y = 0;
            if (!int.TryParse(zPos.text, out int z))
                z = 0;

            Constructor.SetMirror(xMirror.activeSelf, yMirror.activeSelf, zMirror.activeSelf, new Vector3Int(x, y, z));
        }

        public override void OnDeactivate()
        {
            MirrorUpdate();
        }
    }
}