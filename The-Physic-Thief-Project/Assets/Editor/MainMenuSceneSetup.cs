#if UNITY_EDITOR
// ──────────────────────────────────────────────────────────────────────────────
// MainMenuSceneSetup.cs  (Editor-only)
//
// Run via:  Tools → UI Flow → Rebuild MainMenu Scene
//
// Rebuilds Assets/Scenes/MainMenu.unity with the redesigned layout while
// preserving the GameManager and EventSystem that already exist in that scene.
//
// WHAT IT BUILDS
// ──────────────
// MainMenuCanvas (sort 0, ScaleWithScreenSize 1920×1080)
//   Backdrop              — mid-navy Image, full canvas (swap for team art)
//   LeftColumn            — deep-navy strip, left 38% of screen
//   ArtPlaceholderHint    — instructional text on the right half
//   MainPanel             — transparent hit container over the left column
//     TitleLabel           — "THE PHYSICS THIEF" 68pt warm-off-white
//     Subtitle             — "steal the laws of physics" italic
//     StartButton          — StyledButton → MenuButtonEffect (amber marker)
//     OptionsButton        — StyledButton (hidden when optionsPanel is null)
//     QuitButton           — StyledButton
//   OptionsPanel           — OptionsPanel component, full-canvas dark card
//     (volume + sensitivity sliders, Back button, all wired)
//
// MainMenuController is attached to MainMenuCanvas.
// GameManager + EventSystem from the original scene are preserved.
// ──────────────────────────────────────────────────────────────────────────────
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;
using PhysicsThief.Core;
using PhysicsThief.UI;

public static class MainMenuSceneSetup
{
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";

    private const float RefW = 1920f;
    private const float RefH = 1080f;
    private static float LeftColW => RefW * MenuStyle.LeftColumnWidthFraction;

    // ─────────────────────────────────────────────────────────────────────────
    [MenuItem("Tools/UI Flow/Rebuild MainMenu Scene")]
    public static void RebuildMainMenu()
    {
        if (!System.IO.File.Exists(ScenePath))
        {
            Debug.LogError($"[MainMenuSetup] {ScenePath} not found. Create the scene first.");
            return;
        }

        if (!EditorUtility.DisplayDialog("Rebuild MainMenu",
                "This will replace the Canvas in MainMenu.unity with the redesigned layout.\n" +
                "GameManager and EventSystem are preserved.\n\nProceed?",
                "Rebuild", "Cancel"))
            return;

        // Open the scene additively so we can inspect existing objects, then rebuild.
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // ── Preserve GameManager and EventSystem, destroy old Canvas ──────────
        GameManager existingGM  = Object.FindFirstObjectByType<GameManager>();
        EventSystem existingES  = Object.FindFirstObjectByType<EventSystem>();

        // Destroy every Canvas root (old prototype canvas + any stale MainMenu GO).
        var allRoots = scene.GetRootGameObjects();
        foreach (var go in allRoots)
        {
            if (go == existingGM?.gameObject) continue;
            if (go == existingES?.gameObject) continue;
            if (go.GetComponent<Camera>()   != null) continue;
            if (go.GetComponent<Light>()    != null) continue;
            Object.DestroyImmediate(go);
        }

        // ── Ensure the InputSystem EventSystem is correct ─────────────────────
        if (existingES != null && existingES.GetComponent<InputSystemUIInputModule>() == null)
        {
            // Replace the old StandaloneInputModule with InputSystemUIInputModule.
            var oldModule = existingES.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            if (oldModule != null) Object.DestroyImmediate(oldModule);
            existingES.gameObject.AddComponent<InputSystemUIInputModule>();
        }
        else if (existingES == null)
        {
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<InputSystemUIInputModule>();
        }

        // Wire InputActions to the InputSystemUIInputModule.
        var isUIModule = Object.FindFirstObjectByType<InputSystemUIInputModule>();
        if (isUIModule != null)
        {
            var inputAsset = AssetDatabase.FindAssets("t:InputActionAsset")
                .Select(g => AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(
                                 AssetDatabase.GUIDToAssetPath(g)))
                .FirstOrDefault(a => a != null && a.name == "InputSystem_Actions");
            if (inputAsset != null)
            {
                var esSO = new SerializedObject(isUIModule);
                esSO.FindProperty("m_ActionsAsset").objectReferenceValue = inputAsset;
                esSO.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // ── Build new canvas hierarchy ────────────────────────────────────────
        var mmCanvas = MakeCanvas("MainMenuCanvas", 0);
        var mmRoot   = mmCanvas.transform;

        // Backdrop (right half, mid-navy — replace Image.sprite with team art).
        var backdropRT = PanelFull("Backdrop", mmRoot, MenuStyle.BackgroundMid, false);
        backdropRT.gameObject.name = "Backdrop";

        // Left column strip.
        var leftColGO = new GameObject("LeftColumn");
        leftColGO.transform.SetParent(mmRoot, false);
        var leftColRT = leftColGO.AddComponent<RectTransform>();
        leftColRT.anchorMin        = new Vector2(0, 0);
        leftColRT.anchorMax        = new Vector2(0, 1);
        leftColRT.offsetMin        = Vector2.zero;
        leftColRT.offsetMax        = new Vector2(LeftColW, 0);
        var leftColImg = leftColGO.AddComponent<Image>();
        leftColImg.color         = MenuStyle.BackgroundDeep;
        leftColImg.raycastTarget = false;

        // Art placeholder hint (right half).
        {
            var hint = new GameObject("ArtPlaceholderHint");
            hint.transform.SetParent(mmRoot, false);
            var hRT = hint.AddComponent<RectTransform>();
            hRT.anchorMin        = new Vector2(MenuStyle.LeftColumnWidthFraction, 0.1f);
            hRT.anchorMax        = new Vector2(1f, 0.9f);
            hRT.offsetMin = hRT.offsetMax = Vector2.zero;
            var hTxt = hint.AddComponent<TextMeshProUGUI>();
            hTxt.text          = "[ ASSIGN TEAM ART\nBackdrop → Image.sprite ]";
            hTxt.fontSize      = 20;
            hTxt.color         = MenuStyle.SubtitleGrey;
            hTxt.alignment     = TextAlignmentOptions.Center | TextAlignmentOptions.Midline;
            hTxt.raycastTarget = false;
        }

        // MainPanel — transparent container over left column, holds title + buttons.
        var mainPanelGO = new GameObject("MainPanel");
        mainPanelGO.transform.SetParent(mmRoot, false);
        var mainPanelRT = mainPanelGO.AddComponent<RectTransform>();
        mainPanelRT.anchorMin = new Vector2(0, 0);
        mainPanelRT.anchorMax = new Vector2(0, 1);
        mainPanelRT.offsetMin = Vector2.zero;
        mainPanelRT.offsetMax = new Vector2(LeftColW, 0);

        // Title.
        {
            var titleGO = new GameObject("TitleLabel");
            titleGO.transform.SetParent(mainPanelGO.transform, false);
            var tRT = titleGO.AddComponent<RectTransform>();
            tRT.anchorMin = new Vector2(0, 1); tRT.anchorMax = new Vector2(1, 1);
            tRT.pivot     = new Vector2(0, 1);
            tRT.sizeDelta = new Vector2(0, 180f);
            tRT.anchoredPosition = new Vector2(MenuStyle.LeftMargin, -88f);
            var tTxt = titleGO.AddComponent<TextMeshProUGUI>();
            tTxt.text          = "THE PHYSICS\nTHIEF";
            tTxt.fontSize      = MenuStyle.TitleSize;
            tTxt.color         = MenuStyle.TextPrimary;
            tTxt.alignment     = TextAlignmentOptions.Left | TextAlignmentOptions.Top;
            tTxt.lineSpacing   = -10f;
            tTxt.raycastTarget = false;
        }

        // Subtitle.
        {
            var subGO = new GameObject("Subtitle");
            subGO.transform.SetParent(mainPanelGO.transform, false);
            var sRT = subGO.AddComponent<RectTransform>();
            sRT.anchorMin = new Vector2(0, 1); sRT.anchorMax = new Vector2(1, 1);
            sRT.pivot     = new Vector2(0, 1);
            sRT.sizeDelta = new Vector2(0, 36f);
            sRT.anchoredPosition = new Vector2(MenuStyle.LeftMargin, -268f);
            var sTxt = subGO.AddComponent<TextMeshProUGUI>();
            sTxt.text          = "steal the laws of physics";
            sTxt.fontSize      = MenuStyle.SubtitleSize;
            sTxt.color         = MenuStyle.SubtitleGrey;
            sTxt.fontStyle     = FontStyles.Italic;
            sTxt.alignment     = TextAlignmentOptions.Left | TextAlignmentOptions.Midline;
            sTxt.raycastTarget = false;
        }

        // Button stack.
        float buttonW = LeftColW - MenuStyle.LeftMargin - 32f;
        Vector2 btnSize = new Vector2(buttonW, MenuStyle.ButtonHeight);
        float step  = MenuStyle.ButtonHeight + MenuStyle.ButtonSpacing;
        float btnY0 = 20f;

        var startBtn  = StyledButton("StartButton",   "START",   mainPanelGO.transform, btnSize,
                                     new Vector2(MenuStyle.LeftMargin, btnY0));
        var optBtn    = StyledButton("OptionsButton", "OPTIONS", mainPanelGO.transform, btnSize,
                                     new Vector2(MenuStyle.LeftMargin, btnY0 - step));
        var quitBtn   = StyledButton("QuitButton",    "QUIT",    mainPanelGO.transform, btnSize,
                                     new Vector2(MenuStyle.LeftMargin, btnY0 - step * 2f));

        // OptionsPanel (full-canvas overlay).
        var optPanel = BuildOptionsPanel("OptionsPanel", mmRoot, 700f);

        // ── MainMenuController ────────────────────────────────────────────────
        var mmc  = mmCanvas.gameObject.AddComponent<MainMenuController>();
        var mmSO = new SerializedObject(mmc);
        mmSO.FindProperty("mainPanel"    ).objectReferenceValue = mainPanelGO;
        mmSO.FindProperty("optionsPanel" ).objectReferenceValue = optPanel;
        mmSO.FindProperty("startButton"  ).objectReferenceValue = startBtn;
        mmSO.FindProperty("optionsButton").objectReferenceValue = optBtn;
        mmSO.FindProperty("quitButton"   ).objectReferenceValue = quitBtn;
        mmSO.ApplyModifiedPropertiesWithoutUndo();

        // ── Save ──────────────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);

        Debug.Log(
            "[MainMenuSetup] MainMenu.unity rebuilt with redesigned layout.\n" +
            "• Assign team art: select the 'Backdrop' GameObject → Image component → Sprite.\n" +
            "• Start button calls GameManager.StartGame() — set Level Scenes in GameManager Inspector.\n" +
            "• EventSystem uses InputSystemUIInputModule (new Input System).\n" +
            "To preview: File → Open Scene → Assets/Scenes/MainMenu.unity → Play.");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // ──── SHARED HELPERS (identical to UIFlowTestSceneSetup) ────────────────
    // ═════════════════════════════════════════════════════════════════════════

    static Canvas MakeCanvas(string name, int sortOrder)
    {
        var go = new GameObject(name);
        var c  = go.AddComponent<Canvas>();
        c.renderMode   = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = sortOrder;
        var cs = go.AddComponent<CanvasScaler>();
        cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(RefW, RefH);
        cs.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        cs.matchWidthOrHeight  = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return c;
    }

    static RectTransform PanelFull(string name, Transform parent, Color bg, bool raycast = true)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        if (bg.a > 0f)
        {
            var img = go.AddComponent<Image>();
            img.color         = bg;
            img.raycastTarget = raycast;
        }
        return rt;
    }

    static Button StyledButton(string name, string labelText, Transform parent,
                                Vector2 size, Vector2 pos)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt  = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0, 0.5f);
        rt.anchorMax        = new Vector2(0, 0.5f);
        rt.pivot            = new Vector2(0, 0.5f);
        rt.sizeDelta        = size;
        rt.anchoredPosition = pos;

        var hitImg   = go.AddComponent<Image>();
        hitImg.color = MenuStyle.Transparent;
        var btn      = go.AddComponent<Button>();
        btn.targetGraphic = hitImg;

        // Amber left-bar.
        var markerGO  = new GameObject("SelectionMarker");
        markerGO.transform.SetParent(go.transform, false);
        var markerRT  = markerGO.AddComponent<RectTransform>();
        markerRT.anchorMin = new Vector2(0, 0); markerRT.anchorMax = new Vector2(0, 1);
        markerRT.offsetMin = Vector2.zero; markerRT.offsetMax = new Vector2(MenuStyle.MarkerWidth, 0);
        var markerImg = markerGO.AddComponent<Image>(); markerImg.color = MenuStyle.Transparent;

        // Label.
        var lblGO  = new GameObject("Label");
        lblGO.transform.SetParent(go.transform, false);
        var lblRT  = lblGO.AddComponent<RectTransform>();
        lblRT.anchorMin = Vector2.zero; lblRT.anchorMax = Vector2.one;
        lblRT.offsetMin = new Vector2(MenuStyle.MarkerWidth + MenuStyle.ButtonPadLeft, 0);
        lblRT.offsetMax = Vector2.zero;
        var lbl    = lblGO.AddComponent<TextMeshProUGUI>();
        lbl.text          = labelText;
        lbl.fontSize      = MenuStyle.ButtonSize;
        lbl.color         = MenuStyle.TextPrimary;
        lbl.alignment     = TextAlignmentOptions.Left | TextAlignmentOptions.Midline;
        lbl.raycastTarget = false;

        var effect = go.AddComponent<MenuButtonEffect>();
        effect.Init(markerImg, lbl);

        return btn;
    }

    static Slider BuildSlider(string sliderName, Transform parent,
                               Vector2 trackSize, Vector2 pos, float min, float max, float val)
    {
        var go = new GameObject(sliderName);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(trackSize.x, 32f);
        rt.anchoredPosition = pos;

        var bgGO  = new GameObject("Background"); bgGO.transform.SetParent(go.transform, false);
        var bgRT  = bgGO.AddComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0, 0.5f); bgRT.anchorMax = new Vector2(1, 0.5f);
        bgRT.sizeDelta = new Vector2(0, trackSize.y);
        var bgImg = bgGO.AddComponent<Image>(); bgImg.color = new Color(1, 1, 1, 0.15f);

        var fillAreaGO = new GameObject("Fill Area"); fillAreaGO.transform.SetParent(go.transform, false);
        var fillAreaRT = fillAreaGO.AddComponent<RectTransform>();
        fillAreaRT.anchorMin = new Vector2(0, 0.5f); fillAreaRT.anchorMax = new Vector2(1, 0.5f);
        fillAreaRT.sizeDelta = new Vector2(-10f, trackSize.y);

        var fillGO  = new GameObject("Fill"); fillGO.transform.SetParent(fillAreaGO.transform, false);
        var fillRT  = fillGO.AddComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one; fillRT.sizeDelta = new Vector2(10f, 0);
        var fillImg = fillGO.AddComponent<Image>(); fillImg.color = MenuStyle.AccentAmber;

        var hsaGO = new GameObject("Handle Slide Area"); hsaGO.transform.SetParent(go.transform, false);
        var hsaRT = hsaGO.AddComponent<RectTransform>();
        hsaRT.anchorMin = Vector2.zero; hsaRT.anchorMax = Vector2.one;
        hsaRT.offsetMin = new Vector2(8, 0); hsaRT.offsetMax = new Vector2(-8, 0);

        var handleGO  = new GameObject("Handle"); handleGO.transform.SetParent(hsaGO.transform, false);
        var handleRT  = handleGO.AddComponent<RectTransform>();
        handleRT.anchorMin = new Vector2(0, 0.5f); handleRT.anchorMax = new Vector2(0, 0.5f);
        handleRT.sizeDelta = new Vector2(22f, 22f);
        var handleImg = handleGO.AddComponent<Image>(); handleImg.color = Color.white;

        var slider = go.AddComponent<Slider>();
        slider.fillRect   = fillRT; slider.handleRect = handleRT;
        slider.minValue   = min;    slider.maxValue   = max;
        slider.value      = val;    slider.direction  = Slider.Direction.LeftToRight;
        return slider;
    }

    static Slider OptionsRow(string rowName, Transform parent, float yCenter, float panelWidth,
                              float sliderMin, float sliderMax, float sliderDefault,
                              string labelText, out TMP_Text valueLabel)
    {
        float labelW  = 280f;
        float sliderW = panelWidth * 0.38f;
        float valueW  = 90f;
        float totalContent = labelW + 20f + sliderW + 16f + valueW;
        float startX = -(totalContent * 0.5f);

        var lblGO = new GameObject(rowName + "_Label"); lblGO.transform.SetParent(parent, false);
        var lblRT = lblGO.AddComponent<RectTransform>();
        lblRT.anchorMin = new Vector2(0.5f, 0.5f); lblRT.anchorMax = new Vector2(0.5f, 0.5f);
        lblRT.sizeDelta = new Vector2(labelW, 44f);
        lblRT.anchoredPosition = new Vector2(startX + labelW * 0.5f, yCenter);
        var lbl = lblGO.AddComponent<TextMeshProUGUI>();
        lbl.text = labelText; lbl.fontSize = MenuStyle.OptionLabelSize;
        lbl.color = MenuStyle.TextPrimary;
        lbl.alignment = TextAlignmentOptions.Left | TextAlignmentOptions.Midline;
        lbl.raycastTarget = false;

        float sliderX = startX + labelW + 20f + sliderW * 0.5f;
        var slider = BuildSlider(rowName + "_Slider", parent,
                                  new Vector2(sliderW, 10f), new Vector2(sliderX, yCenter),
                                  sliderMin, sliderMax, sliderDefault);

        float valX = startX + labelW + 20f + sliderW + 16f + valueW * 0.5f;
        var valGO  = new GameObject(rowName + "_Value"); valGO.transform.SetParent(parent, false);
        var valRT  = valGO.AddComponent<RectTransform>();
        valRT.anchorMin = new Vector2(0.5f, 0.5f); valRT.anchorMax = new Vector2(0.5f, 0.5f);
        valRT.sizeDelta = new Vector2(valueW, 44f); valRT.anchoredPosition = new Vector2(valX, yCenter);
        var valTxt = valGO.AddComponent<TextMeshProUGUI>();
        valTxt.fontSize = MenuStyle.ValueLabelSize; valTxt.color = MenuStyle.AccentAmber;
        valTxt.alignment = TextAlignmentOptions.Right | TextAlignmentOptions.Midline;
        valTxt.raycastTarget = false;

        valueLabel = valTxt;
        return slider;
    }

    static void Separator(string name, Transform parent, float yPos, float width)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(width, 1f); rt.anchoredPosition = new Vector2(0, yPos);
        var img = go.AddComponent<Image>(); img.color = MenuStyle.SeparatorLine; img.raycastTarget = false;
    }

    static OptionsPanel BuildOptionsPanel(string goName, Transform parent, float cardWidth)
    {
        var panelGO = new GameObject(goName); panelGO.transform.SetParent(parent, false);
        var rt = panelGO.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var card = panelGO.AddComponent<Image>(); card.color = MenuStyle.PanelCard;
        var op   = panelGO.AddComponent<OptionsPanel>();

        // "OPTIONS" heading.
        var headGO = new GameObject("OptionsHeading"); headGO.transform.SetParent(panelGO.transform, false);
        var headRT = headGO.AddComponent<RectTransform>();
        headRT.anchorMin = new Vector2(0.5f, 0.5f); headRT.anchorMax = new Vector2(0.5f, 0.5f);
        headRT.sizeDelta = new Vector2(cardWidth - 80f, 60f); headRT.anchoredPosition = new Vector2(0, 148f);
        var headTxt = headGO.AddComponent<TextMeshProUGUI>();
        headTxt.text = "OPTIONS"; headTxt.fontSize = MenuStyle.PauseTitleSize;
        headTxt.color = MenuStyle.AccentAmber;
        headTxt.alignment = TextAlignmentOptions.Center; headTxt.raycastTarget = false;

        Separator("Sep0", panelGO.transform, 108f, cardWidth * 0.75f);
        var volSlider  = OptionsRow("Volume",      panelGO.transform, 28f,  cardWidth, UISettings.MinVolume,      UISettings.MaxVolume,      UISettings.DefaultVolume,      "MASTER VOLUME",     out var volLabel);
        Separator("Sep1", panelGO.transform, -20f, cardWidth * 0.75f);
        var sensSlider = OptionsRow("Sensitivity", panelGO.transform, -68f, cardWidth, UISettings.MinSensitivity, UISettings.MaxSensitivity, UISettings.DefaultSensitivity, "MOUSE SENSITIVITY",  out var sensLabel);
        Separator("Sep2", panelGO.transform, -116f, cardWidth * 0.75f);

        // Back button.
        var backRoot = new GameObject("BackButton"); backRoot.transform.SetParent(panelGO.transform, false);
        var backRT   = backRoot.AddComponent<RectTransform>();
        backRT.anchorMin = new Vector2(0.5f, 0.5f); backRT.anchorMax = new Vector2(0.5f, 0.5f);
        backRT.sizeDelta = new Vector2(220f, MenuStyle.ButtonHeight); backRT.anchoredPosition = new Vector2(0, -172f);
        var backHit  = backRoot.AddComponent<Image>(); backHit.color = MenuStyle.Transparent;
        var backBtn  = backRoot.AddComponent<Button>(); backBtn.targetGraphic = backHit;

        var backMarkerGO = new GameObject("SelectionMarker"); backMarkerGO.transform.SetParent(backRoot.transform, false);
        var backMarkerRT = backMarkerGO.AddComponent<RectTransform>();
        backMarkerRT.anchorMin = new Vector2(0, 0); backMarkerRT.anchorMax = new Vector2(0, 1);
        backMarkerRT.offsetMin = Vector2.zero; backMarkerRT.offsetMax = new Vector2(MenuStyle.MarkerWidth, 0);
        var backMarkerImg = backMarkerGO.AddComponent<Image>(); backMarkerImg.color = MenuStyle.Transparent;

        var backLblGO = new GameObject("Label"); backLblGO.transform.SetParent(backRoot.transform, false);
        var backLblRT = backLblGO.AddComponent<RectTransform>();
        backLblRT.anchorMin = Vector2.zero; backLblRT.anchorMax = Vector2.one;
        backLblRT.offsetMin = new Vector2(MenuStyle.MarkerWidth + MenuStyle.ButtonPadLeft, 0);
        backLblRT.offsetMax = Vector2.zero;
        var backLbl = backLblGO.AddComponent<TextMeshProUGUI>();
        backLbl.text = "BACK"; backLbl.fontSize = MenuStyle.ButtonSize;
        backLbl.color = MenuStyle.TextPrimary;
        backLbl.alignment = TextAlignmentOptions.Left | TextAlignmentOptions.Midline;
        backLbl.raycastTarget = false;

        var backEffect = backRoot.AddComponent<MenuButtonEffect>(); backEffect.Init(backMarkerImg, backLbl);

        var so = new SerializedObject(op);
        so.FindProperty("volumeSlider"     ).objectReferenceValue = volSlider;
        so.FindProperty("volumeValueLabel" ).objectReferenceValue = volLabel;
        so.FindProperty("sensitivitySlider").objectReferenceValue = sensSlider;
        so.FindProperty("sensitivityLabel" ).objectReferenceValue = sensLabel;
        so.FindProperty("backButton"       ).objectReferenceValue = backBtn;
        so.ApplyModifiedPropertiesWithoutUndo();

        panelGO.SetActive(false);
        return op;
    }
}
#endif
