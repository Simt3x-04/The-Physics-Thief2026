#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class CameraFixTool
{
    [MenuItem("Tools/Camera/Fix Exposure And Popping")]
    public static void Fix()
    {
        var cam = Camera.main;
        if (cam == null) { File.WriteAllText("Temp/camfix_report.txt", "No MainCamera"); return; }
        Undo.RecordObject(cam, "Camera fix");
        cam.useOcclusionCulling = false;          // 旧的遮挡剔除数据会让物体忽隐忽现
        var data = cam.GetComponent<UniversalAdditionalCameraData>();
        if (data != null)
        {
            Undo.RecordObject(data, "Camera fix");
            // 后处理（Bloom + 颗粒）会让画面更曝、更糊，保持关闭
            data.renderPostProcessing = false;
            data.antialiasing = AntialiasingMode.None;
            data.stopNaN = false;
            data.dithering = false;
            EditorUtility.SetDirty(data);
        }
        EditorUtility.SetDirty(cam);
        EditorSceneManager.MarkSceneDirty(cam.gameObject.scene);
        EditorSceneManager.SaveOpenScenes();
        File.WriteAllText("Temp/camfix_report.txt",
            $"camera={cam.name} occlusion={cam.useOcclusionCulling} post={(data != null ? data.renderPostProcessing.ToString() : "no URP data")} aa={(data != null ? data.antialiasing.ToString() : "-")} far={cam.farClipPlane}");
    }
}
#endif
