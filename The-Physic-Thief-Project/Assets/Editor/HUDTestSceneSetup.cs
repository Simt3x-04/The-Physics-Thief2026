#if UNITY_EDITOR
// ──────────────────────────────────────────────────────────────────────────────
// HUDTestSceneSetup.cs  (Editor-only)
//
// Run via:  Tools → UI Flow → Wire HUDtest Scene
//
// Adds the UI integration layer to Davide's HUDtest.unity so that:
//   • A GameManager singleton is present (mainMenuScene=MainMenu, state=Playing)
//   • An AudioManager singleton is present
//   • EventSystem already exists in the scene — this script upgrades its module
//     to InputSystemUIInputModule if needed
//   • DeathScreenCanvas with DeathScreenController is wired to the player health
//   • PauseMenuCanvas with PauseMenuController is wired to the input asset
//   • A UIIntegration GO with HUDTestIntegration wires the full death → retry loop
//   • A SpawnPoint GO marks the initial respawn position
//   • HUDtest is added to Build Settings
//
// WHAT IS PRESERVED
// ─────────────────
// All existing HUDtest objects (HUDTest, HUD, HUDTest canvas, Briefcase slots,
// EventSystem, Camera, lights) are left exactly as they are.
// ──────────────────────────────────────────────────────────────────────────────
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;
using PhysicsThief.Audio;
using PhysicsThief.Core;
using PhysicsThief.Player;
using PhysicsThief.UI;

public static class HUDTestSceneSetup
{
    private const string ScenePath = "Assets/Scenes/HUDtest.unity";

    [MenuItem("Tools/UI Flow/Wire HUDtest Scene")]
    public static void WireHUDtest()
    {
        if (!System.IO.File.Exists(ScenePath))
        {
            Debug.LogError($"[HUDTestSetup] {ScenePath} not found.");
            return;
        }

        if (!EditorUtility.DisplayDialog("Wire HUDtest",
                "Add GameManager, Death screen, Pause screen and UIIntegration to HUDtest.unity?\n" +
                "Existing objects are preserved.",
                "Wire", "Cancel"))
            return;

        WireHUDtestInternal();
    }

    /// <summary>
    /// Called from batch-mode scripts (no dialog, no interactive confirmation needed).
    /// </summary>
    public static void WireHUDtestBatch()
    {
        if (!System.IO.File.Exists(ScenePath))
        {
            Debug.LogError($"[HUDTestSetup] {ScenePath} not found.");
            return;
        }
        WireHUDtestInternal();
    }

    private static void WireHUDtestInternal()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // ── 1. Upgrade EventSystem to InputSystemUIInputModule ────────────────
        var existingES = Object.FindFirstObjectByType<EventSystem>();
        if (existingES != null)
        {
            var oldModule = existingES.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            if (oldModule != null) Object.DestroyImmediate(oldModule);

            if (existingES.GetComponent<InputSystemUIInputModule>() == null)
                existingES.gameObject.AddComponent<InputSystemUIInputModule>();

            // Wire the input asset
            var isModule = existingES.GetComponent<InputSystemUIInputModule>();
            if (isModule != null)
            {
                var ia = AssetDatabase.FindAssets("t:InputActionAsset")
                    .Select(g => AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(
                                     AssetDatabase.GUIDToAssetPath(g)))
                    .FirstOrDefault(a => a != null && a.name == "InputSystem_Actions");
                if (ia != null)
                {
                    var so = new SerializedObject(isModule);
                    so.FindProperty("m_ActionsAsset").objectReferenceValue = ia;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        // ── 2. GameManager ────────────────────────────────────────────────────
        GameManager existingGM = Object.FindFirstObjectByType<GameManager>();
        if (existingGM == null)
        {
            var gmGO = new GameObject("GameManager");
            var gm   = gmGO.AddComponent<GameManager>();
            var gmSO = new SerializedObject(gm);
            {
                var p = gmSO.FindProperty("gameState");
                if (p != null) p.enumValueIndex = (int)GameState.Playing;
            }
            {
                var p = gmSO.FindProperty("persistAcrossScenes");
                if (p != null) p.boolValue = true;
            }
            {
                var p = gmSO.FindProperty("mainMenuScene");
                if (p != null) p.stringValue = "MainMenu";
            }
            gmSO.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[HUDTestSetup] Added GameManager (state=Playing, mainMenuScene=MainMenu).");
        }
        else
        {
            Debug.Log("[HUDTestSetup] GameManager already present — skipping.");
        }

        // ── 3. AudioManager ───────────────────────────────────────────────────
        if (Object.FindFirstObjectByType<AudioManager>() == null)
        {
            var amGO = new GameObject("AudioManager");
            amGO.AddComponent<AudioManager>();
            Debug.Log("[HUDTestSetup] Added AudioManager.");
        }

        // ── 4. SpawnPoint ─────────────────────────────────────────────────────
        var spawnPoint = GameObject.Find("SpawnPoint");
        if (spawnPoint == null)
        {
            spawnPoint = new GameObject("SpawnPoint");
            // Position at the HUDTest GO's location as the default spawn
            var hudTestGO = GameObject.Find("HUDTest");
            if (hudTestGO != null)
                spawnPoint.transform.position = hudTestGO.transform.position;
            else
                spawnPoint.transform.position = new Vector3(0, 0.5f, 0);
            Debug.Log($"[HUDTestSetup] SpawnPoint created at {spawnPoint.transform.position}.");
        }

        // ── 5. Death Screen Canvas ────────────────────────────────────────────
        var existingDSC = Object.FindFirstObjectByType<DeathScreenController>(FindObjectsInactive.Include);
        if (existingDSC == null)
        {
            var deathCanvas = BuildDeathCanvas();
            Debug.Log("[HUDTestSetup] DeathScreenCanvas added.");
        }
        else
        {
            Debug.Log("[HUDTestSetup] DeathScreenController already present — skipping.");
        }

        // ── 6. Pause Menu Canvas ──────────────────────────────────────────────
        var existingPMC = Object.FindFirstObjectByType<PauseMenuController>(FindObjectsInactive.Include);
        if (existingPMC == null)
        {
            var ia = AssetDatabase.FindAssets("t:InputActionAsset")
                .Select(g => AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(
                                 AssetDatabase.GUIDToAssetPath(g)))
                .FirstOrDefault(a => a != null && a.name == "InputSystem_Actions");
            BuildPauseCanvas(ia);
            Debug.Log("[HUDTestSetup] PauseMenuCanvas added.");
        }
        else
        {
            Debug.Log("[HUDTestSetup] PauseMenuController already present — skipping.");
        }

        // ── 7. UIIntegration ──────────────────────────────────────────────────
        var existingIntegration = Object.FindFirstObjectByType<HUDTestIntegration>();
        if (existingIntegration == null)
        {
            var uiGO = new GameObject("UIIntegration");
            var hti  = uiGO.AddComponent<HUDTestIntegration>();

            var so = new SerializedObject(hti);

            // HealthComponent is on the Player GO (not HUDTest) in the new HUDtest
            var playerGO = GameObject.Find("Player");
            if (playerGO != null)
            {
                var hc = playerGO.GetComponent<HealthComponent>();
                if (hc != null) so.FindProperty("playerHealth").objectReferenceValue = hc;

                var pm = playerGO.GetComponent<PlayerMotor>();
                if (pm != null) so.FindProperty("playerMotor").objectReferenceValue = pm;
            }
            else
            {
                Debug.LogWarning("[HUDTestSetup] No 'Player' GO found. " +
                                 "Assign playerHealth and playerMotor manually.");
            }

            // DeathScreenController
            var dsc = Object.FindFirstObjectByType<DeathScreenController>(FindObjectsInactive.Include);
            if (dsc != null) so.FindProperty("deathScreen").objectReferenceValue = dsc;

            // HUDManager (optional — may not exist in new HUDtest)
            var hm = Object.FindFirstObjectByType<HUDManager>(FindObjectsInactive.Include);
            if (hm != null) so.FindProperty("hudManager").objectReferenceValue = hm;

            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[HUDTestSetup] UIIntegration wired to Player HealthComponent + PlayerMotor.");
        }
        else
        {
            Debug.Log("[HUDTestSetup] HUDTestIntegration already present — skipping.");
        }

        // ── 8. Add HUDtest to Build Settings ─────────────────────────────────
        var scenes = EditorBuildSettings.scenes.ToList();
        bool alreadyIn = scenes.Any(s => s.path == ScenePath);
        if (!alreadyIn)
        {
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("[HUDTestSetup] HUDtest.unity added to Build Settings.");
        }

        // ── Save ──────────────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);

        Debug.Log(
            "[HUDTestSetup] Done.\n\n" +
            "PLAY MODE STEPS:\n" +
            "  1. Open HUDtest.unity\n" +
            "  2. Set Game view to 16:9\n" +
            "  3. Press Play (WASD + mouse to move)\n" +
            "  4. Scan a hot object (CampFire 300C, Stove 250C), copy Temperature (key 3), paste onto Metal\n" +
            "     Metal turns orange (Hot state) — walk into it — damage per second\n" +
            "     Quick test: press H (HudDebugInput) for -10 HP | J for +25 HP\n" +
            "  5. At 0 HP → Death screen fades in\n" +
            "  6. Click 'Snooze and retry' → HP restored, movement re-enabled\n" +
            "  7. Press Esc → Pause card\n" +
            "  8. Options → adjust sliders → Back → Resume\n\n" +
            "FROM MAIN MENU: Open MainMenu.unity → Press Play → Click Start → HUDtest loads");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Canvas builders (minimal versions matching the existing UI style)
    // ══════════════════════════════════════════════════════════════════════════

    static Canvas MakeCanvas(string name, int sortOrder)
    {
        var go = new GameObject(name);
        var c  = go.AddComponent<Canvas>();
        c.renderMode   = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = sortOrder;
        var cs = go.AddComponent<CanvasScaler>();
        cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        cs.matchWidthOrHeight  = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return c;
    }

    static RectTransform FullPanel(string name, Transform parent, Color bg)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        if (bg.a > 0f) { var img = go.AddComponent<Image>(); img.color = bg; }
        return rt;
    }

    static TMP_Text Txt(string name, string text, Transform parent, int fontSize, Color col,
                        Vector2 size, Vector2 pos,
                        TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size; rt.anchoredPosition = pos;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = fontSize; t.color = col;
        t.alignment = align; t.raycastTarget = false;
        return t;
    }

    static Button PlainButton(string nm, string lbl, Transform parent, Vector2 pos)
    {
        var go  = new GameObject(nm);
        go.transform.SetParent(parent, false);
        var rt  = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(300, 56); rt.anchoredPosition = pos;
        var img = go.AddComponent<Image>();
        img.color = new Color(0.12f, 0.14f, 0.28f, 0.9f);
        var btn = go.AddComponent<Button>(); btn.targetGraphic = img;
        var lblGO = new GameObject("Label"); lblGO.transform.SetParent(go.transform, false);
        var lblRT = lblGO.AddComponent<RectTransform>();
        lblRT.anchorMin = Vector2.zero; lblRT.anchorMax = Vector2.one;
        lblRT.offsetMin = lblRT.offsetMax = Vector2.zero;
        var t = lblGO.AddComponent<TextMeshProUGUI>();
        t.text = lbl; t.fontSize = 26;
        t.color = new Color(0.961f, 0.929f, 0.839f, 1f);
        t.alignment = TextAlignmentOptions.Center; t.raycastTarget = false;
        return btn;
    }

    static DeathScreenController BuildDeathCanvas()
    {
        var canvas  = MakeCanvas("DeathScreenCanvas", 20);
        var root    = canvas.transform;

        // Fade overlay — transparent, controlled by DeathScreenController
        var fadeRT  = FullPanel("FadeOverlayD", root, Color.clear);
        var fadeImg = fadeRT.gameObject.AddComponent<Image>();
        fadeImg.color = new Color(0, 0, 0, 0); fadeImg.raycastTarget = false;

        // Death panel
        var deathPanelRT = FullPanel("DeathPanel", root, Color.clear);
        deathPanelRT.gameObject.SetActive(false);

        // Gino placeholder
        var ginoGO = new GameObject("GinoImage"); ginoGO.transform.SetParent(deathPanelRT, false);
        var ginoRT = ginoGO.AddComponent<RectTransform>();
        ginoRT.anchorMin = new Vector2(0.5f, 0.5f); ginoRT.anchorMax = new Vector2(0.5f, 0.5f);
        ginoRT.sizeDelta = new Vector2(300, 300); ginoRT.anchoredPosition = new Vector2(0, 80);
        var ginoImg = ginoGO.AddComponent<Image>(); ginoImg.color = new Color(0.4f, 0.4f, 0.4f);

        Txt("NightmareLostLabel", "Nightmare lost\u2026", deathPanelRT,
            42, new Color(0.961f, 0.929f, 0.839f, 1f), new Vector2(600, 60), new Vector2(0, -100));

        var retryBtn     = PlainButton("RetryButton",    "SNOOZE AND RETRY", deathPanelRT, new Vector2(0, -170));
        var mainMenuBtn  = PlainButton("MainMenuButton", "MAIN MENU",         deathPanelRT, new Vector2(0, -240));

        var dsc  = canvas.gameObject.AddComponent<DeathScreenController>();
        var dscSO = new SerializedObject(dsc);
        dscSO.FindProperty("deathPanel"    ).objectReferenceValue = deathPanelRT.gameObject;
        dscSO.FindProperty("fadeOverlay"   ).objectReferenceValue = fadeImg;
        dscSO.FindProperty("ginoImage"     ).objectReferenceValue = ginoImg;
        dscSO.FindProperty("retryButton"   ).objectReferenceValue = retryBtn;
        dscSO.FindProperty("mainMenuButton").objectReferenceValue = mainMenuBtn;
        dscSO.ApplyModifiedPropertiesWithoutUndo();

        return dsc;
    }

    static void BuildPauseCanvas(UnityEngine.InputSystem.InputActionAsset inputAsset)
    {
        var canvas = MakeCanvas("PauseMenuCanvas", 10);
        var root   = canvas.transform;

        // Scrim
        var scrimGO = new GameObject("Scrim"); scrimGO.transform.SetParent(root, false);
        var scrimRT = scrimGO.AddComponent<RectTransform>();
        scrimRT.anchorMin = Vector2.zero; scrimRT.anchorMax = Vector2.one;
        scrimRT.offsetMin = scrimRT.offsetMax = Vector2.zero;
        var scrimImg = scrimGO.AddComponent<Image>();
        scrimImg.color = new Color(0.02f, 0.024f, 0.078f, 0.78f); scrimImg.raycastTarget = true;

        // Card
        var cardGO = new GameObject("PauseCard"); cardGO.transform.SetParent(root, false);
        var cardRT = cardGO.AddComponent<RectTransform>();
        cardRT.anchorMin = new Vector2(0.5f, 0.5f); cardRT.anchorMax = new Vector2(0.5f, 0.5f);
        cardRT.sizeDelta = new Vector2(640, 420); cardRT.anchoredPosition = Vector2.zero;
        var cardImg = cardGO.AddComponent<Image>(); cardImg.color = new Color(0.051f, 0.055f, 0.145f, 0.93f);

        Txt("PauseTitle", "PAUSED", cardGO.transform, 52,
            new Color(1f, 0.702f, 0.278f, 1f), new Vector2(580, 60), new Vector2(0, 140));

        var resumeBtn = PlainButton("ResumeButton",  "RESUME",  cardGO.transform, new Vector2(0,  30));
        var optBtn    = PlainButton("OptionsButton", "OPTIONS", cardGO.transform, new Vector2(0, -50));

        var pausePanelGO = new GameObject("PausePanel"); pausePanelGO.transform.SetParent(root, false);
        var ppRT = pausePanelGO.AddComponent<RectTransform>();
        ppRT.anchorMin = Vector2.zero; ppRT.anchorMax = Vector2.one;
        ppRT.offsetMin = ppRT.offsetMax = Vector2.zero;
        scrimGO.transform.SetParent(pausePanelGO.transform, true);
        cardGO.transform.SetParent(pausePanelGO.transform, true);
        pausePanelGO.SetActive(false);

        // Options panel (reuse existing OptionsPanel script wiring)
        var optPanelGO = new GameObject("PauseOptionsPanel"); optPanelGO.transform.SetParent(root, false);
        var opRT = optPanelGO.AddComponent<RectTransform>();
        opRT.anchorMin = Vector2.zero; opRT.anchorMax = Vector2.one;
        opRT.offsetMin = opRT.offsetMax = Vector2.zero;
        optPanelGO.AddComponent<Image>().color = new Color(0.051f, 0.055f, 0.145f, 0.93f);
        var op = optPanelGO.AddComponent<OptionsPanel>();
        // Build minimal slider rows
        BuildMinimalOptionsPanel(optPanelGO.transform, op);
        optPanelGO.SetActive(false);

        var pmc   = canvas.gameObject.AddComponent<PauseMenuController>();
        var pmcSO = new SerializedObject(pmc);
        pmcSO.FindProperty("pausePanel"   ).objectReferenceValue = pausePanelGO;
        pmcSO.FindProperty("optionsPanel" ).objectReferenceValue = op;
        pmcSO.FindProperty("resumeButton" ).objectReferenceValue = resumeBtn;
        pmcSO.FindProperty("optionsButton").objectReferenceValue = optBtn;
        if (inputAsset != null)
            pmcSO.FindProperty("inputActions").objectReferenceValue = inputAsset;
        pmcSO.ApplyModifiedPropertiesWithoutUndo();
    }

    static void BuildMinimalOptionsPanel(Transform parent, OptionsPanel op)
    {
        // "OPTIONS" heading
        Txt("OptionsHeading", "OPTIONS", parent, 52,
            new Color(1f, 0.702f, 0.278f, 1f), new Vector2(620, 60), new Vector2(0, 148f));

        // Volume slider
        var volSlider = BuildSlider("VolumeSlider", parent, new Vector2(400, 10), new Vector2(0, 28f), 0, 1, 0.8f);
        var volLbl    = Txt("Volume_Label", "MASTER VOLUME", parent, 30,
                            new Color(0.961f, 0.929f, 0.839f), new Vector2(280, 44), new Vector2(-310, 28f));
        var volVal    = Txt("Volume_Value", "80%", parent, 26,
                            new Color(1f, 0.702f, 0.278f), new Vector2(90, 44), new Vector2(290, 28f),
                            TextAlignmentOptions.Right | TextAlignmentOptions.Midline);

        // Sensitivity slider
        var sensSlider = BuildSlider("SensSlider", parent, new Vector2(400, 10), new Vector2(0, -68f), 0.1f, 10f, 1f);
        var sensLbl    = Txt("Sens_Label", "MOUSE SENSITIVITY", parent, 30,
                             new Color(0.961f, 0.929f, 0.839f), new Vector2(280, 44), new Vector2(-310, -68f));
        var sensVal    = Txt("Sens_Value", "1.0", parent, 26,
                             new Color(1f, 0.702f, 0.278f), new Vector2(90, 44), new Vector2(290, -68f),
                             TextAlignmentOptions.Right | TextAlignmentOptions.Midline);

        // Back button
        var backBtn = PlainButton("BackButton", "BACK", parent, new Vector2(0, -172f));

        var so = new SerializedObject(op);
        so.FindProperty("volumeSlider"     ).objectReferenceValue = volSlider;
        so.FindProperty("volumeValueLabel" ).objectReferenceValue = volVal;
        so.FindProperty("sensitivitySlider").objectReferenceValue = sensSlider;
        so.FindProperty("sensitivityLabel" ).objectReferenceValue = sensVal;
        so.FindProperty("backButton"       ).objectReferenceValue = backBtn;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static Slider BuildSlider(string name, Transform parent, Vector2 size, Vector2 pos,
                               float min, float max, float val)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(size.x, 32f); rt.anchoredPosition = pos;

        var bgGO = new GameObject("Background"); bgGO.transform.SetParent(go.transform, false);
        var bgRT = bgGO.AddComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0, 0.5f); bgRT.anchorMax = new Vector2(1, 0.5f); bgRT.sizeDelta = new Vector2(0, size.y);
        bgGO.AddComponent<Image>().color = new Color(1, 1, 1, 0.15f);

        var fillAreaGO = new GameObject("Fill Area"); fillAreaGO.transform.SetParent(go.transform, false);
        var faRT = fillAreaGO.AddComponent<RectTransform>();
        faRT.anchorMin = new Vector2(0, 0.5f); faRT.anchorMax = new Vector2(1, 0.5f); faRT.sizeDelta = new Vector2(-10f, size.y);

        var fillGO = new GameObject("Fill"); fillGO.transform.SetParent(fillAreaGO.transform, false);
        var fillRT = fillGO.AddComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one; fillRT.sizeDelta = new Vector2(10, 0);
        fillGO.AddComponent<Image>().color = new Color(1f, 0.702f, 0.278f);

        var hsaGO = new GameObject("Handle Slide Area"); hsaGO.transform.SetParent(go.transform, false);
        var hsaRT = hsaGO.AddComponent<RectTransform>();
        hsaRT.anchorMin = Vector2.zero; hsaRT.anchorMax = Vector2.one;
        hsaRT.offsetMin = new Vector2(8, 0); hsaRT.offsetMax = new Vector2(-8, 0);

        var handleGO = new GameObject("Handle"); handleGO.transform.SetParent(hsaGO.transform, false);
        var handleRT = handleGO.AddComponent<RectTransform>();
        handleRT.anchorMin = new Vector2(0, 0.5f); handleRT.anchorMax = new Vector2(0, 0.5f);
        handleRT.sizeDelta = new Vector2(22f, 22f);
        handleGO.AddComponent<Image>().color = Color.white;

        var slider = go.AddComponent<Slider>();
        slider.fillRect = fillRT; slider.handleRect = handleRT;
        slider.minValue = min; slider.maxValue = max; slider.value = val;
        slider.direction = Slider.Direction.LeftToRight;
        return slider;
    }
}
#endif
