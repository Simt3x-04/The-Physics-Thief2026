using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using PhysicsThief.Enemies;

/// <summary>Registra con cosa collide il proiettile, solo per la diagnostica del test.</summary>
public class CollisionRecorder : MonoBehaviour
{
    public readonly List<string> Hits = new List<string>();

    private void OnCollisionEnter(Collision collision)
    {
        Hits.Add(collision.gameObject.name);
    }
}

public class SpearBeetleShootingTests
{
    // Assets/Tests/Resources/Enemies/SpearBeetlePrefab.prefab
    private const string BeetlePrefabPath = "Enemies/SpearBeetlePrefab";
    private const float PlayerDistance = 6f;

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

    [UnityTest]
    public IEnumerator SpearBeetle_FiresProjectile_ThatDamagesThePlayer()
    {
        // Floor
        floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.transform.localScale = new Vector3(5f, 1f, 5f);

        // Fake player: pivot at the feet like a real character, in front of the beetle (+Z)
        player = new GameObject("FakePlayer");
        player.tag = "Player";
        player.transform.position = new Vector3(0f, 0f, PlayerDistance);

        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "FakePlayerBody";
        body.transform.SetParent(player.transform, false);
        body.transform.localPosition = new Vector3(0f, 1f, 0f);
        body.transform.localScale = new Vector3(1f, 2f, 1f);

        var dummy = player.AddComponent<DummyTarget>();

        // Real beetle prefab, facing the player
        var prefab = Resources.Load<GameObject>(BeetlePrefabPath);
        Assert.IsNotNull(prefab,
            "Prefab not found: expected Assets/Tests/Resources/Enemies/SpearBeetlePrefab.prefab");

        beetleGo = Object.Instantiate(prefab, new Vector3(0f, 0.1f, 0f), Quaternion.identity);

        // Makes the line-of-sight check see the colliders created this frame
        Physics.SyncTransforms();

        var beetle = beetleGo.GetComponent<SpearBeetle>();
        Assert.IsNotNull(beetle, "The prefab has no SpearBeetle component");

        var data = beetle.Data as SpearBeetleData;
        Assert.IsNotNull(data, "The beetle's Data is missing or is not a SpearBeetleData");

        // 1. The beetle spots the player
        Assert.IsTrue(beetle.TrySpotTarget(),
            "The beetle did not spot the player (check Detection Range >= 8, View Angle, Obstacle Mask)");

        // Stops the roaming AI so the beetle doesn't wander off or turn away during the test
        beetle.enabled = false;

        // 2. Diagnostics: the same conditions that make SpearBeetle.Attack() return early
        Assert.IsNotNull(data.ProjectilePrefab,
            "Projectile Prefab is not assigned in the SpearBeetleData asset used by the test prefab");

        var firePointField = typeof(SpearBeetle).GetField("firePoint",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(firePointField,
            "SpearBeetle has no field named 'firePoint' (renamed? update the name in this test)");

        var firePoint = firePointField.GetValue(beetle) as Transform;
        Assert.IsNotNull(firePoint,
            "Fire Point is not assigned on Assets/Tests/Resources/Enemies/SpearBeetlePrefab");

        Assert.GreaterOrEqual(data.ShootRange, PlayerDistance,
            "Shoot Range in SpearBeetleData is smaller than the distance to the player");

        // 3. The beetle attacks and spawns its projectile
        Assert.IsTrue(beetle.TryAttack(), "TryAttack returned false (dead or on cooldown)");

        var projectile = Object.FindFirstObjectByType<Projectile>();
        Assert.IsNotNull(projectile,
            "The beetle did not spawn a projectile (SpearBeetle.Attack() returned early: check line of sight and Obstacle Mask)");

        // 4. The projectile carries the beetle's own data
        Assert.AreEqual(beetleGo, projectile.Owner, "The projectile should belong to the beetle");
        Assert.AreEqual(data.ProjectileDamage, projectile.Damage, 0.001f,
            "The beetle should pass its own ProjectileDamage to Launch");

        // 5. Diagnostics: is the projectile prefab able to collide at all?
        var projectileCollider = projectile.GetComponentInChildren<Collider>();
        Assert.IsNotNull(projectileCollider,
            "The projectile prefab has no Collider: add a Sphere Collider to it");
        Assert.IsFalse(projectileCollider.isTrigger,
            "The projectile's collider has Is Trigger checked: OnCollisionEnter never fires for triggers");

        var projectileBody = projectile.GetComponent<Rigidbody>();
        Assert.IsFalse(projectileBody.isKinematic,
            "The projectile's Rigidbody is Kinematic: it cannot be moved by velocity");

        var recorder = projectile.gameObject.AddComponent<CollisionRecorder>();
        Vector3 lastPosition = projectile.transform.position;

        // 6. The projectile reaches and damages the player
        float end = Time.time + 3f;
        while (dummy.Hits == 0 && Time.time < end)
        {
            if (projectile != null) lastPosition = projectile.transform.position;
            yield return null;
        }

        string projectileState = projectile == null
            ? $"destroyed, last seen at {lastPosition}"
            : $"still alive at {projectile.transform.position}, velocity {projectile.GetComponent<Rigidbody>().linearVelocity}";
        string collisions = recorder.Hits.Count == 0 ? "nothing" : string.Join(", ", recorder.Hits);

        Assert.AreEqual(1, dummy.Hits,
            $"The player should have been hit exactly once. Player at {player.transform.position}. " +
            $"Projectile {projectileState}. Projectile collided with: {collisions}.");
        Assert.AreEqual(data.ProjectileDamage, dummy.TotalDamage, 0.001f,
            "The player should have taken the beetle's ProjectileDamage");
    }
}