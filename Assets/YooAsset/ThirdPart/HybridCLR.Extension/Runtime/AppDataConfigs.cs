using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace HybridCLR.Extension.Runtime
{
    [UnityEngine.Scripting.Preserve]
    [CreateAssetMenu(fileName = "AppDataConfigs", menuName = "ScriptableObject/Create AppDataConfigs")]
    public class AppDataConfigs : ScriptableObject
    {
        public string StartSceneAddress;
        public List<string> aotDllList;
    }
}