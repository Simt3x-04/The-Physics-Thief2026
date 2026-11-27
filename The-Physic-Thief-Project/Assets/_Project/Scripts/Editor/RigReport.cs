#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using PhysicsThief.Player;

public static class RigReport
{
    [MenuItem("Tools/Report/Player Rig")]
    public static void Report()
    {
        var sb = new StringBuilder();
        var pc = Object.FindFirstObjectByType<PlayerController>();
        foreach (var r in pc.GetComponentsInChildren<Renderer>(true))
        {
            string extra = "";
            if (r is SkinnedMeshRenderer smr)
                extra = $" SKINNED bones={smr.bones.Length} root={(smr.rootBone ? smr.rootBone.name : "-")} sampleBones={string.Join(",", smr.bones.Take(6).Select(b => b ? b.name : "null"))}";
            sb.AppendLine($"{r.GetType().Name} {r.name} active={r.gameObject.activeInHierarchy} enabled={r.enabled} parent={r.transform.parent?.name}{extra}");
        }
        foreach (var a in pc.GetComponentsInChildren<Animator>(true))
            sb.AppendLine($"ANIMATOR on {a.name} enabled={a.enabled} active={a.gameObject.activeInHierarchy} controller={(a.runtimeAnimatorController ? a.runtimeAnimatorController.name : "none")} avatar={(a.avatar ? a.avatar.name : "none")} human={a.isHuman}");
        foreach (var mb in pc.GetComponentsInChildren<MonoBehaviour>(true))
            if (mb) sb.AppendLine($"SCRIPT {mb.GetType().Name} on {mb.name} enabled={mb.enabled}");
        var t = pc.GetComponentsInChildren<Transform>(true);
        foreach (var n in new[] { "mixamorig:RightArm", "mixamorig:RightForeArm", "mixamorig:RightHand", "mixamorig:LeftArm", "mixamorig:LeftForeArm", "mixamorig:LeftHand", "mixamorig:LeftShoulder", "mixamorig:Head", "EyePoint", "mixamorig:LeftHandIndex1", "mixamorig:LeftHandIndex4", "mixamorig:LeftHandThumb1" })
        {
            var x = t.FirstOrDefault(q => q.name == n);
            if (x) sb.AppendLine($"BONE {n} pos={pc.transform.InverseTransformPoint(x.position):F3} right={pc.transform.InverseTransformDirection(x.right):F2} up={pc.transform.InverseTransformDirection(x.up):F2} fwd={pc.transform.InverseTransformDirection(x.forward):F2}");
        }
        File.WriteAllText("Temp/rig_report.txt", sb.ToString());
    }
}
#endif
