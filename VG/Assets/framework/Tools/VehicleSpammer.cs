using System.Collections;
using System.Collections.Generic;
using VehicleBase.Vehicles.XML;
using UnityEngine;

public class VehicleSpammer : MonoBehaviour
{
    public string vehiclename = "test";
    public int spacing = 10;

    public int rows = 10;
    public int columns = 10;

    // Start is called before the first frame update
    void Start()
    {
        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < columns; y++)
            {
                VehicleXMLManager.ins.LoadVehicle(transform.position + transform.forward * y * spacing + transform.right * x * spacing, transform.rotation, vehiclename);
            }
        }
    }
}
