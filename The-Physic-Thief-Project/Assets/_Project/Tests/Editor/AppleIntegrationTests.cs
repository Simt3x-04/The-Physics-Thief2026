using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using PhysicsThief.Core;
using PhysicsThief.Items;
using PhysicsThief.Player;
using PhysicsThief.UI;
using Object = UnityEngine.Object;

namespace PhysicsThief.Tests
{
    /// <summary>
    /// Play Mode integration test of the apple feature on the REAL game: MainMenu -> Start -> VillageScene, the placed
    /// AppleTree_Start, the real player, HUD and GameManager, and E / Esc / mouse sent through the Input System
    /// (the same path PlayerInputHandler and the pause menu read). It runs from the EditMode list because the game code
    /// has no assembly definition (test code must stay out of player builds); it enters Play Mode itself.
    ///
    /// The only things the test does by hand: it teleports Gino next to the tree (PlayerController.Respawn), turns him
    /// towards the target (look angle), sets HP with TakeDamage/ResetHealth, and presses keys. Captures are saved to
    /// TestResults/Captures with an "AUTOMATED TEST INPUT" banner (skipped when Unity runs without a graphics device).
    /// </summary>
    public class AppleIntegrationTests
    {
        private const string MainMenuPath = "Assets/_Project/Scenes/MainMenu.unity";
        private const float LoadTimeout = 120f;

        private Keyboard keyboard;
        private Mouse mouse;
        private PlayerController player;
        private HealthComponent health;
        private PlayerInteractor interactor;
        private PlayerScanner scanner;
        private BriefcaseInventory inventory;
        private HUDManager hud;
        private AppleTree tree;
        private Camera cam;
        private Capture capture;

        // Settings changed for the test, restored in TearDown (which also runs after a failed assertion).
        private bool savedOptionsEnabled;
        private EnterPlayModeOptions savedOptions;
        private InputSettings.EditorInputBehaviorInPlayMode savedInputBehavior;
        private InputSettings.BackgroundBehavior savedBackground;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            savedOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
            savedOptions = EditorSettings.enterPlayModeOptions;
            savedInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            savedBackground = InputSystem.settings.backgroundBehavior;

            // No domain reload: the test keeps running inside Play Mode. Restored in TearDown.
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;

            EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Single);
            yield return new EnterPlayMode();

            // Batch mode has no focused Game view: let the test devices reach the game.
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            keyboard = InputSystem.AddDevice<Keyboard>("AppleTestKeyboard");
            mouse = InputSystem.AddDevice<Mouse>("AppleTestMouse");
            keyboard.MakeCurrent();
            mouse.MakeCurrent();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            capture?.Dispose();
            capture = null;
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            InputSystem.settings.editorInputBehaviorInPlayMode = savedInputBehavior;
            InputSystem.settings.backgroundBehavior = savedBackground;

            if (Application.isPlaying) yield return new ExitPlayMode();

            EditorSettings.enterPlayModeOptionsEnabled = savedOptionsEnabled;
            EditorSettings.enterPlayModeOptions = savedOptions;
        }

        [UnityTest]
        public IEnumerator Apples_FullGameFlow()
        {
            yield return RunFlow();
        }

        private IEnumerator RunFlow()
        {
            // ---- MainMenu -> Start -> VillageScene ----
            yield return WaitUntil(() => Object.FindFirstObjectByType<MainMenuController>() != null, "MainMenu loaded");
            Button start = GetField<Button>(Object.FindFirstObjectByType<MainMenuController>(), "startButton");
            Assert.IsNotNull(start, "MainMenu has a Start button");
            start.onClick.Invoke();

            yield return WaitUntil(() => SceneManager.GetActiveScene().name == "VillageScene"
                                         && GameManager.Instance != null && GameManager.Instance.State == GameState.Playing,
                "MainMenu Start loads VillageScene in the Playing state");
            for (int i = 0; i < 10; i++) yield return null;

            player = Object.FindFirstObjectByType<PlayerController>();
            Assert.IsNotNull(player, "player in VillageScene");
            health = player.GetComponent<HealthComponent>();
            interactor = player.GetComponentInChildren<PlayerInteractor>();
            scanner = player.GetComponentInChildren<PlayerScanner>();
            inventory = player.GetComponent<BriefcaseInventory>();
            hud = Object.FindFirstObjectByType<HUDManager>();
            cam = Camera.main;
            Assert.IsNotNull(interactor, "PlayerInteractor on the player camera");
            Assert.IsNotNull(hud, "HUD in VillageScene");

            GameObject placed = GameObject.Find("AppleTree_Start");
            Assert.IsNotNull(placed, "AppleTree_Start placed in VillageScene (run the Apple Trees setup first)");
            tree = placed.GetComponent<AppleTree>();
            spawnPosition = player.transform.position;
            Assert.AreEqual(3, tree.AvailableCount, "the tree starts with 3 apples");

            capture = new Capture(cam);
            if (!capture.Available) Debug.LogWarning("[AppleIntegrationTests] No graphics device: captures skipped.");

            // ---- the tree in the village ----
            yield return StandAndAim(tree.transform.position + Vector3.up * 2f, 7f, TowardsSpawn());
            capture.Shot("01_tree_in_village", "Teleported 7 m from AppleTree_Start");

            // ---- 60 -> 85 by aiming at an apple ----
            AppleItem first = FirstAvailableApple();
            yield return StandAndAim(first.transform.position, 1.8f, Outward(first));
            Assert.AreSame(first, interactor.CurrentTarget, "the crosshair targets the apple");
            SetHealth(60f);
            yield return null;
            Assert.AreEqual("E — Eat apple (+25 HP)", PromptText(), "prompt under the crosshair");
            Assert.AreEqual("60 / 100", HudHealthText(), "HUD before eating");
            capture.Shot("02_prompt_health_60", "HP set to 60 by the test");

            yield return PressAndRecord(Key.E, "03_eat_apple", "E pressed by the test", 0.6f);
            Assert.AreEqual(85f, health.CurrentHP, "60 + 25 = 85");
            Assert.AreEqual(2, tree.AvailableCount, "one press eats one apple");
            Assert.AreEqual("85 / 100", HudHealthText(), "HUD updated from OnHealthChanged");
            Assert.IsFalse(first.IsAvailable, "the eaten apple is gone");
            Assert.IsFalse(first.GetComponent<Collider>().enabled, "and can no longer be targeted");
            capture.Shot("04_health_85_apple_gone", "after E");

            // ---- 90 -> 100 through the harvesting point (trunk) ----
            yield return StandAndAim(TrunkPoint(), 1.4f, TowardsSpawn());
            Assert.AreSame(tree, interactor.CurrentTarget, "aiming at the trunk targets the tree");
            SetHealth(90f);
            yield return Press(Key.E);
            Assert.AreEqual(100f, health.CurrentHP, "90 + 25 stops at 100");
            Assert.AreEqual(1, tree.AvailableCount);

            // ---- full health keeps the apple ----
            AppleItem last = FirstAvailableApple();
            yield return StandAndAim(last.transform.position, 1.8f, Outward(last));
            yield return Press(Key.E);
            Assert.AreEqual(1, tree.AvailableCount, "at full health the apple stays");
            Assert.IsTrue(last.IsAvailable);
            Assert.AreEqual("Health is already full", HudMessageText());

            // ---- out of range ----
            yield return StandAndAim(last.transform.position, 5f, Outward(last));
            SetHealth(50f);   // after the teleport: Respawn restores full health
            Assert.IsNull(interactor.CurrentTarget, "5 m is out of reach");
            yield return Press(Key.E);
            Assert.AreEqual(50f, health.CurrentHP);
            Assert.AreEqual(1, tree.AvailableCount);

            // ---- behind an obstacle ----
            yield return StandAndAim(last.transform.position, 2.2f, Outward(last));
            SetHealth(50f);
            Vector3 eye = cam.transform.position;
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = Vector3.Lerp(eye, last.transform.position, 0.5f);
            wall.transform.rotation = Quaternion.LookRotation(last.transform.position - eye);
            wall.transform.localScale = new Vector3(1f, 1f, 0.1f);
            Physics.SyncTransforms();
            yield return null;
            Assert.IsNull(interactor.CurrentTarget, "a wall in between blocks the interaction");
            yield return Press(Key.E);
            Assert.AreEqual(50f, health.CurrentHP);
            Object.Destroy(wall);
            yield return null;

            // ---- pause (Esc through the pause menu) blocks eating ----
            yield return StandAndAim(last.transform.position, 1.8f, Outward(last));
            SetHealth(50f);
            yield return Press(Key.Escape);
            yield return WaitUntil(() => GameManager.Instance.State == GameState.Paused, "Esc pauses");
            Assert.IsNull(PromptText(), "no prompt while paused");
            yield return Press(Key.E);
            Assert.AreEqual(50f, health.CurrentHP, "no eating while paused");
            yield return Press(Key.Escape);
            yield return WaitUntil(() => GameManager.Instance.State == GameState.Playing, "Esc resumes");

            // ---- the scanner still works, and a paste on the scan-only tree keeps the slot ----
            yield return StandAndAim(TrunkPoint(), 1.6f, TowardsSpawn());
            yield return SetMouse(right: true, left: false);
            yield return WaitUntil(() => scanner.CurrentTarget != null, "right click scans the tree");
            yield return Press(Key.Digit1);   // copy Mass while scanning
            yield return SetMouse(right: false, left: false);
            Assert.IsTrue(inventory.Model.GetSlot(0).HasValue, "Mass copied into slot 1");
            Assert.AreEqual(800f, inventory.Model.GetSlot(0).Value.value, "tree mass (GDD 7)");
            yield return SetMouse(right: false, left: true);
            yield return SetMouse(right: false, left: false);
            Assert.IsTrue(inventory.Model.GetSlot(0).HasValue, "the refused paste kept the slot");

            // ---- death blocks eating; after Retry it works again ----
            health.TakeDamage(1000f);
            yield return WaitUntil(() => GameManager.Instance.State == GameState.GameOver, "death screen");
            yield return Press(Key.E);
            Assert.AreEqual(1, tree.AvailableCount, "no eating while dead");

            Button retry = GetField<Button>(Object.FindFirstObjectByType<DeathScreenController>(FindObjectsInactive.Include), "retryButton");
            retry.onClick.Invoke();
            yield return WaitUntil(() => GameManager.Instance.State == GameState.Playing && !health.IsDead, "Retry");
            Assert.AreEqual(100f, health.CurrentHP, "Retry restores health");

            yield return StandAndAim(last.transform.position, 1.8f, Outward(last));
            SetHealth(70f);
            yield return Press(Key.E);
            Assert.AreEqual(95f, health.CurrentHP, "eating works after Retry");
            Assert.AreEqual(0, tree.AvailableCount);

            yield return StandAndAim(TrunkPoint(), 1.4f, TowardsSpawn());
            Assert.IsNull(interactor.CurrentTarget, "an empty tree offers nothing");

            // ---- area reset restores exactly the starting apples ----
            tree.ResetState();
            Assert.AreEqual(3, tree.AvailableCount);
            Assert.AreEqual(5, tree.GetComponentsInChildren<AppleItem>(true).Length, "no duplicated fruit");
        }

        // ---------- driving the player ----------

        private Vector3 spawnPosition;

        /// <summary>A point on the trunk collider, 0.9 m above its base: below the lowest fruit, so the ray meets the trunk.</summary>
        private Vector3 TrunkPoint()
        {
            Bounds trunk = tree.GetComponent<CapsuleCollider>().bounds;
            return new Vector3(trunk.center.x, trunk.min.y + 0.9f, trunk.center.z);
        }

        /// <summary>Horizontal direction from the trunk towards the player's spawn (the side the tree faces).</summary>
        private Vector3 TowardsSpawn()
        {
            Vector3 d = spawnPosition - tree.transform.position;
            d.y = 0f;
            return d.sqrMagnitude > 0.01f ? d.normalized : tree.transform.forward;
        }

        /// <summary>Horizontal direction from the trunk out through an apple, so the trunk is never in between.</summary>
        private Vector3 Outward(AppleItem apple)
        {
            Vector3 d = apple.transform.position - tree.transform.position;
            d.y = 0f;
            return d.sqrMagnitude > 0.01f ? d.normalized : TowardsSpawn();
        }

        /// <summary>Teleports Gino 'distance' meters from the target along 'side' and makes him look at it.</summary>
        private IEnumerator StandAndAim(Vector3 target, float distance, Vector3 side)
        {
            Vector3 stand = target + side * distance;

            float groundY = stand.y;
            foreach (RaycastHit hit in Physics.RaycastAll(stand + Vector3.up * 30f, Vector3.down, 80f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.transform.IsChildOf(player.transform) || hit.collider.transform.IsChildOf(tree.transform)) continue;
                groundY = hit.point.y;
                break;
            }
            player.Respawn(new Vector3(stand.x, groundY + 0.1f, stand.z));
            yield return null;

            for (int i = 0; i < 4; i++)   // the eye follows the animated head: converge in a few frames
            {
                Vector3 look = target - cam.transform.position;
                Vector3 flat = new Vector3(look.x, 0f, look.z);
                player.transform.rotation = Quaternion.LookRotation(flat.normalized, Vector3.up);
                float pitch = -Mathf.Atan2(look.y, flat.magnitude) * Mathf.Rad2Deg;   // positive looks down
                SetField(player, "verticalRotation", pitch);
                yield return null;
                yield return null;
            }
        }

        private void SetHealth(float hp)
        {
            health.ResetHealth();
            if (hp < health.MaxHP) health.TakeDamage(health.MaxHP - hp);
        }

        private AppleItem FirstAvailableApple()
        {
            foreach (AppleItem apple in tree.Apples)
            {
                if (apple.IsAvailable) return apple;
            }
            Assert.Fail("no apple left on the tree");
            return null;
        }

        /// <summary>Key down for one input update, then up: one press, read by PlayerInputHandler / the UI actions.</summary>
        private IEnumerator Press(Key key)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            yield return null;
        }

        /// <summary>Same as Press, capturing a frame every rendered frame before, during and after (a short video).</summary>
        private IEnumerator PressAndRecord(Key key, string name, string note, float secondsAfter)
        {
            int frame = 0;
            for (int i = 0; i < 8; i++)
            {
                capture.Frame(name, frame++, note + " (before)");
                yield return null;
            }
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            for (int i = 0; i < 2; i++)
            {
                capture.Frame(name, frame++, note);
                yield return null;
            }
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            float end = Time.realtimeSinceStartup + secondsAfter;
            while (Time.realtimeSinceStartup < end)
            {
                capture.Frame(name, frame++, note + " (after)");
                yield return null;
            }
        }

        private IEnumerator SetMouse(bool right, bool left)
        {
            var state = new MouseState();
            if (right) state = state.WithButton(MouseButton.Right);
            if (left) state = state.WithButton(MouseButton.Left);
            InputSystem.QueueStateEvent(mouse, state);
            yield return null;
            yield return null;
        }

        private static IEnumerator WaitUntil(Func<bool> condition, string what)
        {
            float end = Time.realtimeSinceStartup + LoadTimeout;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > end) Assert.Fail("Timed out waiting for: " + what);
                yield return null;
            }
        }

        // ---------- HUD reading ----------

        private string PromptText()
        {
            TMP_Text label = GetField<TMP_Text>(hud, "promptLabel");
            Assert.IsNotNull(label, "HUD promptLabel assigned");
            return label.enabled && label.gameObject.activeInHierarchy && !string.IsNullOrEmpty(label.text) ? label.text : null;
        }

        private string HudHealthText() => GetField<TMP_Text>(hud, "healthLabel")?.text;
        private string HudMessageText() => GetField<TMP_Text>(hud, "messageLabel")?.text;

        private static T GetField<T>(object owner, string name) where T : class
        {
            FieldInfo field = owner.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, owner.GetType().Name + "." + name);
            return field.GetValue(owner) as T;
        }

        private static void SetField(object owner, string name, object value)
        {
            FieldInfo field = owner.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, owner.GetType().Name + "." + name);
            field.SetValue(owner, value);
        }

        // ---------- captures ----------

        /// <summary>
        /// Renders the game camera with the HUD into PNG files (overlay canvases are switched to the camera for the
        /// moment of the capture), with a banner saying the input comes from the automated test.
        /// </summary>
        private sealed class Capture : IDisposable
        {
            private const int Width = 1280, Height = 720;
            private readonly Camera camera;
            private readonly string folder;
            private readonly GameObject banner;
            private readonly Text bannerText;
            private RenderTexture target;
            private Texture2D readback;

            public bool Available { get; }

            public Capture(Camera camera)
            {
                this.camera = camera;
                folder = Path.Combine(Directory.GetCurrentDirectory(), "TestResults", "Captures");
                Available = camera != null && SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;
                if (!Available) return;

                Directory.CreateDirectory(folder);
                target = new RenderTexture(Width, Height, 24);
                readback = new Texture2D(Width, Height, TextureFormat.RGB24, false);

                banner = new GameObject("AutomatedTestBanner", typeof(Canvas));
                var canvas = banner.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 1000;
                var textGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
                textGO.transform.SetParent(banner.transform, false);
                var rect = (RectTransform)textGO.transform;
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.sizeDelta = new Vector2(0f, 40f);
                bannerText = textGO.GetComponent<Text>();
                bannerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                bannerText.fontSize = 22;
                bannerText.alignment = TextAnchor.MiddleCenter;
                bannerText.color = Color.yellow;
            }

            public void Shot(string name, string note)
            {
                if (!Available) return;
                Save(Path.Combine(folder, name + ".png"), note);
            }

            /// <summary>One frame of a short recording: TestResults/Captures/&lt;name&gt;/frame_000.png ...</summary>
            public void Frame(string name, int index, string note)
            {
                if (!Available) return;
                string dir = Path.Combine(folder, name);
                Directory.CreateDirectory(dir);
                Save(Path.Combine(dir, "frame_" + index.ToString("000") + ".png"), note);
            }

            private void Save(string path, string note)
            {
                bannerText.text = "AUTOMATED TEST INPUT: " + note;
                var overlays = new List<Canvas>();
                foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                {
                    if (c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay && c.gameObject.activeInHierarchy) overlays.Add(c);
                }
                foreach (Canvas c in overlays)
                {
                    c.renderMode = RenderMode.ScreenSpaceCamera;
                    c.worldCamera = camera;
                    c.planeDistance = camera.nearClipPlane + 0.05f;
                }
                Canvas.ForceUpdateCanvases();

                RenderTexture previousTarget = camera.targetTexture, previousActive = RenderTexture.active;
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                readback.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                readback.Apply();
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;

                foreach (Canvas c in overlays) c.renderMode = RenderMode.ScreenSpaceOverlay;
                File.WriteAllBytes(path, readback.EncodeToPNG());
            }

            public void Dispose()
            {
                if (banner != null) Object.Destroy(banner);
                if (target != null) target.Release();
            }

        }
    }
}
