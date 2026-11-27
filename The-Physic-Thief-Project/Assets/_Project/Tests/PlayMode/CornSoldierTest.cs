using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using PhysicsThief.Core;
using PhysicsThief.Enemies;

/// <summary>Fake target that records when it receives each hit.</summary>
public class HitLogTarget : MonoBehaviour, IDamageable
{
    public readonly List<float> HitTimes = new List<float>();
    public float TotalDamage;
    public int Hits => HitTimes.Count;

    public void TakeDamage(float amount)
    {
        HitTimes.Add(Time.time);
        TotalDamage += amount;
    }
}

public class CornSoldierTests
{
    // Assets/_Project/Tests/PlayMode/Resources/Enemies/CornSoldierPrefab.prefab
    private const string SoldierPrefabPath = "Enemies/CornSoldierPrefab";
    private const float PlayerDistance = 6f;
    private const float SettleTime = 0.7f; // the soldier falls onto the floor and stops moving

    private GameObject floor;
    private GameObject soldierGo;
    private GameObject player;

    [TearDown]
    public void TearDown()
    {
        if (floor != null) Object.Destroy(floor);
        if (soldierGo != null) Object.Destroy(soldierGo);
        if (player != null) Object.Destroy(player);

        foreach (var p in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))
            Object.Destroy(p.gameObject);
    }

    // ---------------------------------------------------------------- helpers

    private void BuildScene(out CornSoldier soldier, out CornSoldierData data, out HitLogTarget log)
    {
        floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.transform.localScale = new Vector3(5f, 1f, 5f);

        // Fake player: pivot at the feet like a real character, in front of the soldier (+Z)
        player = new GameObject("FakePlayer");
        player.tag = "Player";
        player.transform.position = new Vector3(0f, 0f, PlayerDistance);

        var torso = GameObject.CreatePrimitive(PrimitiveType.Cube);
        torso.name = "FakePlayerBody";
        torso.transform.SetParent(player.transform, false);
        torso.transform.localPosition = new Vector3(0f, 1f, 0f);
        torso.transform.localScale = new Vector3(1f, 2f, 1f);

        log = player.AddComponent<HitLogTarget>();

        var prefab = Resources.Load<GameObject>(SoldierPrefabPath);
        Assert.IsNotNull(prefab,
            "Prefab not found: expected Assets/_Project/Tests/PlayMode/Resources/Enemies/CornSoldierPrefab.prefab");

        // Spawned a bit above the floor: it falls and settles during SettleTime
        soldierGo = Object.Instantiate(prefab, new Vector3(0f, 1f, 0f), Quaternion.identity); // faces +Z
        Physics.SyncTransforms(); // makes the line-of-sight check see the colliders created this frame

        soldier = soldierGo.GetComponent<CornSoldier>();
        Assert.IsNotNull(soldier, "The prefab has no CornSoldier component");

        data = soldier.Data as CornSoldierData;
        Assert.IsNotNull(data, "The soldier's Data is missing or is not a CornSoldierData");

        // AI off (before its first Update): each test decides when, and if, it starts
        soldier.enabled = false;
    }

    private static void CheckAsset(CornSoldierData data)
    {
        Assert.IsNotNull(data.KernelPrefab,
            "Kernel Prefab is not assigned in the CornSoldierData asset used by the test prefab");
        Assert.GreaterOrEqual(data.KernelsPerBurst, 2,
            "Kernels Per Burst must be >= 2 for this test, to tell a burst from a single shot");
        Assert.Greater(data.TimeBetweenKernels, 0f,
            "Time Between Kernels must be > 0: with 0 the kernels spawn on top of each other and destroy each other");
        Assert.Greater(data.AttackDamage, 0f, "Attack Damage is 0 in the CornSoldierData asset");
        Assert.GreaterOrEqual(data.AttackRange, PlayerDistance,
            $"Attack Range must be >= {PlayerDistance} (the distance to the player)");
    }

    private static Transform GetFirePoint(CornSoldier soldier)
    {
        var field = typeof(CornSoldier).GetField("firePoint", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(field, "CornSoldier has no field named 'firePoint' (renamed? update the test)");

        var firePoint = field.GetValue(soldier) as Transform;
        Assert.IsNotNull(firePoint, "Fire Point is not assigned on Assets/_Project/Tests/PlayMode/Resources/Enemies/CornSoldierPrefab");
        return firePoint;
    }

    /// <summary>Makes the soldier acquire the player as its target. Works while the component is disabled.</summary>
    private static void SpotPlayer(CornSoldier soldier, CornSoldierData data)
    {
        Assert.GreaterOrEqual(data.DetectionRange, PlayerDistance,
            $"Detection Range must be >= {PlayerDistance}");
        Assert.IsTrue(soldier.TrySpotTarget(),
            "The soldier did not spot the player (check Detection Range, View Angle, " +
            "and that the Obstacle Mask does not include the player's layer)");
    }

    // ------------------------------------------------------------------
    // 1. A burst: KernelsPerBurst kernels, one after the other, all hit the player
    // ------------------------------------------------------------------
    [UnityTest]
    public IEnumerator CornSoldier_FiresABurst_OfSpacedKernels_ThatHitThePlayer()
    {
        BuildScene(out var soldier, out var data, out var log);
        CheckAsset(data);

        yield return new WaitForSeconds(SettleTime); // let the soldier land before it shoots

        SpotPlayer(soldier, data);
        Transform firePoint = GetFirePoint(soldier);
        Assert.Greater(firePoint.position.y, 0.2f,
            $"The Fire Point is at y={firePoint.position.y:F2}, almost on the floor: the kernels would hit the ground. " +
            "Move the Fire Point higher on the soldier prefab");

        float start = Time.time;
        Assert.IsTrue(soldier.TryAttack(), "TryAttack returned false (dead or on cooldown)");

        // Right after the call only the FIRST kernel exists: a burst is spread over time, not a single volley
        var kernels = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None);
        Assert.AreEqual(1, kernels.Length,
            "Right after the attack there should be exactly one kernel (the other ones follow after Time Between Kernels). " +
            "Zero means Attack() returned early: check Kernel Prefab, Fire Point and Attack Range");

        var first = kernels[0];
        Assert.AreEqual(soldierGo, first.Owner, "The kernel should belong to the soldier");
        Assert.AreEqual(data.AttackDamage, first.Damage, 0.001f, "The kernel should carry the soldier's Attack Damage");
        Assert.AreEqual(0f, Vector3.Distance(first.transform.position, firePoint.position), 0.05f,
            "The kernel should spawn at the Fire Point");

        var kernelCollider = first.GetComponentInChildren<Collider>();
        Assert.IsNotNull(kernelCollider, "The kernel prefab has no Collider: add a Sphere Collider to it");
        Assert.IsFalse(kernelCollider.isTrigger,
            "The kernel's collider has Is Trigger checked: OnCollisionEnter never fires for triggers");
        Assert.IsFalse(first.GetComponent<Rigidbody>().isKinematic,
            "The kernel's Rigidbody is Kinematic: it cannot be moved by velocity");

        var recorder = first.gameObject.AddComponent<CollisionRecorder>(); // diagnostics only

        // Wait for all the kernels to land
        float timeout = (data.KernelsPerBurst - 1) * data.TimeBetweenKernels + 4f;
        while (log.Hits < data.KernelsPerBurst && Time.time - start < timeout)
            yield return null;

        yield return new WaitForSeconds(0.3f); // time for unexpected extra hits to show up

        string collisions = recorder.Hits.Count == 0 ? "nothing" : string.Join(", ", recorder.Hits);
        Assert.AreEqual(data.KernelsPerBurst, log.Hits,
            $"The burst should hit the player {data.KernelsPerBurst} times, got {log.Hits}. " +
            $"The first kernel collided with: {collisions}.");
        Assert.AreEqual(data.KernelsPerBurst * data.AttackDamage, log.TotalDamage, 0.001f,
            "Every kernel should deal the soldier's Attack Damage");

        // The kernels must arrive one after the other, about Time Between Kernels apart
        for (int i = 1; i < log.HitTimes.Count; i++)
        {
            float gap = log.HitTimes[i] - log.HitTimes[i - 1];
            Assert.GreaterOrEqual(gap, data.TimeBetweenKernels * 0.5f,
                $"Kernels {i} and {i + 1} landed only {gap:F2} s apart (expected about {data.TimeBetweenKernels})");
            Assert.LessOrEqual(gap, data.TimeBetweenKernels + 0.15f,
                $"Kernels {i} and {i + 1} landed {gap:F2} s apart (expected about {data.TimeBetweenKernels})");
        }
    }

    // ------------------------------------------------------------------
    // 2. After a burst the soldier waits Burst Cooldown before the next one
    // ------------------------------------------------------------------
    [UnityTest]
    public IEnumerator CornSoldier_WaitsBurstCooldown_BeforeTheNextBurst()
    {
        BuildScene(out var soldier, out var data, out var log);
        CheckAsset(data);
        Assert.Greater(data.BurstCooldown, 1f,
            "Burst Cooldown must be > 1 s, so the test can tell 'cooldown running' from 'cooldown over'");

        yield return new WaitForSeconds(SettleTime); // let the soldier land before it shoots

        SpotPlayer(soldier, data);

        float burstDuration = (data.KernelsPerBurst - 1) * data.TimeBetweenKernels;

        // First burst. Attack() is called directly: it only depends on Burst Cooldown, not on Attack Cooldown
        float t0 = Time.time;
        soldier.Attack();
        Assert.AreEqual(1, Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length,
            "The first burst did not start (Attack() returned early)");

        while (log.Hits < data.KernelsPerBurst && Time.time - t0 < burstDuration + 4f)
            yield return null;
        Assert.AreEqual(data.KernelsPerBurst, log.Hits, "The first burst did not land completely");

        yield return null; // landed kernels are destroyed at the end of the frame
        yield return null;

        // The cooldown is still running: a new Attack() must NOT start a burst
        Assert.Less(Time.time - t0, burstDuration + data.BurstCooldown - 0.2f,
            "The test ran too slowly: the cooldown had already expired. Raise Burst Cooldown");
        soldier.Attack();
        Assert.AreEqual(0, Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length,
            "A new burst started while Burst Cooldown was still running");

        // After the cooldown a new burst starts
        float remaining = t0 + burstDuration + data.BurstCooldown - Time.time;
        yield return new WaitForSeconds(Mathf.Max(0f, remaining) + 0.4f);

        soldier.Attack();
        Assert.AreEqual(1, Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length,
            "No new burst started after Burst Cooldown had passed");
    }

    // ------------------------------------------------------------------
    // 3. With the AI running, the soldier spots the player and shoots a burst by itself
    // ------------------------------------------------------------------
    [UnityTest]
    public IEnumerator CornSoldier_Ai_SpotsThePlayer_AndShootsABurst()
    {
        BuildScene(out var soldier, out var data, out var log);
        CheckAsset(data);

        Assert.GreaterOrEqual(data.DetectionRange, PlayerDistance, $"Detection Range must be >= {PlayerDistance}");
        Assert.GreaterOrEqual(data.LoseTargetRange, PlayerDistance,
            $"Lose Target Range must be >= {PlayerDistance}, otherwise the soldier gives up immediately");
        Assert.Greater(data.LoseSightTime, 0f,
            "Lose Sight Time is 0: the soldier gives up as soon as it loses sight for a single frame");

        yield return new WaitForSeconds(SettleTime); // let the soldier land first

        // Now the AI starts: Roam spots the player, CombatState calls TryAttack
        soldier.enabled = true;

        float start = Time.time;
        while (log.Hits < data.KernelsPerBurst && Time.time - start < 8f)
            yield return null;

        Assert.GreaterOrEqual(log.Hits, data.KernelsPerBurst,
            $"The soldier hit the player {log.Hits} times in 8 s, expected a burst of {data.KernelsPerBurst}. " +
            "Check View Angle, Turn Speed, Attack Range and that the Obstacle Mask does not include the player's layer.");
    }
}