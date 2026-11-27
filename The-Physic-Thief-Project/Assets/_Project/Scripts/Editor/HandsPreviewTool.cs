#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PhysicsThief.Player;
using UnityEngine.SceneManagement;

public static class HandsPreviewTool
{
    const string DirtyKey = "HandsPreviewDirty";

    // The preview changes the bone pose; reloading the scene from disk = discarding every change made by the preview
    static void ReloadScene()
    {
        var path = SceneManager.GetActiveScene().path;
        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        SessionState.SetBool(DirtyKey, false);
    }

    static void Preview(FirstPersonHandsAction.ActionType type, float time)
    {
        if (EditorApplication.isPlaying) return;
        if (SessionState.GetBool(DirtyKey, false)) ReloadScene();
        var a = Object.FindFirstObjectByType<FirstPersonHandsAction>();
        if (!a || !a.viewmodel) return;
        SessionState.SetBool(DirtyKey, true);
        a.EditorPreview(type, time);
        var cam = a.viewmodel.parent;
        var sv = SceneView.lastActiveSceneView;
        if (sv)
        {
            sv.orthographic = false;
            sv.cameraSettings.fieldOfView = 70f;
            sv.cameraSettings.nearClip = 0.01f;
            sv.cameraSettings.dynamicClip = false;
            float dist = 0.6f;
            float size = dist * Mathf.Tan(70f * 0.5f * Mathf.Deg2Rad);
            sv.LookAt(cam.position + cam.forward * dist, cam.rotation, size, false, true);
            sv.Repaint();
        }
        var sb = new System.Text.StringBuilder($"{type} t={time}\n");
        foreach (var tr in a.viewmodel.GetComponentsInChildren<Transform>(true))
            if (tr.name == "mixamorig:RightHand" || tr.name == "mixamorig:LeftHand" || tr.name == "mixamorig:RightArm" || tr.name == "mixamorig:LeftArm" || tr.name == "mixamorig:RightForeArm" || tr.name == "Bag_Split" || tr.name == "Bag_Front")
                sb.AppendLine($"{tr.name} camLocal={cam.InverseTransformPoint(tr.position):F3}");
        foreach (var r in a.viewmodel.GetComponentsInChildren<Renderer>(true))
            sb.AppendLine($"R {r.name} enabled={r.enabled} active={r.gameObject.activeInHierarchy} center={cam.InverseTransformPoint(r.bounds.center):F3} size={r.bounds.size:F3} layer={r.gameObject.layer}");
        File.WriteAllText("Temp/hands_preview.txt", sb.ToString());
    }

    [MenuItem("Tools/Hands/Preview/Copy 0.5 (bag open)")] static void C05() => Preview(FirstPersonHandsAction.ActionType.Copy, 0.5f);
    [MenuItem("Tools/Hands/Preview/Copy 0.8 (grab)")] static void C08() => Preview(FirstPersonHandsAction.ActionType.Copy, 0.8f);
    [MenuItem("Tools/Hands/Preview/Copy 1.2 (into bag)")] static void C12() => Preview(FirstPersonHandsAction.ActionType.Copy, 1.2f);
    [MenuItem("Tools/Hands/Preview/Paste 1.2 (release)")] static void P12() => Preview(FirstPersonHandsAction.ActionType.Paste, 1.2f);

    [MenuItem("Tools/Hands/Preview/Restore Pose (reload scene)")]
    static void Restore()
    {
        if (EditorApplication.isPlaying) return;
        ReloadScene();
        var sv = SceneView.lastActiveSceneView;
        if (sv) { sv.cameraSettings.fieldOfView = 60f; sv.cameraSettings.dynamicClip = true; sv.Repaint(); }
    }
}
#endif
