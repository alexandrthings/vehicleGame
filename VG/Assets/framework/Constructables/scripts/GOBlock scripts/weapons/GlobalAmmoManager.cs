using System.Collections;
using System.Collections.Generic;
using UnityEditor.Search;
using UnityEngine;

public class GlobalAmmoManager : MonoBehaviour
{
    public static GlobalAmmoManager ins;

    public Object APRound;
    public Object APCRound;
    public Object HERound;
    public Object HEATRound;
    public Object HESHRound;
    public Object AGMRound;
    public Object AGMTandemRound;

    public void Start()
    {
        ins = this;
    }

    public static Object GetAmmoPrefab(int type)
    {
        switch (type)
        {
            case 1:
                return ins.APCRound;
            case 2:
                return ins.HERound;
            case 3:
                return ins.HEATRound;
            case 4:
                return ins.HESHRound;
            case 5:
                return ins.AGMRound;
            case 6:
                return ins.AGMTandemRound;

            default:
                return ins.APRound;
        }
    }

    public Object IRSeeker;
    public Object RADARSeeker;
    public Object TVSeeker;
}
