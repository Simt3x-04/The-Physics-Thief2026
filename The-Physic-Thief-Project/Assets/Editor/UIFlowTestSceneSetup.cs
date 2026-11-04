#if UNITY_EDITOR
// ──────────────────────────────────────────────────────────────────────────────
// UIFlowTestSceneSetup.cs  (Editor-only utility)
//
// Run once via:  Tools > UI Flow > Create UIFlowTest Scene
//
// What it creates:
//   Assets/Scenes/UIFlowTest.unity
//     Main Camera
//     EventSystem  (InputSystemUIInputModule)
//     GameManager  (GameManager component, persistAcrossScenes=true)
//     AudioManager (AudioManager component)
//
//     --- Main Menu Canvas ---
//     MainMenuCanvas (Canvas, CanvasScaler, GraphicRaycaster)
//       MainPanel
//         TitleLabel (TMP_Text "The Physics Thief")
//         StartButton / OptionsButton / QuitButton  (Button + TMP_Text)
//       OptionsPanel (OptionsPanel component)
//         VolumeRow: VolumeLabel, VolumeSlider, VolumeValueLabel
//         SensitivityRow: SensLabel, SensSlider, SensValueLabel
//         BackButton
//     MainMenuController wired to all slots
//
//     --- Pause Canvas ---
//     PauseMenuCanvas (Canvas, separate sort order, PauseMenuController)
//       PausePanel
//         ResumeButton / OptionsButton
//       PauseOptionsPanel (OptionsPanel component, shared style)
//
//     --- Death Canvas ---
//     DeathScreenCanvas (Canvas, DeathScreenController)
//       FadeOverlay (full-screen black Image)
//       DeathPanel
//         GinoImage (placeholder solid-grey Image)
//         NightmareLostLabel (TMP_Text)
//         RetryButton / MainMenuButton
//
//     --- Ending Canvas ---
//     EndingScreenCanvas (Canvas, EndingScreenController)
//       FadeOverlay
//       EndingPanel
//         BedroomImage (placeholder)
//         ThanksLabel (TMP_Text)
//         MainMenuButton
//
//     --- Test Harness ---
//     TestHarness (UIFlowTestHarness, HealthComponent as test player)
//
// After running this command, inspect each Canvas in the Hierarchy, verify
// all serialized fields are green (no missing), and press Play.
// ──────────────────────────────────────────────────────────────────────────────
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using PhysicsThief.Core;
using PhysicsThief.Audio;
using PhysicsThief.UI;
using PhysicsThief.Tests;

public static class UIFlowTestSceneSetup
{
    private const string ScenePath = "Assets/Scenes/UIFlowTest.unity";

    [MenuItem("Tools/UI Flow/Create UIFlowTest Scene")]
    public static void CreateScene()
    {
        // Ask before overwriting.
        if (System.IO.File.Exists(ScenePath))
        {
            if (!EditorUtility.DisplayDialog("UIFlowTest Scene",
                $"'{ScenePath}' already exists. Overwrite?", "Overwrite", "Cancel"))
                return;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // ── Camera ──────────────────────────────────────────────────────────
        Camera cam = Object.FindFirstObjectByType<Camera>();
        if (cam == null)
        {
            var camGO = new GameObject("Main Camera");
            cam = camGO.AddComponent<Camera>();
            camGO.tag = "MainCamera";
            camGO.AddComponent<AudioListener>();
        }

        // ── EventSystem (InputSystem) ────────────────────────────────────────
        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<InputSystemUIInputModule>();

        // ── GameManager ──────────────────────────────────────────────────────
        var gmGO = new GameObject("GameManager");
        var gm   = gmGO.AddComponent<GameManager>();
        // Set to Playing so Pause/Death/Ending can be triggered.
        // The serialized gameState defaults to MainMenu; we call SetState at runtime.
        // For the test scene we want it to start in Playing so Esc works immediately.
        // We achieve this by setting the serialized field via SerializedObject.
        var gmSO = new SerializedObject(gm);
        var stateProp = gmSO.FindProperty("gameState");
        if (stateProp != null) { stateProp.enumValueIndex = (int)GameState.Playing; gmSO.ApplyModifiedPropertiesWithoutUndo(); }
        var persistProp = gmSO.FindProperty("persistAcrossScenes");
        if (persistProp != null) { persistProp.boolValue = true; gmSO.ApplyModifiedPropertiesWithoutUndo(); }

        // ── AudioManager ────────────────────────────────────────────────────
        var amGO = new GameObject("AudioManager");
        amGO.AddComponent<AudioManager>();

        // ── INPUT ASSET reference (for PauseMenuController) ─────────────────
        // Find the asset by searching all .inputactions files so path casing/location doesn't matter.
        var inputAsset = AssetDatabase.FindAssets("t:InputActionAsset")
            .Select(g => AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(
                AssetDatabase.GUIDToAssetPath(g)))
            .FirstOrDefault(a => a != null && a.name == "InputSystem_Actions");
        if (inputAsset == null)
            Debug.LogWarning("[UIFlowTestSetup] Could not find InputSystem_Actions asset. " +
                             "Assign it manually to PauseMenuController in the Inspector.");

        // ── Helpers ─────────────────────────────────────────────────────────
        Canvas MakeCanvas(string name, int sortOrder, bool worldSpace = false)
        {
            var go = new GameObject(name);
            var c  = go.AddComponent<Canvas>();
            c.renderMode  = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = sortOrder;
            var cs = go.AddComponent<CanvasScaler>();
            cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920, 1080);
            cs.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            cs.matchWidthOrHeight  = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return c;
        }

        RectTransform Panel(string name, Transform parent, Color bg)
        {
            var go  = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt  = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            if (bg.a > 0)
            {
                var img   = go.AddComponent<Image>();
                img.color = bg;
            }
            return rt;
        }

        TMP_Text Label(string name, string text, Transform parent, int fontSize = 36)
        {
            var go  = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt  = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(600, 60);
            var t   = go.AddComponent<TextMeshProUGUI>();
            t.text      = text;
            t.fontSize  = fontSize;
            t.alignment = TextAlignmentOptions.Center;
            t.color     = Color.white;
            return t;
        }

        Button MakeButton(string name, string label, Transform parent, Vector2 pos)
        {
            var go  = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt  = go.AddComponent<RectTransform>();
            rt.sizeDelta        = new Vector2(300, 60);
            rt.anchoredPosition = pos;
            var img  = go.AddComponent<Image>();
            img.color = new Color(0.15f, 0.5f, 0.8f, 0.9f);
            var btn  = go.AddComponent<Button>();
            var lbl  = Label(label, label, go.transform, 28);
            lbl.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            lbl.GetComponent<RectTransform>().sizeDelta        = new Vector2(280, 55);
            return btn;
        }

        Slider MakeSlider(string name, Transform parent, Vector2 pos, float min, float max, float val)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta        = new Vector2(400, 40);
            rt.anchoredPosition = pos;

            // Background
            var bgGO  = new GameObject("Background"); bgGO.transform.SetParent(go.transform, false);
            var bgRT  = bgGO.AddComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one; bgRT.sizeDelta = Vector2.zero;
            var bgImg = bgGO.AddComponent<Image>(); bgImg.color = new Color(0.2f,0.2f,0.2f);

            // Fill Area
            var fillArea = new GameObject("Fill Area"); fillArea.transform.SetParent(go.transform, false);
            var faRT     = fillArea.AddComponent<RectTransform>();
            faRT.anchorMin = new Vector2(0,0.25f); faRT.anchorMax = new Vector2(1,0.75f);
            faRT.offsetMin = new Vector2(5,0); faRT.offsetMax = new Vector2(-15,0);

            var fill   = new GameObject("Fill"); fill.transform.SetParent(fillArea.transform, false);
            var fillRT = fill.AddComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one; fillRT.sizeDelta = new Vector2(10,0);
            var fillImg = fill.AddComponent<Image>(); fillImg.color = new Color(0.2f,0.7f,1f);

            // Handle Slide Area
            var hsa   = new GameObject("Handle Slide Area"); hsa.transform.SetParent(go.transform, false);
            var hsaRT = hsa.AddComponent<RectTransform>();
            hsaRT.anchorMin = Vector2.zero; hsaRT.anchorMax = Vector2.one;
            hsaRT.offsetMin = new Vector2(10,0); hsaRT.offsetMax = new Vector2(-10,0);

            var handle   = new GameObject("Handle"); handle.transform.SetParent(hsa.transform, false);
            var handleRT = handle.AddComponent<RectTransform>();
            handleRT.sizeDelta = new Vector2(20,0); handleRT.anchorMin = handleRT.anchorMax = new Vector2(0.5f,0.5f);
            var handleImg = handle.AddComponent<Image>(); handleImg.color = Color.white;

            var slider          = go.AddComponent<Slider>();
            slider.fillRect     = fillRT;
            slider.handleRect   = handleRT;
            slider.minValue     = min;
            slider.maxValue     = max;
            slider.value        = val;
            slider.direction    = Slider.Direction.LeftToRight;
            return slider;
        }

        // ── Build OptionsPanel sub-hierarchy ─────────────────────────────────
        OptionsPanel BuildOptionsPanel(string goName, Transform parent)
        {
            var panelGO = new GameObject(goName);
            panelGO.transform.SetParent(parent, false);
            var rt = panelGO.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var bg = panelGO.AddComponent<Image>(); bg.color = new Color(0.05f,0.1f,0.2f,0.95f);
            var op = panelGO.AddComponent<OptionsPanel>();

            Label("OptionsTitle", "Options", panelGO.transform, 42)
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 200);

            Label("VolumeLabel", "Master Volume", panelGO.transform, 28)
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(-200, 80);
            var volSlider = MakeSlider("VolumeSlider", panelGO.transform, new Vector2(80,80), 0,1, UISettings.DefaultVolume);
            var volLabel  = Label("VolumeValueLabel", "80%", panelGO.transform, 24);
            volLabel.GetComponent<RectTransform>().anchoredPosition = new Vector2(310, 80);

            Label("SensLabel", "Mouse Sensitivity", panelGO.transform, 28)
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(-200, 10);
            var sensSlider = MakeSlider("SensSlider", panelGO.transform, new Vector2(80,10), UISettings.MinSensitivity, UISettings.MaxSensitivity, UISettings.DefaultSensitivity);
            var sensLabel  = Label("SensValueLabel", "1.0", panelGO.transform, 24);
            sensLabel.GetComponent<RectTransform>().anchoredPosition = new Vector2(310, 10);

            var backBtn = MakeButton("BackButton", "Back", panelGO.transform, new Vector2(0, -120));

            // Wire serialized fields via SerializedObject.
            var so = new SerializedObject(op);
            so.FindProperty("volumeSlider"    ).objectReferenceValue = volSlider;
            so.FindProperty("volumeValueLabel").objectReferenceValue = volLabel;
            so.FindProperty("sensitivitySlider").objectReferenceValue = sensSlider;
            so.FindProperty("sensitivityLabel").objectReferenceValue = sensLabel;
            so.FindProperty("backButton"      ).objectReferenceValue = backBtn;
            so.ApplyModifiedPropertiesWithoutUndo();

            panelGO.SetActive(false);  // starts hidden
            return op;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // A. MAIN MENU CANVAS
        // ═══════════════════════════════════════════════════════════════════════
        var mmCanvas = MakeCanvas("MainMenuCanvas", 0);
        var mmRoot   = mmCanvas.transform;

        var mainPanelRT = Panel("MainPanel", mmRoot, new Color(0.05f,0.05f,0.15f,0.97f));
        Label("TitleLabel", "The Physics Thief", mainPanelRT, 56)
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 250);
        var startBtn   = MakeButton("StartButton",   "Start",   mainPanelRT, new Vector2(0,  100));
        var optBtn1    = MakeButton("OptionsButton", "Options", mainPanelRT, new Vector2(0,   20));
        var quitBtn    = MakeButton("QuitButton",    "Quit",    mainPanelRT, new Vector2(0, -60));

        var optPanelMM = BuildOptionsPanel("OptionsPanel", mmRoot);

        var mmc = mmCanvas.gameObject.AddComponent<MainMenuController>();
        var mmSO = new SerializedObject(mmc);
        mmSO.FindProperty("mainPanel"    ).objectReferenceValue = mainPanelRT.gameObject;
        mmSO.FindProperty("optionsPanel" ).objectReferenceValue = optPanelMM;
        mmSO.FindProperty("startButton"  ).objectReferenceValue = startBtn;
        mmSO.FindProperty("optionsButton").objectReferenceValue = optBtn1;
        mmSO.FindProperty("quitButton"   ).objectReferenceValue = quitBtn;
        mmSO.ApplyModifiedPropertiesWithoutUndo();

        // Main menu not visible during test (state=Playing); but it will become visible
        // when LoadMainMenu() is called.  Leave canvas enabled so MainMenuController.Awake runs.

        // ═══════════════════════════════════════════════════════════════════════
        // B. PAUSE CANVAS
        // ═══════════════════════════════════════════════════════════════════════
        var pauseCanvas = MakeCanvas("PauseMenuCanvas", 10);
        var pauseRoot   = pauseCanvas.transform;

        var pausePanelRT = Panel("PausePanel", pauseRoot, new Color(0f,0f,0f,0.75f));
        Label("PauseTitle", "PAUSED", pausePanelRT, 48)
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 120);
        var resumeBtn  = MakeButton("ResumeButton",  "Resume",  pausePanelRT, new Vector2(0,  30));
        var optBtn2    = MakeButton("OptionsButton", "Options", pausePanelRT, new Vector2(0, -50));
        pausePanelRT.gameObject.SetActive(false);

        var optPanelPause = BuildOptionsPanel("PauseOptionsPanel", pauseRoot);

        var pmc  = pauseCanvas.gameObject.AddComponent<PauseMenuController>();
        var pmcSO = new SerializedObject(pmc);
        pmcSO.FindProperty("pausePanel"    ).objectReferenceValue = pausePanelRT.gameObject;
        pmcSO.FindProperty("optionsPanel"  ).objectReferenceValue = optPanelPause;
        pmcSO.FindProperty("resumeButton"  ).objectReferenceValue = resumeBtn;
        pmcSO.FindProperty("optionsButton" ).objectReferenceValue = optBtn2;
        if (inputAsset != null)
            pmcSO.FindProperty("inputActions").objectReferenceValue = inputAsset;
        pmcSO.ApplyModifiedPropertiesWithoutUndo();

        // ═══════════════════════════════════════════════════════════════════════
        // C. DEATH CANVAS
        // ═══════════════════════════════════════════════════════════════════════
        var deathCanvas = MakeCanvas("DeathScreenCanvas", 20);
        var deathRoot   = deathCanvas.transform;

        // FadeOverlayD: starts transparent; DeathScreenController animates alpha.
        // raycastTarget=true so it blocks clicks to gameplay objects once faded in.
        var fadeOvD = Panel("FadeOverlayD", deathRoot, new Color(0, 0, 0, 0));
        var fadeImgD = fadeOvD.gameObject.GetComponent<Image>();
        if (fadeImgD == null) fadeImgD = fadeOvD.gameObject.AddComponent<Image>();
        fadeImgD.color         = new Color(0, 0, 0, 0);   // transparent at start
        fadeImgD.raycastTarget = true;

        var deathPanelRT = Panel("DeathPanel", deathRoot, Color.clear);
        deathPanelRT.gameObject.SetActive(false);

        // Gino placeholder (grey rect) – replace with spiral-eyes art when available.
        var ginoGO = new GameObject("GinoImage");
        ginoGO.transform.SetParent(deathPanelRT, false);
        var ginoRT  = ginoGO.AddComponent<RectTransform>();
        ginoRT.sizeDelta        = new Vector2(300, 300);
        ginoRT.anchoredPosition = new Vector2(0, 80);
        var ginoImg = ginoGO.AddComponent<Image>();
        ginoImg.color = new Color(0.4f, 0.4f, 0.4f);

        Label("NightmareLostLabel", "Nightmare lost…", deathPanelRT, 42)
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -100);
        var retryBtn     = MakeButton("RetryButton",    "Snooze and retry", deathPanelRT, new Vector2(0, -170));
        var deathMenuBtn = MakeButton("MainMenuButton", "Main menu",        deathPanelRT, new Vector2(0, -240));

        var dsc  = deathCanvas.gameObject.AddComponent<DeathScreenController>();
        var dscSO = new SerializedObject(dsc);
        dscSO.FindProperty("deathPanel"        ).objectReferenceValue = deathPanelRT.gameObject;
        dscSO.FindProperty("fadeOverlay"       ).objectReferenceValue = fadeImgD;
        dscSO.FindProperty("ginoImage"         ).objectReferenceValue = ginoImg;
        dscSO.FindProperty("retryButton"       ).objectReferenceValue = retryBtn;
        dscSO.FindProperty("mainMenuButton"    ).objectReferenceValue = deathMenuBtn;
        dscSO.ApplyModifiedPropertiesWithoutUndo();

        // ═══════════════════════════════════════════════════════════════════════
        // D. ENDING CANVAS
        // ═══════════════════════════════════════════════════════════════════════
        var endingCanvas = MakeCanvas("EndingScreenCanvas", 30);
        var endingRoot   = endingCanvas.transform;

        // FadeOverlayE: starts TRANSPARENT (alpha=0). EndingScreenController fades it
        // from black-to-transparent when the ending triggers. Do NOT start opaque —
        // a full-screen black Image with raycastTarget=1 would block all clicks on lower
        // canvases while the ending has not yet been triggered.
        var fadeOvE = Panel("FadeOverlayE", endingRoot, new Color(0, 0, 0, 0));
        var fadeImgE = fadeOvE.gameObject.GetComponent<Image>();
        if (fadeImgE == null) fadeImgE = fadeOvE.gameObject.AddComponent<Image>();
        fadeImgE.color         = new Color(0, 0, 0, 0);   // transparent at start
        fadeImgE.raycastTarget = true;

        var endingPanelRT = Panel("EndingPanel", endingRoot, Color.clear);
        endingPanelRT.gameObject.SetActive(false);

        var bedroomGO  = new GameObject("BedroomImage");
        bedroomGO.transform.SetParent(endingPanelRT, false);
        var bedroomRT  = bedroomGO.AddComponent<RectTransform>();
        bedroomRT.sizeDelta = new Vector2(500, 350);
        bedroomRT.anchoredPosition = new Vector2(0, 80);
        var bedroomImg = bedroomGO.AddComponent<Image>();
        bedroomImg.color = new Color(0.3f,0.3f,0.5f);   // placeholder; replace with bedroom art

        Label("ThanksLabel", "Thanks for playing", endingPanelRT, 48)
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -100);
        var endMenuBtn = MakeButton("MainMenuButton", "Main menu", endingPanelRT, new Vector2(0,-200));

        var esc  = endingCanvas.gameObject.AddComponent<EndingScreenController>();
        var escSO = new SerializedObject(esc);
        escSO.FindProperty("endingPanel"  ).objectReferenceValue = endingPanelRT.gameObject;
        escSO.FindProperty("fadeOverlay"  ).objectReferenceValue = fadeImgE;
        escSO.FindProperty("bedroomImage" ).objectReferenceValue = bedroomImg;
        escSO.FindProperty("mainMenuButton").objectReferenceValue = endMenuBtn;
        escSO.ApplyModifiedPropertiesWithoutUndo();

        // ═══════════════════════════════════════════════════════════════════════
        // E. TEST HARNESS
        // ═══════════════════════════════════════════════════════════════════════
        var harnessGO  = new GameObject("TestHarness");
        var testHealth = harnessGO.AddComponent<HealthComponent>();
        var harness    = harnessGO.AddComponent<UIFlowTestHarness>();

        var hSO = new SerializedObject(harness);
        hSO.FindProperty("testPlayerHealth").objectReferenceValue = testHealth;
        hSO.FindProperty("deathScreen"     ).objectReferenceValue = dsc;
        hSO.ApplyModifiedPropertiesWithoutUndo();

        // Wire health to death screen directly (so in-scene assignment is clear).
        var dscSOb = new SerializedObject(dsc);
        dscSOb.FindProperty("playerHealth").objectReferenceValue = testHealth;
        dscSOb.ApplyModifiedPropertiesWithoutUndo();

        // ═══════════════════════════════════════════════════════════════════════
        // Save scene
        // ═══════════════════════════════════════════════════════════════════════
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);

        // Update build settings GUID now that scene file exists.
        UpdateBuildSettingsGuid();

        Debug.Log($"[UIFlowTestSetup] Scene created at {ScenePath}. " +
                  "Open it in Unity, press Play, then use D/E/P/R/M keys to test each screen. " +
                  "Assign real art to GinoImage and BedroomImage placeholders when available.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    private static void UpdateBuildSettingsGuid()
    {
        // Read the real GUID from the newly-created .meta file.
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
        // Replace the placeholder GUID we wrote earlier.
        content = content.Replace("guid: 00000000000000000000000000000001", $"guid: {guid}");
        System.IO.File.WriteAllText(settingsPath, content);
        Debug.Log($"[UIFlowTestSetup] Updated build settings with UIFlowTest GUID: {guid}");
    }
}
#endif
