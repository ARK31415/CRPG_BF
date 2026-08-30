using System;
using System.IO;
using System.Reflection;
using UnityEngine;

public class BF_HybridCLRVerifyBootstrap : MonoBehaviour
{
    private const string DllFile = "CRPG_BF.HybridCLRVerify.HotUpdate.dll.bytes";
    private const string EntryType = "BF_HybridCLRVerifyEntry";
    private const string PassMessage = "HYBRIDCLR_VERIFY_PASS";

    private void Start()
    {
        try
        {
            string path = Path.Combine(Application.streamingAssetsPath, "HybridCLRVerify", DllFile);
            byte[] bytes = File.ReadAllBytes(path);
            Assembly assembly = Assembly.Load(bytes);
            Type type = assembly.GetType(EntryType, true);
            MethodInfo method = type.GetMethod("Run", BindingFlags.Public | BindingFlags.Static);
            string result = method?.Invoke(null, null) as string;

            if (result != PassMessage)
            {
                throw new Exception($"Unexpected hot-update result: {result}");
            }

            string marker = Path.Combine(Application.persistentDataPath, "hybridclr_verify_pass.txt");
            File.WriteAllText(marker, result);
            Debug.Log($"[HybridCLR Verify] {result}");
            Application.Quit(0);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Application.Quit(1);
        }
    }
}
