using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.XR.ARCore;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Management;

// Project configuration only: no scenes, gameplay or APK are generated here.
public static class EnvironmentSetup
{
    [MenuItem("AR Balance Bridge/Configure Android Tools")]
    public static void ConfigureAndroidTools()
    {
        var tools = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Tools/Android"));
        if (!Directory.Exists(tools))
        {
            // Portable source checkouts may use Android tools installed through Unity Hub.
            if (!File.Exists(Path.Combine(UnityEditor.Android.AndroidExternalToolsSettings.jdkRootPath, "bin/java.exe")) ||
                !File.Exists(Path.Combine(UnityEditor.Android.AndroidExternalToolsSettings.sdkRootPath, "platform-tools/adb.exe")) ||
                !File.Exists(Path.Combine(UnityEditor.Android.AndroidExternalToolsSettings.ndkRootPath, "source.properties")))
                throw new InvalidOperationException("Install Android Build Support, SDK/NDK and OpenJDK through Unity Hub, or prepare Tools/Android in the parent workspace.");
            Configure();
            Debug.Log("ANDROID_TOOLS_CONFIGURED: existing Unity external tool preferences");
            return;
        }
        foreach (var file in new[] { "OpenJDK/bin/java.exe", "SDK/platform-tools/adb.exe", "NDK/source.properties" })
            if (!File.Exists(Path.Combine(tools, file)))
                throw new FileNotFoundException("Android tool setup is incomplete", Path.Combine(tools, file));
        EditorPrefs.SetBool("JdkUseEmbedded", false);
        EditorPrefs.SetBool("SdkUseEmbedded", false);
        EditorPrefs.SetBool("NdkUseEmbedded", false);
        UnityEditor.Android.AndroidExternalToolsSettings.jdkRootPath = Path.Combine(tools, "OpenJDK");
        UnityEditor.Android.AndroidExternalToolsSettings.sdkRootPath = Path.Combine(tools, "SDK");
        UnityEditor.Android.AndroidExternalToolsSettings.ndkRootPath = Path.Combine(tools, "NDK");
        Configure();
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            throw new InvalidOperationException("Android Build Support is not available.");
        Debug.Log("ANDROID_TOOLS_CONFIGURED: " + tools);
        Debug.Log("ACTIVE_BUILD_TARGET: " + EditorUserBuildSettings.activeBuildTarget);
    }

    [MenuItem("AR Balance Bridge/Configure Environment")]
    public static void Configure()
    {
        PlayerSettings.companyName = "Allen";
        PlayerSettings.productName = "AR Balance Bridge";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.allen.arbalancebridge");
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel35;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.runInBackground = false;
        PlayerSettings.bundleVersion = "0.1.0";
        PlayerSettings.Android.bundleVersionCode = 1;

        var serializedSettings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        var inputHandler = serializedSettings.FindProperty("activeInputHandler");
        if (inputHandler == null) throw new InvalidOperationException("Input System setting not found.");
        inputHandler.intValue = 1;
        serializedSettings.ApplyModifiedPropertiesWithoutUndo();

        Directory.CreateDirectory("Assets/XR/Settings");
        AssetDatabase.Refresh();
        const string xrPath = "Assets/XR/Settings/XRGeneralSettingsPerBuildTarget.asset";
        var perTarget = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(xrPath);
        if (perTarget == null)
        {
            perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            AssetDatabase.CreateAsset(perTarget, xrPath);
        }
        EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, perTarget, true);
        if (!perTarget.HasSettingsForBuildTarget(BuildTargetGroup.Android))
            perTarget.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.Android);
        if (!perTarget.HasManagerSettingsForBuildTarget(BuildTargetGroup.Android))
            perTarget.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);
        var general = perTarget.SettingsForBuildTarget(BuildTargetGroup.Android);
        general.InitManagerOnStart = true;
        if (!XRPackageMetadataStore.AssignLoader(general.Manager, "UnityEngine.XR.ARCore.ARCoreLoader", BuildTargetGroup.Android))
            throw new InvalidOperationException("ARCore loader assignment failed.");

        var arcore = ARCoreSettings.GetOrCreateSettings();
        if (!AssetDatabase.Contains(arcore))
            AssetDatabase.CreateAsset(arcore, "Assets/XR/Settings/ARCoreSettings.asset");
        arcore.requirement = ARCoreSettings.Requirement.Required;
        arcore.depth = ARCoreSettings.Requirement.Optional;
        ARCoreSettings.currentSettings = arcore;
        EditorUtility.SetDirty(arcore);
        EditorUtility.SetDirty(perTarget);
        EditorUtility.SetDirty(general);
        EditorUtility.SetDirty(general.Manager);
        AssetDatabase.SaveAssets();
        Debug.Log("ENVIRONMENT_CONFIGURED: Android ARM64 IL2CPP, OpenGLES3, Input System, ARCore Required, landscape. Gameplay and APK remain deferred.");
    }
}

