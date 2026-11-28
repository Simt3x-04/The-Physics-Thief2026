#if UNITY_EDITOR
using System.IO;
using System.Linq;
using PhysicsThief.Enemies;
using UnityEditor;
using UnityEngine;

public static class CarrotAnimTool
{
    const string PrefabPath = "Assets/_Project/Prefabs/Enemies/AngryCarrotPrefab.prefab";
    const string Report = "Temp/carrot_report.txt";

    [MenuItem("Tools/Carrot/Add Animator To Prefab")]
    public static void AddToPrefab()
    {
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var anim = root.GetComponent<AngryCarrotAnimator>();
            if (!anim) anim = root.AddComponent<AngryCarrotAnimator>();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            var vis = root.transform.Find("Visual");
            File.WriteAllText(Report, $"OK added. visual={(vis ? vis.name : "null")} children={root.transform.childCount}");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    static AngryCarrotAnimator SceneCarrot()
    {
        var a = Object.FindObjectsByType<AngryCarrotAnimator>(FindObjectsSortMode.None).FirstOrDefault();
        return a;
    }

    static void Pose(string anim, float t)
    {
        var a = SceneCarrot();
        if (!a) { File.WriteAllText(Report, "no carrot with animator in scene"); return; }
        a.EditorPose(anim, t);
        var sv = SceneView.lastActiveSceneView;
        if (sv)
        {
            var c = a.transform.position + Vector3.up * 0.7f;
            // look at the carrot from its right side, slightly above
            var dir = (a.transform.right * 1f + a.transform.forward * 0.35f + Vector3.up * 0.25f).normalized;
            sv.LookAt(c, Quaternion.LookRotation(-dir), 2.2f, false, false);
            sv.Repaint();
        }
        File.WriteAllText(Report, $"pose {anim} at {a.transform.position:F1}");
    }

    [MenuItem("Tools/Carrot/Preview/Rest")] static void P0() => Pose("rest", 0);
    [MenuItem("Tools/Carrot/Preview/Walk (top of hop)")] static void P1() => Pose("walk", 0.5f);
    [MenuItem("Tools/Carrot/Preview/Run (top of hop)")] static void P2() => Pose("run", 0.5f);
    [MenuItem("Tools/Carrot/Preview/Attack wind-up")] static void P3() => Pose("windup", 0);
    [MenuItem("Tools/Carrot/Preview/Attack strike")] static void P4() => Pose("attack", 0);
    [MenuItem("Tools/Carrot/Preview/Spawn (half out)")] static void P5() => Pose("spawn", 0);
    [MenuItem("Tools/Carrot/Preview/Dead")] static void P6() => Pose("dead", 0);

    [MenuItem("Tools/Carrot/Preview/Reset Pose")]
    static void Reset()
    {
        var a = SceneCarrot();
        if (a) a.EditorReset();
        SceneView.RepaintAll();
    }
    [MenuItem("Tools/Carrot/Preview/Death Puff (0.15s)")] static void Puff1() => PuffPreview(0.15f);
    [MenuItem("Tools/Carrot/Preview/Death Puff (0.45s)")] static void Puff2() => PuffPreview(0.45f);

    static void PuffPreview(float t)
    {
        var a = SceneCarrot();
        if (!a) return;
        foreach (var old in Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
            if (old.name == "DeathPuff") Object.DestroyImmediate(old.gameObject);
        var go = DeathPuff.Spawn(a.transform.position + Vector3.up * 0.65f, 1f);
        var ps = go.GetComponent<ParticleSystem>();
        var main = ps.main; main.stopAction = ParticleSystemStopAction.None;
        ps.Simulate(t, true, true);
        foreach (var r in a.GetComponentsInChildren<Renderer>()) r.enabled = false;   // carrot already vanished
        var sv = SceneView.lastActiveSceneView;
        if (sv)
        {
            var c = a.transform.position + Vector3.up * 0.8f;
            var dir = (a.transform.right + a.transform.forward * 0.35f + Vector3.up * 0.25f).normalized;
            sv.LookAt(c, Quaternion.LookRotation(-dir), 2.6f, false, false);
            sv.Repaint();
        }
        File.WriteAllText(Report, $"puff preview t={t} particles={ps.particleCount}");
    }
}
#endif
