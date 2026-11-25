#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 用切开的包 (Bag_Split.fbx) 替换角色右手里的旧包，并对齐位置
public static class BagSwapTool
{
    const string Report = "Temp/bagswap_report.txt";
    const string OldMeshName = "tripo_convert_870c34c8";

    [MenuItem("Tools/Bag/Swap To Split Bag")]
    public static void Swap()
    {
        var sb = new StringBuilder();
        var player = Object.FindFirstObjectByType<PlayerController>();
        var old = player.GetComponentsInChildren<MeshRenderer>(true)
            .FirstOrDefault(r => r.gameObject.name.StartsWith(OldMeshName));
        if (old == null) { File.WriteAllText(Report, "old bag not found"); return; }
        sb.AppendLine("old bag: " + old.transform.parent.name + "/" + old.name + " bounds " + old.bounds.center.ToString("F3") + " " + old.bounds.size.ToString("F3"));

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/BAG/Bag_Split.fbx");
        if (prefab == null) { File.WriteAllText(Report, "Bag_Split.fbx not imported"); return; }

        var existing = old.transform.parent.Find("Bag_Split");
        if (existing) Object.DestroyImmediate(existing.gameObject);

        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, old.transform.parent);
        inst.name = "Bag_Split";
        Undo.RegisterCreatedObjectUndo(inst, "Swap bag");
        // 只有一个顶层节点时，Unity 会把它当成根并改名为文件名
        var back = inst.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Bag_Back") ?? inst.transform;
        var front = inst.GetComponentsInChildren<Transform>(true).First(t => t.name == "Bag_Front");

        // 先把根放在原点，让 Bag_Back 的世界矩阵等于旧包的世界矩阵
        inst.transform.localPosition = Vector3.zero;
        inst.transform.localRotation = Quaternion.identity;
        inst.transform.localScale = Vector3.one;
        Matrix4x4 want = old.transform.localToWorldMatrix;
        Matrix4x4 backLocalInRoot = inst.transform.worldToLocalMatrix * back.localToWorldMatrix;
        Matrix4x4 rootWorld = want * backLocalInRoot.inverse;
        inst.transform.position = rootWorld.GetColumn(3);
        inst.transform.rotation = rootWorld.rotation;
        var parentScale = old.transform.parent.lossyScale;
        var ls = rootWorld.lossyScale;
        inst.transform.localScale = new Vector3(ls.x / parentScale.x, ls.y / parentScale.y, ls.z / parentScale.z);

        // 材质：原来的包材质 + 深色内衬
        var bagMat = old.sharedMaterial;
        var innerPath = "Assets/BAG/Bag_Inner.mat";
        var inner = AssetDatabase.LoadAssetAtPath<Material>(innerPath);
        if (inner == null)
        {
            inner = new Material(bagMat.shader) { name = "Bag_Inner" };
            var c = new Color(0.16f, 0.09f, 0.05f);
            if (inner.HasProperty("_BaseColor")) inner.SetColor("_BaseColor", c);
            if (inner.HasProperty("_Color")) inner.SetColor("_Color", c);
            if (inner.HasProperty("_Smoothness")) inner.SetFloat("_Smoothness", 0.1f);
            AssetDatabase.CreateAsset(inner, innerPath);
        }
        foreach (var r in inst.GetComponentsInChildren<MeshRenderer>(true))
        {
            r.sharedMaterials = new[] { bagMat, inner };
            r.shadowCastingMode = old.shadowCastingMode;
        }

        // 对比包围盒，确认对齐
        Bounds nb = new Bounds();
        bool first = true;
        foreach (var r in inst.GetComponentsInChildren<MeshRenderer>(true)) { if (first) { nb = r.bounds; first = false; } else nb.Encapsulate(r.bounds); }
        sb.AppendLine("new bounds " + nb.center.ToString("F3") + " " + nb.size.ToString("F3"));
        sb.AppendLine("center diff " + (nb.center - old.bounds.center).magnitude.ToString("F4") + " size diff " + (nb.size - old.bounds.size).magnitude.ToString("F4"));
        sb.AppendLine("front local axis right(world) " + front.right.ToString("F2") + " hinge pos " + front.position.ToString("F3"));

        Undo.RecordObject(old.gameObject, "Hide old bag");
        old.gameObject.SetActive(false);
        EditorSceneManager.MarkSceneDirty(inst.scene);
        EditorSceneManager.SaveOpenScenes();
        sb.AppendLine("OK");
        File.WriteAllText(Report, sb.ToString());
    }

    // 测试：把前半边翻开指定角度，方便在 Scene 里看效果
    [MenuItem("Tools/Bag/Test Open 30")]
    public static void TestOpen() => SetOpen(30f);
    [MenuItem("Tools/Bag/Test Open -30")]
    public static void TestOpenNeg() => SetOpen(-30f);
    [MenuItem("Tools/Bag/Test Close")]
    public static void TestClose() => SetOpen(0f);

    static void SetOpen(float a)
    {
        var front = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).FirstOrDefault(t => t.name == "Bag_Front");
        if (!front) return;
        front.localRotation = Quaternion.Euler(a, 0, 0);
        SceneView.lastActiveSceneView?.Frame(new Bounds(front.position, Vector3.one * 0.6f), false);
    }
}
#endif
