using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using PhysicsThief.Core;
using PhysicsThief.Enemies;

/// <summary>Play Mode integration test using the real HealthComponent.</summary>
public class HornetTests
{
    // Assets/_Project/Tests/PlayMode/Resources/Enemies/HornetPrefab.prefab
    private const string HornetPrefabPath = "Enemies/HornetPrefab";
    private const float PlayerDistance = 6f;

    private GameObject floor;
    private GameObject hornetGo;
    private GameObject player;

    [TearDown]
    public void TearDown()
    {
        if (floor != null) Object.Destroy(floor);
        if (hornetGo != null) Object.Destroy(hornetGo);
        if (player != null) Object.Destroy(player);

        foreach (var p in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))
            Object.Destroy(p.gameObject);
    }

    private static int FirstLayerIn(int mask)
    {
        for (int i = 0; i < 32; i++)
            if ((mask & (1 << i)) != 0) return i;
        return -1;
    }

    private static string DescribeMask(int mask)
    {
        var names = new List<string>();
        for (int i = 0; i < 32; i++)
        {
            if ((mask & (1 << i)) == 0) continue;
            string layerName = LayerMask.LayerToName(i);
            names.Add(string.IsNullOrEmpty(layerName) ? $"layer {i}" : layerName);
        }
        return string.Join(", ", names);
    }

    [UnityTest]
    public IEnumerator Hornet_Flies_ShootsFromTheAir_AndPoisonsThePlayer()
    {
        // ---------- Setup: prefab and floor ----------
        var prefab = Resources.Load<GameObject>(HornetPrefabPath);
        Assert.IsNotNull(prefab,
            "Prefab not found: expected Assets/_Project/Tests/PlayMode/Resources/Enemies/HornetPrefab.prefab");

        var prefabHornet = prefab.GetComponent<Hornet>();
        Assert.IsNotNull(prefabHornet, "The prefab has no Hornet component");

        var groundField = typeof(Hornet).GetField("groundMask", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(groundField, "Hornet has no field named 'groundMask' (renamed? update the test)");

        int groundMask = ((LayerMask)groundField.GetValue(prefabHornet)).value;
        Assert.AreNotEqual(0, groundMask,
            "Ground Mask is empty on the Hornet prefab: select the ground layer, otherwise the hornet never rises");

        // Not blocking: the hornet still flies, only at the wrong height (its own collider is hit first)
        if ((groundMask & (1 << prefab.layer)) != 0)
        {
            Debug.LogWarning(
                $"The hornet's own layer ('{LayerMask.LayerToName(prefab.layer)}') is in its Ground Mask " +
                $"({DescribeMask(groundMask)}): its ground raycast can hit its own collider, " +
                "so it hovers higher than Hover Height.");
        }

        // The floor goes on a layer of the Ground Mask, preferably not the hornet's own
        int groundLayer = FirstLayerIn(groundMask & ~(1 << prefab.layer));
        if (groundLayer < 0) groundLayer = FirstLayerIn(groundMask);

        floor = GameObject.CreatePrimitive(PrimitiveType.Plane); // top of the floor is at y = 0
        floor.transform.localScale = new Vector3(5f, 1f, 5f);
        floor.layer = groundLayer;

        hornetGo = Object.Instantiate(prefab, new Vector3(0f, 0.5f, 0f), Quaternion.identity);
        Physics.SyncTransforms();

        var hornet = hornetGo.GetComponent<Hornet>();
        var data = hornet.Data as HornetData;
        Assert.IsNotNull(data, "The hornet's Data is missing or is not a HornetData");

        var body = hornetGo.GetComponent<Rigidbody>();
        Assert.IsNotNull(body, "The Hornet prefab needs a Rigidbody to fly");
        Assert.IsFalse(body.isKinematic, "The hornet's Rigidbody is Kinematic: it cannot move by velocity");
        Assert.GreaterOrEqual(data.HoverHeight, 1.5f,
            "Hover Height in HornetData should be >= 1.5, so that flying can be told apart from standing");

        // ---------- 1. The hornet flies ----------
        yield return new WaitForSeconds(2f); // time to rise from the floor

        float expectedY = data.HoverHeight;           // floor top is at y = 0
        float tolerance = data.BobAmplitude + 0.4f;   // the hornet bobs up and down around its base height
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        for (int i = 0; i < 10; i++)
        {
            yield return new WaitForSeconds(0.1f);
            float y = hornetGo.transform.position.y;
            minY = Mathf.Min(minY, y);
            maxY = Mathf.Max(maxY, y);
        }

        // Up in the air (at least at Hover Height) and steady, not falling or oscillating wildly
        Assert.GreaterOrEqual(minY, expectedY - tolerance,
            $"The hornet is not up at Hover Height ({expectedY}). It was seen between y={minY:F2} and y={maxY:F2}. " +
            "If it stays near the floor, check that the Hornet's Ground Mask matches the floor's layer.");
        Assert.LessOrEqual(maxY - minY, 2f * data.BobAmplitude + 0.3f,
            $"The hornet's height is not steady: it moved between y={minY:F2} and y={maxY:F2}");

        // Freeze the hornet in the air and stop its AI, so the next steps are deterministic
        hornet.enabled = false;
        body.linearVelocity = Vector3.zero;
        body.constraints = RigidbodyConstraints.FreezeAll;

        float flightY = hornetGo.transform.position.y;
        Vector3 hornetPos = hornetGo.transform.position;

        // ---------- 2. The player appears, with the real HealthComponent ----------
        player = new GameObject("FakePlayer");
        player.tag = "Player";
        player.transform.position = new Vector3(hornetPos.x, 0f, hornetPos.z + PlayerDistance);

        var torso = GameObject.CreatePrimitive(PrimitiveType.Cube);
        torso.name = "FakePlayerBody";
        torso.transform.SetParent(player.transform, false);
        torso.transform.localPosition = new Vector3(0f, 1f, 0f);
        torso.transform.localScale = new Vector3(1f, 2f, 1f);

        var health = player.AddComponent<HealthComponent>();
        Assert.Greater(data.PoisonDps, 0f);
        Assert.Greater(data.PoisonDuration, 0f);
        float expectedPoisonDamage = data.PoisonDps * data.PoisonDuration;
        health.Initialize(Mathf.Max(100f, expectedPoisonDamage + Mathf.Max(0f, data.AttackDamage) + 10f));
        player.AddComponent<DummyTarget>(); // makes sure there is an IDamageable to hit

        hornetGo.transform.rotation = Quaternion.LookRotation(Vector3.forward); // facing the player
        Physics.SyncTransforms();

        float distanceToPlayer = Vector3.Distance(hornetGo.transform.position, player.transform.position);

        Assert.IsTrue(hornet.TrySpotTarget(),
            $"The hornet did not spot the player (Detection Range must be >= {distanceToPlayer:F1}, " +
            "View Angle > 0, and the Obstacle Mask must not include the player's layer)");

        // ---------- Diagnostics: the same conditions that make Attack() fail ----------
        Assert.IsNotNull(data.VenomDartPrefab,
            "Venom Dart Prefab is not assigned in the HornetData asset used by the test prefab");

        var firePointField = typeof(Hornet).GetField("firePoint", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(firePointField, "Hornet has no field named 'firePoint' (renamed? update the test)");
        Assert.IsNotNull(firePointField.GetValue(hornet) as Transform,
            "Fire Point is not assigned on Assets/_Project/Tests/PlayMode/Resources/Enemies/HornetPrefab");

        Assert.GreaterOrEqual(data.AttackRange, distanceToPlayer,
            $"Attack Range in HornetData must be >= {distanceToPlayer:F1} (the distance to the player)");

        // ---------- 3. The hornet shoots from where it flies, towards the player ----------
        Assert.IsTrue(hornet.TryAttack(), "TryAttack returned false (dead or on cooldown)");

        var dart = Object.FindFirstObjectByType<PoisonDart>();
        Assert.IsNotNull(dart, "The hornet did not spawn a PoisonDart (Attack() returned early)");

        Assert.AreEqual(hornetGo, dart.Owner,
            "The dart has no owner: PoisonDart.Launch must call base.Launch(direction, owner, damage)");
        Assert.AreEqual(data.AttackDamage, dart.Damage, 0.001f,
            "The hornet should pass its own AttackDamage to the dart");

        Assert.AreEqual(flightY, dart.transform.position.y, 1.5f,
            "The dart should start up in the air, where the hornet is flying");

        var dartCollider = dart.GetComponentInChildren<Collider>();
        Assert.IsNotNull(dartCollider, "The dart prefab has no Collider: add a Sphere Collider to it");
        Assert.IsFalse(dartCollider.isTrigger,
            "The dart's collider has Is Trigger checked: OnCollisionEnter never fires for triggers");

        var dartBody = dart.GetComponent<Rigidbody>();
        Assert.IsFalse(dartBody.isKinematic, "The dart's Rigidbody is Kinematic: it cannot be moved by velocity");
        Assert.Greater(dartBody.linearVelocity.magnitude, 0.1f,
            "The dart is not moving (Launch not called, or Speed is 0 on the prefab)");

        Vector3 chest = player.transform.position + Vector3.up;
        Vector3 expectedDirection = (chest - dart.transform.position).normalized;
        float alignment = Vector3.Dot(dartBody.linearVelocity.normalized, expectedDirection);
        Assert.Greater(alignment, 0.95f,
            $"The dart is not flying towards the player (alignment {alignment:F2}, 1.0 means a direct line)");

        var recorder = dart.gameObject.AddComponent<CollisionRecorder>(); // defined in SpearBeetleShootingTests.cs
        Vector3 lastPosition = dart.transform.position;

        // ---------- 4. The hit activates real poison ----------
        float end = Time.realtimeSinceStartup + 3f;
        while (!health.IsPoisoned && Time.realtimeSinceStartup < end)
        {
            if (dart != null) lastPosition = dart.transform.position;
            yield return null;
        }

        string dartState = dart == null
            ? $"destroyed, last seen at {lastPosition}"
            : $"still alive at {dart.transform.position}";
        string collisions = recorder.Hits.Count == 0 ? "nothing" : string.Join(", ", recorder.Hits);

        Assert.IsTrue(health.IsPoisoned,
            $"The player was not poisoned. Dart {dartState}. Collisions: {collisions}.");

        // Sample after impact, separating poison from any immediate hit damage.
        float hpAtPoisonStart = health.CurrentHP;
        float poisonStartTime = Time.time;
        float firstFrame = Time.deltaTime;
        float largestFrame = firstFrame;
        float damageTolerance = Mathf.Max(0.01f, data.PoisonDps * firstFrame + 0.01f);
        float poisonDeadline = Time.realtimeSinceStartup + data.PoisonDuration + 3f;
        bool healthDecreasedWhilePoisoned = false;

        // ---------- 5. Real damage over time ----------
        while (health.IsPoisoned && Time.realtimeSinceStartup < poisonDeadline)
        {
            yield return null;
            largestFrame = Mathf.Max(largestFrame, Time.deltaTime);
            if (health.IsPoisoned && health.CurrentHP < hpAtPoisonStart)
                healthDecreasedWhilePoisoned = true;
        }

        Assert.IsFalse(health.IsPoisoned, "Poison did not expire in time");
        Assert.IsFalse(health.IsDead, "The test player should survive the attack");
        float actualPoisonDamage = hpAtPoisonStart - health.CurrentHP;
        Assert.Greater(actualPoisonDamage, 0f, "Poison did not reduce HP");
        Assert.AreEqual(expectedPoisonDamage, actualPoisonDamage, damageTolerance,
            "Unexpected poison damage (allowing one frame of damage before the first sample)");
        Assert.AreEqual(data.PoisonDuration, Time.time - poisonStartTime,
            2f * largestFrame + 0.02f, "Unexpected poison duration");

        // Very short effects can finish in a single frame.
        if (data.PoisonDuration > 2f * largestFrame)
            Assert.IsTrue(healthDecreasedWhilePoisoned, "HP did not decrease during the poison effect");

        // ---------- 6. No further damage after expiry ----------
        float finalHP = health.CurrentHP;
        yield return new WaitForSeconds(0.5f);
        Assert.AreEqual(finalHP, health.CurrentHP, 0.001f,
            "Health continued decreasing after poison expired");
    }
}
