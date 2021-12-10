using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ASTankGame.UI
{
    public class FGUIScript : MonoBehaviour
    {
        public int weaponCount;
        public TextMeshProUGUI fgText;
        public TextMeshProUGUI fgWeaponCountText;

        public void SetText(int fgNumber, int wepCount)
        {
            gameObject.SetActive(true);

            weaponCount = wepCount;
            fgText.text = fgNumber.ToString();
            fgWeaponCountText.text = wepCount.ToString();
        }
    }
}