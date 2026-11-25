#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class IslandReport
{
    [MenuItem("Tools/Island/Report Layout")]
    public static void Report()
    {
        var sb = new StringBuilder();
        foreach (var t in Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None))
        {
            var d = t.terrainData;
            sb.AppendLine($"TERRAIN {t.name} pos={t.transform.position:F2} size={d.size:F2} hmRes={d.heightmapResolution} " +
                $"trees={d.treeInstanceCount} detailLayers={d.detailPrototypes.Length} holes={d.enableHolesTextureCompression} asset={AssetDatabase.GetAssetPath(d)}");
            var h = d.GetHeights(0, 0, d.heightmapResolution, d.heightmapResolution);
            float mn = 1, mx = 0; foreach (var v in h) { mn = Mathf.Min(mn, v); mx = Mathf.Max(mx, v); }
            sb.AppendLine($"   heights min={mn * d.size.y:F2} max={mx * d.size.y:F2}");
        }
        var pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc) sb.AppendLine("PLAYER " + pc.transform.position.ToString("F2"));
        string[] keys = { "fence", "rock", "stone", "house", "home", "tree", "sign", "mill", "tower", "path", "combined", "flower", "grass", "water", "bush" };
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (r is ParticleSystemRenderer) continue;
            string n = r.gameObject.name.ToLower();
            string root = r.transform.root.name;
            var b = r.bounds;
            if (b.size.magnitude < 0.01f) continue;
            sb.AppendLine($"R|{root}|{r.gameObject.name}|{b.center.x:F1}|{b.center.y:F1}|{b.center.z:F1}|{b.size.x:F1}|{b.size.z:F1}");
        }
        File.WriteAllText("Temp/island_report.txt", sb.ToString());
        Debug.Log("[IslandReport] written");
    }
}
#endif
