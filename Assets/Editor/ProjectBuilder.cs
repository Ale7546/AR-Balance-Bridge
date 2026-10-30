using System;
using System.IO;
using BalanceBridge;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.XR.CoreUtils;

public static class ProjectBuilder
{
    public const string ScenePath = "Assets/ARBalanceBridge/Scenes/Main.unity";
    [MenuItem("AR Balance Bridge/Create Main Scene")]
    public static void CreateScene()
    {
        EnvironmentSetup.ConfigureAndroidTools();
        Directory.CreateDirectory("Assets/ARBalanceBridge/Scenes");
        Directory.CreateDirectory("Assets/ARBalanceBridge/Resources");
        if (AssetDatabase.LoadAssetAtPath<Material>("Assets/ARBalanceBridge/Resources/BridgeSurface.mat") == null)
            AssetDatabase.CreateAsset(new Material(Shader.Find("Standard")), "Assets/ARBalanceBridge/Resources/BridgeSurface.mat");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var session = new GameObject("AR Session").AddComponent<ARSession>();
        session.gameObject.AddComponent<ARInputManager>();
        var origin = new GameObject("XR Origin (AR)").AddComponent<XROrigin>();
        var offset = new GameObject("Camera Offset"); offset.transform.SetParent(origin.transform, false);
        var cameraObject = new GameObject("AR Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera"; cameraObject.transform.SetParent(offset.transform, false);
        var camera = cameraObject.GetComponent<Camera>(); camera.nearClipPlane = 0.01f; camera.farClipPlane = 30;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
        origin.Camera = camera; origin.CameraFloorOffsetObject = offset; origin.CameraYOffset = 0;
        var cameraManager = cameraObject.AddComponent<ARCameraManager>(); cameraManager.autoFocusRequested = true;
        cameraManager.requestedFacingDirection = CameraFacingDirection.World;
        cameraObject.AddComponent<ARCameraBackground>();
        var driver = cameraObject.AddComponent<TrackedPoseDriver>();
        var position = new InputAction("AR camera position", InputActionType.Value, "<XRHMD>/centerEyePosition", expectedControlType: "Vector3");
        position.AddBinding("<HandheldARInputDevice>/devicePosition");
        var rotation = new InputAction("AR camera rotation", InputActionType.Value, "<XRHMD>/centerEyeRotation", expectedControlType: "Quaternion");
        rotation.AddBinding("<HandheldARInputDevice>/deviceRotation");
        driver.positionInput = new InputActionProperty(position); driver.rotationInput = new InputActionProperty(rotation);
        var tracking = new InputAction("AR tracking state", InputActionType.Value, "<XRHMD>/trackingState", expectedControlType: "Integer");
        tracking.AddBinding("<HandheldARInputDevice>/trackingState"); driver.trackingStateInput = new InputActionProperty(tracking);
        var planes = origin.gameObject.AddComponent<ARPlaneManager>(); planes.requestedDetectionMode = PlaneDetectionMode.Horizontal;
        var raycasts = origin.gameObject.AddComponent<ARRaycastManager>();
        var game = new GameObject("AR Balance Bridge").AddComponent<GameController>();
        game.ARCamera = camera; game.Planes = planes; game.Raycasts = raycasts; game.Session = session;
        var light = new GameObject("Warm daylight").AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.2f;
        light.transform.rotation = Quaternion.Euler(45, -30, 0); light.shadows = LightShadows.Soft;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.60f, 0.68f, 0.73f);
        QualitySettings.vSyncCount = 0; QualitySettings.antiAliasing = 4; Time.fixedDeltaTime = 0.02f;
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        // Runtime-created materials must survive shader stripping in Android builds.
        var graphics = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
        var shaders = graphics.FindProperty("m_AlwaysIncludedShaders");
        for (int i = shaders.arraySize - 1; i >= 0; i--)
            if (shaders.GetArrayElementAtIndex(i).objectReferenceValue == Shader.Find("Standard"))
            { shaders.GetArrayElementAtIndex(i).objectReferenceValue = null; shaders.DeleteArrayElementAtIndex(i); }
        foreach (var name in new[] { "Sprites/Default", "UI/Default" })
        {
            var shader = Shader.Find(name); bool found = false;
            for (int i = 0; i < shaders.arraySize; i++) if (shaders.GetArrayElementAtIndex(i).objectReferenceValue == shader) found = true;
            if (!found) { shaders.arraySize++; shaders.GetArrayElementAtIndex(shaders.arraySize - 1).objectReferenceValue = shader; }
        }
        graphics.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets(); Debug.Log("SCENE_READY: " + ScenePath);
    }
    [MenuItem("AR Balance Bridge/Build Android APK")]
    public static void BuildAndroid()
    {
        CreateScene();
        string output = Path.GetFullPath("../Builds/Android/ARBalanceBridge.apk");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        EditorUserBuildSettings.buildAppBundle = false;
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { ScenePath }, locationPathName = output, target = BuildTarget.Android, options = BuildOptions.Development });
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Android build failed: " + report.summary.result);
        Debug.Log("APK_READY: " + output + " bytes=" + report.summary.totalSize);
    }
    public static void BuildPractice()
    {
        CreateScene();
        string output = Path.GetFullPath("../Builds/Practice/ARBalanceBridge.exe"); Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { ScenePath }, locationPathName = output, target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Practice build failed");
    }
    public static void VerifyPhysics()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var previous = Physics.simulationMode; Physics.simulationMode = SimulationMode.Script;
        try
        {
            foreach (int level in new[] { 1, 2 })
            foreach (float yaw in new[] { 0f, 47f, 180f })
            {
                var game = new GameObject("Physics integration check").AddComponent<GameController>(); game.PracticeMode = true;
                game.Level = level;
                game.CreateRound(new Pose(new Vector3(0.2f, 0.4f, -0.3f), Quaternion.Euler(0, yaw, 0)));
                Physics.SyncTransforms();
                for (int frame = 0; frame < 800 && game.State == RoundState.Playing; frame++)
                { game.Ball.Step(Vector2.up, game.Course.transform.rotation); Physics.Simulate(0.02f); game.EvaluateRound(); }
                if (game.State != RoundState.Won) throw new Exception("Forward crossing failed at yaw " + yaw + ", position=" + game.Course.transform.InverseTransformPoint(game.Ball.Body.position));
                game.Retry();
                if (game.State != RoundState.Playing || game.Ball.Body.linearVelocity != Vector3.zero || game.Ball.Body.angularVelocity != Vector3.zero) throw new Exception("Retry did not reset state/velocity");
                Physics.SyncTransforms();
                for (int frame = 0; frame < 400 && game.State == RoundState.Playing; frame++)
                { game.Ball.Step(new Vector2(1, 1).normalized, game.Course.transform.rotation); Physics.Simulate(0.02f); game.EvaluateRound(); }
                if (game.State != RoundState.Lost) throw new Exception("Side fall failed at yaw " + yaw);
                game.EvaluateRound(); if (game.State != RoundState.Lost) throw new Exception("Terminal state changed unexpectedly");
                UnityEngine.Object.DestroyImmediate(game.Ball.gameObject); UnityEngine.Object.DestroyImmediate(game.Course); UnityEngine.Object.DestroyImmediate(game.gameObject);
                Debug.Log("PHYSICS_PASS: level " + level + " crossing, retry, fall at yaw " + yaw);
            }
            Debug.Log("PHYSICS_QA_PASS: three real-collision crossings and three falls");
        }
        finally { Physics.simulationMode = previous; }
    }
}
