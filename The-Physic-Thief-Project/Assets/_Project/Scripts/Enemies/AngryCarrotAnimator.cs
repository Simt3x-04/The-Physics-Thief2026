using UnityEngine;

namespace PhysicsThief.Enemies
{
    /// <summary>
    /// Procedural cartoon animation for the AngryCarrot (the model has no bones).
    /// It only moves / tilts / squashes the "Visual" child, so it never touches the AI or the physics.
    ///   Spawn : pops out of the ground (stretch, then squash on landing).
    ///   Walk  : small hops with a side-to-side waddle (RoamState).
    ///   Run   : leans forward "head first", faster and higher hops (AngryCarrotDashState).
    ///   Attack: leans back to wind up, then a quick head-butt forward (EnemyBase.OnAttack).
    ///   Extras: angry shaking while resting between dashes, falls over on death, freezes in place when frozen.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AngryCarrot))]
    public class AngryCarrotAnimator : MonoBehaviour
    {
        [Tooltip("The child that holds the model. Found automatically (named 'Visual').")]
        [SerializeField] private Transform visual;

        [Header("Spawn")]
        [SerializeField] private bool playSpawnOnStart = true;
        [SerializeField] private float spawnDuration = 0.8f;
        [Tooltip("How deep below the ground the carrot starts (meters).")]
        [SerializeField] private float spawnDepth = 1.4f;

        [Header("Walk")]
        [SerializeField] private float walkHopsPerSecond = 2.2f;
        [SerializeField] private float walkHopHeight = 0.08f;
        [SerializeField] private float walkWaddleAngle = 9f;
        [SerializeField] private float walkLean = 6f;

        [Header("Run (dash)")]
        [SerializeField] private float runHopsPerSecond = 4.5f;
        [SerializeField] private float runHopHeight = 0.14f;
        [SerializeField] private float runLean = 28f;
        [SerializeField] private float runWaddleAngle = 4f;

        [Header("Squash & stretch")]
        [SerializeField] private float landingSquash = 0.12f;
        [SerializeField] private float airStretch = 0.08f;

        [Header("Attack")]
        [SerializeField] private float attackDuration = 0.45f;
        [SerializeField] private float attackWindupLean = -18f;
        [SerializeField] private float attackStrikeLean = 45f;
        [SerializeField] private float attackLunge = 0.35f;

        [Header("Other")]
        [SerializeField] private float angryShakeAngle = 4f;
        [SerializeField] private float deathFallDuration = 0.45f;

        [Header("Defeat puff")]
        [Tooltip("After being defeated: seconds of tipping over before the smoke puff.")]
        [SerializeField] private float puffDelay = 0.25f;
        [Tooltip("Size of the smoke puff (1 = carrot size).")]
        [SerializeField] private float puffSize = 1f;
        [Tooltip("How fast the carrot shrinks away inside the puff (seconds).")]
        [SerializeField] private float vanishDuration = 0.12f;
        [Tooltip("Below this horizontal speed (m/s) the carrot counts as standing still.")]
        [SerializeField] private float movingThreshold = 0.25f;

        private AngryCarrot carrot;
        private Rigidbody rb;
        private Vector3 restPos, restScale;
        private Quaternion restRot;

        private float gaitPhase;
        private float walkW, runW, angryW;
        private float spawnT = 1f, attackT = 1f, deathT;
        private float idleTime;
        private Vector3 lastPos;
        private float deathRollSign = 1f;
        private float deadTime;
        private bool puffed;

        public void PlaySpawn() { spawnT = 0f; }
        public void PlayAttack() { attackT = 0f; }

        private void Awake()
        {
            carrot = GetComponent<AngryCarrot>();
            rb = GetComponent<Rigidbody>();
            if (visual == null) visual = transform.Find("Visual");
            if (visual == null && transform.childCount > 0) visual = transform.GetChild(0);
            if (visual == null) { enabled = false; return; }
            restPos = visual.localPosition;
            restRot = visual.localRotation;
            restScale = visual.localScale;
            lastPos = transform.position;
            deathRollSign = Random.value < 0.5f ? -1f : 1f;
        }

        private void OnEnable()
        {
            if (carrot != null) carrot.OnAttack += PlayAttack;
        }

        private void OnDisable()
        {
            if (carrot != null) carrot.OnAttack -= PlayAttack;
        }

        private void Start()
        {
            if (playSpawnOnStart) PlaySpawn();
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            bool frozen = carrot.IsFrozen;
            if (frozen) return;   // keep the current pose: the carrot looks "frozen in place"

            // --- how fast is it going? ---
            Vector3 vel = rb != null ? rb.linearVelocity : (transform.position - lastPos) / Mathf.Max(dt, 1e-5f);
            lastPos = transform.position;
            vel.y = 0f;
            float speed = vel.magnitude;
            bool moving = speed > movingThreshold && !carrot.IsPinned;
            bool dashing = carrot.StateMachine.Current is AngryCarrotDashState;

            float k = 1f - Mathf.Exp(-10f * dt);   // smoothing
            walkW = Mathf.Lerp(walkW, moving && !dashing ? 1f : 0f, k);
            runW = Mathf.Lerp(runW, moving && dashing ? 1f : 0f, k);
            angryW = Mathf.Lerp(angryW, !moving && dashing ? 1f : 0f, k);

            float hops = Mathf.Lerp(walkHopsPerSecond, runHopsPerSecond, runW / Mathf.Max(walkW + runW, 1e-3f));
            if (walkW + runW > 0.01f) gaitPhase += dt * hops;
            idleTime += dt;

            // --- gait ---
            float s = Mathf.Abs(Mathf.Sin(gaitPhase * Mathf.PI));     // 0 on the ground, 1 at the top of the hop
            float side = Mathf.Sin(gaitPhase * Mathf.PI);
            float lift = s * (walkHopHeight * walkW + runHopHeight * runW);
            float pitch = walkLean * walkW + runLean * runW;
            float roll = side * (walkWaddleAngle * walkW + runWaddleAngle * runW);
            float moveW = Mathf.Clamp01(walkW + runW);
            float stretch = 1f + moveW * (airStretch * s - landingSquash * Mathf.Pow(1f - s, 4f));

            // --- idle breathing + angry shaking ---
            float idleW = 1f - Mathf.Clamp01(moveW + angryW);
            stretch += idleW * 0.03f * Mathf.Sin(idleTime * 2.2f);
            roll += angryW * angryShakeAngle * Mathf.Sin(idleTime * 38f);
            pitch += angryW * 8f;   // glaring forward

            float forward = 0f;

            // --- attack ---
            if (attackT < 1f)
            {
                attackT = Mathf.Min(1f, attackT + dt / attackDuration);
                float a = attackT;
                float lean, lunge, sq;
                if (a < 0.3f)        // wind up
                {
                    float u = Smooth(a / 0.3f);
                    lean = Mathf.Lerp(0f, attackWindupLean, u); lunge = -0.05f * u; sq = Mathf.Lerp(1f, 0.9f, u);
                }
                else if (a < 0.55f)  // strike
                {
                    float u = Smooth((a - 0.3f) / 0.25f);
                    lean = Mathf.Lerp(attackWindupLean, attackStrikeLean, u); lunge = Mathf.Lerp(-0.05f, attackLunge, u); sq = Mathf.Lerp(0.9f, 1.12f, u);
                }
                else                 // recover
                {
                    float u = Smooth((a - 0.55f) / 0.45f);
                    lean = Mathf.Lerp(attackStrikeLean, 0f, u); lunge = Mathf.Lerp(attackLunge, 0f, u); sq = Mathf.Lerp(1.12f, 1f, u);
                }
                pitch += lean; forward += lunge; stretch *= sq;
            }

            // --- spawn: pop out of the ground ---
            float spawnOffset = 0f;
            if (spawnT < 1f)
            {
                spawnT = Mathf.Min(1f, spawnT + dt / spawnDuration);
                float rise = EaseOutBack(Mathf.Clamp01(spawnT / 0.7f));
                spawnOffset = -spawnDepth * (1f - rise);
                float landing = Mathf.Clamp01((spawnT - 0.6f) / 0.4f);
                stretch *= spawnT < 0.6f ? 1.15f : 1f + 0.15f * Mathf.Sin(landing * Mathf.PI * 2f) * (1f - landing) - 0.12f * Mathf.Sin(landing * Mathf.PI);
                roll += 10f * Mathf.Sin(spawnT * 18f) * (1f - spawnT);
            }

            // --- death: tip over, then "puff" into white smoke and vanish ---
            if (carrot.IsDead)
            {
                deadTime += dt;
                if (!puffed && deadTime >= puffDelay) Puff();
                if (puffed)
                {
                    float v = Mathf.Clamp01((deadTime - puffDelay) / vanishDuration);
                    float sc = 1f - v * v;
                    visual.localScale = restScale * Mathf.Max(sc, 0f);
                    if (v >= 1f) visual.gameObject.SetActive(false);
                    return;
                }
                deathT = Mathf.Min(1f, deathT + dt / deathFallDuration);
                float f = EaseOutBounce(deathT);
                roll = Mathf.Lerp(roll, 85f * deathRollSign, f);
                pitch = Mathf.Lerp(pitch, 0f, f);
                lift = 0f; forward = 0f;
                stretch = Mathf.Lerp(stretch, 0.92f, f);
            }

            Apply(pitch, roll, lift + spawnOffset, forward, stretch);
        }

        private void Puff()
        {
            puffed = true;
            Bounds b = new Bounds(transform.position + Vector3.up * restPos.y, Vector3.zero);
            bool first = true;
            foreach (var r in visual.GetComponentsInChildren<Renderer>())
            {
                if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds);
            }
            DeathPuff.Spawn(b.center, puffSize);
            // nothing to bump into any more
            foreach (var col in GetComponentsInChildren<Collider>()) col.enabled = false;
            if (rb != null) { rb.linearVelocity = Vector3.zero; rb.isKinematic = true; }
        }

        // Rotates / squashes around the bottom of the carrot (the ground point), in the carrot's own space.
        private void Apply(float pitch, float roll, float up, float forward, float stretch)
        {
            Quaternion r = Quaternion.Euler(pitch, 0f, -roll);
            float wide = 1f / Mathf.Sqrt(Mathf.Max(stretch, 0.2f));
            Vector3 p = restPos;
            p.y *= stretch;
            visual.localPosition = r * p + new Vector3(0f, up, forward);
            visual.localRotation = r * restRot;
            visual.localScale = Vector3.Scale(restScale, new Vector3(wide, stretch, wide));
        }

        private static float Smooth(float x) => x * x * (3f - 2f * x);

        private static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }

        private static float EaseOutBounce(float x)
        {
            const float n1 = 7.5625f, d1 = 2.75f;
            if (x < 1f / d1) return n1 * x * x;
            if (x < 2f / d1) { x -= 1.5f / d1; return n1 * x * x + 0.75f; }
            if (x < 2.5f / d1) { x -= 2.25f / d1; return n1 * x * x + 0.9375f; }
            x -= 2.625f / d1; return n1 * x * x + 0.984375f;
        }

#if UNITY_EDITOR
        /// <summary>Editor-only: shows one pose without entering Play mode.</summary>
        public void EditorPose(string anim, float t)
        {
            if (visual == null) Awake();
            float s = Mathf.Abs(Mathf.Sin(t * Mathf.PI));
            switch (anim)
            {
                case "walk": Apply(walkLean, Mathf.Sin(t * Mathf.PI) * walkWaddleAngle, s * walkHopHeight, 0f, 1f + airStretch * s); break;
                case "run": Apply(runLean, Mathf.Sin(t * Mathf.PI) * runWaddleAngle, s * runHopHeight, 0f, 1f + airStretch * s); break;
                case "attack": Apply(attackStrikeLean, 0f, 0f, attackLunge, 1.12f); break;
                case "windup": Apply(attackWindupLean, 0f, 0f, -0.05f, 0.9f); break;
                case "spawn": Apply(0f, 0f, -spawnDepth * 0.5f, 0f, 1.15f); break;
                case "dead": Apply(0f, 85f, 0f, 0f, 0.92f); break;
                default: Apply(0f, 0f, 0f, 0f, 1f); break;
            }
        }

        public void EditorReset()
        {
            if (visual == null) return;
            visual.localPosition = restPos; visual.localRotation = restRot; visual.localScale = restScale;
        }
#endif
    }
}
