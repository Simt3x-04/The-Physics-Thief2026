using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using PhysicsThief.Core;
using PhysicsThief.Items;
using PhysicsThief.Player;

namespace PhysicsThief.Tests
{
    /// <summary>
    /// Apple trees and healing (GDD 3.6 and 4.2): rules, tree state, the path apple -> PlayerController -> HealthComponent,
    /// and the E-ray target choice. Edit Mode: Awake/Start do not run, the scripts initialize lazily.
    /// </summary>
    [TestFixture]
    public class AppleHealingTests
    {
        // Far above anything an open scene could contain, so the test rays only meet the test objects.
        private static readonly Vector3 Origin = new Vector3(0f, 5000f, 0f);

        private readonly List<GameObject> created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in created)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            created.Clear();
        }

        // ---------- pure rules ----------

        [Test]
        public void Rules_DamagedPlayer_IsHealedBy25()
        {
            Assert.AreEqual(AppleEatResult.Eaten, AppleEatRules.Evaluate(true, true, false, 60f, 100f));
            Assert.AreEqual(25f, AppleEatRules.HealedAmount(60f, 100f, 25f));
        }

        [Test]
        public void Rules_HealingNearMax_StopsAtMax()
        {
            Assert.AreEqual(10f, AppleEatRules.HealedAmount(90f, 100f, 25f));
            Assert.AreEqual(0f, AppleEatRules.HealedAmount(100f, 100f, 25f));
        }

        [Test]
        public void Rules_RefuseFullDeadInactiveAndMissing()
        {
            Assert.AreEqual(AppleEatResult.HealthFull, AppleEatRules.Evaluate(true, true, false, 100f, 100f));
            Assert.AreEqual(AppleEatResult.PlayerDead, AppleEatRules.Evaluate(true, true, true, 0f, 100f));
            Assert.AreEqual(AppleEatResult.GameplayInactive, AppleEatRules.Evaluate(true, false, false, 50f, 100f));
            Assert.AreEqual(AppleEatResult.AppleUnavailable, AppleEatRules.Evaluate(false, true, false, 50f, 100f));
        }

        [Test]
        public void Rules_PromptAndMessages()
        {
            Assert.AreEqual("E — Eat apple (+25 HP)", AppleEatRules.Prompt(25f));
            Assert.AreEqual("Ate an apple: +10 HP", AppleEatRules.ToMessage(AppleEatResult.Eaten, 10f));
            Assert.AreEqual("Health is already full", AppleEatRules.ToMessage(AppleEatResult.HealthFull, 0f));
            Assert.IsNull(AppleEatRules.ToMessage(AppleEatResult.GameplayInactive, 0f));
        }

        [Test]
        public void TreeModel_StartsWithStartingApples_AndConsumesOnce()
        {
            var model = new AppleTreeModel(5, 3);

            Assert.AreEqual(3, model.AvailableCount);
            Assert.IsTrue(model.IsAvailable(2));
            Assert.IsFalse(model.IsAvailable(3), "positions after the starting count start empty");

            Assert.IsTrue(model.TryConsume(1));
            Assert.IsFalse(model.TryConsume(1), "an eaten apple cannot be eaten again");
            Assert.IsFalse(model.TryConsume(4), "an empty position has nothing to eat");
            Assert.IsFalse(model.TryConsume(-1));
            Assert.AreEqual(2, model.AvailableCount);
        }

        [Test]
        public void TreeModel_Restore_GivesBackExactlyTheStartingApples()
        {
            var model = new AppleTreeModel(5, 3);
            model.TryConsume(0);
            model.TryConsume(2);

            model.Restore();
            model.Restore();   // twice: still no duplicates

            Assert.AreEqual(3, model.AvailableCount);
            Assert.IsTrue(model.IsAvailable(0) && model.IsAvailable(1) && model.IsAvailable(2));
            Assert.IsFalse(model.IsAvailable(3));
            Assert.AreEqual(2, new AppleTreeModel(2, 3).StartingCount, "starting count is clamped to the capacity");
        }

        // ---------- apple -> player -> health ----------

        [Test]
        public void EatingAnApple_At60_Gives85_AndRemovesTheApple()
        {
            PlayerController player = CreatePlayer(60f, out HealthComponent health);
            AppleTree tree = CreateTree(3, 3, Vector3.zero);
            AppleItem apple = tree.Apples[0];

            string message = apple.Interact(player);

            Assert.AreEqual(85f, health.CurrentHP);
            Assert.AreEqual("Ate an apple: +25 HP", message);
            Assert.IsFalse(apple.IsAvailable);
            Assert.IsFalse(apple.GetComponent<Collider>().enabled, "the eaten apple can no longer be hit by the E ray");
            Assert.AreEqual(2, tree.AvailableCount);
        }

        [Test]
        public void EatingAnApple_At90_StopsAt100()
        {
            PlayerController player = CreatePlayer(90f, out HealthComponent health);
            AppleTree tree = CreateTree(3, 3, Vector3.zero);

            tree.Apples[0].Interact(player);

            Assert.AreEqual(100f, health.CurrentHP);
            Assert.AreEqual(2, tree.AvailableCount);
        }

        [Test]
        public void FullHealth_KeepsTheApple()
        {
            PlayerController player = CreatePlayer(100f, out HealthComponent health);
            AppleTree tree = CreateTree(3, 3, Vector3.zero);

            string message = tree.Apples[0].Interact(player);

            Assert.AreEqual("Health is already full", message);
            Assert.IsTrue(tree.Apples[0].IsAvailable);
            Assert.AreEqual(3, tree.AvailableCount);
            Assert.AreEqual(100f, health.CurrentHP);
        }

        [Test]
        public void RepeatedInput_CannotEatTheSameAppleTwice()
        {
            PlayerController player = CreatePlayer(40f, out HealthComponent health);
            AppleTree tree = CreateTree(3, 3, Vector3.zero);
            AppleItem apple = tree.Apples[0];

            apple.Interact(player);
            string second = apple.Interact(player);   // same frame: no Update in between

            Assert.IsNull(second);
            Assert.AreEqual(65f, health.CurrentHP);
            Assert.AreEqual(2, tree.AvailableCount);
        }

        [Test]
        public void HarvestingFromTheTree_EatsExactlyOneApple()
        {
            PlayerController player = CreatePlayer(10f, out HealthComponent health);
            AppleTree tree = CreateTree(3, 3, Vector3.zero);

            tree.Interact(player);

            Assert.AreEqual(35f, health.CurrentHP);
            Assert.AreEqual(2, tree.AvailableCount);
        }

        [Test]
        public void DeadPlayer_CannotEat()
        {
            PlayerController player = CreatePlayer(10f, out HealthComponent health);
            health.Die();
            AppleTree tree = CreateTree(3, 3, Vector3.zero);

            Assert.IsNull(tree.Apples[0].Interact(player));
            Assert.IsTrue(health.IsDead);
            Assert.AreEqual(0f, health.CurrentHP);
            Assert.AreEqual(3, tree.AvailableCount);
        }

        [Test]
        public void PausedPlayer_CannotEat()
        {
            PlayerController player = CreatePlayer(50f, out HealthComponent health);
            player.SetInputEnabled(false);   // what HUDTestIntegration does for Paused, GameOver, Ending, Loading, MainMenu
            AppleTree tree = CreateTree(3, 3, Vector3.zero);

            Assert.AreEqual(AppleEatResult.GameplayInactive, tree.Apples[0].Collect(player, out _));
            Assert.AreEqual(50f, health.CurrentHP);
            Assert.IsTrue(tree.Apples[0].IsAvailable);
        }

        [Test]
        public void AfterRetry_ThePlayerCanEatAgain()
        {
            PlayerController player = CreatePlayer(30f, out HealthComponent health);
            AppleTree tree = CreateTree(3, 3, Vector3.zero);
            health.Die();
            tree.Apples[0].Interact(player);

            health.ResetHealth();   // Retry restores full health ...
            health.TakeDamage(50f); // ... and the player gets hurt again

            tree.Apples[0].Interact(player);
            Assert.AreEqual(75f, health.CurrentHP);
            Assert.AreEqual(2, tree.AvailableCount);
        }

        [Test]
        public void Healing_FiresTheHealthEventTheHudListensTo()
        {
            PlayerController player = CreatePlayer(60f, out HealthComponent health);
            AppleTree tree = CreateTree(1, 1, Vector3.zero);
            float reportedCurrent = -1f, reportedMax = -1f;
            health.OnHealthChanged += (current, max) => { reportedCurrent = current; reportedMax = max; };

            tree.Apples[0].Interact(player);

            Assert.AreEqual(85f, reportedCurrent);
            Assert.AreEqual(100f, reportedMax);
        }

        [Test]
        public void ResetState_RestoresTheStartingApples_WithoutDuplicates()
        {
            PlayerController player = CreatePlayer(5f, out _);
            AppleTree tree = CreateTree(5, 3, Vector3.zero);
            tree.Apples[0].Interact(player);
            tree.Apples[1].Interact(player);
            Assert.AreEqual(1, tree.AvailableCount);

            tree.ResetState();

            Assert.AreEqual(3, tree.AvailableCount);
            Assert.AreEqual(5, tree.GetComponentsInChildren<AppleItem>(true).Length, "no fruit objects were added");
            Assert.IsTrue(tree.Apples[0].IsAvailable && tree.Apples[0].GetComponent<Collider>().enabled);
            Assert.IsFalse(tree.Apples[4].IsAvailable, "positions beyond the starting count stay empty");
        }

        // ---------- E ray (needs Physics queries in Edit Mode) ----------

        [Test]
        public void Ray_PicksTheNearestOfOverlappingApples()
        {
            PlayerInteractor interactor = CreateInteractor();
            AppleTree tree = CreateTree(2, 2, Vector3.zero);
            tree.Apples[0].transform.position = Origin + new Vector3(0f, 0f, 1.5f);
            tree.Apples[1].transform.position = Origin + new Vector3(0f, 0f, 1.7f);   // overlapping trigger spheres
            Physics.SyncTransforms();

            Assert.AreSame(tree.Apples[0], interactor.FindTarget(Origin, Vector3.forward));
        }

        [Test]
        public void Ray_IgnoresEatenApples_OutOfRange_AndBehindWalls()
        {
            PlayerController player = CreatePlayer(50f, out _);
            PlayerInteractor interactor = CreateInteractor();
            AppleTree tree = CreateTree(1, 1, Vector3.zero);
            AppleItem apple = tree.Apples[0];

            apple.transform.position = Origin + new Vector3(0f, 0f, 3.6f);   // beyond the 3 m reach
            Physics.SyncTransforms();
            Assert.IsNull(interactor.FindTarget(Origin, Vector3.forward), "out of range");

            apple.transform.position = Origin + new Vector3(0f, 0f, 2.5f);
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            created.Add(wall);
            wall.transform.position = Origin + new Vector3(0f, 0f, 1.5f);
            Physics.SyncTransforms();
            Assert.IsNull(interactor.FindTarget(Origin, Vector3.forward), "behind a wall");

            Object.DestroyImmediate(wall);
            Physics.SyncTransforms();
            Assert.AreSame(apple, interactor.FindTarget(Origin, Vector3.forward), "visible and in reach");

            apple.Interact(player);
            Physics.SyncTransforms();
            Assert.IsNull(interactor.FindTarget(Origin, Vector3.forward), "already eaten");
        }

        [Test]
        public void Scanner_PasteOnScanOnlyApple_IsRefused_AndKeepsTheSlot()
        {
            var scannerGO = new GameObject("TestScanner");
            created.Add(scannerGO);
            scannerGO.transform.position = Origin + new Vector3(10f, 0f, 0f);
            var inventory = scannerGO.AddComponent<BriefcaseInventory>();
            var scanner = scannerGO.AddComponent<PlayerScanner>();
            SetPrivate(scanner, "rayOrigin", scannerGO.transform);
            SetPrivate(scanner, "inventory", inventory);
            inventory.Model.TryAdd(new PhysicsThief.Properties.PropertyValue(PhysicsThief.Properties.AttributeType.Mass, 600f));

            var appleGO = new GameObject("ScanOnlyApple");
            created.Add(appleGO);
            appleGO.transform.position = Origin + new Vector3(10f, 0f, 2f);
            appleGO.AddComponent<SphereCollider>().isTrigger = true;
            var physical = appleGO.AddComponent<PhysicsThief.Properties.PhysicalObject>();
            SetPrivate(physical, "acceptsPastes", false);
            Physics.SyncTransforms();

            string message = null;
            scanner.OnMessage += m => message = m;

            Assert.AreSame(physical, scanner.ScanTarget(), "the apple can still be scanned");
            Assert.IsFalse(scanner.PasteProperty());
            Assert.IsTrue(inventory.Model.GetSlot(0).HasValue, "the stored Mass is still in its slot");
            Assert.IsFalse(physical.HasOverride(PhysicsThief.Properties.AttributeType.Mass));
            StringAssert.Contains("can only be scanned", message);
        }

        private static void SetPrivate(object owner, string name, object value)
        {
            owner.GetType().GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(owner, value);
        }

        // ---------- helpers ----------

        private PlayerController CreatePlayer(float hp, out HealthComponent health)
        {
            var go = new GameObject("TestPlayer");
            created.Add(go);
            go.transform.position = new Vector3(0f, -50f, 0f);   // away from the test rays
            health = go.AddComponent<HealthComponent>();
            var player = go.AddComponent<PlayerController>();   // also adds the CharacterController
            if (hp < health.MaxHP) health.TakeDamage(health.MaxHP - hp);
            return player;
        }

        private PlayerInteractor CreateInteractor()
        {
            var go = new GameObject("TestInteractor");
            created.Add(go);
            go.transform.position = new Vector3(0f, 50f, 0f);
            return go.AddComponent<PlayerInteractor>();
        }

        private AppleTree CreateTree(int fruitPositions, int startingApples, Vector3 position)
        {
            var root = new GameObject("TestAppleTree");
            created.Add(root);
            root.transform.position = position;

            for (int i = 0; i < fruitPositions; i++)
            {
                var apple = new GameObject("Apple_" + i);
                apple.transform.SetParent(root.transform, false);
                apple.transform.localPosition = new Vector3(i * 2f, 30f, 0f);
                var trigger = apple.AddComponent<SphereCollider>();
                trigger.isTrigger = true;
                trigger.radius = 0.15f;
                apple.AddComponent<AppleItem>();
            }

            var tree = root.AddComponent<AppleTree>();
            SetStartingApples(tree, startingApples);
            return tree;
        }

        /// <summary>startingApples is an Inspector field: set it the way the Inspector would, before the tree initializes.</summary>
        private static void SetStartingApples(AppleTree tree, int count)
        {
            var field = typeof(AppleTree).GetField("startingApples",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(tree, count);
        }
    }
}
