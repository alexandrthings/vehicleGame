using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ASTankGame.Vehicles
{
    public interface IControlInput
    {
        float ws { get; }
        float ad { get; }
        float qe { get; }
        float rf { get; }

        Vector3 target { get; }
    }
}