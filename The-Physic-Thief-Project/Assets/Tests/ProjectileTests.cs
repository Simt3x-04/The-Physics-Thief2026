using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using PhysicsThief.Core;
using PhysicsThief.Enemies;

/// <summary>Bersaglio finto: registra i colpi ricevuti.</summary>
public class DummyTarget : MonoBehaviour, IDamageable
{
    public int Hits;
    public float TotalDamage;

    public void TakeDamage(float amount)
    {
        Hits++;
        TotalDamage += amount;
    }
}

public class ProjectileTests
{
    private GameObject owner;
    private GameObject target;
    private Projectile projectile;

    [TearDown]
    public void TearDown()
    {
        if (owner != null) Object.Destroy(owner);
        if (target != null) Object.Destroy(target);
        if (projectile != null) Object.Destroy(projectile.gameObject);
    }

    private static GameObject CreateBox(string name, Vector3 position)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); // include un BoxCollider
        go.name = name;
        go.transform.position = position;
        return go;
    }

    private Projectile CreateProjectile(Vector3 position)
    {
        var go = new GameObject("TestProjectile");
        go.transform.position = position;
        go.AddComponent<SphereCollider>().radius = 0.2f;
        return go.AddComponent<Projectile>(); // RequireComponent aggiunge il Rigidbody
    }

    [UnityTest]
    public IEnumerator Projectile_DamagesTarget_WithPassedDamage_AndDestroysItself()
    {
        owner = CreateBox("Owner", Vector3.zero);
        target = CreateBox("Target", new Vector3(0f, 0f, 6f));
        var dummy = target.AddComponent<DummyTarget>();

        projectile = CreateProjectile(new Vector3(0f, 0f, 2f));
        projectile.Launch(Vector3.forward, owner, 17f);

        yield return WaitUntilOrTimeout(() => dummy.Hits > 0, 3f);
        yield return null; // lascia a Destroy il tempo di completare

        Assert.AreEqual(1, dummy.Hits, "Il bersaglio doveva essere colpito una volta");
        Assert.AreEqual(17f, dummy.TotalDamage, 0.001f, "Il danno deve essere quello passato a Launch");
        Assert.IsTrue(projectile == null, "Il proiettile deve distruggersi dopo l'impatto");
    }

    [UnityTest]
    public IEnumerator Projectile_DoesNotDamageItsOwner()
    {
        owner = CreateBox("Owner", Vector3.zero);
        var ownerDummy = owner.AddComponent<DummyTarget>();

        // Parte davanti all'owner e viene sparato verso di lui
        projectile = CreateProjectile(new Vector3(0f, 0f, 3f));
        projectile.Launch(Vector3.back, owner, 10f);

        yield return new WaitForSeconds(1f);

        Assert.AreEqual(0, ownerDummy.Hits, "Il proiettile non deve colpire chi l'ha sparato");
    }

    private static IEnumerator WaitUntilOrTimeout(System.Func<bool> condition, float timeout)
    {
        float end = Time.time + timeout;
        while (!condition() && Time.time < end)
            yield return null;
    }
}