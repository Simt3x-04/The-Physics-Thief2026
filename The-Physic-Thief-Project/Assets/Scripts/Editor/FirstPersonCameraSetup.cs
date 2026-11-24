#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PhysicsThief.Player;

// 一键设置第一人称摄像机：
// 1. 找到场景里的 PlayerController 和人物头部骨骼
// 2. 在头部骨骼下创建 EyePoint（大致位于两眼之间）
// 3. 把 EyePoint 填入 PlayerController.eyeAnchor
// 4. 把位于头部高度以上的模型（头、头发）设为只投影子
public static class FirstPersonCameraSetup
{
    const string ReportPath = "Temp/fp_setup_report.txt";

    [MenuItem("Tools/First Person/Setup Eye Camera")]
    public static void Setup()
    {
        var log = new StringBuilder();

        var player = Object.FindObjectsByType<PlayerController>(
            FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();

        if (player == null)
        {
            Finish(log.AppendLine("ERROR: 场景里没有找到 PlayerController。"));
            return;
        }
        log.AppendLine("Player: " + Path(player.transform));

        // ---- 找头部骨骼 ----
        Transform head = null;
        var animator = player.GetComponentInChildren<Animator>(true);
        if (animator != null && animator.isHuman)
        {
            head = animator.GetBoneTransform(HumanBodyBones.Head);
        }
        var all = player.GetComponentsInChildren<Transform>(true);
        if (head == null)
        {
            head = all.FirstOrDefault(t => t.name.EndsWith(":Head"))
                ?? all.FirstOrDefault(t => t.name == "Head")
                ?? all.FirstOrDefault(t => t.name.ToLower().EndsWith("head")
                                           && !t.name.ToLower().Contains("top"));
        }
        if (head == null)
        {
            Finish(log.AppendLine("ERROR: 没有找到头部骨骼（名字包含 Head 的物体）。"));
            return;
        }
        log.AppendLine("Head bone: " + Path(head));

        Transform headTop = all.FirstOrDefault(t => t.name.ToLower().Contains("headtop"));
        if (headTop != null) log.AppendLine("HeadTop: " + Path(headTop));

        // ---- 估算眼睛位置（世界坐标） ----
        Vector3 up = player.transform.up;
        Vector3 fwd = player.transform.forward;
        float headLen = headTop != null
            ? Vector3.Distance(head.position, headTop.position)
            : 0.2f * player.transform.lossyScale.y;
        Vector3 eyeWorld = head.position + up * (headLen * 0.4f) + fwd * (headLen * 0.45f);

        // ---- 创建 / 复用 EyePoint ----
        Transform eye = head.Find("EyePoint");
        if (eye == null)
        {
            var go = new GameObject("EyePoint");
            Undo.RegisterCreatedObjectUndo(go, "Create EyePoint");
            eye = go.transform;
            eye.SetParent(head, false);
        }
        Undo.RecordObject(eye, "Move EyePoint");
        eye.position = eyeWorld;
        eye.rotation = player.transform.rotation;
        log.AppendLine("EyePoint world: " + eyeWorld.ToString("F3")
            + "  local-to-player: " + player.transform.InverseTransformPoint(eyeWorld).ToString("F3"));

        // ---- 填入 PlayerController ----
        Undo.RecordObject(player, "Assign eye anchor");
        player.eyeAnchor = eye;
        player.eyeAnchorOffset = Vector3.zero;

        // ---- 找出头/头发模型 ----
        Camera cam = Camera.main;
        float cutY = player.transform.InverseTransformPoint(head.position).y - 0.02f;
        var hide = new List<Renderer>();
        foreach (var r in player.GetComponentsInChildren<Renderer>(true))
        {
            if (cam != null && r.transform.IsChildOf(cam.transform)) continue;
            if (r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer) continue;
            Bounds b = r.bounds;
            float minY = player.transform.InverseTransformPoint(b.min).y;
            float maxY = player.transform.InverseTransformPoint(b.max).y;
            float centerY = player.transform.InverseTransformPoint(b.center).y;
            log.AppendLine($"  Renderer {Path(r.transform)}  minY={minY:F2} centerY={centerY:F2} maxY={maxY:F2}");
            if (centerY >= cutY) hide.Add(r);
        }
        player.hideFromFirstPerson = hide.ToArray();
        log.AppendLine("Hidden renderers: " + hide.Count);
        foreach (var r in hide) log.AppendLine("  - " + Path(r.transform));

        // ---- 检查摄像机上是否有其他控制脚本 ----
        if (cam != null)
        {
            foreach (var mb in cam.GetComponents<MonoBehaviour>())
            {
                if (mb == null) continue;
                string n = mb.GetType().FullName;
                if (n.StartsWith("UnityEngine.")) continue;
                log.AppendLine("Camera script: " + n + " enabled=" + mb.enabled);
            }
        }

        EditorUtility.SetDirty(player);
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
        Selection.activeTransform = eye;

        Finish(log.AppendLine("OK"));
    }

    // 关掉 Kinematic Character Controller 示例里的 ExamplePlayer。
    // 它会在每帧通过 ExampleCharacterCamera 改写摄像机位置/角度，和 PlayerController 冲突。
    [MenuItem("Tools/First Person/Disable KCC ExamplePlayer")]
    public static void DisableExamplePlayer()
    {
        var log = new StringBuilder();
        if (EditorApplication.isPlaying)
        {
            Finish(log.AppendLine("ERROR: 请先停止 Play 模式。"));
            return;
        }
        var type = System.AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType("KinematicCharacterController.Examples.ExamplePlayer"))
            .FirstOrDefault(t => t != null);
        if (type == null)
        {
            Finish(log.AppendLine("ExamplePlayer type not found."));
            return;
        }
        foreach (var obj in Object.FindObjectsByType(type, FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var mb = (MonoBehaviour)obj;
            Undo.RecordObject(mb, "Disable ExamplePlayer");
            mb.enabled = false;
            EditorUtility.SetDirty(mb);
            EditorSceneManager.MarkSceneDirty(mb.gameObject.scene);
            log.AppendLine("Disabled ExamplePlayer on: " + Path(mb.transform));
        }
        EditorSceneManager.SaveOpenScenes();
        Finish(log.AppendLine("Saved. OK"));
    }

    static void Finish(StringBuilder log)
    {
        string text = log.ToString();
        Debug.Log("[FirstPersonCameraSetup]\n" + text);
        try { File.WriteAllText(ReportPath, text); } catch { }
    }

    static string Path(Transform t)
    {
        string p = t.name;
        while (t.parent != null) { t = t.parent; p = t.name + "/" + p; }
        return p;
    }
}
#endif
