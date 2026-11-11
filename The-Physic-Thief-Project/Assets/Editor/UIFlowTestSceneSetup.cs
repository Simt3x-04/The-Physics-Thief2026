#if UNITY_EDITOR
// ──────────────────────────────────────────────────────────────────────────────
// UIFlowTestSceneSetup.cs  (Editor-only, excluded from builds)
//
// Run via:  Tools → UI Flow → Create UIFlowTest Scene
//
// Generates Assets/Scenes/UIFlowTest.unity with:
//   Main Camera + SensitivityDemoCamera (TEST ONLY — shows slider has live effect)
//   EventSystem  (InputSystemUIInputModule, wired to InputSystem_Actions asset)
//   GameManager  (mainMenuScene=UIFlowTest, levelScenes=[UIFlowTest], gameState=MainMenu)
//   AudioManager
//
//   MainMenuCanvas  (sort 0)  — MainMenuController + OptionsPanel + 3 buttons
//   PauseMenuCanvas (sort 10) — PauseMenuController + OptionsPanel + Resume/Options
//   DeathScreenCanvas(sort 20)— DeathScreenController + FadeOverlayD + DeathPanel
//   EndingScreenCanvas(sort 30)—EndingScreenController + FadeOverlayE + EndingPanel
//   TestHarness — UIFlowTestHarness + HealthComponent (dummy player)
//
// Design decisions preserved by regeneration:
//   • FadeOverlay images start with alpha=0, raycastTarget=false (set at runtime by controllers)
//   • GameManager starts in MainMenu state so cursor is visible and timeScale=1 on first frame
//   • levelScenes=[UIFlowTest] so Start() reloads the demo scene (no real level required)
//   • InputSystem_Actions asset looked up by name/type, not hard-coded path
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
using PhysicsThief.Tests;
using PhysicsThief.UI;

public static class UIFlowTestSceneSetup
{
    private const string ScenePath = "Assets/Scenes/UIFlowTest.unity";

    [MenuItem("Tools/UI Flow/Create UIFlowTest Scene")]
    public static void CreateScene()
    {
        if (System.IO.File.Exists(ScenePath))
        {
            if (!EditorUtility.DisplayDialog("UIFlowTest Scene",
                    $"'{ScenePath}' already exists. Overwrite?", "Overwrite", "Cancel"))
                return;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // ── Camera ───────────────────────────────────────────────────────────
        // DefaultGameObjects already creates a camera; reuse it.
        Camera cam = Object.FindFirstObjectByType<Camera>();
        if (cam == null)
        {
            var camGO = new GameObject("Main Camera");
            cam = camGO.AddComponent<Camera>();
            camGO.tag = "MainCamera";
            camGO.AddComponent<AudioListener>();
        }
        // Attach sensitivity demo camera so the slider has a visible effect in Play Mode.
        // Remove this component when real PlayerController.HandleLook() is implemented.
        cam.gameObject.AddComponent<SensitivityDemoCamera>();

        // ── EventSystem (new Input System) ────────────────────────────────────
        // Remove any default EventSystem created by NewScene.
        foreach (var es in Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
            Object.DestroyImmediate(es.gameObject);

        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        var isUIModule = esGO.AddComponent<InputSystemUIInputModule>();

        // Wire the actions asset — look up by type + name so the path doesn't matter.
        var inputAsset = AssetDatabase.FindAssets("t:InputActionAsset")
            .Select(g => AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(
                             AssetDatabase.GUIDToAssetPath(g)))
            .FirstOrDefault(a => a != null && a.name == "InputSystem_Actions");

        if (inputAsset != null)
        {
            var esSO = new SerializedObject(isUIModule);
            esSO.FindProperty("m_ActionsAsset").objectReferenceValue = inputAsset;
            // Individual Point/Click/Navigate/Submit/Cancel action references are auto-resolved
            // by InputSystemUIInputModule at runtime when m_ActionsAsset is assigned.
            // We only need to set the asset reference here.
            esSO.ApplyModifiedPropertiesWithoutUndo();
        }
        else
        {
            Debug.LogWarning("[UIFlowTestSetup] InputSystem_Actions asset not found. " +
                             "Assign it manually to InputSystemUIInputModule in the Inspector.");
        }

        // ── GameManager ───────────────────────────────────────────────────────
        var gmGO = new GameObject("GameManager");
        var gm   = gmGO.AddComponent<GameManager>();
        var gmSO = new SerializedObject(gm);

        // gameState = MainMenu (0): Start() applies correct cursor/timeScale policy.
        var stateProp = gmSO.FindProperty("gameState");
        if (stateProp != null) { stateProp.enumValueIndex = (int)GameState.MainMenu; }

        // persistAcrossScenes = true: survives scene reloads.
        var persistProp = gmSO.FindProperty("persistAcrossScenes");
        if (persistProp != null) { persistProp.boolValue = true; }

        // mainMenuScene + levelScenes: both point to UIFlowTest so the demo is self-contained.
        var mainSceneProp = gmSO.FindProperty("mainMenuScene");
        if (mainSceneProp != null) { mainSceneProp.stringValue = "UIFlowTest"; }

        var levelsProp = gmSO.FindProperty("levelScenes");
        if (levelsProp != null)
        {
            levelsProp.arraySize = 1;
            levelsProp.GetArrayElementAtIndex(0).stringValue = "UIFlowTest";
        }

        gmSO.ApplyModifiedPropertiesWithoutUndo();

        // ── AudioManager ───────────────────────────────────────────────────────
        var amGO = new GameObject("AudioManager");
        amGO.AddComponent<AudioManager>();

        // ═══════════════════════════════════════════════════════════════════════
        // Local helper functions
        // ═══════════════════════════════════════════════════════════════════════

        // Creates a root Canvas with CanvasScaler (ScaleWithScreenSize 1920×1080) and GraphicRaycaster.
        Canvas MakeCanvas(string name, int sortOrder)
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

        // Creates a child panel (full-stretch RectTransform). Adds an Image only when bg.a > 0.
        RectTransform Panel(string name, Transform parent, Color bg)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            if (bg.a > 0f) { var img = go.AddComponent<Image>(); img.color = bg; }
            return rt;
        }

        // Creates a TMP_Text label child at the default sizeDelta.
        TMP_Text Label(string name, string text, Transform parent, int fontSize = 36)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(600, 60);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text      = text;
            t.fontSize  = fontSize;
            t.alignment = TextAlignmentOptions.Center;
            t.color     = Color.white;
            return t;
        }

        // Creates a button (Image background + TMP_Text label).
        Button MakeButton(string name, string label, Transform parent, Vector2 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta        = new Vector2(300, 60);
            rt.anchoredPosition = pos;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.15f, 0.5f, 0.8f, 0.9f);
            var btn = go.AddComponent<Button>();
            // Label inside the button.
            var lbl = Label(label, label, go.transform, 28);
            lbl.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            lbl.GetComponent<RectTransform>().sizeDelta        = new Vector2(280, 55);
            return btn;
        }

        // Builds a fully-wired Unity UI Slider with Background / Fill / Handle sub-objects.
        Slider MakeSlider(string name, Transform parent, Vector2 pos, float min, float max, float val)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta        = new Vector2(400, 40);
            rt.anchoredPosition = pos;

            var bgGO = new GameObject("Background"); bgGO.transform.SetParent(go.transform, false);
            var bgRT = bgGO.AddComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one; bgRT.sizeDelta = Vector2.zero;
            var bgImg = bgGO.AddComponent<Image>(); bgImg.color = new Color(0.2f, 0.2f, 0.2f);

            var fillArea = new GameObject("Fill Area"); fillArea.transform.SetParent(go.transform, false);
            var faRT = fillArea.AddComponent<RectTransform>();
            faRT.anchorMin = new Vector2(0, 0.25f); faRT.anchorMax = new Vector2(1, 0.75f);
            faRT.offsetMin = new Vector2(5, 0); faRT.offsetMax = new Vector2(-15, 0);

            var fill = new GameObject("Fill"); fill.transform.SetParent(fillArea.transform, false);
            var fillRT = fill.AddComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one; fillRT.sizeDelta = new Vector2(10, 0);
            var fillImg = fill.AddComponent<Image>(); fillImg.color = new Color(0.2f, 0.7f, 1f);

            var hsa = new GameObject("Handle Slide Area"); hsa.transform.SetParent(go.transform, false);
            var hsaRT = hsa.AddComponent<RectTransform>();
            hsaRT.anchorMin = Vector2.zero; hsaRT.anchorMax = Vector2.one;
            hsaRT.offsetMin = new Vector2(10, 0); hsaRT.offsetMax = new Vector2(-10, 0);

            var handle = new GameObject("Handle"); handle.transform.SetParent(hsa.transform, false);
            var handleRT = handle.AddComponent<RectTransform>();
            handleRT.sizeDelta = new Vector2(20, 0);
            handleRT.anchorMin = handleRT.anchorMax = new Vector2(0.5f, 0.5f);
            var handleImg = handle.AddComponent<Image>(); handleImg.color = Color.white;

            var slider = go.AddComponent<Slider>();
            slider.fillRect   = fillRT;
            slider.handleRect = handleRT;
            slider.minValue   = min;
            slider.maxValue   = max;
            slider.value      = val;
            slider.direction  = Slider.Direction.LeftToRight;
            return slider;
        }

        // Builds a complete OptionsPanel hierarchy and wires all serialized fields.
        OptionsPanel BuildOptionsPanel(string goName, Transform parent)
        {
            var panelGO = new GameObject(goName);
            panelGO.transform.SetParent(parent, false);
            var rt = panelGO.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var bg = panelGO.AddComponent<Image>(); bg.color = new Color(0.05f, 0.1f, 0.2f, 0.95f);
            var op = panelGO.AddComponent<OptionsPanel>();

            Label("OptionsTitle", "Options", panelGO.transform, 42)
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 200);

            Label("VolumeLabel", "Master Volume", panelGO.transform, 28)
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(-200, 80);
            var volSlider = MakeSlider("VolumeSlider", panelGO.transform,
                                        new Vector2(80, 80), 0, 1, UISettings.DefaultVolume);
            var volLabel = Label("VolumeValueLabel", "80%", panelGO.transform, 24);
            volLabel.GetComponent<RectTransform>().anchoredPosition = new Vector2(310, 80);

            Label("SensLabel", "Mouse Sensitivity", panelGO.transform, 28)
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(-200, 10);
            var sensSlider = MakeSlider("SensSlider", panelGO.transform,
                                         new Vector2(80, 10),
                                         UISettings.MinSensitivity, UISettings.MaxSensitivity,
                                         UISettings.DefaultSensitivity);
            var sensLabel = Label("SensValueLabel", "1.0", panelGO.transform, 24);
            sensLabel.GetComponent<RectTransform>().anchoredPosition = new Vector2(310, 10);

            var backBtn = MakeButton("BackButton", "Back", panelGO.transform, new Vector2(0, -120));

            var so = new SerializedObject(op);
            so.FindProperty("volumeSlider"     ).objectReferenceValue = volSlider;
            so.FindProperty("volumeValueLabel" ).objectReferenceValue = volLabel;
            so.FindProperty("sensitivitySlider").objectReferenceValue = sensSlider;
            so.FindProperty("sensitivityLabel" ).objectReferenceValue = sensLabel;
            so.FindProperty("backButton"       ).objectReferenceValue = backBtn;
            so.ApplyModifiedPropertiesWithoutUndo();

            panelGO.SetActive(false);   // panels start hidden; controllers activate them
            return op;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // A. MAIN MENU CANVAS  (sort 0)
        // ═══════════════════════════════════════════════════════════════════════
        var mmCanvas  = MakeCanvas("MainMenuCanvas", 0);
        var mmRoot    = mmCanvas.transform;

        var mainPanelRT = Panel("MainPanel", mmRoot, new Color(0.05f, 0.05f, 0.15f, 0.97f));
        Label("TitleLabel", "The Physics Thief", mainPanelRT, 56)
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 250);
        var startBtn  = MakeButton("StartButton",   "Start",   mainPanelRT, new Vector2(0,  100));
        var optBtn1   = MakeButton("OptionsButton", "Options", mainPanelRT, new Vector2(0,   20));
        var quitBtn   = MakeButton("QuitButton",    "Quit",    mainPanelRT, new Vector2(0,  -60));
        var optPanelMM = BuildOptionsPanel("OptionsPanel", mmRoot);

        var mmc  = mmCanvas.gameObject.AddComponent<MainMenuController>();
        var mmSO = new SerializedObject(mmc);
        mmSO.FindProperty("mainPanel"    ).objectReferenceValue = mainPanelRT.gameObject;
        mmSO.FindProperty("optionsPanel" ).objectReferenceValue = optPanelMM;
        mmSO.FindProperty("startButton"  ).objectReferenceValue = startBtn;
        mmSO.FindProperty("optionsButton").objectReferenceValue = optBtn1;
        mmSO.FindProperty("quitButton"   ).objectReferenceValue = quitBtn;
        mmSO.ApplyModifiedPropertiesWithoutUndo();

        // ═══════════════════════════════════════════════════════════════════════
        // B. PAUSE CANVAS  (sort 10)
        // ═══════════════════════════════════════════════════════════════════════
        var pauseCanvas = MakeCanvas("PauseMenuCanvas", 10);
        var pauseRoot   = pauseCanvas.transform;

        var pausePanelRT = Panel("PausePanel", pauseRoot, new Color(0f, 0f, 0f, 0.75f));
        Label("PauseTitle", "PAUSED", pausePanelRT, 48)
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 120);
        var resumeBtn  = MakeButton("ResumeButton",  "Resume",  pausePanelRT, new Vector2(0,  30));
        var optBtn2    = MakeButton("OptionsButton", "Options", pausePanelRT, new Vector2(0, -50));
        pausePanelRT.gameObject.SetActive(false);

        var optPanelPause = BuildOptionsPanel("PauseOptionsPanel", pauseRoot);

        var pmc   = pauseCanvas.gameObject.AddComponent<PauseMenuController>();
        var pmcSO = new SerializedObject(pmc);
        pmcSO.FindProperty("pausePanel"   ).objectReferenceValue = pausePanelRT.gameObject;
        pmcSO.FindProperty("optionsPanel" ).objectReferenceValue = optPanelPause;
        pmcSO.FindProperty("resumeButton" ).objectReferenceValue = resumeBtn;
        pmcSO.FindProperty("optionsButton").objectReferenceValue = optBtn2;
        if (inputAsset != null)
            pmcSO.FindProperty("inputActions").objectReferenceValue = inputAsset;
        pmcSO.ApplyModifiedPropertiesWithoutUndo();

        // ═══════════════════════════════════════════════════════════════════════
        // C. DEATH CANVAS  (sort 20)
        // ═══════════════════════════════════════════════════════════════════════
        var deathCanvas = MakeCanvas("DeathScreenCanvas", 20);
        var deathRoot   = deathCanvas.transform;

        // FadeOverlayD: transparent + non-blocking at start. DeathScreenController.Awake()
        // also calls SetFadeRaycast(false), but setting it here keeps the saved YAML clean.
        var fadeOvD = Panel("FadeOverlayD", deathRoot, Color.clear);
        var fadeImgD = fadeOvD.gameObject.AddComponent<Image>();
        fadeImgD.color         = new Color(0, 0, 0, 0);
        fadeImgD.raycastTarget = false;     // enabled at runtime by DeathScreenController

        var deathPanelRT = Panel("DeathPanel", deathRoot, Color.clear);
        deathPanelRT.gameObject.SetActive(false);

        var ginoGO = new GameObject("GinoImage");
        ginoGO.transform.SetParent(deathPanelRT, false);
        var ginoRT = ginoGO.AddComponent<RectTransform>();
        ginoRT.sizeDelta        = new Vector2(300, 300);
        ginoRT.anchoredPosition = new Vector2(0, 80);
        var ginoImg = ginoGO.AddComponent<Image>();
        ginoImg.color = new Color(0.4f, 0.4f, 0.4f);   // grey placeholder — replace with spiral-eyes art

        Label("NightmareLostLabel", "Nightmare lost\u2026", deathPanelRT, 42)
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -100);
        var retryBtn     = MakeButton("RetryButton",    "Snooze and retry", deathPanelRT, new Vector2(0, -170));
        var deathMenuBtn = MakeButton("MainMenuButton", "Main menu",        deathPanelRT, new Vector2(0, -240));

        var dsc   = deathCanvas.gameObject.AddComponent<DeathScreenController>();
        var dscSO = new SerializedObject(dsc);
        dscSO.FindProperty("deathPanel"    ).objectReferenceValue = deathPanelRT.gameObject;
        dscSO.FindProperty("fadeOverlay"   ).objectReferenceValue = fadeImgD;
        dscSO.FindProperty("ginoImage"     ).objectReferenceValue = ginoImg;
        dscSO.FindProperty("retryButton"   ).objectReferenceValue = retryBtn;
        dscSO.FindProperty("mainMenuButton").objectReferenceValue = deathMenuBtn;
        dscSO.ApplyModifiedPropertiesWithoutUndo();

        // ═══════════════════════════════════════════════════════════════════════
        // D. ENDING CANVAS  (sort 30)
        // ═══════════════════════════════════════════════════════════════════════
        var endingCanvas = MakeCanvas("EndingScreenCanvas", 30);
        var endingRoot   = endingCanvas.transform;

        // FadeOverlayE: transparent + non-blocking at start. EndingScreenController.Awake()
        // also calls SetFadeRaycast(false); keeping it false here keeps saved YAML clean.
        // IMPORTANT: do NOT start this opaque — a full-screen alpha=1 Image on sort=30 would
        // intercept all mouse clicks before they reach the MainMenuCanvas (sort=0).
        var fadeOvE = Panel("FadeOverlayE", endingRoot, Color.clear);
        var fadeImgE = fadeOvE.gameObject.AddComponent<Image>();
        fadeImgE.color         = new Color(0, 0, 0, 0);
        fadeImgE.raycastTarget = false;     // enabled at runtime by EndingScreenController

        var endingPanelRT = Panel("EndingPanel", endingRoot, Color.clear);
        endingPanelRT.gameObject.SetActive(false);

        var bedroomGO = new GameObject("BedroomImage");
        bedroomGO.transform.SetParent(endingPanelRT, false);
        var bedroomRT = bedroomGO.AddComponent<RectTransform>();
        bedroomRT.sizeDelta        = new Vector2(500, 350);
        bedroomRT.anchoredPosition = new Vector2(0, 80);
        var bedroomImg = bedroomGO.AddComponent<Image>();
        bedroomImg.color = new Color(0.3f, 0.3f, 0.5f);   // blue placeholder — replace with bedroom art

        Label("ThanksLabel", "Thanks for playing", endingPanelRT, 48)
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -100);
        var endMenuBtn = MakeButton("MainMenuButton", "Main menu", endingPanelRT, new Vector2(0, -200));

        var esc   = endingCanvas.gameObject.AddComponent<EndingScreenController>();
        var escSO = new SerializedObject(esc);
        escSO.FindProperty("endingPanel"   ).objectReferenceValue = endingPanelRT.gameObject;
        escSO.FindProperty("fadeOverlay"   ).objectReferenceValue = fadeImgE;
        escSO.FindProperty("bedroomImage"  ).objectReferenceValue = bedroomImg;
        escSO.FindProperty("mainMenuButton").objectReferenceValue = endMenuBtn;
        escSO.ApplyModifiedPropertiesWithoutUndo();

        // ═══════════════════════════════════════════════════════════════════════
        // E. TEST HARNESS  (keyboard triggers for all five screens)
        // ═══════════════════════════════════════════════════════════════════════
        var harnessGO  = new GameObject("TestHarness");
        var testHealth = harnessGO.AddComponent<HealthComponent>();
        var harness    = harnessGO.AddComponent<UIFlowTestHarness>();

        var hSO = new SerializedObject(harness);
        hSO.FindProperty("testPlayerHealth").objectReferenceValue = testHealth;
        hSO.FindProperty("deathScreen"     ).objectReferenceValue = dsc;
        hSO.ApplyModifiedPropertiesWithoutUndo();

        // Wire test health to death screen so pressing D triggers the real fade.
        var dscSOb = new SerializedObject(dsc);
        dscSOb.FindProperty("playerHealth").objectReferenceValue = testHealth;
        dscSOb.ApplyModifiedPropertiesWithoutUndo();

        // ═══════════════════════════════════════════════════════════════════════
        // Save
        // ═══════════════════════════════════════════════════════════════════════
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        UpdateBuildSettingsGuid();

        Debug.Log(
            "[UIFlowTestSetup] UIFlowTest scene created.\n" +
            "Open Assets/Scenes/UIFlowTest.unity and press Play.\n" +
            "  Start → reloads demo scene as 'level' (all UI survives in DontDestroyOnLoad)\n" +
            "  Esc   → Pause → Resume / Options / Esc-chain\n" +
            "  D     → Death screen    R → Retry    M → Main Menu\n" +
            "  E     → Ending screen\n" +
            "  F1    → Smoke test (check Console)\n" +
            "Assign real art to GinoImage (spiral-eyes) and BedroomImage when available.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    private static void UpdateBuildSettingsGuid()
    {
        string metaPath = ScenePath + ".meta";
        if (!System.IO.File.Exists(metaPath)) return;

        string guid = "";
        foreach (var line in System.IO.File.ReadAllLines(metaPath))
        {
            if (line.TrimStart().StartsWith("guid:"))
            {
                guid = line.Split(':')[1].Trim();
                break;
            }
        }
        if (string.IsNullOrEmpty(guid)) return;

        string settingsPath = "ProjectSettings/EditorBuildSettings.asset";
        string content = System.IO.File.ReadAllText(settingsPath);
        content = content.Replace("guid: 00000000000000000000000000000001", $"guid: {guid}");
        System.IO.File.WriteAllText(settingsPath, content);
        Debug.Log($"[UIFlowTestSetup] Build settings updated with UIFlowTest GUID: {guid}");
    }
}
#endif
