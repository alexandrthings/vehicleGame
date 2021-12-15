using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VehicleBase.Characters.Magic
{
    public interface MagicCharacter
    {
        float Charge { get; set; }
        int SelectedElement { get; set; }
    }
}