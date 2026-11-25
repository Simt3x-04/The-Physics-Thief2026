using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using PhysicsThief.Core;
using PhysicsThief.Level;
using PhysicsThief.Player;
using PhysicsThief.Properties;
using PhysicsThief.Properties.Reactors;
using PhysicsThief.UI;
using Object = UnityEngine.Object;

namespace PhysicsThief.EditorTools
{
    /// <summary>
    /// Project setup tools: wires the village map with the colleague's controller, the HUD and the game flow,
    /// builds the physical-object prefabs, connects the main menu and tidies the asset folders.
    /// Everything lives under the menu  Tools > Physics Thief.
    ///
    /// Why a tool and not hand-edited scene files: the village scene is 150 MB of YAML; doing the edits inside
    /// Unity keeps every reference and GUID valid. Each step can be run more than once (it never duplicates
    /// something that already exists) and the scene steps support Undo.
    ///
    /// Recommended order: "0 - Run Full Setup", test the game from the main menu, then "7 - Delete Old ...".
    /// </summary>
    public static class PhysicsThiefTools
    {
        private const string Menu = "Tools/Physics Thief/";
        private const string LogTag = "[Physics Thief Tools] ";

        private const string MainMenuSceneName = "MainMenu";
        private const string VillageSceneName = "VillageScene";
        private const string HudTestSceneName = "HUDtest";
        private const string UiFlowSceneName = "UIFlowTest";

        // Components of the Kinematic Character Controller package that must not stay on the map.
        // Matched by name so the tool works with or without the package installed (it is git-ignored).
        private static readonly string[] KccComponentNames =
        {
            "KinematicCharacterMotor", "ExampleCharacterController", "ExamplePlayer", "ExampleCharacterCamera"
        };

        // Scene roots of UIFlowTest that become prefabs: (name in the scene, prefab name).
        private static readonly string[][] UiScreens =
        {
            new[] { "PauseMenuCanvas", "UI_PauseMenu" },
            new[] { "DeathScreenCanvas", "UI_DeathScreen" },
            new[] { "EndingScreenCanvas", "UI_EndingScreen" },
        };

        // ====================================================================================================
        // 0 - everything in one go
        // ====================================================================================================

        [MenuItem(Menu + "0 - Run Full Setup (recommended)", false, 0)]
        public static void RunFullSetup()
        {
            string plan =
                "This will, in order:\n\n" +
                "  - save the Pause / Death / Ending screens of UIFlowTest as prefabs\n" +
                "  - save the test objects of HUDtest as prefabs (nothing is deleted)\n" +
                "  - build the base prefabs and the objects with a look (barrels, rocks, shovel...)\n" +
                "  - open VillageScene and wire the player, the HUD, the screens and the GameManager\n" +
                "  - make the main menu 'Play' button load VillageScene and fix the Scene List\n\n" +
                "Old files (HUDtest, PlayerMotor, KCC) are NOT deleted here: use step 7 after testing.\n" +
                "VillageScene is saved at the end (it is big: a few seconds).";
            if (!EditorUtility.DisplayDialog("Physics Thief - full setup", plan, "Run", "Cancel")) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var report = new List<string>();
            RunFullSetupSteps(report);
            ShowReport("Full setup", report);
        }

        /// <summary>
        /// Same as step 0 without dialogs, for the command line:
        /// Unity -batchmode -quit -executeMethod PhysicsThief.EditorTools.PhysicsThiefTools.RunFullSetupBatch
        /// Exits with code 1 if a step throws.
        /// </summary>
        public static void RunFullSetupBatch()
        {
            var report = new List<string>();
            bool ok = RunFullSetupSteps(report);
            Debug.Log(LogTag + "BATCH REPORT BEGIN\n" + string.Join("\n", report) + "\n" + LogTag + "BATCH REPORT END");
            if (!ok) EditorApplication.Exit(1);
        }

        private static bool RunFullSetupSteps(List<string> report)
        {
            try
            {
                report.Add(ExtractUiScreensCore());
                report.Add(ConvertHudTestObjectsCore());
                report.Add(CreatePhysicalObjectPrefabsCore());

                string villagePath = FindScenePath(VillageSceneName);
                if (villagePath == null)
                {
                    report.Add("Gameplay setup SKIPPED: scene '" + VillageSceneName + "' not found.");
                }
                else
                {
                    Scene village = OpenSceneSingle(villagePath);
                    report.Add(SetupGameplayCore(village));
                    EditorSceneManager.SaveScene(village);
                }

                report.Add(ConnectMainMenuCore());
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                report.Add("STOPPED by an error: " + e.Message + " (details in the Console). Fix it and run the step again.");
                return false;
            }
        }

        // ====================================================================================================
        // 1 - gameplay setup in the open scene
        // ====================================================================================================

        [MenuItem(Menu + "1 - Setup Gameplay In Open Scene", false, 1)]
        public static void SetupGameplay()
        {
            Scene scene = SceneManager.GetActiveScene();
            string summary = SetupGameplayCore(scene);
            ShowReport("Gameplay setup", new List<string> { summary, "Press Ctrl+S to save the scene." });
        }

        private static string SetupGameplayCore(Scene scene)
        {
            PlayerController controller = FindInScene<PlayerController>(scene);
            if (controller == null)
                return "Gameplay setup SKIPPED: no PlayerController in scene '" + scene.name + "'. Open the map scene first.";

            Undo.SetCurrentGroupName("Physics Thief gameplay setup");
            int undoGroup = Undo.GetCurrentGroup();
            var notes = new List<string>();

            GameObject playerGo = controller.gameObject;

            // 1) The old KCC controller goes away from the character, the camera and the scene.
            int removed = RemoveKccLeftovers(playerGo);
            Camera cam = FindGameplayCamera(scene);
            if (cam != null) removed += RemoveKccLeftovers(cam.gameObject);
            removed += RemoveKccPlayerObject(scene, playerGo);
            if (removed > 0) notes.Add("removed " + removed + " old KCC component(s)/object(s)");

            // 2) The character becomes "Player" (tag included).
            Undo.RecordObject(playerGo, "Rename player");
            playerGo.name = "Player";
            playerGo.tag = "Player";

            // 3) Components that live on the player root.
            PlayerInputHandler input = GetOrAdd<PlayerInputHandler>(playerGo);
            BriefcaseInventory inventory = GetOrAdd<BriefcaseInventory>(playerGo);
            HealthComponent health = GetOrAdd<HealthComponent>(playerGo);

            // 4) The camera becomes a child of the player (as the controller does at runtime) and carries the scanner.
            PlayerScanner scanner = null;
            if (cam == null)
            {
                notes.Add("WARNING: no camera tagged MainCamera found: scanner not added");
            }
            else
            {
                Undo.RecordObject(cam.gameObject, "Rename camera");
                cam.gameObject.name = "Main Camera";
                Undo.SetTransformParent(cam.transform, playerGo.transform, "Parent camera to player");
                Undo.RecordObject(cam.transform, "Place camera at the eyes");
                cam.transform.localPosition = EyeLocalPosition(controller);
                cam.transform.localRotation = Quaternion.identity;

                scanner = GetOrAdd<PlayerScanner>(cam.gameObject);
                AimIndicator aim = GetOrAdd<AimIndicator>(cam.gameObject);

                // The scanner wakes up before the controller moves the camera at runtime, so the references
                // are set here and not left to GetComponentInParent.
                SetRef(scanner, "rayOrigin", cam.transform);
                SetRef(scanner, "inventory", inventory);
                SetRef(scanner, "input", input);
                SetRef(aim, "scanner", scanner);
                SetRef(controller, "cameraRoot", cam.transform);
                notes.Add("camera -> child of Player with scanner + aim line");
            }

            SetRef(controller, "healthComp", health);
            SetRef(controller, "briefcase", inventory);
            SetRef(controller, "input", input);
            if (scanner != null) SetRef(controller, "scanner", scanner);

            // 5) HUD.
            HUDManager hud = FindInScene<HUDManager>(scene);
            if (hud == null)
            {
                GameObject hudPrefab = LoadPrefabByName("HUD");
                if (hudPrefab == null)
                {
                    notes.Add("WARNING: HUD prefab not found");
                }
                else
                {
                    hud = InstantiateInScene(hudPrefab, scene, "Add HUD").GetComponent<HUDManager>();
                    notes.Add("added the HUD");
                }
            }
            if (hud != null)
            {
                SetRef(hud, "playerHealth", health);
                SetRef(hud, "inventory", inventory);
                if (scanner != null) SetRef(hud, "scanner", scanner);
                RemoveLegacyCrosshair(scene);
            }

            // 6) Pause / death / ending screens.
            DeathScreenController death = EnsureScreen<DeathScreenController>(scene, "UI_DeathScreen", notes);
            if (death != null) SetRef(death, "playerHealth", health);
            EnsureScreen<PauseMenuController>(scene, "UI_PauseMenu", notes);
            EnsureScreen<EndingScreenController>(scene, "UI_EndingScreen", notes);

            // 7) GameManager (lets you press Play directly in the map). A duplicate coming from the main menu
            //    destroys itself (Singleton), so the scene works both ways.
            GameManager gameManager = FindInScene<GameManager>(scene);
            if (gameManager == null)
            {
                var gmObject = new GameObject("GameManager");
                Undo.RegisterCreatedObjectUndo(gmObject, "Add GameManager");
                gameManager = Undo.AddComponent<GameManager>(gmObject);
                SetGameManagerConfig(gameManager, scene.name, true);
                notes.Add("added a GameManager (so the map also runs on its own)");
            }

            // 8) The integration component that links player, GameManager and screens.
            HUDTestIntegration integration = FindInScene<HUDTestIntegration>(scene);
            if (integration == null)
            {
                var integrationObject = new GameObject("GameplayIntegration");
                Undo.RegisterCreatedObjectUndo(integrationObject, "Add GameplayIntegration");
                integration = Undo.AddComponent<HUDTestIntegration>(integrationObject);
                notes.Add("added GameplayIntegration");
            }
            SetRef(integration, "player", controller);
            SetRef(integration, "playerHealth", health);
            if (death != null) SetRef(integration, "deathScreen", death);
            if (hud != null) SetRef(integration, "hudManager", hud);

            if (FindInScene<EventSystem>(scene) == null)
                notes.Add("WARNING: no EventSystem in the scene: UI buttons will not respond (GameObject > UI > Event System)");

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(scene);
            return "Gameplay setup in '" + scene.name + "': " + string.Join("; ", notes) + ".";
        }

        private static int RemoveKccLeftovers(GameObject go)
        {
            int removed = 0;
            foreach (Component component in go.GetComponents<Component>())
            {
                if (component == null) continue;   // missing scripts are handled below
                if (Array.IndexOf(KccComponentNames, component.GetType().Name) < 0) continue;
                Undo.DestroyObjectImmediate(component);
                removed++;
            }
            removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);

            // The KCC capsule and its kinematic Rigidbody are useless next to a CharacterController
            // (and a Rigidbody must go after the scripts that required it, which are already removed).
            if (go.GetComponent<CharacterController>() != null)
            {
                foreach (CapsuleCollider capsule in go.GetComponents<CapsuleCollider>())
                {
                    Undo.DestroyObjectImmediate(capsule);
                    removed++;
                }
                Rigidbody body = go.GetComponent<Rigidbody>();
                if (body != null)
                {
                    Undo.DestroyObjectImmediate(body);
                    removed++;
                }
            }
            return removed;
        }

        /// <summary>The KCC sample scene object "Player" (only a disabled ExamplePlayer script): not needed any more.</summary>
        private static int RemoveKccPlayerObject(Scene scene, GameObject keep)
        {
            int removed = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root == keep || root.name != "Player" || root.transform.childCount != 0) continue;

                bool onlyKcc = root.GetComponents<Component>().All(c =>
                    c == null || c is Transform || Array.IndexOf(KccComponentNames, c.GetType().Name) >= 0);
                if (!onlyKcc) continue;

                Undo.DestroyObjectImmediate(root);
                removed++;
            }
            return removed;
        }

        private static Camera FindGameplayCamera(Scene scene)
        {
            Camera fallback = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Camera camera in root.GetComponentsInChildren<Camera>(true))
                {
                    if (camera.CompareTag("MainCamera") && camera.gameObject.activeInHierarchy) return camera;
                    if (camera.name == "ExampleCamera") fallback = camera;
                }
            }
            return fallback;
        }

        /// <summary>Where the controller will put the camera: on the eye anchor, or at the configured height.</summary>
        private static Vector3 EyeLocalPosition(PlayerController controller)
        {
            var so = new SerializedObject(controller);
            SerializedProperty eye = so.FindProperty("eyeAnchor");
            if (eye != null && eye.objectReferenceValue is Transform anchor)
                return controller.transform.InverseTransformPoint(anchor.position);

            SerializedProperty height = so.FindProperty("cameraHeight");
            SerializedProperty forward = so.FindProperty("cameraForwardOffset");
            return new Vector3(0f, height != null ? height.floatValue : 1.43f, forward != null ? forward.floatValue : 0.15f);
        }

        /// <summary>The map had a bare crosshair canvas; the HUD has its own, two would overlap.</summary>
        private static void RemoveLegacyCrosshair(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name != "Canvas" || root.GetComponentInChildren<HUDManager>(true) != null) continue;

                Transform crosshair = root.transform.Find("Crosshair");
                if (crosshair == null) continue;

                if (root.transform.childCount == 1) Undo.DestroyObjectImmediate(root);
                else Undo.DestroyObjectImmediate(crosshair.gameObject);
            }
        }

        private static T EnsureScreen<T>(Scene scene, string prefabName, List<string> notes) where T : Component
        {
            T existing = FindInScene<T>(scene);
            if (existing != null) return existing;

            GameObject prefab = LoadPrefabByName(prefabName);
            if (prefab == null)
            {
                notes.Add("WARNING: prefab " + prefabName + " not found (run step 2)");
                return null;
            }

            GameObject instance = InstantiateInScene(prefab, scene, "Add " + prefabName);
            notes.Add("added " + prefabName);
            return instance.GetComponent<T>();
        }

        private static void SetGameManagerConfig(GameManager gameManager, string levelScene, bool startPlaying)
        {
            var so = new SerializedObject(gameManager);
            SerializedProperty menu = so.FindProperty("mainMenuScene");
            if (menu != null) menu.stringValue = MainMenuSceneName;

            SerializedProperty levels = so.FindProperty("levelScenes");
            if (levels != null)
            {
                levels.arraySize = 1;
                levels.GetArrayElementAtIndex(0).stringValue = levelScene;
            }

            if (startPlaying)
            {
                SerializedProperty state = so.FindProperty("gameState");
                if (state != null) state.enumValueIndex = Array.IndexOf(Enum.GetValues(typeof(GameState)), GameState.Playing);
            }
            so.ApplyModifiedProperties();
        }

        // ====================================================================================================
        // 2 - UI screens from UIFlowTest -> prefabs
        // ====================================================================================================

        [MenuItem(Menu + "2 - Extract UI Screens From UIFlowTest", false, 2)]
        public static void ExtractUiScreens()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            ShowReport("UI screens", new List<string> { ExtractUiScreensCore() });
        }

        private static string ExtractUiScreensCore()
        {
            string scenePath = FindScenePath(UiFlowSceneName);
            if (scenePath == null) return "UI screens SKIPPED: scene '" + UiFlowSceneName + "' not found.";

            string folder = PrefabFolder("UI");
            int created = 0, existing = 0;
            var missing = new List<string>();

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            try
            {
                foreach (string[] screen in UiScreens)
                {
                    string path = folder + "/" + screen[1] + ".prefab";
                    if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
                    {
                        existing++;
                        continue;
                    }

                    GameObject root = FindRootByName(scene, screen[0]);
                    if (root == null)
                    {
                        missing.Add(screen[0]);
                        continue;
                    }

                    // Not "AndConnect": the object in UIFlowTest stays as it was.
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    created++;
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }

            AssetDatabase.SaveAssets();
            string text = "UI screens: " + created + " prefab(s) created, " + existing + " already existed (" + folder + ").";
            if (missing.Count > 0) text += " Not found in UIFlowTest: " + string.Join(", ", missing) + ".";
            return text;
        }

        // ====================================================================================================
        // 3 - base prefabs and objects with a look
        // ====================================================================================================

        private sealed class PhysicalSpec
        {
            public string PrefabName;
            public string DisplayName;
            public string ArtName;       // name of the art prefab (ignored for models)
            public string FolderHint;    // the art asset path must contain this text
            public bool IsModel;         // true: a model file (fbx) found by folder, false: a prefab found by name
            public MaterialTag Material;
            public float Mass = 1f;
            public float Hardness = 1f;
            public float FitSize;        // 0 = keep the size of the art, otherwise the longest side in metres
        }

        // Base values are only what differs from the defaults (Mass 1, Hardness 1, Temperature 20...).
        private static readonly PhysicalSpec[] PhysicalSpecs =
        {
            new PhysicalSpec { PrefabName = "PO_Barrel01", DisplayName = "Wooden barrel", ArtName = "Barrel01", FolderHint = "Stylized", Material = MaterialTag.Wood, Mass = 25f, Hardness = 3f },
            new PhysicalSpec { PrefabName = "PO_Barrel02", DisplayName = "Wooden barrel", ArtName = "Barrel02", FolderHint = "Stylized", Material = MaterialTag.Wood, Mass = 25f, Hardness = 3f },
            new PhysicalSpec { PrefabName = "PO_Rock01", DisplayName = "Boulder", ArtName = "Rock01", FolderHint = "Stylized", Material = MaterialTag.Generic, Mass = 120f, Hardness = 8f },
            new PhysicalSpec { PrefabName = "PO_Rock02", DisplayName = "Boulder", ArtName = "Rock02", FolderHint = "Stylized", Material = MaterialTag.Generic, Mass = 120f, Hardness = 8f },
            new PhysicalSpec { PrefabName = "PO_Rock03", DisplayName = "Boulder", ArtName = "Rock03", FolderHint = "Stylized", Material = MaterialTag.Generic, Mass = 120f, Hardness = 8f },
            new PhysicalSpec { PrefabName = "PO_Rock04", DisplayName = "Boulder", ArtName = "Rock04", FolderHint = "Stylized", Material = MaterialTag.Generic, Mass = 120f, Hardness = 8f },
            new PhysicalSpec { PrefabName = "PO_Shovel", DisplayName = "Shovel", ArtName = "Shovel", FolderHint = "Stylized", Material = MaterialTag.Metal, Mass = 4f, Hardness = 6f },
            new PhysicalSpec { PrefabName = "PO_Fork", DisplayName = "Garden fork", ArtName = "Fork", FolderHint = "Stylized", Material = MaterialTag.Metal, Mass = 3f, Hardness = 5f },
            new PhysicalSpec { PrefabName = "PO_PipeSmall", DisplayName = "Metal pipe", ArtName = "PipeSmall", FolderHint = "Stylized", Material = MaterialTag.Metal, Mass = 10f, Hardness = 6f },
            new PhysicalSpec { PrefabName = "PO_MathHomework", DisplayName = "Math homework", FolderHint = "Compito matematica", IsModel = true, Material = MaterialTag.Paper, Mass = 0.1f, Hardness = 1f, FitSize = 0.4f },
        };

        [MenuItem(Menu + "3 - Create Physical Object Prefabs", false, 3)]
        public static void CreatePhysicalObjectPrefabs()
        {
            ShowReport("Physical object prefabs", new List<string> { CreatePhysicalObjectPrefabsCore() });
        }

        private static string CreatePhysicalObjectPrefabsCore()
        {
            string folder = PrefabFolder("PhysicalObjects");
            int created = 0, existing = 0;
            var problems = new List<string>();

            // Everything is built in a hidden preview scene so the scene you have open is never touched.
            Scene preview = EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject dynamicBase = EnsureBasePrefab(preview, folder + "/PO_Base_Dynamic.prefab", true);
                EnsureBasePrefab(preview, folder + "/PO_Base_Static.prefab", false);

                foreach (PhysicalSpec spec in PhysicalSpecs)
                {
                    string path = folder + "/" + spec.PrefabName + ".prefab";
                    if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
                    {
                        existing++;
                        continue;
                    }

                    GameObject art = LoadArt(spec);
                    if (art == null)
                    {
                        problems.Add(spec.PrefabName + " (art not found)");
                        continue;
                    }

                    if (BuildVariant(preview, dynamicBase, path, spec, art)) created++;
                    else problems.Add(spec.PrefabName + " (saved, but a value did not stick: check it in the Inspector)");
                }
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
            }

            AssetDatabase.SaveAssets();
            string text = "Physical objects: " + created + " created, " + existing + " already existed (" + folder + "). " +
                          "PO_Base_Dynamic = 'Dinamico', PO_Base_Static = 'Fisso'.";
            if (problems.Count > 0) text += " Problems: " + string.Join("; ", problems) + ".";
            return text;
        }

        private static GameObject EnsureBasePrefab(Scene scene, string path, bool dynamicBody)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var root = new GameObject(Path.GetFileNameWithoutExtension(path));
            SceneManager.MoveGameObjectToScene(root, scene);

            root.AddComponent<BoxCollider>();
            if (dynamicBody) root.AddComponent<Rigidbody>();   // before the reactor that requires it
            root.AddComponent<PhysicalObject>();
            if (dynamicBody) root.AddComponent<RigidbodyReactor>();
            root.AddComponent<TemperatureReactor>();
            root.AddComponent<HardnessReactor>();
            root.AddComponent<MagnetismReactor>();
            root.AddComponent<PropertyTimerDisplay>();

            // A plain cube so the base prefab is visible when used on its own; the variants hide it.
            var placeholder = new GameObject("Placeholder");
            placeholder.transform.SetParent(root.transform, false);
            placeholder.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            placeholder.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            placeholder.AddComponent<MeshRenderer>().sharedMaterial = DefaultMaterial();

            BoxCollider box = root.GetComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.5f, 0f);
            box.size = Vector3.one;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static bool BuildVariant(Scene scene, GameObject basePrefab, string path, PhysicalSpec spec, GameObject art)
        {
            var root = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab, scene);
            root.name = spec.PrefabName;

            // Hide the placeholder cube of the base (an instance child cannot be deleted, only overridden).
            Transform placeholder = root.transform.Find("Placeholder");
            if (placeholder != null)
            {
                MeshRenderer placeholderRenderer = placeholder.GetComponent<MeshRenderer>();
                if (placeholderRenderer != null)
                {
                    placeholderRenderer.enabled = false;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(placeholderRenderer);
                }
            }

            // The art keeps its look; its own colliders are switched off, the root BoxCollider is the one that counts.
            var model = (GameObject)PrefabUtility.InstantiatePrefab(art, root.transform);
            model.name = "Model";
            foreach (Collider artCollider in model.GetComponentsInChildren<Collider>(true))
            {
                artCollider.enabled = false;
                PrefabUtility.RecordPrefabInstancePropertyModifications(artCollider);
            }

            if (spec.FitSize > 0f && TryGetBounds(model, out Bounds raw))
            {
                float longest = Mathf.Max(raw.size.x, Mathf.Max(raw.size.y, raw.size.z));
                if (longest > 0.0001f) model.transform.localScale *= spec.FitSize / longest;
            }

            // Pivot at the bottom centre of the model, so the object stands on the ground where you drop it.
            if (TryGetBounds(model, out Bounds bounds))
            {
                model.transform.position += new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
                TryGetBounds(model, out bounds);

                BoxCollider box = root.GetComponent<BoxCollider>();
                box.size = new Vector3(Mathf.Max(bounds.size.x, 0.05f), Mathf.Max(bounds.size.y, 0.05f), Mathf.Max(bounds.size.z, 0.05f));
                box.center = bounds.center;
                PrefabUtility.RecordPrefabInstancePropertyModifications(box);
            }

            // Physical data.
            var so = new SerializedObject(root.GetComponent<PhysicalObject>());
            SerializedProperty displayName = so.FindProperty("displayName");
            SerializedProperty material = so.FindProperty("material");
            SerializedProperty baseline = so.FindProperty("baselineProperties");
            if (displayName != null) displayName.stringValue = spec.DisplayName;
            if (material != null) material.enumValueIndex = (int)spec.Material;
            if (baseline != null)
            {
                var values = new List<PropertyValue>();
                if (!Mathf.Approximately(spec.Mass, 1f)) values.Add(new PropertyValue(AttributeType.Mass, spec.Mass));
                if (!Mathf.Approximately(spec.Hardness, 1f)) values.Add(new PropertyValue(AttributeType.Hardness, spec.Hardness));

                baseline.arraySize = values.Count;
                for (int i = 0; i < values.Count; i++)
                {
                    SerializedProperty element = baseline.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("type").enumValueIndex = (int)values[i].type;
                    element.FindPropertyRelative("value").floatValue = values[i].value;
                }
            }
            so.ApplyModifiedProperties();

            Rigidbody body = root.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.mass = spec.Mass;
                PrefabUtility.RecordPrefabInstancePropertyModifications(body);
            }

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);

            // Check what was really written: the scanner shows this name and reads these values.
            var savedObject = saved != null ? saved.GetComponent<PhysicalObject>() : null;
            return savedObject != null && savedObject.DisplayName == spec.DisplayName &&
                   Mathf.Approximately(savedObject.GetBaseValue(AttributeType.Mass), spec.Mass);
        }

        private static GameObject LoadArt(PhysicalSpec spec)
        {
            string path;
            if (spec.IsModel)
            {
                path = FindAssetPath("t:Model", p => p.IndexOf(spec.FolderHint, StringComparison.OrdinalIgnoreCase) >= 0);
            }
            else
            {
                path = FindAssetPath(spec.ArtName + " t:Prefab",
                    p => Path.GetFileNameWithoutExtension(p) == spec.ArtName &&
                         p.IndexOf(spec.FolderHint, StringComparison.OrdinalIgnoreCase) >= 0);
            }
            return path != null ? AssetDatabase.LoadAssetAtPath<GameObject>(path) : null;
        }

        private static bool TryGetBounds(GameObject root, out Bounds bounds)
        {
            bounds = new Bounds();
            bool found = false;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;   // not particles or lines
                if (!found)
                {
                    bounds = renderer.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }
            return found;
        }

        private static Material DefaultMaterial()
        {
            RenderPipelineAsset pipeline = GraphicsSettings.currentRenderPipeline;
            Material material = pipeline != null ? pipeline.defaultMaterial : null;
            return material != null ? material : AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
        }

        // ====================================================================================================
        // 4 - the test objects of HUDtest -> prefabs
        // ====================================================================================================

        [MenuItem(Menu + "4 - Convert HUDtest Objects To Prefabs", false, 4)]
        public static void ConvertHudTestObjects()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            ShowReport("HUDtest objects", new List<string> { ConvertHudTestObjectsCore() });
        }

        private static string ConvertHudTestObjectsCore()
        {
            string scenePath = FindScenePath(HudTestSceneName);
            if (scenePath == null) return "HUDtest objects SKIPPED: scene '" + HudTestSceneName + "' not found (already removed?).";

            string folder = PrefabFolder("TestObjects");
            int created = 0, existing = 0;

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            try
            {
                // The plate holds a reference to its gate: they are saved together so the link survives.
                GameObject plate = FindRootByName(scene, "Plate");
                GameObject gate = FindRootByName(scene, "Gate");
                if (plate != null && gate != null && plate.GetComponent<PressurePlate>() != null && gate.GetComponent<Gate>() != null)
                {
                    var pair = new GameObject("PuzzlePlateAndGate");
                    SceneManager.MoveGameObjectToScene(pair, scene);
                    pair.transform.position = plate.transform.position;
                    plate.transform.SetParent(pair.transform, true);
                    gate.transform.SetParent(pair.transform, true);
                    if (SavePrefabIfNew(pair, folder + "/PuzzlePlateAndGate.prefab")) created++; else existing++;
                }

                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    if (root.GetComponent<PhysicalObject>() == null) continue;
                    if (SavePrefabIfNew(root, folder + "/" + root.name + ".prefab")) created++; else existing++;
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);   // the regrouping above is discarded: HUDtest is not saved
            }

            AssetDatabase.SaveAssets();
            return "HUDtest objects: " + created + " prefab(s) created, " + existing + " already existed (" + folder + ").";
        }

        private static bool SavePrefabIfNew(GameObject go, string path)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return false;
            PrefabUtility.SaveAsPrefabAsset(go, path);
            return true;
        }

        // ====================================================================================================
        // 5 - main menu -> village
        // ====================================================================================================

        [MenuItem(Menu + "5 - Connect Main Menu To Village", false, 5)]
        public static void ConnectMainMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            ShowReport("Main menu", new List<string> { ConnectMainMenuCore() });
        }

        private static string ConnectMainMenuCore()
        {
            string menuPath = FindScenePath(MainMenuSceneName);
            string villagePath = FindScenePath(VillageSceneName);
            if (menuPath == null || villagePath == null)
                return "Main menu SKIPPED: scene '" + (menuPath == null ? MainMenuSceneName : VillageSceneName) + "' not found.";

            string menuResult;
            Scene menu = EditorSceneManager.OpenScene(menuPath, OpenSceneMode.Additive);
            try
            {
                GameManager gameManager = FindInScene<GameManager>(menu);
                if (gameManager == null)
                {
                    menuResult = "no GameManager in the MainMenu scene (nothing changed)";
                }
                else
                {
                    SetGameManagerConfig(gameManager, VillageSceneName, false);
                    EditorSceneManager.MarkSceneDirty(menu);
                    EditorSceneManager.SaveScene(menu);
                    menuResult = "the main menu GameManager now loads '" + VillageSceneName + "'";
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(menu, true);
            }

            // Scene List: MainMenu first, then the village; the old test scene is dropped.
            var scenes = EditorBuildSettings.scenes.ToList();
            scenes.RemoveAll(s => Path.GetFileNameWithoutExtension(s.path) == HudTestSceneName);
            PlaceInSceneList(scenes, menuPath, 0);
            PlaceInSceneList(scenes, villagePath, 1);
            EditorBuildSettings.scenes = scenes.ToArray();

            return "Main menu: " + menuResult + ". Scene List: " + string.Join(", ", scenes.Select(s => Path.GetFileNameWithoutExtension(s.path))) + ".";
        }

        private static void PlaceInSceneList(List<EditorBuildSettingsScene> scenes, string path, int index)
        {
            scenes.RemoveAll(s => s.path == path);
            scenes.Insert(Mathf.Min(index, scenes.Count), new EditorBuildSettingsScene(path, true));
        }

        // ====================================================================================================
        // 6 - replace selected props with their physical version
        // ====================================================================================================

        [MenuItem(Menu + "Replace Selected Props With Physical Versions", false, 20)]
        public static void ReplaceSelectedWithPhysical()
        {
            GameObject[] selection = Selection.gameObjects;
            if (selection.Length == 0)
            {
                Dialog("Replace props", "Select one or more props (e.g. Barrel01, Rock02) in the Hierarchy first.");
                return;
            }

            int replaced = 0;
            var skipped = new List<string>();
            var created = new List<Object>();

            Undo.SetCurrentGroupName("Replace props with physical versions");
            int undoGroup = Undo.GetCurrentGroup();

            foreach (GameObject go in selection)
            {
                if (!PrefabUtility.IsAnyPrefabInstanceRoot(go))
                {
                    skipped.Add(go.name + " (not the root of a prefab)");
                    continue;
                }
                if (PrefabUtility.GetOutermostPrefabInstanceRoot(go) != go)
                {
                    skipped.Add(go.name + " (inside another prefab: unpack that one first)");
                    continue;
                }

                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(go);
                string physicalName = "PO_" + (source != null ? source.name : go.name);
                GameObject physical = LoadPrefabByName(physicalName);
                if (physical == null)
                {
                    skipped.Add(go.name + " (no " + physicalName + " prefab)");
                    continue;
                }

                var replacement = (GameObject)PrefabUtility.InstantiatePrefab(physical, go.scene);
                Undo.RegisterCreatedObjectUndo(replacement, "Replace with physical version");

                Transform t = replacement.transform;
                t.SetParent(go.transform.parent, false);
                t.SetPositionAndRotation(go.transform.position, go.transform.rotation);
                t.localScale = go.transform.localScale;
                t.SetSiblingIndex(go.transform.GetSiblingIndex());
                replacement.name = go.name;

                created.Add(replacement);
                Undo.DestroyObjectImmediate(go);
                replaced++;
            }

            Undo.CollapseUndoOperations(undoGroup);
            if (created.Count > 0) Selection.objects = created.ToArray();
            if (replaced > 0) EditorSceneManager.MarkAllScenesDirty();

            string text = replaced + " prop(s) replaced.";
            if (skipped.Count > 0) text += "\nSkipped:\n  - " + string.Join("\n  - ", skipped);
            Debug.Log(LogTag + text);
            Dialog("Replace props", text);
        }

        // ====================================================================================================
        // 7 - delete what is not needed any more
        // ====================================================================================================

        [MenuItem(Menu + "7 - Delete Old Test Scene And Controller", false, 30)]
        public static void DeleteOldStuff()
        {
            var targets = new List<string>();
            AddIfNotNull(targets, FindScenePath(HudTestSceneName));
            AddIfNotNull(targets, FindAssetPath("PlayerMotor t:Script", p => Path.GetFileNameWithoutExtension(p) == "PlayerMotor"));
            AddIfNotNull(targets, FindAssetPath("FirstPersonCameraSetup t:Script", p => Path.GetFileNameWithoutExtension(p) == "FirstPersonCameraSetup"));
            if (AssetDatabase.IsValidFolder("Assets/KinematicCharacterController")) targets.Add("Assets/KinematicCharacterController");
            if (File.Exists("Assets/Scriptszip.zip")) targets.Add("Assets/Scriptszip.zip");

            if (targets.Count == 0)
            {
                Dialog("Delete old stuff", "Nothing to delete: the old test scene, PlayerMotor and KCC are already gone.");
                return;
            }

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                string openPath = SceneManager.GetSceneAt(i).path;
                if (targets.Contains(openPath))
                {
                    Dialog("Delete old stuff", "'" + openPath + "' is open. Open VillageScene (File > Open Scene) and run this again.");
                    return;
                }
            }

            string message =
                "These will be DELETED from the project:\n\n  - " + string.Join("\n  - ", targets) +
                "\n\nBefore you continue:\n" +
                "  - VillageScene must already be set up and SAVED (step 0 or 1), otherwise it keeps references to the KCC scripts.\n" +
                "  - Git can bring everything back (except the KCC folder, which is git-ignored: re-download it from the Asset Store).";
            if (!EditorUtility.DisplayDialog("Delete old test scene and controller", message, "Delete", "Cancel")) return;

            var failed = new List<string>();
            foreach (string path in targets)
            {
                if (!AssetDatabase.DeleteAsset(path)) failed.Add(path);
            }
            AssetDatabase.Refresh();

            string text = (targets.Count - failed.Count) + " item(s) deleted.";
            if (failed.Count > 0) text += " Could not delete: " + string.Join(", ", failed) + " (delete them by hand).";
            Debug.Log(LogTag + text);
            Dialog("Delete old stuff", text);
        }

        // ====================================================================================================
        // 8 - what is used, what is not
        // ====================================================================================================

        [MenuItem(Menu + "8 - Report Asset Usage", false, 40)]
        public static void ReportAssetUsage()
        {
            var sceneRoots = new List<string>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
                if (scene.enabled) sceneRoots.Add(scene.path);
            foreach (string name in new[] { MainMenuSceneName, VillageSceneName })
            {
                string path = FindScenePath(name);
                if (path != null && !sceneRoots.Contains(path)) sceneRoots.Add(path);
            }

            HashSet<string> used;
            EditorUtility.DisplayProgressBar("Physics Thief", "Collecting what the game scenes use...", 0.3f);
            try
            {
                used = new HashSet<string>(AssetDatabase.GetDependencies(sceneRoots.ToArray(), true));
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            var sb = new StringBuilder();
            sb.AppendLine("ASSET USAGE (scenes analysed: " + string.Join(", ", sceneRoots.Select(Path.GetFileNameWithoutExtension)) + ")");
            sb.AppendLine("Scripts are not counted: they are used from code, not only from scenes.");
            sb.AppendLine();

            var bigFiles = new List<KeyValuePair<string, long>>();
            foreach (string folder in AssetDatabase.GetSubFolders("Assets"))
            {
                string fullFolder = Path.Combine(projectRoot, folder);
                long total = 0, usedBytes = 0;
                int files = 0, usedFiles = 0;

                foreach (string file in Directory.GetFiles(fullFolder, "*", SearchOption.AllDirectories))
                {
                    string extension = Path.GetExtension(file).ToLowerInvariant();
                    if (extension == ".meta" || extension == ".cs" || extension == ".asmdef") continue;

                    string assetPath = file.Substring(projectRoot.Length + 1).Replace('\\', '/');
                    long size = new FileInfo(file).Length;
                    files++;
                    total += size;
                    if (used.Contains(assetPath))
                    {
                        usedFiles++;
                        usedBytes += size;
                    }
                    if (size > 50L * 1024 * 1024) bigFiles.Add(new KeyValuePair<string, long>(assetPath, size));
                }

                sb.AppendLine(string.Format("{0,-42} {1,5} files {2,8:F1} MB   used by the game: {3,5} files {4,8:F1} MB",
                    folder, files, total / 1048576.0, usedFiles, usedBytes / 1048576.0));
            }

            sb.AppendLine();
            sb.AppendLine("FILES OVER 50 MB (GitHub warns at 50 MB and refuses 100 MB unless they go through Git LFS):");
            foreach (KeyValuePair<string, long> big in bigFiles.OrderByDescending(b => b.Value))
                sb.AppendLine(string.Format("  {0,8:F1} MB  {1}", big.Value / 1048576.0, big.Key));

            Debug.Log(LogTag + sb);
            Dialog("Asset usage", "The report is in the Console (select the message to read it all).\n\n" +
                                  "A folder with 0 used files is a candidate for deletion; check it before removing.");
        }

        // ====================================================================================================
        // 9 - one folder for everything
        // ====================================================================================================

        // Own content only. Packages that expect their own location (TextMesh Pro, URP Settings, the asset-store
        // packs with scripts) stay where they are, except the stylized environment which is just art.
        private static readonly string[][] OrganizePlan =
        {
            new[] { "Assets/Scripts", "Assets/_Project/Scripts" },
            new[] { "Assets/Scenes", "Assets/_Project/Scenes" },
            new[] { "Assets/Prefabs", "Assets/_Project/Prefabs" },
            new[] { "Assets/Tests", "Assets/_Project/Tests" },
            new[] { "Assets/GINO", "Assets/_Project/Art/Characters/GINO" },
            new[] { "Assets/BAG", "Assets/_Project/Art/Characters/Briefcase" },
            new[] { "Assets/NEMICI", "Assets/_Project/Art/Enemies" },
            new[] { "Assets/Textures", "Assets/_Project/Art/Textures" },
            new[] { "Assets/StylizedEnvironement", "Assets/_Project/Art/Environment/StylizedEnvironment" },
            new[] { "Assets/InputSystem_Actions.inputactions", "Assets/_Project/Input/InputSystem_Actions.inputactions" },
        };

        [MenuItem(Menu + "9 - Organize Assets Into _Project", false, 50)]
        public static void OrganizeAssets()
        {
            var plan = new List<string[]>();
            foreach (string[] move in OrganizePlan)
            {
                bool exists = AssetDatabase.IsValidFolder(move[0]) || File.Exists(move[0]);
                bool destinationFree = !AssetDatabase.IsValidFolder(move[1]) && !File.Exists(move[1]);
                if (exists && destinationFree) plan.Add(move);
            }

            if (plan.Count == 0)
            {
                Dialog("Organize assets", "Nothing to move: everything is already inside Assets/_Project.");
                return;
            }

            string message =
                "These moves keep every GUID, so scenes and prefabs do not break:\n\n" +
                string.Join("\n", plan.Select(m => "  " + m[0].Replace("Assets/", "") + "  ->  " + m[1].Replace("Assets/", ""))) +
                "\n\nCommit your work first. VillageScene is tracked by Git LFS by file name (see .gitattributes), " +
                "so moving it is safe.";
            if (!EditorUtility.DisplayDialog("Organize assets into _Project", message, "Move", "Cancel")) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var failed = new List<string>();
            foreach (string[] move in plan)
            {
                EnsureFolder(Path.GetDirectoryName(move[1]).Replace('\\', '/'));
                string error = AssetDatabase.MoveAsset(move[0], move[1]);
                if (!string.IsNullOrEmpty(error)) failed.Add(move[0] + ": " + error);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string text = (plan.Count - failed.Count) + " of " + plan.Count + " folder(s) moved.";
            if (failed.Count > 0) text += "\nFailed:\n  - " + string.Join("\n  - ", failed);
            Debug.Log(LogTag + text);
            Dialog("Organize assets", text);
        }

        // ====================================================================================================
        // shared helpers
        // ====================================================================================================

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T found = root.GetComponentInChildren<T>(true);
                if (found != null) return found;
            }
            return null;
        }

        private static GameObject FindRootByName(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == name) return root;
            return null;
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            return component != null ? component : Undo.AddComponent<T>(go);
        }

        /// <summary>Sets a serialized object reference by field name (private [SerializeField] fields included).</summary>
        private static bool SetRef(Object target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(fieldName);
            if (property == null || property.propertyType != SerializedPropertyType.ObjectReference)
            {
                Debug.LogWarning(LogTag + target.GetType().Name + " has no reference field '" + fieldName + "' (renamed?): left as it was.");
                return false;
            }
            property.objectReferenceValue = value;
            so.ApplyModifiedProperties();
            return true;
        }

        private static GameObject InstantiateInScene(GameObject prefab, Scene scene, string undoName)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            Undo.RegisterCreatedObjectUndo(instance, undoName);
            return instance;
        }

        private static string FindAssetPath(string filter, Func<string, bool> accept)
        {
            foreach (string guid in AssetDatabase.FindAssets(filter))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (accept == null || accept(path)) return path;
            }
            return null;
        }

        private static GameObject LoadPrefabByName(string name)
        {
            string path = FindAssetPath(name + " t:Prefab", p => Path.GetFileNameWithoutExtension(p) == name);
            return path != null ? AssetDatabase.LoadAssetAtPath<GameObject>(path) : null;
        }

        private static string FindScenePath(string name)
        {
            return FindAssetPath(name + " t:Scene", p => Path.GetFileNameWithoutExtension(p) == name);
        }

        private static Scene OpenSceneSingle(string path)
        {
            Scene active = SceneManager.GetActiveScene();
            return active.path == path ? active : EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        }

        /// <summary>Prefab folder: inside _Project once the assets are organized, otherwise Assets/Prefabs.</summary>
        private static string PrefabFolder(string subFolder)
        {
            string root = AssetDatabase.IsValidFolder("Assets/_Project/Prefabs") ? "Assets/_Project/Prefabs" : "Assets/Prefabs";
            string path = root + "/" + subFolder;
            EnsureFolder(path);
            return path;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static void AddIfNotNull(List<string> list, string value)
        {
            if (!string.IsNullOrEmpty(value)) list.Add(value);
        }

        private static void ShowReport(string title, List<string> lines)
        {
            string text = string.Join("\n\n", lines);
            Debug.Log(LogTag + title + ":\n" + text);
            Dialog(title, text);
        }

        private static void Dialog(string title, string message)
        {
            EditorUtility.DisplayDialog(title, message, "OK");
        }
    }
}
