using AquaSys.Tools;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestCrc : MonoBehaviour
{
    public string filePath0;
    public string filePath1;

    // Start is called before the first frame update
    void Start()
    {
       Debug.Log($"file0: {filePath0} "+ Crc32Helper.CalcHash(filePath0));
        Debug.Log($"file1: {filePath1} " + Crc32Helper.CalcHash(filePath1));

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
