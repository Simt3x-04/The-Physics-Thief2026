#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PhysicsThief.Player;
using Object = UnityEngine.Object;

// Turns the big meadow into a round island in the sea. Parameters are in Temp/island_config.json.
public static class IslandTool
{
    [Serializable]
    public class Config
    {
        public float centerX = 345f, centerZ = 450f;
        public float radius = 60f;          // radius of the flat area (left as it is)
        public float beachWidth = 14f;      // width of the sloping beach
        public float waterBelowGround = 0.6f;
        public float seaDepth = 5f;         // how far the sea floor is below the water surface
        public float wallHeight = 40f;
        public bool movePlayer = true;
        public float spawnX = float.NaN, spawnZ = float.NaN;
        public string waterMaterial = "Assets/_Project/Art/Environment/IslandGenerated/Ocean.mat";
    }

    const string ConfigPath = "Temp/island_config.json";
    const string ReportPath = "Temp/island_apply_report.txt";
    const string GenFolder = "Assets/_Project/Art/Environment/IslandGenerated";

    static Config LoadConfig()
    {
        var c = new Config();
        if (File.Exists(ConfigPath)) JsonUtility.FromJsonOverwrite(File.ReadAllText(ConfigPath), c);
        return c;
    }

    static Vector3 Center(Config c) => new Vector3(c.centerX, 0, c.centerZ);
    static float Dist(Vector3 p, Config c) => new Vector2(p.x - c.centerX, p.z - c.centerZ).magnitude;

    static float GroundY(Vector3 xz)
    {
        float best = float.NegativeInfinity;
        foreach (var t in Terrain.activeTerrains)
            best = Mathf.Max(best, t.SampleHeight(xz) + t.transform.position.y);
        return float.IsNegativeInfinity(best) ? 0 : best;
    }

    static float EdgeGround(Config c)
    {
        float sum = 0; int n = 72;
        for (int i = 0; i < n; i++)
        {
            float a = i * Mathf.PI * 2 / n;
            sum += GroundY(Center(c) + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * c.radius);
        }
        return sum / n;
    }

    // ---------------- Preview ----------------
    [MenuItem("Tools/Island/1. Preview Circle")]
    public static void Preview()
    {
        var c = LoadConfig();
        var old = GameObject.Find("_IslandPreview");
        if (old) Object.DestroyImmediate(old);
        var go = new GameObject("_IslandPreview");
        float gy = EdgeGround(c);
        AddRing(go.transform, "Land edge (green)", c, c.radius, gy + 1.5f, Color.green);
        AddRing(go.transform, "Wall (red)", c, c.radius + c.beachWidth * 0.15f, gy + 1.5f, Color.red);
        AddRing(go.transform, "Sea starts (blue)", c, c.radius + c.beachWidth, gy + 1.5f, Color.blue);

        var sv = SceneView.lastActiveSceneView;
        if (sv != null)
        {
            sv.orthographic = false;
            sv.LookAt(new Vector3(c.centerX, gy, c.centerZ), Quaternion.Euler(90, 0, 0), (c.radius + c.beachWidth) * 2.6f);
            sv.Repaint();
        }
        var sb = new StringBuilder();
        sb.AppendLine($"Preview center=({c.centerX},{c.centerZ}) r={c.radius} edgeGround={gy:F2} centerGround={GroundY(Center(c)):F2}");
        File.WriteAllText(ReportPath, sb.ToString());
        Debug.Log("[IslandTool] " + sb);
    }

    static void AddRing(Transform parent, string name, Config c, float r, float y, Color col)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var lr = go.AddComponent<LineRenderer>();
        int n = 128;
        lr.positionCount = n;
        lr.loop = true;
        lr.useWorldSpace = true;
        lr.widthMultiplier = 1.2f;
        lr.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = col;
        for (int i = 0; i < n; i++)
        {
            float a = i * Mathf.PI * 2 / n;
            lr.SetPosition(i, new Vector3(c.centerX + Mathf.Cos(a) * r, y, c.centerZ + Mathf.Sin(a) * r));
        }
    }

    // ---------------- Apply ----------------
    [MenuItem("Tools/Island/2. Apply Island")]
    public static void Apply()
    {
        var log = new StringBuilder();
        try { ApplyInner(log); }
        catch (Exception e) { log.AppendLine("EXCEPTION: " + e); }
        File.WriteAllText(ReportPath, log.ToString());
        Debug.Log("[IslandTool]\n" + log);
    }

    static void ApplyInner(StringBuilder log)
    {
        if (EditorApplication.isPlaying) { log.AppendLine("ERROR: stop Play mode first"); return; }
        var c = LoadConfig();
        var prev = GameObject.Find("_IslandPreview");
        if (prev) Object.DestroyImmediate(prev);
        var old = GameObject.Find("_Island");
        if (old) { log.AppendLine("ERROR: _Island already exists, island already applied."); return; }

        float edgeY = EdgeGround(c);
        // The water surface must be below the lowest ground inside the island, otherwise the middle gets flooded
        float interiorMin = edgeY;
        for (float x = -c.radius; x <= c.radius; x += 2f)
            for (float z = -c.radius; z <= c.radius; z += 2f)
                if (x * x + z * z <= c.radius * c.radius)
                    interiorMin = Mathf.Min(interiorMin, GroundY(new Vector3(c.centerX + x, 0, c.centerZ + z)));
        float waterY = Mathf.Min(edgeY, interiorMin) - c.waterBelowGround;
        log.AppendLine($"interiorMinGround={interiorMin:F2}");
        float floorY = waterY - c.seaDepth;
        float keepR = c.radius + c.beachWidth * 0.1f;
        float wallR = c.radius + c.beachWidth * 0.15f;
        log.AppendLine($"center=({c.centerX},{c.centerZ}) r={c.radius} edgeY={edgeY:F2} waterY={waterY:F2} floorY={floorY:F2}");

        // ---- 1. Terrain ----
        var byData = Terrain.activeTerrains.GroupBy(t => t.terrainData);
        foreach (var g in byData)
        {
            var d = g.Key;
            var t0 = g.First();
            Vector3 P = t0.transform.position;
            Vector3 S = d.size;
            int res = d.heightmapResolution;
            var h = d.GetHeights(0, 0, res, res);
            var w = new float[res, res];
            float maxW = float.NegativeInfinity, minW = float.PositiveInfinity;
            for (int y = 0; y < res; y++)
                for (int x = 0; x < res; x++)
                {
                    float wx = P.x + (float)x / (res - 1) * S.x;
                    float wz = P.z + (float)y / (res - 1) * S.z;
                    float W = P.y + h[y, x] * S.y;
                    float dist = new Vector2(wx - c.centerX, wz - c.centerZ).magnitude;
                    if (dist > c.radius)
                    {
                        float tt = Mathf.Clamp01((dist - c.radius) / c.beachWidth);
                        float s = tt * tt * (3 - 2 * tt);
                        W = Mathf.Min(W, Mathf.Lerp(W, floorY, s));
                        if (tt >= 1f) W = Mathf.Min(W, floorY);
                    }
                    w[y, x] = W;
                    maxW = Mathf.Max(maxW, W); minW = Mathf.Min(minW, W);
                }
            float newPY = Mathf.Min(P.y, minW - 0.5f);
            float newSY = Mathf.Max(S.y, maxW - newPY + 1f);
            Undo.RegisterCompleteObjectUndo(d, "Island terrain");
            for (int y = 0; y < res; y++)
                for (int x = 0; x < res; x++)
                    h[y, x] = Mathf.Clamp01((w[y, x] - newPY) / newSY);
            d.size = new Vector3(S.x, newSY, S.z);
            d.SetHeights(0, 0, h);
            foreach (var t in g)
            {
                Undo.RecordObject(t.transform, "Island terrain move");
                var tp = t.transform.position; tp.y = newPY; t.transform.position = tp;
                t.Flush();
            }
            EditorUtility.SetDirty(d);
            log.AppendLine($"Terrain data {d.name}: posY {P.y}->{newPY:F2}, sizeY {S.y}->{newSY:F2}, terrains={g.Count()}");
        }

        // ---- 2. Objects outside the circle ----
        var player = Object.FindFirstObjectByType<PlayerController>();
        var skipRoots = new HashSet<Transform>();
        if (player) skipRoots.Add(player.transform);
        if (Camera.main) skipRoots.Add(Camera.main.transform);

        bool Skip(Transform tr)
        {
            if (tr.GetComponentInParent<Terrain>() != null) return true;
            foreach (var s in skipRoots) if (tr == s || tr.IsChildOf(s)) return true;
            return false;
        }

        if (!AssetDatabase.IsValidFolder(GenFolder)) AssetDatabase.CreateFolder("Assets", "IslandGenerated");
        var outside = new HashSet<Renderer>();
        int trimmed = 0, trimFail = 0;
        var trimmedMeshes = new Dictionary<Mesh, Mesh>();
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (Skip(r.transform)) continue;
            var b = r.bounds;
            if (b.center.y > edgeY + 30f) continue;                 // clouds in the sky and the like
            if (Mathf.Max(b.size.x, b.size.z) > 700f) continue;     // skybox-like objects
            float big = Mathf.Max(b.size.x, b.size.z);
            float dc = Dist(b.center, c);
            float far = dc + big * 0.75f;
            var mf = r.GetComponent<MeshFilter>();
            if (big > 15f && mf != null && mf.sharedMesh != null && far > keepR)
            {
                if (dc - big * 0.75f > keepR) { outside.Add(r); continue; }
                try
                {
                    var src = mf.sharedMesh;
                    if (!trimmedMeshes.TryGetValue(src, out var nm) || true)
                    {
                        nm = TrimMesh(src, r.transform, c, keepR);
                    }
                    if (nm == null) { outside.Add(r); continue; }
                    string path = AssetDatabase.GenerateUniqueAssetPath($"{GenFolder}/{Sanitize(r.gameObject.name)}_island.asset");
                    AssetDatabase.CreateAsset(nm, path);
                    Undo.RecordObject(mf, "Trim mesh");
                    mf.sharedMesh = nm;
                    trimmed++;
                    log.AppendLine($"  trimmed {Path(r.transform)}");
                }
                catch (Exception e) { trimFail++; log.AppendLine($"  TRIM FAIL {Path(r.transform)}: {e.Message}"); }
                continue;
            }
            if (dc > keepR) outside.Add(r);
        }

        // Going bottom-up, find the highest objects whose renderers are all outside the circle and hide them as a whole
        var allR = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        var hasInside = new HashSet<Transform>();
        foreach (var r in allR)
            if (!outside.Contains(r))
                for (var t = r.transform; t != null; t = t.parent) hasInside.Add(t);
        var toHide = new HashSet<GameObject>();
        foreach (var r in outside)
        {
            Transform top = r.transform;
            while (top.parent != null && top.parent.parent != null && !hasInside.Contains(top.parent)) top = top.parent;
            toHide.Add(top.gameObject);
        }
        foreach (var go in toHide)
        {
            Undo.RecordObject(go, "Hide outside");
            go.SetActive(false);
        }
        log.AppendLine($"Hidden objects: {toHide.Count}, trimmed meshes: {trimmed}, trim fails: {trimFail}");

        // ---- 3. Island root object: water + invisible wall ----
        var root = new GameObject("_Island");
        Undo.RegisterCreatedObjectUndo(root, "Island");

        var water = GameObject.CreatePrimitive(PrimitiveType.Plane);
        water.name = "Sea";
        water.transform.SetParent(root.transform, false);
        water.transform.position = new Vector3(c.centerX, waterY, c.centerZ);
        water.transform.localScale = new Vector3(150, 1, 150); // 1500m
        Object.DestroyImmediate(water.GetComponent<Collider>());
        var mat = AssetDatabase.LoadAssetAtPath<Material>(c.waterMaterial);
        if (mat != null) water.GetComponent<MeshRenderer>().sharedMaterial = mat;
        else log.AppendLine("Water material not found: " + c.waterMaterial);

        var wall = new GameObject("AirWall");
        wall.transform.SetParent(root.transform, false);
        int segs = 96;
        float segW = 2 * Mathf.PI * wallR / segs * 1.1f;
        for (int i = 0; i < segs; i++)
        {
            float a = (i + 0.5f) * Mathf.PI * 2 / segs;
            var seg = new GameObject("Wall_" + i);
            seg.transform.SetParent(wall.transform, false);
            seg.transform.position = new Vector3(c.centerX + Mathf.Cos(a) * (wallR + 0.5f), edgeY + c.wallHeight * 0.5f - 5f, c.centerZ + Mathf.Sin(a) * (wallR + 0.5f));
            seg.transform.rotation = Quaternion.LookRotation(new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)));
            var bc = seg.AddComponent<BoxCollider>();
            bc.size = new Vector3(segW, c.wallHeight, 1f);
        }
        log.AppendLine($"Wall radius {wallR:F1}, water y {waterY:F2}");

        // ---- 4. Spawn point ----
        if (c.movePlayer && player != null && Dist(player.transform.position, c) > c.radius - 3f)
        {
            float sx = float.IsNaN(c.spawnX) ? c.centerX : c.spawnX;
            float sz = float.IsNaN(c.spawnZ) ? c.centerZ : c.spawnZ;
            float gy = GroundY(new Vector3(sx, 0, sz));
            if (Physics.Raycast(new Vector3(sx, gy + 100f, sz), Vector3.down, out var hit, 200f, ~0, QueryTriggerInteraction.Ignore))
                gy = Mathf.Max(gy, hit.point.y);
            Undo.RecordObject(player.transform, "Move player");
            var cc = player.GetComponent<CharacterController>();
            bool en = cc && cc.enabled; if (cc) cc.enabled = false;
            player.transform.position = new Vector3(sx, gy + 0.1f, sz);
            if (cc) cc.enabled = en;
            log.AppendLine($"Player moved to {player.transform.position:F2}");
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(root.scene);
        EditorSceneManager.SaveOpenScenes();
        log.AppendLine("Saved. OK");
    }

    [MenuItem("Tools/Island/3. Snap Player To Ground")]
    public static void SnapPlayer()
    {
        var player = Object.FindFirstObjectByType<PlayerController>();
        if (!player) return;
        Physics.SyncTransforms();
        var p = player.transform.position;
        float gy = GroundY(p);
        // Only consider things slightly higher than the terrain (e.g. steps), ignore anything high above
        if (Physics.Raycast(new Vector3(p.x, gy + 2.5f, p.z), Vector3.down, out var hit, 5f, ~0, QueryTriggerInteraction.Ignore)
            && !hit.transform.IsChildOf(player.transform))
            gy = Mathf.Max(gy, hit.point.y);
        Undo.RecordObject(player.transform, "Snap player");
        player.transform.position = new Vector3(p.x, gy + 0.1f, p.z);
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
        EditorSceneManager.SaveOpenScenes();
        File.WriteAllText(ReportPath, $"Player snapped to {player.transform.position:F2} (terrain {GroundY(p):F2})");
    }

    [MenuItem("Tools/Island/4. Use Simple Ocean Material")]
    public static void OceanMaterial()
    {
        const string path = GenFolder + "/Ocean.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            var sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(sh) { name = "Ocean" };
            AssetDatabase.CreateAsset(mat, path);
        }
        var col = new Color(0.10f, 0.42f, 0.62f, 1f);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", col);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.85f);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.85f);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
        var sea = GameObject.Find("_Island/Sea");
        if (sea)
        {
            var mr = sea.GetComponent<MeshRenderer>();
            Undo.RecordObject(mr, "Ocean material");
            mr.sharedMaterial = mat;
            EditorSceneManager.MarkSceneDirty(sea.scene);
            EditorSceneManager.SaveOpenScenes();
        }
        File.WriteAllText(ReportPath, "Ocean material: " + mat.shader.name + " sea=" + (sea != null));
    }

    static Mesh TrimMesh(Mesh src, Transform tr, Config c, float keepR)
    {
        var verts = src.vertices;
        var world = new Vector3[verts.Length];
        var m = tr.localToWorldMatrix;
        for (int i = 0; i < verts.Length; i++) world[i] = m.MultiplyPoint3x4(verts[i]);
        bool In(int i) => Dist(world[i], c) <= keepR;

        var nm = Object.Instantiate(src);
        nm.name = src.name + "_island";
        int kept = 0;
        for (int sm = 0; sm < src.subMeshCount; sm++)
        {
            var tris = src.GetTriangles(sm);
            var keep = new List<int>(tris.Length);
            for (int i = 0; i < tris.Length; i += 3)
                if (In(tris[i]) && In(tris[i + 1]) && In(tris[i + 2]))
                { keep.Add(tris[i]); keep.Add(tris[i + 1]); keep.Add(tris[i + 2]); }
            nm.SetTriangles(keep, sm);
            kept += keep.Count;
        }
        if (kept == 0) { Object.DestroyImmediate(nm); return null; }
        nm.RecalculateBounds();
        return nm;
    }

    static string Sanitize(string s) => new string(s.Select(ch => char.IsLetterOrDigit(ch) || ch == '_' ? ch : '_').ToArray());
    static string Path(Transform t) { string p = t.name; while (t.parent) { t = t.parent; p = t.name + "/" + p; } return p; }
}
#endif
