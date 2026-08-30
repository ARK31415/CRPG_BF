using System;
using System.IO;
using HybridCLR.Editor;
using HybridCLR.Editor.Commands;
using HybridCLR.Editor.Settings;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public static class BF_HybridCLRVerifyBuild
{
    public const string ScenePath = "Assets/Scenes/HybridCLRVerify.unity";
    public const string AssemblyName = "CRPG_BF.HybridCLRVerify.HotUpdate";

    private const string DllFile = AssemblyName + ".dll";
    private const string DllAsset = "Assets/StreamingAssets/HybridCLRVerify/" + DllFile + ".bytes";
    private const string Output = "Temp/HybridCLRVerifyBuild/CRPG_BF_HybridCLR_Verify.exe";
    private const string AddressablesBuildKey = "Addressables.BuildAddressablesWithPlayerBuild";

    [MenuItem("Tools/CRPG_BF/HybridCLR/Build Minimal Verify Player")]
    public static void Build()
    {
        BuildTarget target = BuildTarget.StandaloneWindows64;
        Prepare();
        string output = Path.GetFullPath(Output);
        bool hadPreference = EditorPrefs.HasKey(AddressablesBuildKey);
        bool buildAddressables = EditorPrefs.GetBool(AddressablesBuildKey, true);
        EditorPrefs.SetBool(AddressablesBuildKey, false);

        try
        {
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = output,
                target = target,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                throw new Exception($"HybridCLR verify build failed: {report.summary.result}");
            }

            Debug.Log($"[HybridCLR Verify] Build succeeded: {output}");
        }
        finally
        {
            if (hadPreference)
            {
                EditorPrefs.SetBool(AddressablesBuildKey, buildAddressables);
            }
            else
            {
                EditorPrefs.DeleteKey(AddressablesBuildKey);
            }
        }
    }

    [MenuItem("Tools/CRPG_BF/HybridCLR/Prepare Minimal Verify Player")]
    public static void Prepare()
    {
        BuildTarget target = BuildTarget.StandaloneWindows64;
        ConfigureHybridCLR();
        CompileDllCommand.CompileDll(target, false);
        CopyDll(target);
        Debug.Log("[HybridCLR Verify] Hot-update DLL prepared.");
    }

    private static void ConfigureHybridCLR()
    {
        HybridCLRSettings settings = HybridCLRSettings.Instance;
        settings.enable = true;
        settings.hotUpdateAssemblies = new[] { AssemblyName };
        settings.hotUpdateAssemblyDefinitions ??= Array.Empty<AssemblyDefinitionAsset>();
        settings.preserveHotUpdateAssemblies ??= Array.Empty<string>();
        settings.externalHotUpdateAssembliyDirs ??= Array.Empty<string>();
        settings.patchAOTAssemblies ??= Array.Empty<string>();
        HybridCLRSettings.Save();
    }

    private static void CopyDll(BuildTarget target)
    {
        string source = Path.Combine(SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target), DllFile);
        string destination = Path.GetFullPath(DllAsset);
        Directory.CreateDirectory(Path.GetDirectoryName(destination));
        File.Copy(source, destination, true);
        AssetDatabase.ImportAsset(DllAsset, ImportAssetOptions.ForceUpdate);
    }
}
