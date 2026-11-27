using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using PhysicsThief.Items;
using PhysicsThief.Player;
using PhysicsThief.Properties;
using PhysicsThief.UI;
using Object = UnityEngine.Object;

namespace PhysicsThief.EditorTools
{
    /// <summary>
    /// One-click setup of the apple feature, under the menu  Tools > Physics Thief > Apple Trees.
    /// "Setup All" does, in Edit Mode:
    ///   1. Apple art (low-poly meshes + URP materials) in Art/Items/Apple, and the prefabs Prefabs/Items/Apple and
    ///      Prefabs/Items/AppleTree (the village SmallTree as visual, a trunk collider, 5 fruit positions, 3 apples).
    ///   2. An "InteractPrompt" text in the HUD prefab, assigned to HUDManager.promptLabel.
    ///   3. A PlayerInteractor on the player camera and one AppleTree near the player spawn in VillageScene (saved).
    /// It is safe to run again: existing assets, the prompt, the interactor and the placed tree are kept, never duplicated.
    /// Command line (Unity closed):  -batchmode -quit -projectPath ... -executeMethod PhysicsThief.EditorTools.AppleTreeSetup.RunFromCommandLine
    /// </summary>
    public static class AppleTreeSetup
    {
        private const string Menu = "Tools/Physics Thief/Apple Trees/";
        private const string LogTag = "[Apple Trees] ";

        private const string ArtFolder = "Assets/_Project/Art/Items/Apple";
        private const string PrefabFolder = "Assets/_Project/Prefabs/Items";
        public const string ApplePrefabPath = PrefabFolder + "/Apple.prefab";
        public const string TreePrefabPath = PrefabFolder + "/AppleTree.prefab";
        private const string SourceTreePath = "Assets/_Project/Art/Environment/StylizedEnvironment/Art/Vegetation/Prefabs/Tree/SmallTree.prefab";
        private const string HudPrefabPath = "Assets/_Project/Prefabs/HUD.prefab";
        private const string VillageScenePath = "Assets/_Project/Scenes/VillageScene.unity";

        private const string PlacementRootName = "AppleTrees";
        private const string PromptObjectName = "InteractPrompt";

        private const float AppleSize = 0.17f;       // meters across: a bit bigger than a real apple, so it reads in first person
        private const int FruitPositions = 5;
        private const int StartingApples = 3;
        private const float LowestApple = 1.5f;      // fruit height range above the ground (meters)
        private const float HighestApple = 3.4f;     // eyes ~1.45 m + 3 m reach: still reachable standing under the tree

        // ---------- menu ----------

        [MenuItem(Menu + "Setup All (Assets, HUD, VillageScene)", false, 40)]
        public static void SetupAllMenu()
        {
            if (!CheckEditMode()) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            string report;
            try
            {
                report = SetupAll() + "\n\nChecks:\n" + Verify();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                report = "FAILED: " + e.Message + "\nSee the Console for details.";
            }
            Debug.Log(LogTag + report);
            EditorUtility.DisplayDialog("Apple Trees", report, "OK");
        }

        [MenuItem(Menu + "1 - Create Apple Assets And Prefabs", false, 51)]
        public static void CreateAssetsMenu()
        {
            if (!CheckEditMode()) return;
            CreateAssetsAndPrefabs();
        }

        [MenuItem(Menu + "2 - Add Interaction Prompt To HUD", false, 52)]
        public static void SetupHudMenu()
        {
            if (!CheckEditMode()) return;
            SetupHudPrompt();
        }

        [MenuItem(Menu + "3 - Place Example Tree In VillageScene", false, 53)]
        public static void PlaceMenu()
        {
            if (!CheckEditMode()) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            PlaceInVillageScene(OpenVillageScene());
        }

        /// <summary>
        /// Batch mode entry point (no dialogs). Runs the setup TWICE and checks that the second run changed nothing
        /// (idempotent), then writes a report to the file given with  -appleSetupReport &lt;path&gt;
        /// (default: Logs/AppleTreeSetup-report.txt). Exit code 0 on success, 1 on failure.
        /// </summary>
        public static void RunFromCommandLine()
        {
            var report = new List<string>();
            int exitCode = 0;
            try
            {
                report.Add("== Run 1 ==");
                report.Add(SetupAll());
                string first = Verify();
                report.Add(first);

                report.Add("== Run 2 (must not add anything) ==");
                report.Add(SetupAll());
                string second = Verify();
                report.Add(second);

                if (first != second) throw new InvalidOperationException("The second run changed the result:\n" + first + "\n---\n" + second);
                report.Add("RESULT: OK (idempotent)");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                report.Add("RESULT: FAILED - " + e.Message);
                exitCode = 1;
            }

            string path = ArgumentValue("-appleSetupReport") ?? "Logs/AppleTreeSetup-report.txt";
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path)));
            System.IO.File.WriteAllText(path, string.Join("\n", report) + "\n");
            Debug.Log(LogTag + "Report written to " + path + "\n" + string.Join("\n", report));
            EditorApplication.Exit(exitCode);
        }

        /// <summary>Counts what the setup must leave behind; throws if something is missing or duplicated.</summary>
        public static string Verify()
        {
            var lines = new List<string>();
            void Check(bool ok, string what)
            {
                lines.Add((ok ? "OK   " : "FAIL ") + what);
                if (!ok) throw new InvalidOperationException("Check failed: " + what + "\n" + string.Join("\n", lines));
            }

            GameObject apple = AssetDatabase.LoadAssetAtPath<GameObject>(ApplePrefabPath);
            GameObject treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TreePrefabPath);
            Check(apple != null && apple.GetComponent<AppleItem>() != null, "Apple.prefab with AppleItem");
            Check(treePrefab != null && treePrefab.GetComponent<AppleTree>() != null, "AppleTree.prefab with AppleTree");
            Check(treePrefab.GetComponentsInChildren<AppleItem>(true).Length == FruitPositions, FruitPositions + " fruit positions in AppleTree.prefab");

            HUDManager hud = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath).GetComponentInChildren<HUDManager>(true);
            Check(new SerializedObject(hud).FindProperty("promptLabel").objectReferenceValue != null, "HUD.prefab promptLabel assigned");
            int prompts = 0;
            foreach (Transform t in hud.transform.root.GetComponentsInChildren<Transform>(true))
                if (t.name == PromptObjectName) prompts++;
            Check(prompts == 1, "exactly one " + PromptObjectName + " in HUD.prefab (found " + prompts + ")");

            Scene scene = SceneManager.GetActiveScene();
            Check(scene.path == VillageScenePath, "VillageScene is open");
            Check(Object.FindObjectsByType<PlayerInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 1,
                "exactly one PlayerInteractor in VillageScene");
            AppleTree[] trees = Object.FindObjectsByType<AppleTree>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Check(trees.Length == 1, "exactly one AppleTree in VillageScene (found " + trees.Length + ")");
            Check(trees[0].name == "AppleTree_Start" && PrefabUtility.GetCorrespondingObjectFromSource(trees[0].gameObject) == treePrefab,
                "AppleTree_Start is an instance of AppleTree.prefab");
            lines.Add("     AppleTree_Start at " + trees[0].transform.position.ToString("F2"));
            return string.Join("\n", lines);
        }

        private static string ArgumentValue(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name) return args[i + 1];
            }
            return null;
        }

        public static string SetupAll()
        {
            Scene village = OpenVillageScene();   // first: temporary objects below live (and die) in this scene
            var report = new List<string>();
            report.Add(CreateAssetsAndPrefabs());
            report.Add(SetupHudPrompt());
            report.Add(PlaceInVillageScene(village));
            AssetDatabase.SaveAssets();
            return string.Join("\n", report);
        }

        private static bool CheckEditMode()
        {
            if (!EditorApplication.isPlaying) return true;
            EditorUtility.DisplayDialog("Apple Trees", "Exit Play Mode first: the setup edits assets and the scene.", "OK");
            return false;
        }

        private static Scene OpenVillageScene()
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.path == VillageScenePath) return active;
            return EditorSceneManager.OpenScene(VillageScenePath, OpenSceneMode.Single);
        }

        // ---------- 1. art and prefabs ----------

        public static string CreateAssetsAndPrefabs()
        {
            EnsureFolder(ArtFolder);
            EnsureFolder(PrefabFolder);

            Mesh body = GetOrCreateMesh(ArtFolder + "/Apple_Body.asset", BuildAppleBody);
            Mesh stem = GetOrCreateMesh(ArtFolder + "/Apple_Stem.asset", BuildStem);
            Mesh leaf = GetOrCreateMesh(ArtFolder + "/Apple_Leaf.asset", BuildLeaf);
            Material red = GetOrCreateMaterial(ArtFolder + "/M_Apple_Red.mat", new Color(0.80f, 0.12f, 0.10f), 0.35f);
            Material brown = GetOrCreateMaterial(ArtFolder + "/M_Apple_Stem.mat", new Color(0.36f, 0.22f, 0.12f), 0.1f);
            Material green = GetOrCreateMaterial(ArtFolder + "/M_Apple_Leaf.mat", new Color(0.32f, 0.62f, 0.18f), 0.15f);

            var lines = new List<string>();

            GameObject applePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ApplePrefabPath);
            if (applePrefab == null)
            {
                applePrefab = BuildApplePrefab(body, stem, leaf, red, brown, green);
                lines.Add("Created " + ApplePrefabPath);
            }
            else lines.Add("Kept existing " + ApplePrefabPath);

            if (AssetDatabase.LoadAssetAtPath<GameObject>(TreePrefabPath) == null)
            {
                lines.Add(BuildTreePrefab(applePrefab));
            }
            else lines.Add("Kept existing " + TreePrefabPath);

            AssetDatabase.SaveAssets();
            string text = string.Join("\n", lines);
            Debug.Log(LogTag + text);
            return text;
        }

        private static GameObject BuildApplePrefab(Mesh body, Mesh stem, Mesh leaf, Material red, Material brown, Material green)
        {
            var root = new GameObject("Apple");
            try
            {
                // Trigger: the player and objects pass through, but the E ray and the scanner ray still hit it.
                // A bit larger than the fruit so it is easy to aim at.
                var aim = root.AddComponent<SphereCollider>();
                aim.isTrigger = true;
                aim.radius = AppleSize * 0.8f;

                var visual = new GameObject("Visual").transform;
                visual.SetParent(root.transform, false);

                float top = AppleTopY();
                AddPart(visual, "Body", body, red, Vector3.zero, Quaternion.identity);
                AddPart(visual, "Stem", stem, brown, new Vector3(0f, top - 0.008f, 0f), Quaternion.Euler(0f, 0f, 12f));
                AddPart(visual, "Leaf", leaf, green, new Vector3(0.004f, top + 0.02f, 0f), Quaternion.Euler(0f, 35f, 20f));

                var item = root.AddComponent<AppleItem>();
                var itemSO = new SerializedObject(item);
                itemSO.FindProperty("itemName").stringValue = "Apple";
                itemSO.FindProperty("healAmount").floatValue = AppleEatRules.DefaultHealAmount;
                itemSO.FindProperty("visual").objectReferenceValue = visual;
                itemSO.ApplyModifiedPropertiesWithoutUndo();

                // Scannable like the other props (GDD 7: apple 0.2 kg, 15 C, hardness 2). Scan-only: no reactors, pastes are refused.
                ConfigurePhysicalObject(root.AddComponent<PhysicalObject>(), "Apple", MaterialTag.Organic,
                    (AttributeType.Mass, 0.2f), (AttributeType.Temperature, 15f), (AttributeType.Hardness, 2f));

                return PrefabUtility.SaveAsPrefabAsset(root, ApplePrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static string BuildTreePrefab(GameObject applePrefab)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(SourceTreePath);
            if (source == null) throw new InvalidOperationException("Village tree not found: " + SourceTreePath);

            var root = new GameObject("AppleTree");
            try
            {
                // The village tree is nested as the visual, so art changes to SmallTree reach every apple tree.
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(source);
                visual.name = "Visual";
                visual.transform.SetParent(root.transform, false);
                // SmallTree.prefab keeps the world pose it had in the demo scene (~(227, 4, 452), tilted): reset it so the
                // trunk sits on the AppleTree origin.
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;

                Collider[] treeColliders = visual.GetComponentsInChildren<Collider>(true);
                Physics.SyncTransforms();

                bool hitBackfaces = Physics.queriesHitBackfaces;
                Physics.queriesHitBackfaces = true;   // leaf cards can be single-sided
                TreeShape shape;
                try
                {
                    shape = ProbeTree(treeColliders);
                }
                finally
                {
                    Physics.queriesHitBackfaces = hitBackfaces;
                }

                // The whole-mesh collider of the village tree would block the player under the canopy and hide the apples
                // from the E ray: a simple trunk collider replaces it.
                foreach (Collider c in treeColliders) c.enabled = false;

                var trunk = root.AddComponent<CapsuleCollider>();
                trunk.direction = 1;
                trunk.radius = shape.trunkRadius;
                trunk.height = Mathf.Max(shape.trunkHeight, trunk.radius * 2f);
                trunk.center = new Vector3(shape.trunkCenter.x, trunk.height * 0.5f, shape.trunkCenter.z);

                var fruitRoot = new GameObject("Apples").transform;
                fruitRoot.SetParent(root.transform, false);

                var apples = new List<AppleItem>();
                for (int i = 0; i < shape.fruit.Count; i++)
                {
                    var apple = (GameObject)PrefabUtility.InstantiatePrefab(applePrefab);
                    apple.name = "Apple_" + (i + 1);
                    apple.transform.SetParent(fruitRoot, false);
                    apple.transform.localPosition = shape.fruit[i];
                    apple.transform.localRotation = Quaternion.Euler(0f, i * 67f, (i % 2 == 0 ? 6f : -8f));
                    apples.Add(apple.GetComponent<AppleItem>());
                }

                var tree = root.AddComponent<AppleTree>();
                var treeSO = new SerializedObject(tree);
                SerializedProperty list = treeSO.FindProperty("apples");
                list.arraySize = apples.Count;
                for (int i = 0; i < apples.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = apples[i];
                treeSO.FindProperty("startingApples").intValue = StartingApples;
                treeSO.ApplyModifiedPropertiesWithoutUndo();

                // GDD 7: tree 800 kg, hardness 5. Scan-only: copying its mass is useful, pasting on it is refused.
                ConfigurePhysicalObject(root.AddComponent<PhysicalObject>(), "Apple tree", MaterialTag.Wood,
                    (AttributeType.Mass, 800f), (AttributeType.Hardness, 5f));

                PrefabUtility.SaveAsPrefabAsset(root, TreePrefabPath);

                var heights = new List<string>();
                foreach (Vector3 p in shape.fruit) heights.Add(p.y.ToString("0.00") + " m");
                return "Created " + TreePrefabPath + ": trunk r=" + trunk.radius.ToString("0.00") + " h=" + trunk.height.ToString("0.0")
                       + ", fruit heights " + string.Join(", ", heights) + " (first " + StartingApples + " start with an apple)"
                       + (shape.usedFallback ? "\nWARNING: canopy not detected, fallback fruit positions used: check the prefab." : "");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private struct TreeShape
        {
            public Vector3 trunkCenter;
            public float trunkRadius;
            public float trunkHeight;
            public List<Vector3> fruit;
            public bool usedFallback;
        }

        /// <summary>
        /// Measures the village tree with rays against its own mesh collider: the trunk (horizontal rays at knee height)
        /// and the underside of the canopy (vertical rays from the ground). Apples hang just under the lowest leaves.
        /// </summary>
        private static TreeShape ProbeTree(Collider[] colliders)
        {
            var shape = new TreeShape { trunkCenter = Vector3.zero, trunkRadius = 0.25f, trunkHeight = 2.6f, fruit = new List<Vector3>() };

            // Trunk: 8 rays towards the axis.
            var trunkHits = new List<Vector3>();
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                if (RaycastAny(colliders, new Ray(dir * 4f + Vector3.up * 0.6f, -dir), 4f, out RaycastHit hit)) trunkHits.Add(hit.point);
            }
            if (trunkHits.Count >= 4)
            {
                Vector3 center = Vector3.zero;
                foreach (Vector3 p in trunkHits) center += p;
                center /= trunkHits.Count;
                center.y = 0f;

                float radius = 0f;
                foreach (Vector3 p in trunkHits) radius += Vector2.Distance(new Vector2(p.x, p.z), new Vector2(center.x, center.z));
                shape.trunkCenter = center;
                shape.trunkRadius = Mathf.Clamp(radius / trunkHits.Count + 0.03f, 0.12f, 0.6f);
            }

            // Canopy underside around the trunk.
            float lowestCanopy = float.MaxValue;
            var candidates = new List<Vector3>();
            for (int k = 0; k < FruitPositions; k++)
            {
                float angle = (k * 360f / FruitPositions + 20f) * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

                bool found = false;
                Vector3 best = Vector3.zero;
                for (float r = shape.trunkRadius + 0.5f; r <= 2.4f; r += 0.15f)
                {
                    Vector3 origin = shape.trunkCenter + dir * r + Vector3.up * 0.2f;
                    if (!RaycastAny(colliders, new Ray(origin, Vector3.up), 8f, out RaycastHit hit)) continue;

                    float y = hit.point.y - AppleSize * 0.55f;   // hang right under the leaves
                    lowestCanopy = Mathf.Min(lowestCanopy, hit.point.y);
                    if (y < LowestApple || y > HighestApple) continue;
                    if (!found || y < best.y)
                    {
                        best = new Vector3(origin.x, y, origin.z);
                        found = true;
                    }
                }

                if (!found)
                {
                    best = shape.trunkCenter + dir * (shape.trunkRadius + 0.8f) + Vector3.up * (2.2f + 0.15f * k);
                    shape.usedFallback = true;
                }
                candidates.Add(best);
            }

            // The lowest (easiest to reach) fruit first: the starting apples are the first N of the list.
            candidates.Sort((a, b) => a.y.CompareTo(b.y));
            shape.fruit = candidates;
            if (lowestCanopy < float.MaxValue) shape.trunkHeight = Mathf.Clamp(lowestCanopy, 1.8f, 4f);
            return shape;
        }

        private static bool RaycastAny(Collider[] colliders, Ray ray, float distance, out RaycastHit best)
        {
            best = default;
            bool any = false;
            foreach (Collider c in colliders)
            {
                if (c == null || !c.enabled) continue;
                if (c.Raycast(ray, out RaycastHit hit, distance) && (!any || hit.distance < best.distance))
                {
                    best = hit;
                    any = true;
                }
            }
            return any;
        }

        private static void ConfigurePhysicalObject(PhysicalObject physical, string displayName, MaterialTag material,
            params (AttributeType type, float value)[] baseline)
        {
            var so = new SerializedObject(physical);
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("material").enumValueIndex = (int)material;
            so.FindProperty("acceptsPastes").boolValue = false;   // scan-only: no reactors here, a paste would waste the slot
            SerializedProperty list = so.FindProperty("baselineProperties");
            list.arraySize = baseline.Length;
            for (int i = 0; i < baseline.Length; i++)
            {
                SerializedProperty entry = list.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("type").enumValueIndex = (int)baseline[i].type;
                entry.FindPropertyRelative("value").floatValue = baseline[i].value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddPart(Transform parent, string name, Mesh mesh, Material material, Vector3 position, Quaternion rotation)
        {
            var part = new GameObject(name);
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localRotation = rotation;
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            part.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        // ---------- 2. HUD prompt ----------

        public static string SetupHudPrompt()
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(HudPrefabPath);
            try
            {
                HUDManager hud = contents.GetComponentInChildren<HUDManager>(true);
                if (hud == null) throw new InvalidOperationException("No HUDManager in " + HudPrefabPath);

                var so = new SerializedObject(hud);
                SerializedProperty promptProp = so.FindProperty("promptLabel");
                if (promptProp.objectReferenceValue != null) return "HUD prompt already set up";

                var message = so.FindProperty("messageLabel").objectReferenceValue as TMP_Text;
                Canvas canvas = hud.GetComponentInParent<Canvas>(true);
                if (canvas == null) canvas = contents.GetComponentInChildren<Canvas>(true);
                Transform parent = canvas != null ? canvas.transform : hud.transform;

                Transform existing = FindDeep(contents.transform, PromptObjectName);
                TMP_Text label = existing != null ? existing.GetComponent<TMP_Text>() : null;
                if (label == null)
                {
                    // Copy the message label so the prompt uses the same font and style as the rest of the HUD.
                    GameObject go = message != null
                        ? Object.Instantiate(message.gameObject, parent, false)
                        : new GameObject(PromptObjectName, typeof(RectTransform), typeof(TextMeshProUGUI));
                    go.name = PromptObjectName;
                    if (message == null) go.transform.SetParent(parent, false);
                    StripToText(go);
                    label = go.GetComponent<TMP_Text>();
                }

                var rect = (RectTransform)label.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(0f, -70f);   // just under the crosshair
                rect.sizeDelta = new Vector2(900f, 44f);
                label.text = string.Empty;
                label.fontSize = 28f;
                label.alignment = TextAlignmentOptions.Center;
                label.color = Color.white;
                label.raycastTarget = false;
                label.gameObject.SetActive(true);
                label.enabled = false;   // HUDManager.ShowPrompt toggles the component, never the GameObject

                promptProp.objectReferenceValue = label;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(contents, HudPrefabPath);
                return "Added '" + PromptObjectName + "' to " + HudPrefabPath;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        /// <summary>Keeps only the text on a copied label (no children, no CanvasGroup or other scripts).</summary>
        private static void StripToText(GameObject go)
        {
            for (int i = go.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(go.transform.GetChild(i).gameObject);
            foreach (Component c in go.GetComponents<Component>())
            {
                if (c is Transform || c is CanvasRenderer || c is TMP_Text) continue;
                Object.DestroyImmediate(c);
            }
        }

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name) return t;
            }
            return null;
        }

        // ---------- 3. VillageScene ----------

        public static string PlaceInVillageScene(Scene scene)
        {
            var lines = new List<string>();

            PlayerController player = Object.FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
            if (player == null) throw new InvalidOperationException("No PlayerController in " + scene.path);

            // E interaction: next to the scanner, on the camera that is a child of the Player.
            PlayerScanner scanner = player.GetComponentInChildren<PlayerScanner>(true);
            Transform cam = scanner != null ? scanner.transform : player.GetComponentInChildren<Camera>(true)?.transform;
            if (cam == null) throw new InvalidOperationException("No player camera found under " + player.name);

            PlayerInteractor interactor = cam.GetComponent<PlayerInteractor>();
            if (interactor == null)
            {
                interactor = Undo.AddComponent<PlayerInteractor>(cam.gameObject);
                lines.Add("Added PlayerInteractor to " + cam.name);
            }
            else lines.Add("Kept PlayerInteractor on " + cam.name);

            var interactorSO = new SerializedObject(interactor);
            interactorSO.FindProperty("rayOrigin").objectReferenceValue = cam;
            interactorSO.FindProperty("input").objectReferenceValue = player.GetComponent<PlayerInputHandler>();
            interactorSO.FindProperty("player").objectReferenceValue = player;
            interactorSO.ApplyModifiedProperties();

            // Example tree near the spawn.
            GameObject placementRoot = null;
            foreach (GameObject go in scene.GetRootGameObjects())
            {
                if (go.name == PlacementRootName) placementRoot = go;
            }

            if (placementRoot != null && placementRoot.GetComponentInChildren<AppleTree>(true) != null)
            {
                lines.Add("Example apple tree already in the scene: not added again");
            }
            else
            {
                GameObject treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TreePrefabPath);
                if (treePrefab == null) throw new InvalidOperationException("Run '1 - Create Apple Assets And Prefabs' first.");

                if (placementRoot == null)
                {
                    placementRoot = new GameObject(PlacementRootName);
                    SceneManager.MoveGameObjectToScene(placementRoot, scene);
                    Undo.RegisterCreatedObjectUndo(placementRoot, "Add apple trees");
                }

                Vector3 spot = FindTreeSpot(player.transform, out string note);
                var tree = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab, scene);
                Undo.RegisterCreatedObjectUndo(tree, "Add apple tree");
                tree.name = "AppleTree_Start";
                Undo.SetTransformParent(tree.transform, placementRoot.transform, "Add apple tree");
                tree.transform.position = spot;

                Vector3 toPlayer = player.transform.position - spot;
                toPlayer.y = 0f;
                if (toPlayer.sqrMagnitude > 0.01f) tree.transform.rotation = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);

                lines.Add("Placed AppleTree_Start at " + spot.ToString("F1") + " (" + note + ")");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save " + scene.path);
            lines.Add("Saved " + scene.path);

            string text = string.Join("\n", lines);
            Debug.Log(LogTag + text);
            return text;
        }

        /// <summary>
        /// A flat, free spot on the ground a few meters ahead of the spawn and to one side, visible from the spawn.
        /// Tries several offsets and keeps the first that passes all checks.
        /// </summary>
        private static Vector3 FindTreeSpot(Transform player, out string note)
        {
            Physics.SyncTransforms();

            Vector3 forward = player.forward;
            forward.y = 0f;
            forward = forward.sqrMagnitude > 0.01f ? forward.normalized : Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 eye = player.position + Vector3.up * 1.5f;

            Vector2[] offsets =
            {
                new Vector2(7f, 3.5f), new Vector2(7f, -3.5f), new Vector2(9f, 4f), new Vector2(9f, -4f),
                new Vector2(6f, 5f), new Vector2(6f, -5f), new Vector2(11f, 3f), new Vector2(11f, -3f),
                new Vector2(5f, 4f), new Vector2(5f, -4f), new Vector2(13f, 4f), new Vector2(13f, -4f)
            };

            Vector3? fallback = null;
            foreach (bool terrainOnly in new[] { true, false })
            {
                foreach (Vector2 o in offsets)
                {
                    Vector3 p = player.position + forward * o.x + right * o.y;
                    if (!GroundAt(p, player, terrainOnly, out RaycastHit ground)) continue;
                    if (fallback == null) fallback = ground.point;
                    if (ground.normal.y < 0.85f) continue;                           // too steep
                    if (!IsClear(ground.point, player)) continue;                    // a house, rock or fence is there
                    if (Blocked(eye, ground.point + Vector3.up * 1.5f, player)) continue;   // not visible from the spawn

                    note = (terrainOnly ? "on terrain" : "on ground mesh") + ", " + o.x + " m ahead, " + Mathf.Abs(o.y) + " m to the "
                           + (o.y >= 0f ? "right" : "left") + " of the spawn";
                    return ground.point;
                }
            }

            note = "WARNING: no clear spot found, placed at the first ground point: move it by hand";
            return fallback ?? player.position + forward * 7f;
        }

        private static bool GroundAt(Vector3 p, Transform player, bool terrainOnly, out RaycastHit ground)
        {
            ground = default;
            RaycastHit[] hits = Physics.RaycastAll(p + Vector3.up * 40f, Vector3.down, 120f, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            bool found = false;
            foreach (RaycastHit h in hits)
            {
                if (h.collider.transform.IsChildOf(player)) continue;
                if (h.distance >= best) continue;
                best = h.distance;
                ground = h;
                found = true;
            }
            if (!found) return false;
            return !terrainOnly || ground.collider is TerrainCollider;
        }

        /// <summary>No sizeable collider (taller than 0.6 m) where the trunk and the canopy go.</summary>
        private static bool IsClear(Vector3 groundPoint, Transform player)
        {
            Collider[] around = Physics.OverlapCapsule(groundPoint + Vector3.up * 0.7f, groundPoint + Vector3.up * 4f, 2.2f,
                ~0, QueryTriggerInteraction.Ignore);
            foreach (Collider c in around)
            {
                if (c is TerrainCollider) continue;
                if (c.transform.IsChildOf(player)) continue;
                if (c.bounds.size.y < 0.6f) continue;   // grass, flowers, pebbles
                return false;
            }
            return true;
        }

        private static bool Blocked(Vector3 from, Vector3 to, Transform player)
        {
            foreach (RaycastHit h in Physics.RaycastAll(from, to - from, Vector3.Distance(from, to), ~0, QueryTriggerInteraction.Ignore))
            {
                if (!h.collider.transform.IsChildOf(player)) return true;
            }
            return false;
        }

        // ---------- assets ----------

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
        }

        private static Mesh GetOrCreateMesh(string path, Func<Mesh> build)
        {
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) return existing;

            Mesh mesh = build();
            mesh.name = System.IO.Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static Material GetOrCreateMaterial(string path, Color color, float smoothness)
        {
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var material = new Material(shader) { color = color };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        // ---------- low-poly meshes (flat shaded, origin at the apple center) ----------

        // Apple outline from the bottom center to the top center (radius, height), 1 = apple size.
        private static readonly Vector2[] AppleProfile =
        {
            new Vector2(0.00f, 0.07f), new Vector2(0.13f, 0.03f), new Vector2(0.29f, 0.00f), new Vector2(0.43f, 0.07f),
            new Vector2(0.50f, 0.22f), new Vector2(0.52f, 0.40f), new Vector2(0.49f, 0.58f), new Vector2(0.42f, 0.73f),
            new Vector2(0.30f, 0.83f), new Vector2(0.16f, 0.86f), new Vector2(0.06f, 0.82f), new Vector2(0.00f, 0.76f)
        };

        private const float AppleCenterY = 0.43f;

        private static float AppleTopY()
        {
            return (AppleProfile[AppleProfile.Length - 1].y - AppleCenterY) * AppleSize;
        }

        private static Mesh BuildAppleBody()
        {
            var profile = new Vector2[AppleProfile.Length];
            for (int i = 0; i < profile.Length; i++)
                profile[i] = new Vector2(AppleProfile[i].x * AppleSize, (AppleProfile[i].y - AppleCenterY) * AppleSize);
            return Lathe(profile, 10);
        }

        private static Mesh BuildStem()
        {
            Vector2[] profile =
            {
                new Vector2(0f, 0f), new Vector2(0.007f, 0f), new Vector2(0.006f, 0.045f), new Vector2(0f, 0.045f)
            };
            return Lathe(profile, 6);
        }

        private static Mesh BuildLeaf()
        {
            // A diamond with a slight fold along the middle, visible from both sides.
            Vector3 stemEnd = Vector3.zero, tip = new Vector3(0.07f, 0.004f, 0f);
            Vector3 left = new Vector3(0.032f, 0.008f, 0.018f), right = new Vector3(0.032f, 0.008f, -0.018f);
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            AddTriangle(vertices, triangles, stemEnd, left, tip);
            AddTriangle(vertices, triangles, stemEnd, tip, right);
            AddTriangle(vertices, triangles, stemEnd, tip, left);    // back faces
            AddTriangle(vertices, triangles, stemEnd, right, tip);
            return ToMesh(vertices, triangles);
        }

        /// <summary>Revolves a (radius, height) outline around the Y axis. Faces point outwards; points with radius 0 close the caps.</summary>
        private static Mesh Lathe(Vector2[] profile, int segments)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (int j = 0; j < profile.Length - 1; j++)
            {
                for (int i = 0; i < segments; i++)
                {
                    float a0 = i * Mathf.PI * 2f / segments;
                    float a1 = (i + 1) * Mathf.PI * 2f / segments;
                    Vector3 p00 = Ring(profile[j], a0), p01 = Ring(profile[j], a1);
                    Vector3 p10 = Ring(profile[j + 1], a0), p11 = Ring(profile[j + 1], a1);

                    if (profile[j + 1].x > 0f) AddTriangle(vertices, triangles, p00, p10, p11);
                    if (profile[j].x > 0f) AddTriangle(vertices, triangles, p00, p11, p01);
                }
            }
            return ToMesh(vertices, triangles);
        }

        private static Vector3 Ring(Vector2 point, float angle)
        {
            return new Vector3(Mathf.Cos(angle) * point.x, point.y, Mathf.Sin(angle) * point.x);
        }

        private static void AddTriangle(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c)
        {
            // Separate vertices per face: flat shading, the low-poly look of the village.
            triangles.Add(vertices.Count); vertices.Add(a);
            triangles.Add(vertices.Count); vertices.Add(b);
            triangles.Add(vertices.Count); vertices.Add(c);
        }

        private static Mesh ToMesh(List<Vector3> vertices, List<int> triangles)
        {
            var mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
