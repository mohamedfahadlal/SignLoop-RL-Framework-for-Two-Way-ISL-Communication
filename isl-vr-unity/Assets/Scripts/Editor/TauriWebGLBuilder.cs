using UnityEditor;
using UnityEngine;
using System.IO;

public class TauriWebGLBuilder {
    [MenuItem("SignLoop/1. Switch Target to WebGL")]
    public static void SwitchTarget() {
        Debug.Log("Switching build target to WebGL...");
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
    }

    [MenuItem("SignLoop/2. Build WebGL to Tauri App")]
    public static void BuildWebGL() {
        string[] scenes = { "Assets/Scenes/DesktopTestScene.unity" };
        string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../../signloop-app/public/UnityBuild"));
        
        Debug.Log("Building WebGL to: " + path);
        
        BuildPlayerOptions options = new BuildPlayerOptions();
        options.scenes = scenes;
        options.locationPathName = path;
        options.target = BuildTarget.WebGL;
        options.options = BuildOptions.None;

        BuildPipeline.BuildPlayer(options);
        Debug.Log("WebGL Build Complete!");
    }
}
