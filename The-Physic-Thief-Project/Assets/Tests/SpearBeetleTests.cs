using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using PhysicsThief.Core;
using PhysicsThief.Enemies;
using System.Collections.Generic;
using System.Reflection;

/// <summary>HealthComponent che registra il danno che gli viene chiesto di applicare, senza applicarlo.</summary>
public class DamageRecordingHealthComponent : HealthComponent
{
    public readonly List<float> DamageCalls = new List<float>();

    public override void TakeDamage(float amount)
    {
        DamageCalls.Add(amount);
    }
}

public class SpearBeetleTests
{
    // Assets/Tests/Resources/Enemies/SpearBeetlePrefab.prefab
    private const string BeetlePrefabPath = "Enemies/SpearBeetlePrefab";
    private const float PlayerDistance = 5f;

    private GameObject floor;
    private GameObject beetleGo;
    private GameObject player;

    [TearDown]
    public void TearDown()
    {
        if (floor != null) Object.Destroy(floor);
        if (beetleGo != null) Object.Destroy(beetleGo);
        if (player != null) Object.Destroy(player);

        foreach (var p in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))
            Object.Destroy(p.gameObject);
    }

    private GameObject SpawnBeetle(Vector3 position)
    {
        var prefab = Resources.Load<GameObject>(BeetlePrefabPath);
        Assert.IsNotNull(prefab,
            "Prefab not found: expected Assets/Tests/Resources/Enemies/SpearBeetlePrefab.prefab");
        return Object.Instantiate(prefab, position, Quaternion.identity); // faces +Z
    }

    private static float ExpectedLoss(SpearBeetleData data, float rawDamage, float shieldReduction)
    {
        // Same path as EnemyBase.TakeDamage: shield reduction first, then EnemyData.ApplyDamageReduction
        return data.ApplyDamageReduction(rawDamage * (1f - shieldReduction));
    }

    // ------------------------------------------------------------------
    // 1. Melee: the beetle chases the player and hits him with its spear
    // ------------------------------------------------------------------
    [UnityTest]
    public IEnumerator SpearBeetle_ChasesThePlayer_AndHitsItInMelee()
    {
        // Floor
        floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.transform.localScale = new Vector3(5f, 1f, 5f);

        // Fake player: pivot at the feet like a real character, in front of the beetle (+Z)
        player = new GameObject("FakePlayer");
        player.tag = "Player";
        player.transform.position = new Vector3(0f, 0f, PlayerDistance);

        var torso = GameObject.CreatePrimitive(PrimitiveType.Cube);
        torso.name = "FakePlayerBody";
        torso.transform.SetParent(player.transform, false);
        torso.transform.localPosition = new Vector3(0f, 1f, 0f);
        torso.transform.localScale = new Vector3(1f, 2f, 1f);

        var dummy = player.AddComponent<DummyTarget>();

        // Real beetle: from here on its AI runs for real (Roam -> spots the player -> CombatState)
        beetleGo = SpawnBeetle(new Vector3(0f, 0.1f, 0f));
        Physics.SyncTransforms();

        var beetle = beetleGo.GetComponent<SpearBeetle>();
        Assert.IsNotNull(beetle, "The prefab has no SpearBeetle component");

        var data = beetle.Data as SpearBeetleData;
        Assert.IsNotNull(data, "The beetle's Data is missing or is not a SpearBeetleData");

        // Sanity checks on the asset: all these default to 0 in EnemyData
        Assert.Greater(data.MoveSpeed, 0f, "Move Speed is 0 in the SpearBeetleData asset: the beetle cannot chase");
        Assert.Greater(data.AttackRange, 0f, "Attack Range is 0 in the SpearBeetleData asset: the beetle cannot hit");
        Assert.Greater(data.AttackDamage, 0f, "Attack Damage is 0 in the SpearBeetleData asset");
        Assert.GreaterOrEqual(data.DetectionRange, PlayerDistance,
            $"Detection Range must be >= {PlayerDistance} so the beetle can see the player");

        float startDistance = Vector3.Distance(beetleGo.transform.position, player.transform.position);

        // Wait for the first hit
        float end = Time.time + 8f;
        while (dummy.Hits == 0 && Time.time < end)
            yield return null;

        float finalDistance = Vector3.Distance(beetleGo.transform.position, player.transform.position);

        Assert.GreaterOrEqual(dummy.Hits, 1,
            $"The beetle never hit the player in 8 s. Distance went from {startDistance:F2} to {finalDistance:F2} " +
            $"(Attack Range {data.AttackRange}). If it did not get closer: check Detection Range, View Angle, " +
            "Turn Speed and that the Obstacle Mask does not include the player's layer. " +
            "If it stopped short: Attack Range is smaller than the gap between the two colliders.");

        Assert.Less(finalDistance, startDistance, "The beetle should have moved towards the player");
        Assert.LessOrEqual(finalDistance, data.AttackRange + 0.5f,
            "The beetle hit the player from farther away than its Attack Range");
        Assert.AreEqual(dummy.Hits * data.AttackDamage, dummy.TotalDamage, 0.001f,
            "Each hit should deal the beetle's Attack Damage");

        // It is a melee enemy now: no projectile may belong to it
        foreach (var p in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))
            Assert.AreNotEqual(beetleGo, p.Owner, "The beetle should not fire projectiles anymore");
    }

    // ------------------------------------------------------------------
// 2. Shield: the beetle passes the REDUCED damage to its HealthComponent
// ------------------------------------------------------------------
[UnityTest]
public IEnumerator SpearBeetle_Shield_PassesReducedDamage_ToTheHealthComponent()
{
    beetleGo = SpawnBeetle(Vector3.zero);

    var beetle = beetleGo.GetComponent<SpearBeetle>();
    Assert.IsNotNull(beetle, "The prefab has no SpearBeetle component");

    var data = beetle.Data as SpearBeetleData;
    Assert.IsNotNull(data, "The beetle's Data is missing or is not a SpearBeetleData");

    // The beetle talks to this HealthComponent, which only records the damage it is asked to apply
    var recorder = beetleGo.AddComponent<DamageRecordingHealthComponent>();
    var healthField = typeof(EnemyBase).GetField("healthComp", BindingFlags.NonPublic | BindingFlags.Instance);
    Assert.IsNotNull(healthField, "EnemyBase has no field named 'healthComp' (renamed? update the test)");
    healthField.SetValue(beetle, recorder);

    beetle.enabled = false; // no AI: only the damage logic is under test

    // Sanity checks on the asset
    Assert.Greater(data.ApplyDamageReduction(10f), 0f,
        "EnemyData.ApplyDamageReduction returns 0: the test would pass without checking anything");
    Assert.Less(data.ArmorDamageReduction, 1f, "Armor Damage Reduction of 1 makes the shield absorb everything");
    Assert.Greater(data.ArmorDamageReduction, data.ShatteredDamageReduction,
        "The intact shield should reduce more damage than the broken one");

    const float hit = 10f;
    Assert.IsFalse(beetle.IsArmorShattered, "The shield should start intact");

    // 1. Intact shield: the HealthComponent receives the reduced damage
    beetle.TakeDamage(hit);
    Assert.AreEqual(1, recorder.DamageCalls.Count,
        "The beetle should call HealthComponent.TakeDamage exactly once per hit");
    Assert.AreEqual(ExpectedLoss(data, hit, data.ArmorDamageReduction), recorder.DamageCalls[0], 0.001f,
        "With the shield intact the HealthComponent should receive the damage reduced by the shield");

    // 2. Broken shield: the HealthComponent receives a different amount
    beetle.ShatterArmor();
    Assert.IsTrue(beetle.IsArmorShattered, "ShatterArmor should break the shield");

    beetle.TakeDamage(hit);
    Assert.AreEqual(2, recorder.DamageCalls.Count, "The second hit should call HealthComponent.TakeDamage again");
    Assert.AreEqual(ExpectedLoss(data, hit, data.ShatteredDamageReduction), recorder.DamageCalls[1], 0.001f,
        "With the shield broken the HealthComponent should receive the broken-shield damage");

    yield return null;
}
}