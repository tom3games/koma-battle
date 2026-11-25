using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Build
{
    const string ScenePath = "Assets/Scenes/Main.unity";

    static string Arg(string name, string def)
    {
        var a = Environment.GetCommandLineArgs();
        for (int i = 0; i < a.Length - 1; i++) if (a[i] == name) return a[i + 1];
        return def;
    }

    static void Mat(string name, string shader, bool emis = false)
    {
        var path = "Assets/Resources/" + name + ".mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
        var sh = Shader.Find(shader);
        if (sh == null) { Debug.LogError("KOMA no shader " + shader); return; }
        var m = new Material(sh);
        if (emis) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Color.white); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; }
        AssetDatabase.CreateAsset(m, path);
    }

    public static void MakeMats()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        Mat("M_Std", "Standard");
        Mat("M_StdEmis", "Standard", true);
        Mat("M_Vtx", "Sprites/Default");
        Mat("M_Add", "Legacy Shaders/Particles/Additive");
        Mat("M_Alpha", "Legacy Shaders/Particles/Alpha Blended");
        AssetDatabase.SaveAssets();
    }

    public static void MakeScene()
    {
        MakeMats();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var boot = Type.GetType("KomaBoot, Assembly-CSharp");
        if (boot != null)
        {
            foreach (var go in scene.GetRootGameObjects()) UnityEngine.Object.DestroyImmediate(go);
            var g = new GameObject("KomaBoot");
            g.AddComponent(boot);
        }
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
    }

    public static void WebGL()
    {
        MakeScene();
        var outDir = Arg("-out", "../build");
        var comp = Arg("-compression", "gzip");
        PlayerSettings.companyName = "tom3games";
        PlayerSettings.productName = "コマバトル";
        PlayerSettings.WebGL.compressionFormat = comp == "none" ? WebGLCompressionFormat.Disabled :
            comp == "brotli" ? WebGLCompressionFormat.Brotli : WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = comp != "none";
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.nameFilesAsHashes = false;
        var tpl = Arg("-template", "");
        if (tpl != "") PlayerSettings.WebGL.template = tpl;
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.High);
        PlayerSettings.stripEngineCode = true;
        PlayerSettings.runInBackground = true;
        var t0 = DateTime.Now;
        var opts = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = outDir,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };
        var rep = BuildPipeline.BuildPlayer(opts);
        Debug.Log("KOMA_BUILD result=" + rep.summary.result + " size=" + rep.summary.totalSize + " sec=" + (DateTime.Now - t0).TotalSeconds.ToString("F1"));
        if (rep.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) EditorApplication.Exit(1);
    }
}
