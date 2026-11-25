#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// 生成第一人称手臂：复制角色模型，只保留两条手臂和包，挂到主镜头下面，
// 用单独的叠加镜头（Overlay Camera）渲染，避免和身体或场景穿模。
public static class HandsSetupTool
{
    const string LayerName = "FPArms";
    const float Scale = 1.0f;
    static readonly Vector3 ShoulderMidInCamera = new Vector3(0f, -0.17f, 0.03f);
    static readonly string[] KeepRenderers = { "tripo_part_5_Clone", "tripo_part_6_Clone", "tripo_part_9_Clone", "tripo_part_10_Clone", "Bag_Split", "Bag_Front" };

    static int EnsureLayer()
    {
        var tm = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tm.FindProperty("layers");
        for (int i = 0; i < layers.arraySize; i++)
            if (layers.GetArrayElementAtIndex(i).stringValue == LayerName) return i;
        for (int i = 8; i < layers.arraySize; i++)
            if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue))
            {
                layers.GetArrayElementAtIndex(i).stringValue = LayerName;
                tm.ApplyModifiedProperties();
                return i;
            }
        return -1;
    }

    [MenuItem("Tools/Hands/Build First-Person Viewmodel")]
    public static void Build()
    {
        var sb = new StringBuilder();
        var pc = Object.FindFirstObjectByType<PlayerController>();
        var cam = Camera.main;
        if (!pc || !cam) { File.WriteAllText("Temp/hands_report.txt", "player or main camera missing"); return; }
        int layer = EnsureLayer();
        if (layer < 0) { File.WriteAllText("Temp/hands_report.txt", "no free layer"); return; }

        foreach (var n in new[] { "FP_Viewmodel", "FP_ViewmodelCamera" })
        {
            var o = cam.transform.Find(n);
            if (o) Object.DestroyImmediate(o.gameObject);
        }

        var gino = pc.GetComponentsInChildren<Transform>(true).First(t => t.name == "GINO by mixamo");
        var vm = Object.Instantiate(gino.gameObject);
        vm.name = "FP_Viewmodel";
        Undo.RegisterCreatedObjectUndo(vm, "Build viewmodel");

        // 清理：不要眼睛锚点、旧包、脚本、碰撞体、动画器
        foreach (var t in vm.GetComponentsInChildren<Transform>(true).ToArray())
        {
            if (!t) continue;
            if (t.name == "EyePoint" || t.name.StartsWith("tripo_convert_870c34c8")) Object.DestroyImmediate(t.gameObject);
        }
        foreach (var c in vm.GetComponentsInChildren<Component>(true).ToArray())
            if (c is MonoBehaviour || c is Collider || c is Animator || c is Rigidbody) Object.DestroyImmediate(c);

        foreach (var r in vm.GetComponentsInChildren<Renderer>(true))
        {
            r.enabled = KeepRenderers.Contains(r.gameObject.name);
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            if (r is SkinnedMeshRenderer smr) smr.updateWhenOffscreen = true;
        }
        foreach (var t in vm.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;

        vm.transform.SetParent(cam.transform, false);
        vm.transform.localRotation = Quaternion.identity;
        vm.transform.localScale = gino.lossyScale * Scale;
        var la = vm.GetComponentsInChildren<Transform>(true).First(t => t.name == "mixamorig:LeftArm");
        var ra = vm.GetComponentsInChildren<Transform>(true).First(t => t.name == "mixamorig:RightArm");
        Vector3 mid = cam.transform.InverseTransformPoint((la.position + ra.position) * 0.5f);
        vm.transform.localPosition += ShoulderMidInCamera - mid;

        // 第一人称的包缩小一点，不然会挡住大半个画面
        var vmBag = vm.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Bag_Split");
        if (vmBag) { vmBag.localScale *= 0.7f; vmBag.localPosition *= 0.7f; }   // 以手（提手）为中心缩小

        // 叠加镜头：只画手臂层，永远画在场景上面
        var oc = new GameObject("FP_ViewmodelCamera");
        oc.transform.SetParent(cam.transform, false);
        var c2 = oc.AddComponent<Camera>();
        c2.cullingMask = 1 << layer;
        c2.nearClipPlane = 0.01f; c2.farClipPlane = 5f; c2.fieldOfView = 70f;
        var data = c2.GetUniversalAdditionalCameraData();
        data.renderType = CameraRenderType.Overlay;
        data.renderShadows = false;
        var baseData = cam.GetUniversalAdditionalCameraData();
        baseData.cameraStack.RemoveAll(x => x == null);
        baseData.cameraStack.Add(c2);
        cam.cullingMask &= ~(1 << layer);
        Undo.RegisterCreatedObjectUndo(oc, "Build viewmodel camera");

        // 重新添加组件，让所有参数回到代码里的默认值
        var old = pc.GetComponent<FirstPersonHandsAction>();
        if (old) Object.DestroyImmediate(old);
        var a = Undo.AddComponent<FirstPersonHandsAction>(pc.gameObject);
        a.player = pc;
        a.viewmodel = vm.transform;
        a.worldBag = pc.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Bag_Split" && !t.IsChildOf(vm.transform));
        EditorUtility.SetDirty(a);

        EditorSceneManager.MarkSceneDirty(pc.gameObject.scene);
        EditorSceneManager.SaveOpenScenes();
        sb.AppendLine($"layer={layer} vmPos={vm.transform.localPosition:F3} vmScale={vm.transform.localScale:F3}");
        sb.AppendLine("kept renderers: " + string.Join(", ", vm.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled).Select(r => r.name)));
        sb.AppendLine($"worldBag={(a.worldBag ? a.worldBag.name : "null")} stack={baseData.cameraStack.Count}");
        File.WriteAllText("Temp/hands_report.txt", sb.ToString());
    }
}
#endif
