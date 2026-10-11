using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// 双端打包：Windows 为主，Android 顺带。
/// 菜单「精灵收集 / 打包 Windows」或「精灵收集 / 打包 APK」，
/// 命令行：Unity -batchmode -quit -projectPath . -executeMethod BuildGame.BuildWindows
/// </summary>
public static class BuildGame
{
    /// <summary>
    /// UBA pre-export / 手动调用都行：保证场景存在，并注册到 Build Settings。
    /// UBA 的标准构建从 EditorBuildSettings.scenes 读场景列表，空列表会直接报
    /// "There were no scenes configured to build!"，与传给 BuildPlayer 的数组无关。
    /// </summary>
    public static void EnsureScene()
    {
        // 云构建机上资源可能还没导入完，重试几次
        for (int attempt = 0; attempt < 3; attempt++)
        {
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.isLoaded || scene.name != "Main")
                ProjectSetup.BuildScene();

            // 验证场景文件真的生成了
            if (System.IO.File.Exists("Assets/Scenes/Main.unity"))
            {
                Debug.Log($"场景已就绪 (尝试 {attempt + 1}/3)");
                break;
            }
            Debug.LogWarning($"场景文件未生成，重试 {attempt + 1}/3...");
            System.Threading.Thread.Sleep(5000);
        }
        if (!System.IO.File.Exists("Assets/Scenes/Main.unity"))
            throw new System.Exception("场景生成失败：Assets/Scenes/Main.unity 不存在，检查 GLB 是否导入成功");

        // 注册到 Build Settings：UBA 标准构建只认这里的列表
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true)
        };
        Debug.Log("场景已注册到 Build Settings: Assets/Scenes/Main.unity");
    }

    static void CommonSettings()
    {
        PlayerSettings.companyName = "Bo";
        PlayerSettings.productName = "精灵收集";
        PlayerSettings.bundleVersion = "0.1.0";
    }

    [MenuItem("精灵收集 / 打包 Windows")]
    public static void BuildWindows()
    {
        EnsureScene();
        CommonSettings();
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Standalone, "com.bo.creaturegame");
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.IL2CPP);
        // 竖屏窗口：手机 UI 布局直接沿用
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;

        var scenes = new[] { "Assets/Scenes/Main.unity" };
        System.IO.Directory.CreateDirectory("Builds/Windows");
        var report = BuildPipeline.BuildPlayer(scenes, "Builds/Windows/creature-game.exe",
            BuildTarget.StandaloneWindows64, BuildOptions.None);
        Debug.Log("Windows 打包结果: " + report.summary.result);
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            EditorApplication.Exit(1);
    }

    // 仅用于 CI 验证：Linux 构建不需要额外模块
    public static void BuildLinuxTest()
    {
        EnsureScene();
        CommonSettings();
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Standalone, "com.bo.creaturegame");
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.IL2CPP);

        var scenes = new[] { "Assets/Scenes/Main.unity" };
        System.IO.Directory.CreateDirectory("Builds/LinuxTest");
        var report = BuildPipeline.BuildPlayer(scenes, "Builds/LinuxTest/creature-game.x86_64",
            BuildTarget.StandaloneLinux64, BuildOptions.None);
        Debug.Log("Linux 测试打包结果: " + report.summary.result);
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            EditorApplication.Exit(1);
    }

    [MenuItem("精灵收集 / 打包 WebGL")]
    public static void BuildWebGL()
    {
        EnsureScene();
        CommonSettings();
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.WebGL, "com.bo.creaturegame");
        // WebGL 模板：默认
        PlayerSettings.WebGL.template = "APPLICATION:Default";

        var scenes = new[] { "Assets/Scenes/Main.unity" };
        System.IO.Directory.CreateDirectory("Builds/WebGL");
        var report = BuildPipeline.BuildPlayer(scenes, "Builds/WebGL",
            BuildTarget.WebGL, BuildOptions.None);
        Debug.Log("WebGL 打包结果: " + report.summary.result);
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            EditorApplication.Exit(1);
    }

    [MenuItem("精灵收集 / 打包 APK")]
    public static void BuildAndroid()
    {
        EnsureScene();
        CommonSettings();
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.bo.creaturegame");
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        // Demo 包用 Mono：IL2CPP 在小内存构建机上容易 OOM；正式 release 再切 IL2CPP
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;

        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.androidBuildType = AndroidBuildType.Release;

        var scenes = new[] { "Assets/Scenes/Main.unity" };
        System.IO.Directory.CreateDirectory("Builds");
        var report = BuildPipeline.BuildPlayer(scenes, "Builds/creature-game.apk",
            BuildTarget.Android, BuildOptions.None);
        Debug.Log("APK 打包结果: " + report.summary.result);
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            EditorApplication.Exit(1);
    }
}
