using System.Collections;
using UnityEngine;
using PhysicsThief.Player;

namespace PhysicsThief.Items
{
    /// <summary>
    /// Apple from trees (E key): restores 25 HP (GDD 3.6 and 4.2).
    /// Usually a child of an AppleTree, which owns the "is there an apple here" state and restores the fruit on an
    /// area reset. A loose apple (no tree above it) keeps its own state.
    /// An eaten apple is never destroyed: its colliders switch off at once (so it cannot be picked twice, not even in
    /// the same frame) and its visual shrinks and hides. A reset shows the same object again, so fruit never duplicates.
    /// </summary>
    public class AppleItem : PowerUpItem, IInteractable
    {
        [SerializeField] private float healAmount = AppleEatRules.DefaultHealAmount;

        [Header("Visual")]
        [Tooltip("Child holding the apple meshes, hidden while the apple is eaten. If empty, the renderers below this object are hidden instead.")]
        [SerializeField] private Transform visual;
        [Tooltip("Seconds of the shrink 'pop' when the apple is eaten.")]
        [SerializeField] private float popDuration = 0.15f;

        private AppleTree tree;
        private bool consumed;
        private bool initialized;
        private Collider[] colliders;
        private Renderer[] renderers;
        private Vector3 visualScale = Vector3.one;
        private Coroutine popRoutine;
        private string prompt;

        public float HealAmount => healAmount;
        public bool IsAvailable => !consumed;

        /// <summary>The tree this apple hangs on (null for a loose apple).</summary>
        public AppleTree Tree
        {
            get
            {
                EnsureInitialized();
                return tree;
            }
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (initialized) return;
            initialized = true;

            colliders = GetComponentsInChildren<Collider>(true);
            renderers = GetComponentsInChildren<Renderer>(true);
            if (visual == transform) visual = null;   // hiding the root would also stop the pop coroutine
            if (visual != null) visualScale = visual.localScale;
            prompt = AppleEatRules.Prompt(healAmount);

            // Last, once this apple is ready: let the parent tree register its fruit (it calls AttachTo on its own apples).
            // An AppleItem that the tree does not list stays a loose apple.
            AppleTree parentTree = GetComponentInParent<AppleTree>(true);
            if (parentTree != null) parentTree.Initialize();
        }

        /// <summary>Called by the tree when it registers its fruit.</summary>
        public void AttachTo(AppleTree owner)
        {
            EnsureInitialized();
            tree = owner;
        }

        // ---------- interaction (E) ----------

        public bool CanInteract(PlayerController player)
        {
            return IsAvailable;
        }

        public string GetPrompt(PlayerController player)
        {
            EnsureInitialized();
            return prompt;
        }

        public string Interact(PlayerController player)
        {
            AppleEatResult result = Collect(player, out float healed);
            return AppleEatRules.ToMessage(result, healed);
        }

        public override void OnCollect(PlayerController player)
        {
            Collect(player, out _);
        }

        /// <summary>Eats the apple if the player can: heals through PlayerController.EatApple, then removes the fruit.</summary>
        public AppleEatResult Collect(PlayerController player, out float healedAmount)
        {
            healedAmount = 0f;
            EnsureInitialized();

            if (consumed) return AppleEatResult.AppleUnavailable;
            if (player == null) return AppleEatResult.NoHealthComponent;

            AppleEatResult result = player.EatApple(this, out healedAmount);
            if (result == AppleEatResult.Eaten) MarkConsumed();
            return result;
        }

        /// <summary>Apples are picked with E only, never by walking into them.</summary>
        protected override void OnTriggerEnter(Collider other) { }

        private void MarkConsumed()
        {
            if (tree != null) tree.NotifyConsumed(this);   // the tree updates its count and hides this apple
            else SetAvailable(false, true);
        }

        // ---------- state and visuals ----------

        /// <summary>
        /// Shows or hides the apple. Hiding switches the colliders off immediately; with 'animate' the visual shrinks
        /// first (Play Mode only). Called by the tree, or by the apple itself when it has no tree.
        /// </summary>
        public void SetAvailable(bool available, bool animate)
        {
            EnsureInitialized();
            consumed = !available;

            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null) colliders[i].enabled = available;
            }

            if (popRoutine != null)
            {
                StopCoroutine(popRoutine);
                popRoutine = null;
            }

            if (available)
            {
                ShowVisual(true);
                return;
            }

            if (animate && visual != null && popDuration > 0f && Application.isPlaying && isActiveAndEnabled)
                popRoutine = StartCoroutine(PopRoutine());
            else
                ShowVisual(false);
        }

        private IEnumerator PopRoutine()
        {
            for (float t = 0f; t < popDuration; t += Time.deltaTime)
            {
                visual.localScale = visualScale * (1f - t / popDuration);
                yield return null;
            }
            ShowVisual(false);
            popRoutine = null;
        }

        private void ShowVisual(bool show)
        {
            if (visual != null)
            {
                visual.localScale = visualScale;
                visual.gameObject.SetActive(show);
                return;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null) renderers[i].enabled = show;
            }
        }
    }
}
