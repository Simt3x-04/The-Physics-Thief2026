
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using PhysicsThief.Core;
using PhysicsThief.Enemies;

public class SpearBeetleTests
{
    // Assets/Tests/Resources/Enemies/SpearBeetlePrefab.prefab
    private const string BeetlePrefabPath = "Enemies/SpearBeetlePrefab";
    private const float PlayerDistance = 5f;
    private const float DamageTolerance = 0.001f;

    private GameObject floor;
    private GameObject beetleGo;
    private GameObject player;

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (floor != null) Object.Destroy(floor);
        if (beetleGo != null) Object.Destroy(beetleGo);
        if (player != null) Object.Destroy(player);

        foreach (var p in Object.FindObjectsByType<Projectile>(
            FindObjectsSortMode.None))
        {
            Object.Destroy(p.gameObject);
        }

        yield return null;
    }

    private GameObject SpawnBeetle(Vector3 position)
    {
        var prefab = Resources.Load<GameObject>(BeetlePrefabPath);

        Assert.IsNotNull(prefab,
            "Prefab not found: expected Assets/Tests/Resources/Enemies/SpearBeetlePrefab.prefab");

        return Object.Instantiate(
            prefab, position, Quaternion.identity);
    }

    private static float ExpectedLoss(
        SpearBeetleData data,
        float rawDamage,
        float shieldReduction)
    {
        // Shield reduction first, then EnemyData damage reduction.
        return data.ApplyDamageReduction(
            rawDamage * (1f - shieldReduction));
    }

    // ------------------------------------------------------------------
    // 1. Melee: the beetle chases the player and hits him with its spear.
    // The damage is applied to the real HealthComponent.
    // ------------------------------------------------------------------

    [UnityTest]
    public IEnumerator SpearBeetle_ChasesThePlayer_AndHitsItInMelee()
    {
        // ---------- Setup: floor ----------

        floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.transform.localScale = new Vector3(5f, 1f, 5f);

        // ---------- Setup: fake player ----------

        player = new GameObject("FakePlayer");
        player.tag = "Player";
        player.transform.position =
            new Vector3(0f, 0f, PlayerDistance);

        var torso = GameObject.CreatePrimitive(PrimitiveType.Cube);
        torso.name = "FakePlayerBody";
        torso.transform.SetParent(player.transform, false);
        torso.transform.localPosition = new Vector3(0f, 1f, 0f);
        torso.transform.localScale = new Vector3(1f, 2f, 1f);

        // Use the real HealthComponent.
        var playerHealth = player.AddComponent<HealthComponent>();
        playerHealth.Initialize(100f);

        // Adapter that forwards IDamageable calls to HealthComponent.
        var target = player.AddComponent<HealthDamageableTestTarget>();

        // ---------- Setup: real beetle ----------

        beetleGo = SpawnBeetle(new Vector3(0f, 0.1f, 0f));
        Physics.SyncTransforms();

        var beetle = beetleGo.GetComponent<SpearBeetle>();

        Assert.IsNotNull(beetle,
            "The prefab has no SpearBeetle component");

        var data = beetle.Data as SpearBeetleData;

        Assert.IsNotNull(data,
            "The beetle's Data is missing or is not a SpearBeetleData");

        // ---------- Sanity checks ----------

        Assert.Greater(data.MoveSpeed, 0f,
            "Move Speed is 0: the beetle cannot chase the player");

        Assert.Greater(data.AttackRange, 0f,
            "Attack Range is 0: the beetle cannot hit the player");

        Assert.Greater(data.AttackDamage, 0f,
            "Attack Damage is 0");

        Assert.GreaterOrEqual(data.DetectionRange, PlayerDistance,
            $"Detection Range must be >= {PlayerDistance} " +
            "so the beetle can see the player");

        // Make sure the player survives the first attack.
        playerHealth.Initialize(
            Mathf.Max(100f, data.AttackDamage + 10f));

        float initialHP = playerHealth.CurrentHP;

        float startDistance = Vector3.Distance(
            beetleGo.transform.position,
            player.transform.position);

        // ---------- Wait for the first real hit ----------

        float end = Time.realtimeSinceStartup + 8f;

        while (playerHealth.CurrentHP >= initialHP &&
               Time.realtimeSinceStartup < end)
        {
            yield return null;
        }

        float finalDistance = Vector3.Distance(
            beetleGo.transform.position,
            player.transform.position);

        // Stop the AI to prevent further attacks.
        beetle.enabled = false;

        float actualDamage =
            initialHP - playerHealth.CurrentHP;

        // ---------- Verify that the player was hit ----------

        Assert.GreaterOrEqual(target.Hits, 1,
            $"The beetle never hit the player in 8 seconds. " +
            $"Distance went from {startDistance:F2} " +
            $"to {finalDistance:F2} " +
            $"(Attack Range: {data.AttackRange}). " +
            "Check Detection Range, View Angle, Turn Speed " +
            "and Obstacle Mask.");

        Assert.Greater(actualDamage, 0f,
            "The player's HealthComponent did not receive damage");

        // ---------- Verify movement ----------

        Assert.Less(finalDistance, startDistance,
            "The beetle should have moved towards the player");

        Assert.LessOrEqual(
            finalDistance,
            data.AttackRange + 0.5f,
            "The beetle hit the player from farther away " +
            "than its Attack Range");

        // ---------- Verify actual HP loss ----------

        float expectedDamage =
            target.Hits * data.AttackDamage;

        Assert.AreEqual(
            expectedDamage,
            actualDamage,
            DamageTolerance,
            "The player did not lose the expected amount of HP");

        Assert.AreEqual(
            initialHP - expectedDamage,
            playerHealth.CurrentHP,
            DamageTolerance,
            "The player's remaining HP is incorrect");

        Assert.IsFalse(playerHealth.IsDead,
            "The player should survive the attack");

        // ---------- Verify that no projectiles were fired ----------

        foreach (var p in Object.FindObjectsByType<Projectile>(
            FindObjectsSortMode.None))
        {
            Assert.AreNotEqual(beetleGo, p.Owner,
                "The beetle is a melee enemy and should not fire projectiles");
        }
    }

    // ------------------------------------------------------------------
    // 2. Shield: the beetle receives reduced damage when its shield
    // is intact, and increased damage when its shield is shattered.
    // The actual HP loss is measured through the real HealthComponent.
    // ------------------------------------------------------------------

    [UnityTest]
    public IEnumerator SpearBeetle_Shield_ReducesActualHealthDamage()
    {
        // ---------- Setup: real beetle ----------

        beetleGo = SpawnBeetle(Vector3.zero);

        var beetle = beetleGo.GetComponent<SpearBeetle>();

        Assert.IsNotNull(beetle,
            "The prefab has no SpearBeetle component");

        var data = beetle.Data as SpearBeetleData;

        Assert.IsNotNull(data,
            "The beetle's Data is missing or is not a SpearBeetleData");

        // Retrieve the real HealthComponent already on the prefab.
        var health = beetleGo.GetComponent<HealthComponent>();

        Assert.IsNotNull(health,
            "The beetle prefab has no HealthComponent");

        // Disable AI: only the damage logic is being tested.
        beetle.enabled = false;

        // ---------- Sanity checks ----------

        Assert.Greater(data.ApplyDamageReduction(10f), 0f,
            "EnemyData.ApplyDamageReduction returns 0: " +
            "the test would pass without checking anything");

        Assert.Less(data.ArmorDamageReduction, 1f,
            "Armor Damage Reduction of 1 makes the shield absorb everything");

        Assert.Greater(
            data.ArmorDamageReduction,
            data.ShatteredDamageReduction,
            "The intact shield should reduce more damage than the broken one");

        const float hit = 10f;

        // Calculate expected damage for both shield conditions.
        float expectedIntactDamage = ExpectedLoss(
            data,
            hit,
            data.ArmorDamageReduction);

        float expectedShatteredDamage = ExpectedLoss(
            data,
            hit,
            data.ShatteredDamageReduction);

        // Ensure that the beetle survives both attacks.
        health.Initialize(Mathf.Max(
            data.MaxHP,
            expectedIntactDamage + expectedShatteredDamage + 10f));

        float initialHP = health.CurrentHP;

        // ---------- 1. Intact shield ----------

        Assert.IsFalse(beetle.IsArmorShattered,
            "The shield should start intact");

        float hpBeforeIntactHit = health.CurrentHP;

        beetle.TakeDamage(hit);

        float intactDamage =
            hpBeforeIntactHit - health.CurrentHP;

        Assert.AreEqual(
            expectedIntactDamage,
            intactDamage,
            DamageTolerance,
            "With the shield intact, the HealthComponent " +
            "should receive damage reduced by the shield");

        Assert.AreEqual(
            initialHP - expectedIntactDamage,
            health.CurrentHP,
            DamageTolerance,
            "Incorrect remaining HP after the intact-shield hit");

        Assert.IsFalse(health.IsDead,
            "The beetle should survive the first hit");

        // ---------- 2. Shatter the shield ----------

        beetle.ShatterArmor();

        Assert.IsTrue(beetle.IsArmorShattered,
            "ShatterArmor should break the shield");

        // ---------- 3. Broken shield ----------

        float hpBeforeShatteredHit = health.CurrentHP;

        beetle.TakeDamage(hit);

        float shatteredDamage =
            hpBeforeShatteredHit - health.CurrentHP;

        Assert.AreEqual(
            expectedShatteredDamage,
            shatteredDamage,
            DamageTolerance,
            "With the shield broken, the HealthComponent " +
            "should receive the broken-shield damage");

        // ---------- 4. Compare both hits ----------

        Assert.Greater(
            shatteredDamage,
            intactDamage,
            "The beetle should receive more damage " +
            "after its shield is shattered");

        float expectedFinalHP =
            initialHP
            - expectedIntactDamage
            - expectedShatteredDamage;

        Assert.AreEqual(
            expectedFinalHP,
            health.CurrentHP,
            DamageTolerance,
            "Incorrect final HP after both attacks");

        Assert.IsFalse(health.IsDead,
            "The beetle should survive both hits");

        yield return null;
    }
}

// ------------------------------------------------------------------
// Test helper: connects IDamageable to the real HealthComponent.
// This component does not simulate damage: it forwards every hit
// to the actual health system.
// ------------------------------------------------------------------

[RequireComponent(typeof(HealthComponent))]
public class HealthDamageableTestTarget : MonoBehaviour, IDamageable
{
    private HealthComponent health;

    public int Hits { get; private set; }

    private void Awake()
    {
        health = GetComponent<HealthComponent>();
    }

    public void TakeDamage(float amount)
    {
        Hits++;
        health.TakeDamage(amount);
    }
}
