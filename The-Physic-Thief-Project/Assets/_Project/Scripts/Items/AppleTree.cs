using System;
using System.Collections.Generic;
using UnityEngine;
using PhysicsThief.Core;
using PhysicsThief.Player;

namespace PhysicsThief.Items
{
    /// <summary>
    /// A tree with apples (GDD 3.6). It owns which fruit positions still hold an apple (AppleTreeModel).
    /// - Aim at an apple and press E to eat that apple.
    /// - Aiming at the tree itself (the trunk collider) also works: it eats the available apple closest to the player.
    ///   This is the harvesting point that is always reachable from the ground.
    /// - Apples are finite: an eaten apple stays gone until ResetState() (IResettable), which the area reset calls.
    ///   Reloading the scene also brings them back, since nothing is saved.
    /// </summary>
    [DisallowMultipleComponent]
    public class AppleTree : MonoBehaviour, IInteractable, IResettable
    {
        [Tooltip("Fruit positions on this tree. If empty, the AppleItems among the children are used.")]
        [SerializeField] private List<AppleItem> apples = new List<AppleItem>();

        [Tooltip("Apples on the tree at the start and after each reset: the first N fruit positions of the list.")]
        [SerializeField, Min(0)] private int startingApples = 3;

        private AppleTreeModel model;
        private readonly Dictionary<int, string> promptCache = new Dictionary<int, string>();

        /// <summary>Fired whenever the number of apples changes (eaten or reset).</summary>
        public event Action<AppleTree> OnApplesChanged;

        public int AvailableCount
        {
            get
            {
                EnsureInitialized();
                return model.AvailableCount;
            }
        }

        public int Capacity
        {
            get
            {
                EnsureInitialized();
                return model.Capacity;
            }
        }

        public int StartingApples => startingApples;

        public IReadOnlyList<AppleItem> Apples
        {
            get
            {
                EnsureInitialized();
                return apples;
            }
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnValidate()
        {
            if (apples.Count > 0 && startingApples > apples.Count)
                Debug.LogWarning($"[AppleTree] '{name}': {startingApples} starting apples but only {apples.Count} fruit positions.", this);
        }

        /// <summary>Registers the fruit and applies the starting apples. Safe to call many times (Awake, or an apple asking first).</summary>
        public void Initialize()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (model != null) return;

            apples.RemoveAll(a => a == null);
            if (apples.Count == 0) apples.AddRange(GetComponentsInChildren<AppleItem>(true));

            model = new AppleTreeModel(apples.Count, startingApples);
            for (int i = 0; i < apples.Count; i++) apples[i].AttachTo(this);
            ApplyModel();
        }

        // ---------- fruit state ----------

        /// <summary>Called by an apple of this tree after it has been eaten.</summary>
        public void NotifyConsumed(AppleItem apple)
        {
            EnsureInitialized();

            int index = apples.IndexOf(apple);
            if (!model.TryConsume(index)) return;   // not ours, or already eaten

            apple.SetAvailable(false, true);
            OnApplesChanged?.Invoke(this);
        }

        /// <summary>Area reset: exactly the starting apples come back (same objects, no duplicates).</summary>
        public void ResetState()
        {
            EnsureInitialized();
            model.Restore();
            ApplyModel();
            OnApplesChanged?.Invoke(this);
        }

        private void ApplyModel()
        {
            for (int i = 0; i < apples.Count; i++)
            {
                apples[i].SetAvailable(model.IsAvailable(i), false);
            }
        }

        // ---------- interaction on the tree itself (harvesting point) ----------

        public bool CanInteract(PlayerController player)
        {
            return AvailableCount > 0;
        }

        public string GetPrompt(PlayerController player)
        {
            AppleItem next = FindClosestAvailable(player);
            if (next == null) return null;

            int key = model.AvailableCount * 1000 + Mathf.RoundToInt(next.HealAmount);
            if (!promptCache.TryGetValue(key, out string text))
            {
                text = AppleEatRules.Prompt(next.HealAmount) + "  ·  " + model.AvailableCount + " left";
                promptCache[key] = text;
            }
            return text;
        }

        public string Interact(PlayerController player)
        {
            AppleItem next = FindClosestAvailable(player);
            return next != null ? next.Interact(player) : null;
        }

        /// <summary>The available apple nearest to the player (list order breaks ties, so the choice is predictable).</summary>
        public AppleItem FindClosestAvailable(PlayerController player)
        {
            EnsureInitialized();

            Vector3 from = player != null ? player.transform.position : transform.position;
            AppleItem best = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < apples.Count; i++)
            {
                if (!model.IsAvailable(i)) continue;

                float distance = (apples[i].transform.position - from).sqrMagnitude;
                if (distance < bestDistance)
                {
                    best = apples[i];
                    bestDistance = distance;
                }
            }
            return best;
        }
    }
}
