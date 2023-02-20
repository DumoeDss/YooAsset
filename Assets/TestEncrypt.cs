using AquaSys.Patch.Encryption;
using AquaSys.Tools;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class TestEncrypt : MonoBehaviour
{
    public string filePath;
    public string newFilePath;
    // Start is called before the first frame update
    void Start()
    {
        Stopwatch sw = Stopwatch.StartNew();    
        AESEncrypt.Encrypt(filePath, newFilePath, "saflhafjalagaga");
        sw.Stop();
        Debug.Log($"AESEncrypt.Encrypt: {sw.ElapsedMilliseconds}");
        sw.Restart();
        StreamTools.WriteByteFile($"{newFilePath}_new.png",AESEncrypt.DecryptFile(newFilePath, "saflhafjalagaga"));
        sw.Stop();
        Debug.Log($"AESEncrypt.DecryptFile: {sw.ElapsedMilliseconds}");
        //sw.Restart();
        //AESEncrypt.AESEncryptFile(filePath, System.Text.Encoding.UTF8.GetBytes("saflhafjalagaga"), false);
        //sw.Stop();
        //Debug.Log($"AESEncrypt.AESEncryptFile: {sw.ElapsedMilliseconds}");
        //sw.Restart();
        //AESEncrypt.AESDecryptFile(filePath+ ".enc", System.Text.Encoding.UTF8.GetBytes("saflhafjalagaga"), false);
        //sw.Stop();
        //Debug.Log($"AESEncrypt.AESDecryptFile: {sw.ElapsedMilliseconds}");
        //sw.Restart();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
