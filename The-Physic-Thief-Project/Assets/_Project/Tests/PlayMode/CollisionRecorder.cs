using System.Collections.Generic;
using UnityEngine;

/// <summary>Records what an object collides with. Only used to diagnose tests (e.g. HornetTests).</summary>
public class CollisionRecorder : MonoBehaviour
{
    public readonly List<string> Hits = new List<string>();

    private void OnCollisionEnter(Collision collision)
    {
        Hits.Add(collision.gameObject.name);
    }
}