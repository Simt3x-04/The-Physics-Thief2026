using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using PhysicsThief.Enemies;

/// <summary>Muove un oggetto in linea retta a velocita' costante (il "player" che scappa).</summary>
public class StraightRunner : MonoBehaviour
{
    public Vector3 Velocity;

    private void Update()
    {
        transform.position += Velocity * Time.deltaTime;
    }
}

public class AngryCarrotTests
{
    // Assets/Tests/Resources/Enemies/AngryCarrotPrefab.prefab
    private const string CarrotPrefabPath = "Enemies/AngryCarrotPrefab";
    private const float PlayerDistance = 6f;

    private GameObject floor;
    private GameObject carrotGo;
    private GameObject player;

    [TearDown]
    public void TearDown()
    {
        if (floor != null) Object.Destroy(floor);
        if (carrotGo != null) Object.Destroy(carrotGo);
        if (player != null) Object.Destroy(player);
    }

    // ---------------------------------------------------------------- helpers

    private void BuildScene(out AngryCarrot carrot, out AngryCarrotData data, out HitLogTarget log, out Rigidbody body)
    {
        floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.transform.localScale = new Vector3(20f, 1f, 20f); // 200 x 200 units, the carrot can run far

        // Fake player: pivot at the feet like a real character, in front of the carrot (+Z)
        player = new GameObject("FakePlayer");
        player.tag = "Player";
        player.transform.position = new Vector3(0f, 0f, PlayerDistance);

        var torso = GameObject.CreatePrimitive(PrimitiveType.Cube);
        torso.name = "FakePlayerBody";
        torso.transform.SetParent(player.transform, false);
        torso.transform.localPosition = new Vector3(0f, 1f, 0f);
        torso.transform.localScale = new Vector3(1f, 2f, 1f);

        log = player.AddComponent<HitLogTarget>();

        var prefab = Resources.Load<GameObject>(CarrotPrefabPath);
        Assert.IsNotNull(prefab,
            "Prefab not found: expected Assets/Tests/Resources/Enemies/AngryCarrotPrefab.prefab");

        carrotGo = Object.Instantiate(prefab, new Vector3(0f, 0.6f, 0f), Quaternion.identity); // faces +Z
        Physics.SyncTransforms(); // makes the line-of-sight check see the colliders created this frame

        carrot = carrotGo.GetComponent<AngryCarrot>();
        Assert.IsNotNull(carrot, "The prefab has no AngryCarrot component");

        data = carrot.Data as AngryCarrotData;
        Assert.IsNotNull(data, "The carrot's Data is missing or is not an AngryCarrotData");

        body = carrotGo.GetComponent<Rigidbody>();
        Assert.IsNotNull(body, "The carrot prefab needs a Rigidbody to move");
        Assert.IsFalse(body.isKinematic, "The carrot's Rigidbody is Kinematic: it cannot move by velocity");
    }

    private static void CheckAiSetup(AngryCarrotData data)
    {
        Assert.Greater(data.ChargeSpeed, 0f, "Charge Speed is 0 in the AngryCarrotData asset");
        Assert.Greater(data.DashDuration, 0f, "Dash Duration is 0 in the AngryCarrotData asset");
        Assert.Greater(data.AttackDamage, 0f, "Attack Damage is 0 in the AngryCarrotData asset");
        Assert.Greater(data.AttackRange, 0f, "Attack Range is 0 in the AngryCarrotData asset");
        Assert.GreaterOrEqual(data.DetectionRange, PlayerDistance,
            $"Detection Range must be >= {PlayerDistance} (the distance to the player)");
        Assert.GreaterOrEqual(data.LoseTargetRange, PlayerDistance,
            $"Lose Target Range must be >= {PlayerDistance}, otherwise the carrot gives up immediately");
        Assert.Greater(data.LoseSightTime, 0f,
            "Lose Sight Time is 0: the carrot gives up as soon as it loses sight for a single frame");
    }

    private static float HorizontalSpeed(Rigidbody body)
    {
        Vector3 v = body.linearVelocity;
        v.y = 0f;
        return v.magnitude;
    }

    // ------------------------------------------------------------------
    // 1. The carrot sees the player, dashes at Charge Speed and hits him in melee
    // ------------------------------------------------------------------
    [UnityTest]
    public IEnumerator AngryCarrot_DashesToThePlayer_AndHitsInMelee()
    {
        BuildScene(out var carrot, out var data, out var log, out var body);
        CheckAiSetup(data);
        Assert.GreaterOrEqual(data.ChargeSpeed * data.DashDuration, PlayerDistance,
            "Charge Speed x Dash Duration is shorter than the distance to the player: the dash cannot reach him");
        Assert.GreaterOrEqual(data.ChargeCooldown, 1f,
            "Charge Cooldown must be >= 1 s for the recovery check of this test");

        // The AI stays enabled: Roam spots the player, then the dash state takes over
        float maxSpeed = 0f;
        float start = Time.time;
        while (log.Hits == 0 && Time.time - start < 6f)
        {
            maxSpeed = Mathf.Max(maxSpeed, HorizontalSpeed(body));
            yield return null;
        }

        float distanceAtHit = Vector3.Distance(carrotGo.transform.position, player.transform.position);

        Assert.GreaterOrEqual(log.Hits, 1,
            $"The carrot never hit the player in 6 s (top speed {maxSpeed:F1}). If it did not move: check " +
            "Detection Range, View Angle, Turn Speed and that the Obstacle Mask does not include the player's layer. " +
            "If it stopped short: Attack Range is smaller than the gap between the two colliders.");
        Assert.GreaterOrEqual(maxSpeed, data.ChargeSpeed * 0.8f,
            $"The carrot never reached its Charge Speed ({data.ChargeSpeed}): top speed was {maxSpeed:F1}");
        Assert.LessOrEqual(distanceAtHit, data.AttackRange + 0.5f,
            "The carrot hit the player from farther away than its Attack Range");
        Assert.AreEqual(log.Hits * data.AttackDamage, log.TotalDamage, 0.001f,
            "Each hit should deal the carrot's Attack Damage");

        // The dash ends on contact: the carrot stands still while it recovers
        yield return new WaitForSeconds(0.25f);
        Assert.Less(HorizontalSpeed(body), 0.5f,
            "After hitting the player the carrot should stop and recover (Charge Cooldown)");
    }

    // ------------------------------------------------------------------
    // 2. The dash lasts Dash Duration seconds (the player keeps running away, so there is no contact)
    // ------------------------------------------------------------------
    [UnityTest]
    public IEnumerator AngryCarrot_Dash_LastsDashDuration()
    {
        BuildScene(out var carrot, out var data, out var log, out var body);
        CheckAiSetup(data);
        Assert.Less(data.AttackRange, 3.5f,
            "Attack Range must be < 3.5 for this test: the carrot must not reach the player before the dash ends");

        // The player runs slower than the carrot, but the gap only shrinks by 2 units during the whole dash
        var runner = player.AddComponent<StraightRunner>();
        runner.Velocity = Vector3.forward * Mathf.Max(0.1f, data.ChargeSpeed - 2f / data.DashDuration);

        float dashStart = -1f;
        float dashEnd = -1f;
        float begin = Time.time;
        float timeout = data.DashDuration + 4f;

        while (dashEnd < 0f && Time.time - begin < timeout)
        {
            float speed = HorizontalSpeed(body);
            if (dashStart < 0f)
            {
                if (speed >= data.ChargeSpeed * 0.8f) dashStart = Time.time;
            }
            else if (speed < data.ChargeSpeed * 0.5f)
            {
                dashEnd = Time.time;
            }
            yield return null;
        }

        Assert.GreaterOrEqual(dashStart, 0f,
            "The carrot never started dashing (its speed never reached 80% of Charge Speed)");
        Assert.GreaterOrEqual(dashEnd, 0f,
            $"The dash did not end within {timeout:F1} s: the carrot never slowed down");
        Assert.AreEqual(0, log.Hits, "The carrot reached the player: the dash ended by contact, not by time");

        float duration = dashEnd - dashStart;
        Assert.AreEqual(data.DashDuration, duration, 0.4f,
            $"The dash lasted {duration:F2} s, expected Dash Duration ({data.DashDuration} s)");
    }

    // ------------------------------------------------------------------
    // 3. While dashing the carrot steers towards the player, at a limited turn rate
    // ------------------------------------------------------------------
    [UnityTest]
    public IEnumerator AngryCarrot_Dash_SteersTowardsThePlayer_AtLimitedTurnRate()
    {
        BuildScene(out var carrot, out var data, out var log, out var body);
        CheckAiSetup(data);
        Assert.Greater(data.DashTurnSpeed, 0f, "Dash Turn Speed is 0: the dash is a straight line, nothing to steer");
        Assert.Less(data.AttackRange, 3f, "Attack Range must be < 3 for this test");
        Assert.GreaterOrEqual(data.LoseTargetRange, 10f, "Lose Target Range must be >= 10 for this test");

        // Wait until the dash is running
        float begin = Time.time;
        while (HorizontalSpeed(body) < data.ChargeSpeed * 0.8f && Time.time - begin < 3f)
            yield return null;
        Assert.GreaterOrEqual(HorizontalSpeed(body), data.ChargeSpeed * 0.8f, "The carrot never started dashing");

        // The player suddenly appears 6 units to the carrot's right
        Vector3 carrotPos = carrotGo.transform.position;
        player.transform.position = new Vector3(carrotPos.x + 6f, 0f, carrotPos.z);
        Vector3 forwardBefore = carrotGo.transform.forward;
        float t0 = Time.time;

        yield return new WaitForSeconds(0.5f);

        float elapsed = Time.time - t0;
        Vector3 forwardAfter = carrotGo.transform.forward;
        float turned = Vector3.Angle(forwardBefore, forwardAfter);
        float maxTurn = data.DashTurnSpeed * elapsed;

        Assert.Greater(Vector3.Dot(forwardAfter, Vector3.right), 0f,
            "The carrot did not turn towards the player (who is on its right). " +
            "Check the steering condition in AngryCarrotDashState.TickDash: it must be sqrMagnitude > 0.0001f");
        Assert.GreaterOrEqual(turned, Mathf.Min(maxTurn, 60f) * 0.5f,
            $"The carrot turned only {turned:F0} degrees in {elapsed:F2} s (Dash Turn Speed {data.DashTurnSpeed} degrees/s)");
        Assert.LessOrEqual(turned, maxTurn + 10f,
            $"The carrot turned {turned:F0} degrees in {elapsed:F2} s: faster than Dash Turn Speed ({data.DashTurnSpeed} degrees/s)");
    }

    // ------------------------------------------------------------------
    // 4. The melee hit only lands if the player is in front and within Attack Range
    // ------------------------------------------------------------------
    [UnityTest]
    public IEnumerator AngryCarrot_MeleeHit_OnlyLandsInFrontAndInRange()
    {
        BuildScene(out var carrot, out var data, out var log, out var body);
        Assert.Greater(data.AttackDamage, 0f, "Attack Damage is 0 in the AngryCarrotData asset");
        Assert.Greater(data.AttackRange, 0.5f, "Attack Range must be > 0.5 for this test");
        Assert.Less(data.MeleeHalfAngle, 170f, "Melee Half Angle must be < 170, otherwise the carrot can hit behind itself");

        // Player right in front of the carrot, inside the Attack Range
        player.transform.position = new Vector3(0f, 0f, data.AttackRange * 0.8f);
        Physics.SyncTransforms();

        Assert.IsTrue(carrot.TrySpotTarget(),
            "The carrot did not spot the player (check Detection Range, View Angle, " +
            "and that the Obstacle Mask does not include the player's layer)");

        carrot.enabled = false; // no AI: only the hit logic is under test
        foreach (var c in player.GetComponentsInChildren<Collider>()) c.enabled = false; // no physical contact

        // 1. In front and in range: the hit lands
        Assert.IsTrue(carrot.IsTargetInMeleeReach(), "The player is in front and in range: it should be in melee reach");
        carrot.Attack();
        Assert.AreEqual(1, log.Hits, "The melee attack should have hit the player");
        Assert.AreEqual(data.AttackDamage, log.TotalDamage, 0.001f, "The hit should deal Attack Damage");

        // 2. In front but too far: no hit
        player.transform.position = new Vector3(0f, 0f, data.AttackRange + 1f);
        Assert.IsFalse(carrot.IsTargetInMeleeReach(), "The player is out of range: it should not be in melee reach");
        carrot.Attack();
        Assert.AreEqual(1, log.Hits, "The carrot hit a player who was out of its Attack Range");

        // 3. In range but behind the carrot: no hit
        player.transform.position = new Vector3(0f, 0f, -data.AttackRange * 0.8f);
        Assert.IsFalse(carrot.IsTargetInMeleeReach(), "The player is behind the carrot: it should not be in melee reach");
        carrot.Attack();
        Assert.AreEqual(1, log.Hits, "The carrot hit a player who was behind it");

        yield return null;
    }
}