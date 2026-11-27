using System.Collections.Generic;
using UnityEngine;

namespace PhysicsThief.Properties.Reactors
{
    /// <summary>
    /// Shared by the reactors, so several tints can coexist on one object (GDD 3.3: several attributes can be
    /// active at once). Each reactor sets or clears ITS OWN tint; the most recently set one is shown.
    /// Reactors get it with ObjectTint.For(gameObject): it is added automatically.
    /// </summary>
    public class ObjectTint : MonoBehaviour
    {
        private class Request
        {
            public object source;
            public Color color;
        }

        private readonly List<Request> requests = new List<Request>();
        private Renderer[] renderers;
        private MaterialPropertyBlock block;

        public static ObjectTint For(GameObject target)
        {
            ObjectTint tint = target.GetComponent<ObjectTint>();
            return tint != null ? tint : target.AddComponent<ObjectTint>();
        }

        private void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>();
            block = new MaterialPropertyBlock();
        }

        public void Set(object source, Color color)
        {
            Remove(source);
            requests.Add(new Request { source = source, color = color });
            Apply();
        }

        public void Clear(object source)
        {
            Remove(source);
            Apply();
        }

        private void Remove(object source)
        {
            requests.RemoveAll(r => r.source == source);
        }

        private void Apply()
        {
            if (renderers == null) return;

            bool any = requests.Count > 0;
            Color color = any ? requests[requests.Count - 1].color : Color.white;

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;

                if (!any)
                {
                    renderers[i].SetPropertyBlock(null);   // back to the material's own color
                    continue;
                }
                renderers[i].GetPropertyBlock(block);
                block.SetColor("_BaseColor", color);   // URP shaders
                block.SetColor("_Color", color);       // built-in shaders
                renderers[i].SetPropertyBlock(block);
            }
        }
    }
}
