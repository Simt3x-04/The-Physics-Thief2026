using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PhysicsThief.Properties;

namespace PhysicsThief.UI
{
    /// <summary>
    /// World-space timer bars above an object while it has pasted properties (GDD 8.1 and 3.8).
    /// One row per active attribute (several can be active at once, GDD 3.3): its name and a bar that shrinks
    /// with the remaining time. The panel always faces the camera, keeps a readable size at any distance and
    /// disappears by itself when you look away. In the last second the bar flashes red and the object flickers.
    /// The UI is built from code, so no prefab or Inspector wiring is needed: just add this component
    /// to every object that can receive a pasted property.
    /// </summary>
    [RequireComponent(typeof(PhysicalObject))]
    public class PropertyTimerDisplay : MonoBehaviour
    {
        [Header("Placement")]
        [Tooltip("Meters between the top of the object and the bars.")]
        [SerializeField] private float heightOffset = 0.25f;

        [Header("Size")]
        [Tooltip("World size of one canvas unit. 0.006 makes a bar about 1 m wide.")]
        [SerializeField] private float unitsToMeters = 0.006f;
        [SerializeField] private float rowWidth = 170f;
        [SerializeField] private float rowHeight = 20f;
        [SerializeField] private float rowSpacing = 4f;
        [SerializeField] private float labelWidth = 62f;

        [Header("Distance")]
        [Tooltip("At this distance the panel has its normal size; farther away it grows so it keeps the same apparent size.")]
        [SerializeField] private float referenceDistance = 6f;
        [SerializeField] private float minScaleFactor = 0.5f;
        [SerializeField] private float maxScaleFactor = 4f;
        [Tooltip("Beyond this distance the bars are hidden.")]
        [SerializeField] private float maxVisibleDistance = 30f;

        [Header("Last-second warning (GDD 3.8)")]
        [SerializeField] private float warningTime = 1f;
        [SerializeField] private float flashSpeed = 8f;
        [SerializeField] private Color warningColor = new Color(0.94f, 0.27f, 0.27f, 1f);
        [SerializeField] private Color barBackgroundColor = new Color(0.06f, 0.09f, 0.16f, 0.85f);
        [Tooltip("Makes the object blink during the last second.")]
        [SerializeField] private bool flickerObject = true;

        private class Row
        {
            public RectTransform rect;
            public RectTransform fill;
            public Image fillImage;
        }

        private PhysicalObject physObj;
        private Renderer[] renderers;
        private Camera cam;
        private GameObject canvasObject;
        private RectTransform canvasRect;
        private Row[] rows;
        private int activeRowCount;
        private bool isFlickering;

        private void Awake()
        {
            physObj = GetComponent<PhysicalObject>();
            renderers = GetComponentsInChildren<Renderer>();   // collected before the timer UI exists
            BuildUi();
        }

        private void OnEnable()
        {
            physObj.OnPropertyValueChanged += HandleValueChanged;
            physObj.OnOverrideExpired += HandleOverrideExpired;
            Refresh();
        }

        private void OnDisable()
        {
            physObj.OnPropertyValueChanged -= HandleValueChanged;
            physObj.OnOverrideExpired -= HandleOverrideExpired;
            if (canvasObject != null) canvasObject.SetActive(false);
            StopFlicker();
        }

        private void OnDestroy()
        {
            if (canvasObject != null) Destroy(canvasObject);
        }

        private void HandleValueChanged(AttributeType type, float value) { Refresh(); }
        private void HandleOverrideExpired(AttributeType type) { Refresh(); }

        /// <summary>Shows one row for each attribute that currently has a pasted value, stacked from the bottom.</summary>
        private void Refresh()
        {
            int shown = 0;
            foreach (AttributeType type in Enum.GetValues(typeof(AttributeType)))
            {
                Row row = rows[(int)type];
                bool active = physObj.HasOverride(type);
                row.rect.gameObject.SetActive(active);
                if (active)
                {
                    row.rect.anchoredPosition = new Vector2(0f, shown * (rowHeight + rowSpacing));
                    shown++;
                }
            }
            activeRowCount = shown;
            canvasObject.SetActive(shown > 0);
        }

        private void LateUpdate()
        {
            if (activeRowCount == 0)
            {
                StopFlicker();
                return;
            }

            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            // Anchor the panel above the object. It is NOT parented to it, so it never rotates or scales with the object.
            Bounds bounds = GetWorldBounds();
            Vector3 anchor = new Vector3(bounds.center.x, bounds.max.y + heightOffset, bounds.center.z);
            canvasRect.position = anchor;

            // Billboard: copy the camera rotation, so the panel is parallel to the screen and always upright.
            canvasRect.rotation = cam.transform.rotation;

            float distance = Vector3.Distance(cam.transform.position, anchor);
            bool visible = distance <= maxVisibleDistance;
            if (canvasObject.activeSelf != visible) canvasObject.SetActive(visible);

            float factor = Mathf.Clamp(distance / referenceDistance, minScaleFactor, maxScaleFactor);
            canvasRect.localScale = Vector3.one * (unitsToMeters * factor);

            UpdateBars();
        }

        /// <summary>Shrinks every active bar and handles the last-second warning (red flash + object flicker).</summary>
        private void UpdateBars()
        {
            bool warning = false;

            foreach (AttributeType type in Enum.GetValues(typeof(AttributeType)))
            {
                Row row = rows[(int)type];
                if (!row.rect.gameObject.activeSelf) continue;

                float duration = physObj.GetDuration(type);
                float remaining = physObj.GetRemainingTime(type);
                float fraction = duration > 0f ? Mathf.Clamp01(remaining / duration) : 0f;
                row.fill.anchorMax = new Vector2(fraction, 1f);

                bool inWarning = remaining <= warningTime;
                warning |= inWarning;
                bool flashRed = inWarning && Mathf.PingPong(Time.time * flashSpeed, 1f) > 0.5f;
                row.fillImage.color = flashRed ? warningColor : AttributePalette.Get(type);
            }

            if (flickerObject && warning)
            {
                SetRenderersEnabled(Mathf.Repeat(Time.time * 10f, 1f) > 0.5f);
                isFlickering = true;
            }
            else
            {
                StopFlicker();
            }
            // TODO: play the ticking sound during the last second (AudioManager, GDD 3.8).
        }

        private Bounds GetWorldBounds()
        {
            if (renderers == null || renderers.Length == 0) return new Bounds(transform.position, Vector3.zero);

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private void StopFlicker()
        {
            if (!isFlickering) return;
            SetRenderersEnabled(true);
            isFlickering = false;
        }

        private void SetRenderersEnabled(bool state)
        {
            for (int i = 0; i < renderers.Length; i++) renderers[i].enabled = state;
        }

        // ---------- UI built from code ----------

        private void BuildUi()
        {
            canvasObject = new GameObject(name + "_PropertyTimers", typeof(RectTransform), typeof(Canvas));
            canvasRect = (RectTransform)canvasObject.transform;
            canvasRect.sizeDelta = new Vector2(rowWidth, rowHeight);
            canvasRect.pivot = new Vector2(0.5f, 0f);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 10;

            rows = new Row[Enum.GetValues(typeof(AttributeType)).Length];
            foreach (AttributeType type in Enum.GetValues(typeof(AttributeType)))
            {
                rows[(int)type] = BuildRow(type);
            }
            canvasObject.SetActive(false);
        }

        private Row BuildRow(AttributeType type)
        {
            var row = new Row();

            var rowObject = new GameObject(type + "Row", typeof(RectTransform));
            row.rect = (RectTransform)rowObject.transform;
            row.rect.SetParent(canvasRect, false);
            row.rect.anchorMin = new Vector2(0.5f, 0f);
            row.rect.anchorMax = new Vector2(0.5f, 0f);
            row.rect.pivot = new Vector2(0.5f, 0f);
            row.rect.sizeDelta = new Vector2(rowWidth, rowHeight);

            // Attribute name, tinted with the attribute color.
            var labelObject = new GameObject("Label", typeof(RectTransform));
            var labelRect = (RectTransform)labelObject.transform;
            labelRect.SetParent(row.rect, false);
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(0f, 1f);
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.sizeDelta = new Vector2(labelWidth, 0f);
            labelRect.anchoredPosition = Vector2.zero;
            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            label.text = type.ToString().ToUpperInvariant();
            label.fontSize = 14f;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.color = AttributePalette.Get(type);

            // Dark bar background.
            var backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(Image));
            var backgroundRect = (RectTransform)backgroundObject.transform;
            backgroundRect.SetParent(row.rect, false);
            backgroundRect.anchorMin = new Vector2(0f, 0.2f);
            backgroundRect.anchorMax = new Vector2(1f, 0.8f);
            backgroundRect.offsetMin = new Vector2(labelWidth + 4f, 0f);
            backgroundRect.offsetMax = Vector2.zero;
            backgroundObject.GetComponent<Image>().color = barBackgroundColor;

            // Fill: its right anchor is the remaining fraction (1 = full, 0 = empty).
            var fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            row.fill = (RectTransform)fillObject.transform;
            row.fill.SetParent(backgroundRect, false);
            row.fill.anchorMin = Vector2.zero;
            row.fill.anchorMax = Vector2.one;
            row.fill.offsetMin = Vector2.zero;
            row.fill.offsetMax = Vector2.zero;
            row.fillImage = fillObject.GetComponent<Image>();
            row.fillImage.color = AttributePalette.Get(type);

            rowObject.SetActive(false);
            return row;
        }
    }
}
