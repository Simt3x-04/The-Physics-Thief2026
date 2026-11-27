using System.Collections.Generic;
using UnityEngine;

/// <summary>Registra con cosa collide un oggetto. Serve solo alla diagnostica dei test (es. HornetTests).</summary>
public class CollisionRecorder : MonoBehaviour
{
    public readonly List<string> Hits = new List<string>();

    private void OnCollisionEnter(Collision collision)
    {
        Hits.Add(collision.gameObject.name);
    }
}