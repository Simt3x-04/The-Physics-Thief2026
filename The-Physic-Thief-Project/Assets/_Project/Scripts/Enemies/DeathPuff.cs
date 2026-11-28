using UnityEngine;

namespace PhysicsThief.Enemies
{
    /// <summary>
    /// Cartoon "puff" of white smoke, used when an enemy is defeated.
    /// Built entirely in code (no prefab needed): call DeathPuff.Spawn(position, size).
    /// The effect destroys itself when it is finished.
    /// </summary>
    public static class DeathPuff
    {
        private static Material material;
        private static Texture2D texture;

        /// <param name="position">World position (usually the middle of the enemy).</param>
        /// <param name="size">1 = carrot-sized enemy (about 1.3 m tall). Bigger enemies: larger value.</param>
        public static GameObject Spawn(Vector3 position, float size = 1f, Color? tint = null)
        {
            var go = new GameObject("DeathPuff");
            go.transform.position = position;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            Color c = tint ?? new Color(1f, 1f, 1f, 0.95f);

            var main = ps.main;
            main.duration = 0.5f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.55f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f * size, 3.2f * size);
            main.startSize = new ParticleSystem.MinMaxCurve(0.35f * size, 0.75f * size);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = c;
            main.gravityModifier = -0.15f;                 // smoke drifts up a little
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 64;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, 22),
                new ParticleSystem.Burst(0.06f, 8),
            });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.35f * size;

            // slow down quickly, like a soft cloud
            var limit = ps.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = 0.4f * size;
            limit.dampen = 0.25f;

            // grow, then shrink away
            var sizeOL = ps.sizeOverLifetime;
            sizeOL.enabled = true;
            sizeOL.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.4f), new Keyframe(0.25f, 1.1f), new Keyframe(0.7f, 1f), new Keyframe(1f, 0f)));

            // fade out
            var colorOL = ps.colorOverLifetime;
            colorOL.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.9f, 0.9f, 0.92f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.55f), new GradientAlphaKey(0f, 1f) });
            colorOL.color = g;

            var rotOL = ps.rotationOverLifetime;
            rotOL.enabled = true;
            rotOL.z = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.sharedMaterial = GetMaterial();
            r.sortingFudge = 0f;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;

            ps.Play();
            return go;
        }

        private static Material GetMaterial()
        {
            if (material != null) return material;
            // Sprites/Default: alpha-blended, unlit, uses the particle colors, always included in builds.
            material = new Material(Shader.Find("Sprites/Default")) { name = "DeathPuff (runtime)" };
            material.mainTexture = GetTexture();
            return material;
        }

        // A soft, slightly lumpy round blob, generated once.
        private static Texture2D GetTexture()
        {
            if (texture != null) return texture;
            const int n = 64;
            texture = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "DeathPuff blob" };
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    float ang = Mathf.Atan2(dy, dx);
                    float edge = 0.86f + 0.06f * Mathf.Sin(ang * 5f) + 0.03f * Mathf.Sin(ang * 9f + 1f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / edge;
                    float a = Mathf.Clamp01((1f - d) / 0.18f);           // soft edge, solid middle (cartoon look)
                    float shade = Mathf.Lerp(0.86f, 1f, Mathf.Clamp01(1f - (dy + 0.3f) * 0.6f)); // a bit darker at the bottom
                    px[y * n + x] = new Color(shade, shade, shade, a);
                }
            texture.SetPixels(px);
            texture.Apply();
            return texture;
        }
    }
}
