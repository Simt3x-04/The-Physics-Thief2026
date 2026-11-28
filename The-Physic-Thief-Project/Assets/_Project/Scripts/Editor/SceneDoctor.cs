#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class SceneDoctor
{
    static string P(Transform t) { var p = t.name; while (t.parent) { t = t.parent; p = t.name + "/" + p; } return p; }

    [MenuItem("Tools/Report/Scene Doctor")]
    public static void Run()
    {
        var sb = new StringBuilder();
        sb.AppendLine("== Cameras");
        foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            sb.AppendLine($"{P(c.transform)} active={c.gameObject.activeInHierarchy} enabled={c.enabled} tag={c.tag} depth={c.depth} pos={c.transform.position:F2}");
        sb.AppendLine("== AudioListeners");
        foreach (var a in Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            sb.AppendLine($"{P(a.transform)} active={a.gameObject.activeInHierarchy} enabled={a.enabled}");
        sb.AppendLine("== PlayerControllers");
        foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var n = mb.GetType().Name;
            if (n.Contains("Player") || n.Contains("FirstPerson") || n.Contains("CharacterController"))
                sb.AppendLine($"{n} on {P(mb.transform)} active={mb.gameObject.activeInHierarchy} enabled={mb.enabled} pos={mb.transform.position:F2} tag={mb.tag}");
        }
        foreach (var cc in Object.FindObjectsByType<CharacterController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            sb.AppendLine($"CharacterController on {P(cc.transform)} active={cc.gameObject.activeInHierarchy}");
        sb.AppendLine("== Tagged Player");
        foreach (var g in GameObject.FindGameObjectsWithTag("Player")) sb.AppendLine(P(g.transform) + " pos=" + g.transform.position.ToString("F2"));
        sb.AppendLine("== bee");
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(t => t.name.ToLower().Contains("bee") || t.name.ToLower().Contains("hornet")))
            sb.AppendLine($"{P(t)} pos={t.position:F2} comps=" + string.Join(",", t.GetComponents<Component>().Select(c => c ? c.GetType().Name : "MISSING")));
        sb.AppendLine("== Player hierarchy (depth<=3) local pos / world pos");
        var pl = GameObject.FindGameObjectWithTag("Player");
        if (pl)
        {
            void Walk(Transform t, int d)
            {
                if (d > 3) return;
                sb.AppendLine(new string(' ', d * 2) + $"{t.name} active={t.gameObject.activeSelf} local={t.localPosition:F2} world={t.position:F2}");
                foreach (Transform c in t) if (!c.name.StartsWith("mixamorig") && !c.name.StartsWith("tripo_part")) Walk(c, d + 1);
            }
            Walk(pl.transform, 0);
            var pc = pl.GetComponent<MonoBehaviour>();
            foreach (var t in pl.GetComponentsInChildren<Transform>(true))
                if (t.name == "EyePoint" || t.name == "mixamorig:Head" || t.name == "mixamorig:Hips")
                    sb.AppendLine($"{P(t)} world={t.position:F2}");
            var so = new SerializedObject(pl.GetComponents<MonoBehaviour>().First(m => m && m.GetType().Name == "PlayerController"));
            var ea = so.FindProperty("eyeAnchor");
            sb.AppendLine("eyeAnchor -> " + (ea != null && ea.objectReferenceValue ? P(((Component)ea.objectReferenceValue).transform) : "null"));
        }
        File.WriteAllText("Temp/scene_doctor.txt", sb.ToString());
    }
    // 把被单独拖走的角色模型和 Player 重新对齐：Player 移到模型所在位置，模型回到 Player 内部原点
    [MenuItem("Tools/Report/Fix Player Model Offset")]
    public static void FixModel()
    {
        var pl = GameObject.FindGameObjectWithTag("Player");
        var gino = pl ? pl.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "GINO by mixamo") : null;
        if (!gino) { File.WriteAllText("Temp/scene_doctor.txt", "not found"); return; }
        Vector3 target = gino.position;
        Undo.RecordObject(pl.transform, "Fix player");
        Undo.RecordObject(gino, "Fix player");
        var cc = pl.GetComponent<CharacterController>();
        pl.transform.position = new Vector3(target.x, pl.transform.position.y, target.z);
        gino.localPosition = Vector3.zero;
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(pl.scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        File.WriteAllText("Temp/scene_doctor.txt", $"Player now {pl.transform.position:F2}, GINO local {gino.localPosition:F2} world {gino.position:F2}");
    }
}
#endif
