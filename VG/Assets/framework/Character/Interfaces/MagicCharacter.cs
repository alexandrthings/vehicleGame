using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ASTankGame.Characters.Magic
{
    public interface MagicCharacter
    {
        float Charge { get; set; }
        int SelectedElement { get; set; }
    }
}