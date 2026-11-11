#if UNITY_EDITOR
// ──────────────────────────────────────────────────────────────────────────────
// UIFlowTestSceneSetup.cs  (Editor-only, excluded from builds)
//
// Run via:  Tools → UI Flow → Create UIFlowTest Scene
//
// REDESIGN (Nov 2026)
// ───────────────────
// Main Menu   — left-column layout (Dorfromantik/From Dust style)
//               • Full-bleed two-tone backdrop: deep navy left, mid-navy right
//               • Left column: "THE PHYSICS THIEF" title + subtitle, then
//                 Start / Options / Quit stacked with an amber left-bar marker
//               • Right side: backdrop image placeholder (swap for real art)
//               • 5 px amber bar appears on hover/focus; text goes amber on select
//
// Pause Menu  — compact dark card centered over dimmed-but-visible gameplay
//               • 72 % opacity scrim, then a small card (640 × 400 at ref)
//               • "PAUSED" heading, Resume + Options buttons, same amber style
//               • No full-screen blackout — game is visible behind
//
// Options     — shared panel used from both Main Menu and Pause
//               • Two rows: Master Volume, Mouse Sensitivity
//               • Label left / slider center / value right
//               • Amber slider handle, separator line between rows
//               • "OPTIONS" heading, Back button at bottom
//
// All logic (GameManager, Death, Ending screens, TestHarness) is unchanged.
// Death / Ending canvases are rebuilt at the end with the same code as before.
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

    // ── Reference resolution ─────────────────────────────────────────────────
    private const float RefW = 1920f;
    private const float RefH = 1080f;

    // ── Left column width in pixels at reference resolution ──────────────────
    private static float LeftColW => RefW * MenuStyle.LeftColumnWidthFraction; // ≈ 729 px

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
        Camera cam = Object.FindFirstObjectByType<Camera>();
        if (cam == null)
        {
            var camGO = new GameObject("Main Camera");
            cam = camGO.AddComponent<Camera>();
            camGO.tag = "MainCamera";
            camGO.AddComponent<AudioListener>();
        }
        cam.gameObject.AddComponent<SensitivityDemoCamera>();

        // ── EventSystem ──────────────────────────────────────────────────────
        foreach (var es in Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
            Object.DestroyImmediate(es.gameObject);

        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        var isUIModule = esGO.AddComponent<InputSystemUIInputModule>();

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
        else
        {
            Debug.LogWarning("[UIFlowTestSetup] InputSystem_Actions asset not found. " +
                             "Assign it manually to InputSystemUIInputModule.");
        }

        // ── GameManager ───────────────────────────────────────────────────────
        var gmGO = new GameObject("GameManager");
        var gm   = gmGO.AddComponent<GameManager>();
        var gmSO = new SerializedObject(gm);
        {
            var p = gmSO.FindProperty("gameState");
            if (p != null) p.enumValueIndex = (int)GameState.MainMenu;
        }
        {
            var p = gmSO.FindProperty("persistAcrossScenes");
            if (p != null) p.boolValue = true;
        }
        {
            var p = gmSO.FindProperty("mainMenuScene");
            if (p != null) p.stringValue = "UIFlowTest";
        }
        {
            var p = gmSO.FindProperty("levelScenes");
            if (p != null) { p.arraySize = 1; p.GetArrayElementAtIndex(0).stringValue = "UIFlowTest"; }
        }
        gmSO.ApplyModifiedPropertiesWithoutUndo();

        // ── AudioManager ──────────────────────────────────────────────────────
        var amGO = new GameObject("AudioManager");
        amGO.AddComponent<AudioManager>();

        // ═════════════════════════════════════════════════════════════════════
        // ──── LOCAL HELPERS ────────────────────────────────────────────────
        // ═════════════════════════════════════════════════════════════════════

        // Creates a root Canvas (ScreenSpaceOverlay, ScaleWithScreenSize 1920×1080).
        Canvas MakeCanvas(string name, int sortOrder)
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

        // Full-stretch child panel.  Adds an Image only when bg.a > 0.
        RectTransform Panel(string name, Transform parent, Color bg, bool raycast = true)
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

        // Child panel with explicit pixel size + anchored position.
        RectTransform Box(string name, Transform parent, Vector2 size, Vector2 pos, Color bg)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.sizeDelta        = size;
            rt.anchoredPosition = pos;
            if (bg.a > 0f)
            {
                var img = go.AddComponent<Image>();
                img.color = bg;
            }
            return rt;
        }

        // TMP label — pivot / anchor centred, explicit size.
        TMP_Text Txt(string name, string text, Transform parent, int fontSize,
                     Color col, Vector2 size, Vector2 pos,
                     TextAlignmentOptions align = TextAlignmentOptions.Left)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.sizeDelta        = size;
            rt.anchoredPosition = pos;
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text           = text;
            t.fontSize       = fontSize;
            t.color          = col;
            t.alignment      = align;
            t.raycastTarget  = false;
            return t;
        }

        // ── Styled menu button (amber left-bar + text, MenuButtonEffect) ──────
        //   parent : the container that owns the button row
        //   label  : display text
        //   pos    : anchoredPosition of the row's root RT (pivot bottom-left of left column)
        //   width  : RT width (usually LeftColW - LeftMargin)
        Button StyledButton(string name, string labelText, Transform parent,
                            Vector2 size, Vector2 pos)
        {
            // Root GO — transparent background, hit-area sized.
            var go  = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt  = go.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0, 0.5f);
            rt.anchorMax        = new Vector2(0, 0.5f);
            rt.pivot            = new Vector2(0, 0.5f);
            rt.sizeDelta        = size;
            rt.anchoredPosition = pos;

            // Invisible background that catches raycasts.
            var hitImg   = go.AddComponent<Image>();
            hitImg.color = MenuStyle.Transparent;

            var btn      = go.AddComponent<Button>();
            btn.targetGraphic = hitImg;

            // Amber marker bar — anchored to the left edge, full height.
            var markerGO  = new GameObject("SelectionMarker");
            markerGO.transform.SetParent(go.transform, false);
            var markerRT  = markerGO.AddComponent<RectTransform>();
            markerRT.anchorMin = new Vector2(0, 0);
            markerRT.anchorMax = new Vector2(0, 1);
            markerRT.offsetMin = Vector2.zero;
            markerRT.offsetMax = new Vector2(MenuStyle.MarkerWidth, 0);
            var markerImg = markerGO.AddComponent<Image>();
            markerImg.color = MenuStyle.Transparent;   // hidden until hover/focus

            // Label — left-aligned, offset right of the marker.
            var lblGO  = new GameObject("Label");
            lblGO.transform.SetParent(go.transform, false);
            var lblRT  = lblGO.AddComponent<RectTransform>();
            lblRT.anchorMin        = Vector2.zero;
            lblRT.anchorMax        = Vector2.one;
            lblRT.offsetMin        = new Vector2(MenuStyle.MarkerWidth + MenuStyle.ButtonPadLeft, 0);
            lblRT.offsetMax        = Vector2.zero;
            var lbl    = lblGO.AddComponent<TextMeshProUGUI>();
            lbl.text           = labelText;
            lbl.fontSize       = MenuStyle.ButtonSize;
            lbl.color          = MenuStyle.TextPrimary;
            lbl.alignment      = TextAlignmentOptions.Left | TextAlignmentOptions.Midline;
            lbl.raycastTarget  = false;

            // Wire up MenuButtonEffect.
            var effect = go.AddComponent<MenuButtonEffect>();
            effect.Init(markerImg, lbl);

            return btn;
        }

        // ── Horizontal separator line ─────────────────────────────────────────
        RectTransform Separator(string name, Transform parent, float yPos, float width)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.sizeDelta        = new Vector2(width, 1f);
            rt.anchoredPosition = new Vector2(0, yPos);
            var img = go.AddComponent<Image>();
            img.color         = MenuStyle.SeparatorLine;
            img.raycastTarget = false;
            return rt;
        }

        // ── Options slider row ────────────────────────────────────────────────
        //   Returns the Slider component; also creates label + value label.
        Slider OptionsRow(string rowName, Transform parent,
                          float yCenter, float panelWidth,
                          float sliderMin, float sliderMax, float sliderDefault,
                          string labelText,
                          out TMP_Text valueLabel)
        {
            float labelW  = 280f;
            float sliderW = panelWidth * 0.38f;
            float valueW  = 90f;
            float totalContent = labelW + 20f + sliderW + 16f + valueW;
            float startX  = -(totalContent * 0.5f);   // centre the whole row

            // Row label.
            var lblGO = new GameObject(rowName + "_Label");
            lblGO.transform.SetParent(parent, false);
            var lblRT = lblGO.AddComponent<RectTransform>();
            lblRT.anchorMin        = new Vector2(0.5f, 0.5f);
            lblRT.anchorMax        = new Vector2(0.5f, 0.5f);
            lblRT.sizeDelta        = new Vector2(labelW, 44f);
            lblRT.anchoredPosition = new Vector2(startX + labelW * 0.5f, yCenter);
            var lbl = lblGO.AddComponent<TextMeshProUGUI>();
            lbl.text          = labelText;
            lbl.fontSize      = MenuStyle.OptionLabelSize;
            lbl.color         = MenuStyle.TextPrimary;
            lbl.alignment     = TextAlignmentOptions.Left | TextAlignmentOptions.Midline;
            lbl.raycastTarget = false;

            // Slider.
            float sliderX = startX + labelW + 20f + sliderW * 0.5f;
            var slider = BuildSlider(rowName + "_Slider", parent,
                                     new Vector2(sliderW, 10f),
                                     new Vector2(sliderX, yCenter),
                                     sliderMin, sliderMax, sliderDefault);

            // Value readout.
            float valX = startX + labelW + 20f + sliderW + 16f + valueW * 0.5f;
            var valGO  = new GameObject(rowName + "_Value");
            valGO.transform.SetParent(parent, false);
            var valRT  = valGO.AddComponent<RectTransform>();
            valRT.anchorMin        = new Vector2(0.5f, 0.5f);
            valRT.anchorMax        = new Vector2(0.5f, 0.5f);
            valRT.sizeDelta        = new Vector2(valueW, 44f);
            valRT.anchoredPosition = new Vector2(valX, yCenter);
            var valTxt = valGO.AddComponent<TextMeshProUGUI>();
            valTxt.fontSize      = MenuStyle.ValueLabelSize;
            valTxt.color         = MenuStyle.AccentAmber;
            valTxt.alignment     = TextAlignmentOptions.Right | TextAlignmentOptions.Midline;
            valTxt.raycastTarget = false;

            valueLabel = valTxt;
            return slider;
        }

        // ── Thin-track slider (amber fill + white handle) ─────────────────────
        Slider BuildSlider(string sliderName, Transform parent,
                           Vector2 trackSize, Vector2 pos,
                           float min, float max, float val)
        {
            var go = new GameObject(sliderName);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.sizeDelta        = new Vector2(trackSize.x, 32f);
            rt.anchoredPosition = pos;

            // Track background.
            var bgGO  = new GameObject("Background"); bgGO.transform.SetParent(go.transform, false);
            var bgRT  = bgGO.AddComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0, 0.5f); bgRT.anchorMax = new Vector2(1, 0.5f);
            bgRT.sizeDelta = new Vector2(0, trackSize.y);
            var bgImg = bgGO.AddComponent<Image>();
            bgImg.color = new Color(1, 1, 1, 0.15f);

            // Fill area.
            var fillAreaGO = new GameObject("Fill Area"); fillAreaGO.transform.SetParent(go.transform, false);
            var fillAreaRT = fillAreaGO.AddComponent<RectTransform>();
            fillAreaRT.anchorMin = new Vector2(0, 0.5f); fillAreaRT.anchorMax = new Vector2(1, 0.5f);
            fillAreaRT.sizeDelta = new Vector2(-10f, trackSize.y);

            var fillGO  = new GameObject("Fill"); fillGO.transform.SetParent(fillAreaGO.transform, false);
            var fillRT  = fillGO.AddComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
            fillRT.sizeDelta = new Vector2(10f, 0);
            var fillImg = fillGO.AddComponent<Image>();
            fillImg.color = MenuStyle.AccentAmber;

            // Handle slide area.
            var hsaGO = new GameObject("Handle Slide Area"); hsaGO.transform.SetParent(go.transform, false);
            var hsaRT = hsaGO.AddComponent<RectTransform>();
            hsaRT.anchorMin = Vector2.zero; hsaRT.anchorMax = Vector2.one;
            hsaRT.offsetMin = new Vector2(8, 0); hsaRT.offsetMax = new Vector2(-8, 0);

            var handleGO  = new GameObject("Handle"); handleGO.transform.SetParent(hsaGO.transform, false);
            var handleRT  = handleGO.AddComponent<RectTransform>();
            handleRT.anchorMin = new Vector2(0, 0.5f); handleRT.anchorMax = new Vector2(0, 0.5f);
            handleRT.sizeDelta = new Vector2(22f, 22f);
            var handleImg = handleGO.AddComponent<Image>();
            handleImg.color = Color.white;

            var slider          = go.AddComponent<Slider>();
            slider.fillRect     = fillRT;
            slider.handleRect   = handleRT;
            slider.minValue     = min;
            slider.maxValue     = max;
            slider.value        = val;
            slider.direction    = Slider.Direction.LeftToRight;
            return slider;
        }

        // ── Full OptionsPanel sub-hierarchy ───────────────────────────────────
        OptionsPanel BuildOptionsPanel(string goName, Transform parent, float cardWidth)
        {
            var panelGO = new GameObject(goName);
            panelGO.transform.SetParent(parent, false);
            var rt = panelGO.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            // Dark card behind the options rows.
            var card = panelGO.AddComponent<Image>();
            card.color = MenuStyle.PanelCard;

            var op = panelGO.AddComponent<OptionsPanel>();

            // "OPTIONS" heading.
            Txt("OptionsHeading", "OPTIONS", panelGO.transform,
                MenuStyle.PauseTitleSize, MenuStyle.AccentAmber,
                new Vector2(cardWidth - 80f, 60f), new Vector2(0, 148f),
                TextAlignmentOptions.Center);

            // Separator under heading.
            Separator("Sep0", panelGO.transform, 108f, cardWidth * 0.75f);

            // Volume row  (y = +30).
            var volSlider = OptionsRow("Volume", panelGO.transform,
                                       28f, cardWidth,
                                       UISettings.MinVolume, UISettings.MaxVolume,
                                       UISettings.DefaultVolume,
                                       "MASTER VOLUME", out var volLabel);

            // Mid separator.
            Separator("Sep1", panelGO.transform, -20f, cardWidth * 0.75f);

            // Sensitivity row  (y = -68).
            var sensSlider = OptionsRow("Sensitivity", panelGO.transform,
                                        -68f, cardWidth,
                                        UISettings.MinSensitivity, UISettings.MaxSensitivity,
                                        UISettings.DefaultSensitivity,
                                        "MOUSE SENSITIVITY", out var sensLabel);

            // Bottom separator.
            Separator("Sep2", panelGO.transform, -116f, cardWidth * 0.75f);

            // Back button — centred, bottom of card.
            var backRoot = new GameObject("BackButton");
            backRoot.transform.SetParent(panelGO.transform, false);
            var backRT   = backRoot.AddComponent<RectTransform>();
            backRT.anchorMin        = new Vector2(0.5f, 0.5f);
            backRT.anchorMax        = new Vector2(0.5f, 0.5f);
            backRT.sizeDelta        = new Vector2(220f, MenuStyle.ButtonHeight);
            backRT.anchoredPosition = new Vector2(0, -172f);
            var backHit  = backRoot.AddComponent<Image>();
            backHit.color = MenuStyle.Transparent;
            var backBtn  = backRoot.AddComponent<Button>();
            backBtn.targetGraphic = backHit;

            // Marker.
            var backMarkerGO  = new GameObject("SelectionMarker");
            backMarkerGO.transform.SetParent(backRoot.transform, false);
            var backMarkerRT  = backMarkerGO.AddComponent<RectTransform>();
            backMarkerRT.anchorMin = new Vector2(0, 0); backMarkerRT.anchorMax = new Vector2(0, 1);
            backMarkerRT.offsetMin = Vector2.zero; backMarkerRT.offsetMax = new Vector2(MenuStyle.MarkerWidth, 0);
            var backMarkerImg = backMarkerGO.AddComponent<Image>(); backMarkerImg.color = MenuStyle.Transparent;

            // Label.
            var backLblGO = new GameObject("Label"); backLblGO.transform.SetParent(backRoot.transform, false);
            var backLblRT = backLblGO.AddComponent<RectTransform>();
            backLblRT.anchorMin = Vector2.zero; backLblRT.anchorMax = Vector2.one;
            backLblRT.offsetMin = new Vector2(MenuStyle.MarkerWidth + MenuStyle.ButtonPadLeft, 0);
            backLblRT.offsetMax = Vector2.zero;
            var backLbl = backLblGO.AddComponent<TextMeshProUGUI>();
            backLbl.text          = "BACK";
            backLbl.fontSize      = MenuStyle.ButtonSize;
            backLbl.color         = MenuStyle.TextPrimary;
            backLbl.alignment     = TextAlignmentOptions.Left | TextAlignmentOptions.Midline;
            backLbl.raycastTarget = false;

            var backEffect = backRoot.AddComponent<MenuButtonEffect>();
            backEffect.Init(backMarkerImg, backLbl);

            // Wire OptionsPanel serialized fields.
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

        // ═════════════════════════════════════════════════════════════════════
        // A. MAIN MENU CANVAS  (sort 0)
        // ═════════════════════════════════════════════════════════════════════
        var mmCanvas = MakeCanvas("MainMenuCanvas", 0);
        var mmRoot   = mmCanvas.transform;

        // ── Full-bleed backdrop ───────────────────────────────────────────────
        // Right half: mid-navy (placeholder for team art — assign a Sprite here).
        var backdropRT = Panel("Backdrop", mmRoot, MenuStyle.BackgroundMid, false);
        backdropRT.gameObject.GetComponent<Image>().raycastTarget = false;

        // Left column: deep navy panel (Dorfromantik left-strip).
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

        // Art-hint label — right half placeholder (remove when real art is set on Backdrop).
        {
            var hint = new GameObject("ArtPlaceholderHint");
            hint.transform.SetParent(mmRoot, false);
            var hRT = hint.AddComponent<RectTransform>();
            hRT.anchorMin        = new Vector2(MenuStyle.LeftColumnWidthFraction, 0.1f);
            hRT.anchorMax        = new Vector2(1f, 0.9f);
            hRT.offsetMin = hRT.offsetMax = Vector2.zero;
            var hTxt = hint.AddComponent<TextMeshProUGUI>();
            hTxt.text          = "[ ASSIGN ART HERE\nBackdrop Image component\non the Backdrop GameObject ]";
            hTxt.fontSize      = 22;
            hTxt.color         = MenuStyle.SubtitleGrey;
            hTxt.alignment     = TextAlignmentOptions.Center | TextAlignmentOptions.Midline;
            hTxt.raycastTarget = false;
        }

        // ── MainPanel — holds the title + buttons ─────────────────────────────
        var mainPanelGO = new GameObject("MainPanel");
        mainPanelGO.transform.SetParent(mmRoot, false);
        var mainPanelRT = mainPanelGO.AddComponent<RectTransform>();
        mainPanelRT.anchorMin        = new Vector2(0, 0);
        mainPanelRT.anchorMax        = new Vector2(0, 1);
        mainPanelRT.offsetMin        = Vector2.zero;
        mainPanelRT.offsetMax        = new Vector2(LeftColW, 0);

        // Title.  Anchored top-left with top margin of 180 px.
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

        // Subtitle / tagline.
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

        // Button stack — anchored to bottom of left column.
        // Positions are measured from vertical center of the column.
        //   Start   y = -48
        //   Options y = -48 - (ButtonHeight + ButtonSpacing) = -128
        //   Quit    y = -208
        float buttonW = LeftColW - MenuStyle.LeftMargin - 32f;  // right margin 32
        Vector2 btnSize = new Vector2(buttonW, MenuStyle.ButtonHeight);

        // Pivot is (0, 0.5) so anchoredPosition.x = MenuStyle.LeftMargin.
        // The parent is mainPanelGO; we use anchor (0, 0.5) + explicit y offsets from center.
        float btnY0 = 20f;    // top button slightly above center for visual balance
        float step  = MenuStyle.ButtonHeight + MenuStyle.ButtonSpacing;

        var startBtn  = StyledButton("StartButton",   "START",   mainPanelGO.transform,
                                     btnSize, new Vector2(MenuStyle.LeftMargin, btnY0));
        var optBtn1   = StyledButton("OptionsButton", "OPTIONS", mainPanelGO.transform,
                                     btnSize, new Vector2(MenuStyle.LeftMargin, btnY0 - step));
        var quitBtn   = StyledButton("QuitButton",    "QUIT",    mainPanelGO.transform,
                                     btnSize, new Vector2(MenuStyle.LeftMargin, btnY0 - step * 2));

        // Options panel (full-canvas, overlays everything).
        var optPanelMM = BuildOptionsPanel("OptionsPanel", mmRoot, 700f);

        // Wire MainMenuController.
        var mmc  = mmCanvas.gameObject.AddComponent<MainMenuController>();
        var mmSO = new SerializedObject(mmc);
        mmSO.FindProperty("mainPanel"    ).objectReferenceValue = mainPanelGO;
        mmSO.FindProperty("optionsPanel" ).objectReferenceValue = optPanelMM;
        mmSO.FindProperty("startButton"  ).objectReferenceValue = startBtn;
        mmSO.FindProperty("optionsButton").objectReferenceValue = optBtn1;
        mmSO.FindProperty("quitButton"   ).objectReferenceValue = quitBtn;
        mmSO.ApplyModifiedPropertiesWithoutUndo();

        // ═════════════════════════════════════════════════════════════════════
        // B. PAUSE CANVAS  (sort 10)
        // ═════════════════════════════════════════════════════════════════════
        var pauseCanvas = MakeCanvas("PauseMenuCanvas", 10);
        var pauseRoot   = pauseCanvas.transform;

        // Scrim — semi-transparent, keeps game visible (From Dust / a757ba8f reference).
        var scrimGO  = new GameObject("Scrim");
        scrimGO.transform.SetParent(pauseRoot, false);
        var scrimRT  = scrimGO.AddComponent<RectTransform>();
        scrimRT.anchorMin = Vector2.zero; scrimRT.anchorMax = Vector2.one;
        scrimRT.offsetMin = scrimRT.offsetMax = Vector2.zero;
        var scrimImg = scrimGO.AddComponent<Image>();
        scrimImg.color         = MenuStyle.PauseScrim;
        scrimImg.raycastTarget = true;   // block clicks through to game

        // Dark card — compact, centred (640 × 420 at ref).
        float cardW = 640f; float cardH = 420f;
        var cardRT  = Box("PauseCard", pauseRoot, new Vector2(cardW, cardH), Vector2.zero,
                          MenuStyle.PanelCard);
        cardRT.gameObject.GetComponent<Image>().raycastTarget = false;

        // "PAUSED" heading inside card.
        Txt("PauseTitle", "PAUSED", cardRT,
            MenuStyle.PauseTitleSize, MenuStyle.AccentAmber,
            new Vector2(cardW - 60f, 60f), new Vector2(0, 140f),
            TextAlignmentOptions.Center);

        // Separator under heading.
        Separator("Sep", cardRT, 100f, cardW * 0.70f);

        // Pause buttons — centred inside card.
        float pbW     = 320f;
        float pbY0    = 20f;
        float pbStep  = MenuStyle.ButtonHeight + 10f;

        var resumeBtn = StyledButton("ResumeButton",  "RESUME",  cardRT,
                                     new Vector2(pbW, MenuStyle.ButtonHeight),
                                     new Vector2(-pbW * 0.5f, pbY0));
        var pOptBtn   = StyledButton("OptionsButton", "OPTIONS", cardRT,
                                     new Vector2(pbW, MenuStyle.ButtonHeight),
                                     new Vector2(-pbW * 0.5f, pbY0 - pbStep));

        // PausePanel contains card+scrim; we set it inactive when not paused.
        var pausePanelGO = new GameObject("PausePanel");
        pausePanelGO.transform.SetParent(pauseRoot, false);
        var pausePanelRT = pausePanelGO.AddComponent<RectTransform>();
        pausePanelRT.anchorMin = Vector2.zero; pausePanelRT.anchorMax = Vector2.one;
        pausePanelRT.offsetMin = pausePanelRT.offsetMax = Vector2.zero;

        // Re-parent scrim and card under PausePanel so the whole thing hides together.
        scrimGO.transform.SetParent(pausePanelGO.transform, true);
        cardRT.transform.SetParent(pausePanelGO.transform, true);

        pausePanelGO.SetActive(false);

        // Options panel for pause (also full-canvas).
        var optPanelPause = BuildOptionsPanel("PauseOptionsPanel", pauseRoot, 700f);

        // Wire PauseMenuController.
        var pmc   = pauseCanvas.gameObject.AddComponent<PauseMenuController>();
        var pmcSO = new SerializedObject(pmc);
        pmcSO.FindProperty("pausePanel"   ).objectReferenceValue = pausePanelGO;
        pmcSO.FindProperty("optionsPanel" ).objectReferenceValue = optPanelPause;
        pmcSO.FindProperty("resumeButton" ).objectReferenceValue = resumeBtn;
        pmcSO.FindProperty("optionsButton").objectReferenceValue = pOptBtn;
        if (inputAsset != null)
            pmcSO.FindProperty("inputActions").objectReferenceValue = inputAsset;
        pmcSO.ApplyModifiedPropertiesWithoutUndo();

        // ═════════════════════════════════════════════════════════════════════
        // C. DEATH CANVAS  (sort 20)  — unchanged from previous implementation
        // ═════════════════════════════════════════════════════════════════════
        var deathCanvas = MakeCanvas("DeathScreenCanvas", 20);
        var deathRoot   = deathCanvas.transform;

        var fadeOvD  = Panel("FadeOverlayD", deathRoot, Color.clear);
        var fadeImgD = fadeOvD.gameObject.AddComponent<Image>();
        fadeImgD.color         = new Color(0, 0, 0, 0);
        fadeImgD.raycastTarget = false;

        var deathPanelRT = Panel("DeathPanel", deathRoot, Color.clear);
        deathPanelRT.gameObject.SetActive(false);

        var ginoGO  = new GameObject("GinoImage");
        ginoGO.transform.SetParent(deathPanelRT, false);
        var ginoRT  = ginoGO.AddComponent<RectTransform>();
        ginoRT.sizeDelta = new Vector2(300, 300); ginoRT.anchoredPosition = new Vector2(0, 80);
        var ginoImg = ginoGO.AddComponent<Image>(); ginoImg.color = new Color(0.4f, 0.4f, 0.4f);

        var nightmareLbl = Txt("NightmareLostLabel", "Nightmare lost\u2026", deathPanelRT,
            42, MenuStyle.TextPrimary, new Vector2(600, 60), new Vector2(0, -100),
            TextAlignmentOptions.Center);

        // Death buttons reuse legacy plain-button helper (not styled menu buttons —
        // the death screen has its own aesthetic defined in the GDD).
        Button PlainButton(string n, string lbl, Transform p, Vector2 pos)
        {
            var go  = new GameObject(n);
            go.transform.SetParent(p, false);
            var rt  = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(300, 56); rt.anchoredPosition = pos;
            var img = go.AddComponent<Image>(); img.color = new Color(0.12f, 0.14f, 0.28f, 0.9f);
            var btn = go.AddComponent<Button>(); btn.targetGraphic = img;
            var lblGO = new GameObject("Label"); lblGO.transform.SetParent(go.transform, false);
            var lblRT = lblGO.AddComponent<RectTransform>();
            lblRT.anchorMin = Vector2.zero; lblRT.anchorMax = Vector2.one; lblRT.offsetMin = lblRT.offsetMax = Vector2.zero;
            var t = lblGO.AddComponent<TextMeshProUGUI>();
            t.text = lbl; t.fontSize = 26; t.color = MenuStyle.TextPrimary;
            t.alignment = TextAlignmentOptions.Center; t.raycastTarget = false;
            return btn;
        }

        var retryBtn     = PlainButton("RetryButton",    "SNOOZE AND RETRY", deathPanelRT, new Vector2(0, -170));
        var deathMenuBtn = PlainButton("MainMenuButton", "MAIN MENU",         deathPanelRT, new Vector2(0, -240));

        var dsc   = deathCanvas.gameObject.AddComponent<DeathScreenController>();
        var dscSO = new SerializedObject(dsc);
        dscSO.FindProperty("deathPanel"    ).objectReferenceValue = deathPanelRT.gameObject;
        dscSO.FindProperty("fadeOverlay"   ).objectReferenceValue = fadeImgD;
        dscSO.FindProperty("ginoImage"     ).objectReferenceValue = ginoImg;
        dscSO.FindProperty("retryButton"   ).objectReferenceValue = retryBtn;
        dscSO.FindProperty("mainMenuButton").objectReferenceValue = deathMenuBtn;
        dscSO.ApplyModifiedPropertiesWithoutUndo();

        // ═════════════════════════════════════════════════════════════════════
        // D. ENDING CANVAS  (sort 30)  — unchanged
        // ═════════════════════════════════════════════════════════════════════
        var endingCanvas = MakeCanvas("EndingScreenCanvas", 30);
        var endingRoot   = endingCanvas.transform;

        var fadeOvE  = Panel("FadeOverlayE", endingRoot, Color.clear);
        var fadeImgE = fadeOvE.gameObject.AddComponent<Image>();
        fadeImgE.color = new Color(0, 0, 0, 0); fadeImgE.raycastTarget = false;

        var endingPanelRT = Panel("EndingPanel", endingRoot, Color.clear);
        endingPanelRT.gameObject.SetActive(false);

        var bedroomGO  = new GameObject("BedroomImage");
        bedroomGO.transform.SetParent(endingPanelRT, false);
        var bedroomRT  = bedroomGO.AddComponent<RectTransform>();
        bedroomRT.sizeDelta = new Vector2(500, 350); bedroomRT.anchoredPosition = new Vector2(0, 80);
        var bedroomImg = bedroomGO.AddComponent<Image>(); bedroomImg.color = new Color(0.3f, 0.3f, 0.5f);

        Txt("ThanksLabel", "Thanks for playing", endingPanelRT,
            48, MenuStyle.TextPrimary, new Vector2(640, 60), new Vector2(0, -100),
            TextAlignmentOptions.Center);

        var endMenuBtn = PlainButton("MainMenuButton", "MAIN MENU", endingPanelRT, new Vector2(0, -200));

        var esc   = endingCanvas.gameObject.AddComponent<EndingScreenController>();
        var escSO = new SerializedObject(esc);
        escSO.FindProperty("endingPanel"   ).objectReferenceValue = endingPanelRT.gameObject;
        escSO.FindProperty("fadeOverlay"   ).objectReferenceValue = fadeImgE;
        escSO.FindProperty("bedroomImage"  ).objectReferenceValue = bedroomImg;
        escSO.FindProperty("mainMenuButton").objectReferenceValue = endMenuBtn;
        escSO.ApplyModifiedPropertiesWithoutUndo();

        // ═════════════════════════════════════════════════════════════════════
        // E. TEST HARNESS
        // ═════════════════════════════════════════════════════════════════════
        var harnessGO  = new GameObject("TestHarness");
        var testHealth = harnessGO.AddComponent<HealthComponent>();
        var harness    = harnessGO.AddComponent<UIFlowTestHarness>();
        var hSO        = new SerializedObject(harness);
        hSO.FindProperty("testPlayerHealth").objectReferenceValue = testHealth;
        hSO.FindProperty("deathScreen"     ).objectReferenceValue = dsc;
        hSO.ApplyModifiedPropertiesWithoutUndo();
        var dscSOb = new SerializedObject(dsc);
        dscSOb.FindProperty("playerHealth").objectReferenceValue = testHealth;
        dscSOb.ApplyModifiedPropertiesWithoutUndo();

        // ═════════════════════════════════════════════════════════════════════
        // Save
        // ═════════════════════════════════════════════════════════════════════
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        UpdateBuildSettingsGuid();

        Debug.Log(
            "[UIFlowTestSetup] REDESIGNED scene saved to " + ScenePath + "\n\n" +
            "MAIN MENU  — left column (deep navy), title top-left, START/OPTIONS/QUIT stacked.\n" +
            "             Right side: mid-navy placeholder — assign your Sprite to Backdrop.\n" +
            "PAUSE      — compact dark card (640×420) over semi-transparent scrim.\n" +
            "OPTIONS    — full-canvas dark card, two rows, amber slider fill.\n\n" +
            "Press Play → Start/Options/Quit, Esc for Pause, D/E for death and ending.");
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
            { guid = line.Split(':')[1].Trim(); break; }
        }
        if (string.IsNullOrEmpty(guid)) return;
        string sp      = "ProjectSettings/EditorBuildSettings.asset";
        string content = System.IO.File.ReadAllText(sp);
        content = content.Replace("guid: 00000000000000000000000000000001", $"guid: {guid}");
        System.IO.File.WriteAllText(sp, content);
        Debug.Log($"[UIFlowTestSetup] Build settings → UIFlowTest GUID: {guid}");
    }
}
#endif
