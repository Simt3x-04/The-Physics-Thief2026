using UnityEngine;

namespace PhysicsThief.UI
{
    /// <summary>
    /// Shared visual constants for Main Menu, Pause and Options.
    ///
    /// PALETTE (Dorfromantik / From Dust inspired, navy-dream theme)
    /// ─────────────────────────────────────────────────────────────
    ///  BackgroundDeep  — deep navy for the full-bleed backdrop left column
    ///  BackgroundMid   — slightly lighter navy for the right half / card interiors
    ///  PanelCard       — dark indigo used for pause overlay and options card
    ///  TextPrimary     — warm off-white, all labels and unselected button text
    ///  TextDim         — dimmed off-white for secondary/disabled items
    ///  AccentAmber     — amber for the active/selected item marker and focused slider
    ///  AccentAmberDim  — muted amber for hover state
    ///  PressedTint     — slightly desaturated amber for pressed state
    ///  SeparatorLine   — thin divider between options rows
    ///  SubtitleGrey    — small credit / version text
    ///
    /// LAYOUT
    /// ─────
    ///  LeftColumnWidthFraction  — fraction of 1920 width the left panel occupies
    ///  LeftMargin               — horizontal padding inside the left column
    ///  ButtonHeight             — natural hit area height for menu buttons
    ///  ButtonSpacing            — gap between consecutive buttons
    ///  MarkerWidth              — width of the amber left-side selection bar
    ///
    /// TYPOGRAPHY
    /// ──────────
    ///  TitleSize    — The Physics Thief title
    ///  ButtonSize   — Start / Options / Quit / Resume
    ///  PauseTitleSize
    ///  OptionLabelSize
    ///  ValueLabelSize
    ///  SubtitleSize
    /// </summary>
    public static class MenuStyle
    {
        // ── Palette ──────────────────────────────────────────────────────────
        /// <summary>Deep navy, full-bleed backdrop and left column.</summary>
        public static readonly Color BackgroundDeep  = new Color(0.078f, 0.082f, 0.188f, 1f);  // #141535

        /// <summary>Mid navy, gradient right half of main-menu backdrop.</summary>
        public static readonly Color BackgroundMid   = new Color(0.102f, 0.118f, 0.259f, 1f);  // #1A1E42

        /// <summary>Dark indigo card for Pause overlay and Options panel.</summary>
        public static readonly Color PanelCard       = new Color(0.051f, 0.055f, 0.145f, 0.93f);// #0D0E25 93%

        /// <summary>Warm off-white — primary text colour.</summary>
        public static readonly Color TextPrimary     = new Color(0.961f, 0.929f, 0.839f, 1f);  // #F5EDD6

        /// <summary>Dimmed text for unselected / inactive items.</summary>
        public static readonly Color TextDim         = new Color(0.961f, 0.929f, 0.839f, 0.45f);

        /// <summary>Amber — selected / focused row, active slider handle.</summary>
        public static readonly Color AccentAmber     = new Color(1.000f, 0.702f, 0.278f, 1f);  // #FFB347

        /// <summary>Muted amber — hover state.</summary>
        public static readonly Color AccentAmberDim  = new Color(1.000f, 0.702f, 0.278f, 0.55f);

        /// <summary>Pressed / click feedback tint.</summary>
        public static readonly Color PressedTint     = new Color(0.839f, 0.573f, 0.173f, 1f);  // #D59222

        /// <summary>Thin separator line between option rows.</summary>
        public static readonly Color SeparatorLine   = new Color(0.961f, 0.929f, 0.839f, 0.12f);

        /// <summary>Muted grey for version / subtitle text.</summary>
        public static readonly Color SubtitleGrey    = new Color(0.961f, 0.929f, 0.839f, 0.30f);

        /// <summary>Completely transparent — used as a clear image fill.</summary>
        public static readonly Color Transparent     = new Color(0, 0, 0, 0);

        /// <summary>Pause scrim: semi-opaque dark overlay over gameplay.</summary>
        public static readonly Color PauseScrim      = new Color(0.020f, 0.024f, 0.078f, 0.78f);

        // ── Layout ───────────────────────────────────────────────────────────
        /// <summary>Fraction of reference width (1920) used by the left-column panel.</summary>
        public const float LeftColumnWidthFraction = 0.38f;

        /// <summary>Pixels from screen edge to text in the left column (at 1920 ref).</summary>
        public const float LeftMargin = 90f;

        /// <summary>Hit-area height for each menu button (px at 1920×1080 ref).</summary>
        public const float ButtonHeight = 64f;

        /// <summary>Vertical gap between consecutive button rows.</summary>
        public const float ButtonSpacing = 16f;

        /// <summary>Width of the amber selection marker bar on the left of the button.</summary>
        public const float MarkerWidth = 5f;

        /// <summary>Horizontal inner padding inside a styled menu button.</summary>
        public const float ButtonPadLeft = 24f;

        // ── Typography ───────────────────────────────────────────────────────
        /// <summary>Main title "The Physics Thief".</summary>
        public const int TitleSize = 68;

        /// <summary>Menu action buttons (Start / Options / Quit / Resume).</summary>
        public const int ButtonSize = 36;

        /// <summary>"PAUSED" heading inside the pause card.</summary>
        public const int PauseTitleSize = 52;

        /// <summary>Option row labels (Master Volume, Mouse Sensitivity).</summary>
        public const int OptionLabelSize = 30;

        /// <summary>Option value readout (80%, 1.0).</summary>
        public const int ValueLabelSize = 26;

        /// <summary>Tagline / subtitle beneath the title.</summary>
        public const int SubtitleSize = 20;
    }
}
