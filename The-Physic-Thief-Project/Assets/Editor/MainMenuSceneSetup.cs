#if UNITY_EDITOR
// ──────────────────────────────────────────────────────────────────────────────
// MainMenuSceneSetup.cs  (Editor-only)
//
// Run via:  Tools → UI Flow → Rebuild MainMenu Scene
//
// Builds Assets/Scenes/MainMenu.unity as a full-screen forest composition
// matching the Eternal Garden reference image:
//
//   BACKGROUND
//   ──────────
//   • Full-canvas Backdrop Image (assign real forest art here — Sprite slot).
//   • ForestBackdrop component on Backdrop generates a layered procedural forest
//     approximation using solid-colour Images (no external textures).
//     Replace it by assigning a Sprite to Backdrop's Image component.
//
//   TITLE  (centred, upper third)
//   ──────
//   • "THE PHYSICS" at ~44 pt, centred, white — smaller top line
//   • "THIEF" at ~90 pt, centred, white — larger bottom line, 1.4× scale
//   • Tight negative line spacing.  Spans roughly 19–36% screen height.
//   • TMP outline material gives a soft green glow (OutlineColor set to forest teal).
//
//   MENU  (centred)
//   ────
//   • START at ≈49% screen height, OPTIONS at ≈55%, QUIT at ≈61%.
//   • Spacing ≈6% screen height (64 px at 1080p reference).
//   • White uppercase text, expanded letter spacing.
//   • START receives initial keyboard focus.
//   • EternalMenuButtonEffect drives the white rounded-rect focus outline:
//       focused/hovered → white outline appears   (outline sprite is generated at runtime)
//       normal          → plain white text only
//   • No amber bar, no left column, no navy panel, no tagline.
//
//   OPTIONS PANEL  (unchanged — dark card, full canvas overlay)
//
// All existing button handlers and settings wiring are preserved.
// GameManager + EventSystem are preserved from the original scene.
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

    // ── Layout constants matching the Eternal Garden reference ───────────────
    // Vertical anchors (0 = bottom, 1 = top at 1080p)
    private const float TitleTopAnchor   = 0.81f;  // top of "THE PHYSICS" ≈ 19% from top
    private const float TitleBotAnchor   = 0.64f;  // bottom of "THIEF"    ≈ 36% from top
    private const float StartYAnchor     = 0.51f;  // START  ≈ 49% from top
    private const float OptionsYAnchor   = 0.45f;  // OPTIONS≈ 55% from top
    private const float QuitYAnchor      = 0.39f;  // QUIT   ≈ 61% from top

    // Button hit-area size at reference resolution
    private const float BtnW  = 400f;
    private const float BtnH  = 60f;

    // ── Font sizes ────────────────────────────────────────────────────────────
    private const int TopLinePt    = 54;   // "THE PHYSICS" — smaller
    private const int BotLinePt    = 90;   // "THIEF" — ~1.4× larger
    private const int MenuItemPt   = 38;   // START / OPTIONS / QUIT
    private const float LetterSpacing = 4f; // expanded letter spacing for menu items

    // ── Colour: forest teal used for title glow ────────────────────────────
    private static readonly Color TitleGlow = new Color(0.102f, 0.361f, 0.322f, 1f);
    private static readonly Color White     = Color.white;
    private static readonly Color Clear     = new Color(0, 0, 0, 0);

    // ─────────────────────────────────────────────────────────────────────────
    [MenuItem("Tools/UI Flow/Rebuild MainMenu Scene")]
    public static void RebuildMainMenu()
    {
        if (!System.IO.File.Exists(ScenePath))
        {
            Debug.LogError($"[MainMenuSetup] {ScenePath} not found.");
            return;
        }

        if (!EditorUtility.DisplayDialog("Rebuild MainMenu",
                "Replace the Canvas in MainMenu.unity with the Eternal Garden layout?\n" +
                "GameManager and EventSystem are preserved.",
                "Rebuild", "Cancel"))
            return;

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // ── Preserve GameManager, EventSystem, Camera ─────────────────────────
        GameManager existingGM = Object.FindFirstObjectByType<GameManager>();
        EventSystem existingES = Object.FindFirstObjectByType<EventSystem>();

        foreach (var go in scene.GetRootGameObjects())
        {
            if (go == existingGM?.gameObject) continue;
            if (go == existingES?.gameObject) continue;
            if (go.GetComponent<Camera>() != null) continue;
            if (go.GetComponent<Light>()  != null) continue;
            Object.DestroyImmediate(go);
        }

        // ── Ensure correct EventSystem ────────────────────────────────────────
        EnsureInputSystemES(existingES);

        // ══════════════════════════════════════════════════════════════════════
        // BUILD CANVAS
        // ══════════════════════════════════════════════════════════════════════
        var mmCanvas = MakeCanvas("MainMenuCanvas", 0);
        var mmRoot   = mmCanvas.transform;

        // ── BACKGROUND ───────────────────────────────────────────────────────
        // Full-canvas Image — assign real forest art here (Sprite → Image.sprite).
        var backdropGO = new GameObject("Backdrop");
        backdropGO.transform.SetParent(mmRoot, false);
        var backdropRT  = backdropGO.AddComponent<RectTransform>();
        backdropRT.anchorMin = Vector2.zero; backdropRT.anchorMax = Vector2.one;
        backdropRT.offsetMin = backdropRT.offsetMax = Vector2.zero;
        var backdropImg = backdropGO.AddComponent<Image>();
        backdropImg.color         = Color.white;    // white so a sprite shows at full colour
        backdropImg.raycastTarget = false;
        // ForestBackdrop paints a procedural forest as child Images until real art is added.
                // Load background sprite if already imported; otherwise ForestBackdrop is the placeholder.
        var bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/MainMenuBackground.jpg");
        if (bgSprite != null)
        {
            backdropImg.sprite  = bgSprite;
            backdropImg.type    = Image.Type.Simple;
            backdropImg.preserveAspect = false;
        }
        else
        {
            backdropGO.AddComponent<ForestBackdrop>();
            Debug.LogWarning("[MainMenuSetup] MainMenuBackground.jpg not found at Assets/Textures/UI/. " +
                             "Import a cleaned copy of the original JPG there, then run this tool again.");
        }

        // ── MAIN PANEL (title + buttons) ─────────────────────────────────────
        var mainPanelGO = new GameObject("MainPanel");
        mainPanelGO.transform.SetParent(mmRoot, false);
        var mainPanelRT = mainPanelGO.AddComponent<RectTransform>();
        mainPanelRT.anchorMin = Vector2.zero; mainPanelRT.anchorMax = Vector2.one;
        mainPanelRT.offsetMin = mainPanelRT.offsetMax = Vector2.zero;

        // ── TITLE ─────────────────────────────────────────────────────────────
        // "THE PHYSICS" — smaller top line
        {
            var go = new GameObject("TitleTop");
            go.transform.SetParent(mainPanelGO.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, TitleBotAnchor + (TitleTopAnchor - TitleBotAnchor) * 0.5f);
            rt.anchorMax        = new Vector2(0.5f, TitleTopAnchor);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.sizeDelta        = new Vector2(900f, 90f);
            rt.anchoredPosition = Vector2.zero;
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text              = "THE PHYSICS";
            t.fontSize          = TopLinePt;
            t.fontStyle         = FontStyles.Bold;
            t.color             = White;
            t.alignment         = TextAlignmentOptions.Center | TextAlignmentOptions.Midline;
            t.characterSpacing  = 2f;
            t.raycastTarget     = false;
            ApplyTitleMaterial(t);
        }

        // "THIEF" — larger bottom line (~1.4× the top line)
        {
            var go = new GameObject("TitleBottom");
            go.transform.SetParent(mainPanelGO.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, TitleBotAnchor);
            rt.anchorMax        = new Vector2(0.5f, TitleBotAnchor + (TitleTopAnchor - TitleBotAnchor) * 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.sizeDelta        = new Vector2(900f, 120f);
            rt.anchoredPosition = Vector2.zero;
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text              = "THIEF";
            t.fontSize          = BotLinePt;
            t.fontStyle         = FontStyles.Bold;
            t.color             = White;
            t.alignment         = TextAlignmentOptions.Center | TextAlignmentOptions.Midline;
            t.characterSpacing  = 2f;
            t.raycastTarget     = false;
            ApplyTitleMaterial(t);
        }

        // ── MENU BUTTONS ──────────────────────────────────────────────────────
        var startBtn  = EternalButton("StartButton",   "START",   mainPanelGO.transform, StartYAnchor);
        var optBtn    = EternalButton("OptionsButton", "OPTIONS", mainPanelGO.transform, OptionsYAnchor);
        var quitBtn   = EternalButton("QuitButton",    "QUIT",    mainPanelGO.transform, QuitYAnchor);

        // ── OPTIONS PANEL (unchanged) ─────────────────────────────────────────
        var optPanel = BuildOptionsPanel("OptionsPanel", mmRoot, 700f);

        // ── MAIN MENU CONTROLLER ──────────────────────────────────────────────
        var mmc  = mmCanvas.gameObject.AddComponent<MainMenuController>();
        var so   = new SerializedObject(mmc);
        so.FindProperty("mainPanel"    ).objectReferenceValue = mainPanelGO;
        so.FindProperty("optionsPanel" ).objectReferenceValue = optPanel;
        so.FindProperty("startButton"  ).objectReferenceValue = startBtn;
        so.FindProperty("optionsButton").objectReferenceValue = optBtn;
        so.FindProperty("quitButton"   ).objectReferenceValue = quitBtn;
        so.ApplyModifiedPropertiesWithoutUndo();

        // ── SAVE ──────────────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);

        Debug.Log(
            "[MainMenuSetup] Eternal Garden layout saved to MainMenu.unity.\n\n" +
            "BACKDROP: select 'Backdrop' GameObject → Image → Source Image.\n" +
            "  Assign your forest art Sprite there, then remove ForestBackdrop component.\n\n" +
            "TITLE GLOW: uses LiberationSans SDF Outline material with TitleGlow teal.\n" +
            "  For a heavier glow, assign a custom TMP material with a bolder outline.\n\n" +
            "Press Play → START/OPTIONS/QUIT centred over forest, white focus outline on START.");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // HELPERS
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a centred button at <paramref name="yAnchor"/> (0=bottom, 1=top).
    /// The button uses EternalMenuButtonEffect: focused = white rounded-rect outline.
    /// </summary>
    static Button EternalButton(string goName, string labelText, Transform parent, float yAnchor)
    {
        var go = new GameObject(goName);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, yAnchor);
        rt.anchorMax        = new Vector2(0.5f, yAnchor);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(BtnW, BtnH);
        rt.anchoredPosition = Vector2.zero;

        // Transparent hit area.
        var hitImg = go.AddComponent<Image>();
        hitImg.color = Clear;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = hitImg;

        // Focus outline Image (sized to match button, procedural sprite applied by effect).
        var outlineGO = new GameObject("FocusOutline");
        outlineGO.transform.SetParent(go.transform, false);
        var oRT = outlineGO.AddComponent<RectTransform>();
        oRT.anchorMin = Vector2.zero; oRT.anchorMax = Vector2.one;
        oRT.offsetMin = new Vector2(-6, -6); oRT.offsetMax = new Vector2(6, 6); // slightly larger than label
        var oImg = outlineGO.AddComponent<Image>();
        oImg.color         = Clear;        // hidden by default; EternalMenuButtonEffect shows it on focus
        oImg.raycastTarget = false;
        oImg.type          = Image.Type.Sliced;
        oImg.pixelsPerUnitMultiplier = 1f;

        // Label.
        var lblGO = new GameObject("Label");
        lblGO.transform.SetParent(go.transform, false);
        var lRT = lblGO.AddComponent<RectTransform>();
        lRT.anchorMin = Vector2.zero; lRT.anchorMax = Vector2.one;
        lRT.offsetMin = lRT.offsetMax = Vector2.zero;
        var lbl = lblGO.AddComponent<TextMeshProUGUI>();
        lbl.text             = labelText;
        lbl.fontSize         = MenuItemPt;
        lbl.fontStyle        = FontStyles.Bold;
        lbl.color            = White;
        lbl.alignment        = TextAlignmentOptions.Center | TextAlignmentOptions.Midline;
        lbl.characterSpacing = LetterSpacing;
        lbl.raycastTarget    = false;

        // Wire EternalMenuButtonEffect.
        var effect = go.AddComponent<EternalMenuButtonEffect>();
        effect.Init(oImg, lbl);

        return btn;
    }

    /// <summary>
    /// Applies a soft teal outline to the title text to approximate the Eternal Garden glow.
    /// Uses the LiberationSans SDF Outline material that ships with the project.
    /// </summary>
    static void ApplyTitleMaterial(TextMeshProUGUI t)
    {
        // Look for the outline material that ships with TMP.
        var mat = AssetDatabase.FindAssets("LiberationSans SDF - Outline t:Material")
            .Select(g => AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g)))
            .FirstOrDefault(m => m != null);

        if (mat != null)
        {
            // Clone the material so we can tint the outline without affecting other users.
            var clone = new Material(mat);
            if (clone.HasProperty("_OutlineColor"))
                clone.SetColor("_OutlineColor", TitleGlow);
            if (clone.HasProperty("_OutlineWidth"))
                clone.SetFloat("_OutlineWidth", 0.08f);
            t.fontSharedMaterial = clone;
        }
        // If no outline material is found, the text renders in plain white — still correct.
    }

    static void EnsureInputSystemES(EventSystem existingES)
    {
        if (existingES != null && existingES.GetComponent<InputSystemUIInputModule>() == null)
        {
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

        var isUIModule = Object.FindFirstObjectByType<InputSystemUIInputModule>();
        if (isUIModule == null) return;

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

    // ── Options panel (unchanged from previous version) ───────────────────────
    static Slider BuildSlider(string sliderName, Transform parent,
                               Vector2 trackSize, Vector2 pos, float min, float max, float val)
    {
        var go = new GameObject(sliderName);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(trackSize.x, 32f); rt.anchoredPosition = pos;

        var bgGO  = new GameObject("Background"); bgGO.transform.SetParent(go.transform, false);
        var bgRT  = bgGO.AddComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0, 0.5f); bgRT.anchorMax = new Vector2(1, 0.5f);
        bgRT.sizeDelta = new Vector2(0, trackSize.y);
        bgGO.AddComponent<Image>().color = new Color(1, 1, 1, 0.15f);

        var fillAreaGO = new GameObject("Fill Area"); fillAreaGO.transform.SetParent(go.transform, false);
        var fillAreaRT = fillAreaGO.AddComponent<RectTransform>();
        fillAreaRT.anchorMin = new Vector2(0, 0.5f); fillAreaRT.anchorMax = new Vector2(1, 0.5f);
        fillAreaRT.sizeDelta = new Vector2(-10f, trackSize.y);

        var fillGO  = new GameObject("Fill"); fillGO.transform.SetParent(fillAreaGO.transform, false);
        var fillRT  = fillGO.AddComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one; fillRT.sizeDelta = new Vector2(10f, 0);
        fillGO.AddComponent<Image>().color = MenuStyle.AccentAmber;

        var hsaGO = new GameObject("Handle Slide Area"); hsaGO.transform.SetParent(go.transform, false);
        var hsaRT = hsaGO.AddComponent<RectTransform>();
        hsaRT.anchorMin = Vector2.zero; hsaRT.anchorMax = Vector2.one;
        hsaRT.offsetMin = new Vector2(8, 0); hsaRT.offsetMax = new Vector2(-8, 0);

        var handleGO  = new GameObject("Handle"); handleGO.transform.SetParent(hsaGO.transform, false);
        var handleRT  = handleGO.AddComponent<RectTransform>();
        handleRT.anchorMin = new Vector2(0, 0.5f); handleRT.anchorMax = new Vector2(0, 0.5f);
        handleRT.sizeDelta = new Vector2(22f, 22f);
        handleGO.AddComponent<Image>().color = Color.white;

        var slider = go.AddComponent<Slider>();
        slider.fillRect = fillRT; slider.handleRect = handleRT;
        slider.minValue = min; slider.maxValue = max; slider.value = val;
        slider.direction = Slider.Direction.LeftToRight;
        return slider;
    }

    static Slider OptionsRow(string rowName, Transform parent, float yCenter, float panelWidth,
                              float sliderMin, float sliderMax, float sliderDefault,
                              string labelText, out TMP_Text valueLabel)
    {
        float labelW = 280f; float sliderW = panelWidth * 0.38f; float valueW = 90f;
        float totalContent = labelW + 20f + sliderW + 16f + valueW;
        float startX = -(totalContent * 0.5f);

        var lblGO = new GameObject(rowName + "_Label"); lblGO.transform.SetParent(parent, false);
        var lblRT = lblGO.AddComponent<RectTransform>();
        lblRT.anchorMin = new Vector2(0.5f, 0.5f); lblRT.anchorMax = new Vector2(0.5f, 0.5f);
        lblRT.sizeDelta = new Vector2(labelW, 44f); lblRT.anchoredPosition = new Vector2(startX + labelW * 0.5f, yCenter);
        var lbl = lblGO.AddComponent<TextMeshProUGUI>();
        lbl.text = labelText; lbl.fontSize = MenuStyle.OptionLabelSize;
        lbl.color = MenuStyle.TextPrimary;
        lbl.alignment = TextAlignmentOptions.Left | TextAlignmentOptions.Midline;
        lbl.raycastTarget = false;

        var slider = BuildSlider(rowName + "_Slider", parent,
                                  new Vector2(sliderW, 10f), new Vector2(startX + labelW + 20f + sliderW * 0.5f, yCenter),
                                  sliderMin, sliderMax, sliderDefault);

        var valGO = new GameObject(rowName + "_Value"); valGO.transform.SetParent(parent, false);
        var valRT = valGO.AddComponent<RectTransform>();
        valRT.anchorMin = new Vector2(0.5f, 0.5f); valRT.anchorMax = new Vector2(0.5f, 0.5f);
        valRT.sizeDelta = new Vector2(valueW, 44f);
        valRT.anchoredPosition = new Vector2(startX + labelW + 20f + sliderW + 16f + valueW * 0.5f, yCenter);
        var valTxt = valGO.AddComponent<TextMeshProUGUI>();
        valTxt.fontSize = MenuStyle.ValueLabelSize; valTxt.color = MenuStyle.AccentAmber;
        valTxt.alignment = TextAlignmentOptions.Right | TextAlignmentOptions.Midline;
        valTxt.raycastTarget = false;

        valueLabel = valTxt; return slider;
    }

    static void Separator(string name, Transform parent, float yPos, float width)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(width, 1f); rt.anchoredPosition = new Vector2(0, yPos);
        go.AddComponent<Image>().color = MenuStyle.SeparatorLine;
    }

    static OptionsPanel BuildOptionsPanel(string goName, Transform parent, float cardWidth)
    {
        var panelGO = new GameObject(goName); panelGO.transform.SetParent(parent, false);
        var rt = panelGO.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        panelGO.AddComponent<Image>().color = MenuStyle.PanelCard;
        var op = panelGO.AddComponent<OptionsPanel>();

        var headGO = new GameObject("OptionsHeading"); headGO.transform.SetParent(panelGO.transform, false);
        var headRT = headGO.AddComponent<RectTransform>();
        headRT.anchorMin = new Vector2(0.5f, 0.5f); headRT.anchorMax = new Vector2(0.5f, 0.5f);
        headRT.sizeDelta = new Vector2(cardWidth - 80f, 60f); headRT.anchoredPosition = new Vector2(0, 148f);
        var headTxt = headGO.AddComponent<TextMeshProUGUI>();
        headTxt.text = "OPTIONS"; headTxt.fontSize = MenuStyle.PauseTitleSize;
        headTxt.color = MenuStyle.AccentAmber;
        headTxt.alignment = TextAlignmentOptions.Center; headTxt.raycastTarget = false;

        Separator("Sep0", panelGO.transform, 108f, cardWidth * 0.75f);
        var volSlider  = OptionsRow("Volume",      panelGO.transform, 28f,  cardWidth, UISettings.MinVolume,      UISettings.MaxVolume,      UISettings.DefaultVolume,      "MASTER VOLUME",    out var volLabel);
        Separator("Sep1", panelGO.transform, -20f, cardWidth * 0.75f);
        var sensSlider = OptionsRow("Sensitivity", panelGO.transform, -68f, cardWidth, UISettings.MinSensitivity, UISettings.MaxSensitivity, UISettings.DefaultSensitivity, "MOUSE SENSITIVITY", out var sensLabel);
        Separator("Sep2", panelGO.transform, -116f, cardWidth * 0.75f);

        // Back button — reuse EternalButton style.
        var backRoot = new GameObject("BackButton"); backRoot.transform.SetParent(panelGO.transform, false);
        var backRT   = backRoot.AddComponent<RectTransform>();
        backRT.anchorMin = new Vector2(0.5f, 0.5f); backRT.anchorMax = new Vector2(0.5f, 0.5f);
        backRT.sizeDelta = new Vector2(220f, MenuStyle.ButtonHeight); backRT.anchoredPosition = new Vector2(0, -172f);
        var backHit  = backRoot.AddComponent<Image>(); backHit.color = Clear;
        var backBtn  = backRoot.AddComponent<Button>(); backBtn.targetGraphic = backHit;

        var backOutGO = new GameObject("FocusOutline"); backOutGO.transform.SetParent(backRoot.transform, false);
        var boRT = backOutGO.AddComponent<RectTransform>();
        boRT.anchorMin = Vector2.zero; boRT.anchorMax = Vector2.one;
        boRT.offsetMin = new Vector2(-6,-6); boRT.offsetMax = new Vector2(6,6);
        var boImg = backOutGO.AddComponent<Image>(); boImg.color = Clear; boImg.raycastTarget = false; boImg.type = Image.Type.Sliced;

        var backLblGO = new GameObject("Label"); backLblGO.transform.SetParent(backRoot.transform, false);
        var backLblRT = backLblGO.AddComponent<RectTransform>();
        backLblRT.anchorMin = Vector2.zero; backLblRT.anchorMax = Vector2.one;
        backLblRT.offsetMin = backLblRT.offsetMax = Vector2.zero;
        var backLbl = backLblGO.AddComponent<TextMeshProUGUI>();
        backLbl.text = "BACK"; backLbl.fontSize = MenuStyle.ButtonSize;
        backLbl.color = MenuStyle.TextPrimary;
        backLbl.alignment = TextAlignmentOptions.Center | TextAlignmentOptions.Midline;
        backLbl.characterSpacing = LetterSpacing; backLbl.raycastTarget = false;
        var backEffect = backRoot.AddComponent<EternalMenuButtonEffect>(); backEffect.Init(boImg, backLbl);

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
