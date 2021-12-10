using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AngleConv
{
    public static float Conv360to180(float value)
    {
        if (value > 360)
            value -= 360;
        else if (value < 0)
            value += 360;

        if (value <= 180)
            return value;
        else
            return -(360 - value);
    }

    public static float Conv360to180Inv(float value)
    {
        if (value > 360)
            value -= 360;
        else if (value < 0)
            value += 360;

        if (value <= 180)
            return -value;
        else
            return (360 - value);
    }
}
