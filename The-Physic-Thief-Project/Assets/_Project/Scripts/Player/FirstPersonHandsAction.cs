using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PhysicsThief.Properties;

namespace PhysicsThief.Player
{
    /// <summary>
    /// First-person hand animation for copy / paste.
    /// Uses a pair of "first-person arms" attached to the camera (FP_Viewmodel, built by Tools/Hands/Build First-Person Viewmodel),
    /// rendered by a separate overlay camera, so they never clip into the body or the scene and never change the player's view direction.
    /// Copy : the right hand lifts the bag to the lower right and opens it -> the left hand reaches out and "grabs" -> puts it in the bag -> closes and lowers it.
    /// Paste: the right hand lifts the bag and opens it -> the left hand takes something out -> reaches forward and lets go -> closes and lowers the bag.
    /// Plays when the PlayerScanner really copies or pastes. Movement is locked during the action (looking around is not).
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class FirstPersonHandsAction : MonoBehaviour
    {
        public enum ActionType { Copy, Paste }

        [Header("References")]
        public PlayerController player;
        [Tooltip("First-person arms attached under the camera (FP_Viewmodel).")]
        public Transform viewmodel;
        [Tooltip("The real bag on the character body, hidden during the action.")]
        public Transform worldBag;

        [Header("Trigger")]
        [Tooltip("The animation follows this scanner. Found under the player if left empty.")]
        public PlayerScanner scanner;
        public bool requireStanding = true;

        [Header("Poses (relative to the camera: x right, y up, z forward, meters)")]
        public Vector3 rightHandHold = new Vector3(0.18f, 0.03f, 0.28f);
        public Vector3 leftHandReady = new Vector3(-0.17f, -0.06f, 0.28f);
        public Vector3 leftHandReach = new Vector3(-0.09f, 0.03f, 0.36f);
        [Tooltip("How far the bag opening is below the handle (meters).")]
        public float mouthBelowHand = 0.05f;
        [Tooltip("How far to the left of the bag the left hand goes in (meters), so the left arm does not cross in front of the bag.")]
        public float mouthLeftOffset = 0.0f;
        [Tooltip("How far toward the open side of the bag the left hand goes in (meters).")]
        public float mouthTowardOpening = 0.04f;
        [Tooltip("How much further down the left hand dips into the opening.")]
        public float mouthDip = 0.02f;
        public Vector3 rightElbowHint = new Vector3(1.0f, -0.4f, 0.0f);
        public Vector3 leftElbowHint = new Vector3(-1.0f, -0.9f, 0.0f);

        [Header("Palm direction (camera space)")]
        [Tooltip("Right palm: facing inward (left) and slightly toward the camera.")]
        public Vector3 rightPalmDir = new Vector3(-1f, -0.1f, -0.4f);
        [Tooltip("Left palm: facing inward (right) and slightly toward the camera.")]
        public Vector3 leftPalmDir = new Vector3(1f, -0.2f, -0.4f);
        [Tooltip("Palm direction while the left hand is in the bag: down and inward.")]
        public Vector3 leftPalmDirInBag = new Vector3(0.6f, -0.8f, -0.2f);

        [Header("Bag")]
        [Tooltip("Tilt of the bag opening toward the camera, so the open bag is easy to see.")]
        public float bagTiltTowardCamera = 15f;
        public float bagOpenAngle = 30f;
        [Tooltip("Rotation of the bag around the vertical axis. 90 = side toward the camera, open side toward the left hand.")]
        public float bagYaw = 90f;
        [Tooltip("How much the bag swings to the left (toward the body), to avoid clipping into the right forearm.")]
        public float bagSwingInward = 18f;
        [Tooltip("Where the handle sits in the palm: meters from the wrist along the fingers.")]
        public float gripAlongFingers = 0.07f;
        [Tooltip("Where the handle sits in the palm: meters toward the palm side.")]
        public float gripTowardPalm = 0.025f;

        [Header("Timing (seconds)")]
        public float duration = 1.75f;
        [Range(0.5f, 2f)] public float speed = 1f;

        [Header("Fingers")]
        public float fingerCurlAngle = 45f;
        [Range(0, 1)] public float rightGrip = 0.6f;

        public bool IsPlaying => playing;
        public event System.Action<ActionType, int> OnActionFinished;

        Transform cam;
        Transform rArm, rFore, rHand, lArm, lFore, lHand, bag, bagFront;
        Renderer[] vmRenderers = new Renderer[0];
        Renderer[] worldBagRenderers = new Renderer[0];
        readonly Dictionary<Transform, Quaternion> rest = new Dictionary<Transform, Quaternion>();
        readonly List<(Transform bone, Vector3 axis, float scale)> lCurl = new List<(Transform, Vector3, float)>();
        readonly List<(Transform bone, Vector3 axis, float scale)> rCurl = new List<(Transform, Vector3, float)>();
        Vector3 bagUpInBag, bagFrontInBag, bagHandleInBag;
        Vector3 bagRestLocalPos; Quaternion bagRestLocalRot;
        Vector3 rPalmInHand, rFingersInHand, lPalmInHand, lFingersInHand;
        Quaternion bagFrontRest;
        float openSign = 1f;
        Renderer bagBackR, bagFrontR;
        bool initialized;

        bool playing, needsRestore;
        float t;
        ActionType type;
        int attribute, lastCopiedAttribute;

        PlayerScanner subscribedScanner;
        bool started;

        // Subscribed from Start, not Awake: PlayerController.Start moves the camera (which holds the scanner) under the player.
        void Start()
        {
            started = true;
            Init();
            Subscribe();
        }

        void OnEnable()
        {
            if (started) Subscribe();
        }

        void Subscribe()
        {
            if (subscribedScanner) return;
            if (!scanner) scanner = GetComponentInChildren<PlayerScanner>(true);
            if (!scanner)
            {
                Debug.LogWarning("[FirstPersonHandsAction] No PlayerScanner found: the hands will not animate.", this);
                return;
            }
            subscribedScanner = scanner;
            subscribedScanner.OnPropertyCopied += HandleCopied;
            subscribedScanner.OnPropertyPasted += HandlePasted;
        }

        void Unsubscribe()
        {
            if (!subscribedScanner) return;
            subscribedScanner.OnPropertyCopied -= HandleCopied;
            subscribedScanner.OnPropertyPasted -= HandlePasted;
            subscribedScanner = null;
        }

        void HandleCopied(AttributeType attributeType) => Play(ActionType.Copy, (int)attributeType);
        void HandlePasted() => Play(ActionType.Paste);

        void Init()
        {
            if (initialized) return;
            if (!player) player = GetComponent<PlayerController>();
            if (!viewmodel)
            {
                Debug.LogError("[FirstPersonHandsAction] No viewmodel assigned: run Tools/Hands/Build First-Person Viewmodel first.", this);
                enabled = false; return;
            }
            cam = viewmodel.parent;
            var all = viewmodel.GetComponentsInChildren<Transform>(true);
            Transform F(string n) => all.FirstOrDefault(x => x.name == "mixamorig:" + n);
            rArm = F("RightArm"); rFore = F("RightForeArm"); rHand = F("RightHand");
            lArm = F("LeftArm"); lFore = F("LeftForeArm"); lHand = F("LeftHand");
            bag = all.FirstOrDefault(x => x.name == "Bag_Split");
            bagFront = all.FirstOrDefault(x => x.name == "Bag_Front");
            vmRenderers = viewmodel.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled).ToArray();
            if (worldBag) worldBagRenderers = worldBag.GetComponentsInChildren<Renderer>(true);

            foreach (var b in new[] { rArm, rFore, rHand, lArm, lFore, lHand }) rest[b] = b.localRotation;
            (lPalmInHand, lFingersInHand) = SetupFingers(all, "Left", lCurl, lHand);
            (rPalmInHand, rFingersInHand) = SetupFingers(all, "Right", rCurl, rHand);
            foreach (var c in lCurl.Concat(rCurl)) rest[c.bone] = c.bone.localRotation;

            if (bag && bagFront)
            {
                bagFrontRest = bagFront.localRotation;
                var backR = bag.GetComponent<Renderer>();
                var frontR = bagFront.GetComponent<Renderer>();
                bagBackR = backR; bagFrontR = frontR;
                Bounds b2 = backR.bounds; b2.Encapsulate(frontR.bounds);
                Vector3 upW = (b2.center - bagFront.position).normalized;
                Vector3 frontW = Vector3.ProjectOnPlane(frontR.bounds.center - backR.bounds.center, upW).normalized;
                bagUpInBag = Quaternion.Inverse(bag.rotation) * upW;
                bagFrontInBag = Quaternion.Inverse(bag.rotation) * frontW;
                // Highest point of the handle (along the bag's "up"), used as the point held in the palm
                var mb = bag.GetComponent<MeshFilter>().sharedMesh.bounds;   // the bag's local bounds (no readable mesh needed at runtime)
                Vector3 u = bagUpInBag;
                float ext = Mathf.Abs(u.x) * mb.extents.x + Mathf.Abs(u.y) * mb.extents.y + Mathf.Abs(u.z) * mb.extents.z;
                bagHandleInBag = mb.center + u * (ext * 0.95f);
                bagRestLocalPos = bag.localPosition; bagRestLocalRot = bag.localRotation;
                float d0 = Vector3.Distance(frontR.bounds.center, backR.bounds.center);
                bagFront.localRotation = bagFrontRest * Quaternion.Euler(20f, 0, 0);
                float d1 = Vector3.Distance(frontR.bounds.center, backR.bounds.center);
                bagFront.localRotation = bagFrontRest;
                openSign = d1 >= d0 ? 1f : -1f;
            }
            SetViewmodelVisible(false);
            initialized = true;
        }

        (Vector3 palm, Vector3 fingers) SetupFingers(Transform[] all, string side, List<(Transform, Vector3, float)> list, Transform hand)
        {
            Transform F(string n) => all.FirstOrDefault(x => x.name == "mixamorig:" + side + n);
            var i1 = F("HandIndex1"); var i4 = F("HandIndex4"); var r1 = F("HandRing1");
            if (!i1 || !i4 || !r1) return (Vector3.up, Vector3.forward);
            Vector3 across = (r1.position - i1.position).normalized;
            Vector3 fingerDir = (i4.position - i1.position).normalized;
            Vector3 palm = Vector3.Cross(across, fingerDir).normalized;
            // With the arms hanging naturally, the palms face the body's center line
            Vector3 toBody = Vector3.ProjectOnPlane(viewmodel.position - i1.position, viewmodel.up);
            if (Vector3.Dot(palm, toBody) < 0) palm = -palm;
            foreach (var f in new[] { "Index", "Ring" })
                for (int k = 1; k <= 3; k++)
                {
                    var b = F("Hand" + f + k);
                    if (!b) continue;
                    var child = F("Hand" + f + (k + 1));
                    Vector3 dir = child ? (child.position - b.position).normalized : fingerDir;
                    Vector3 axisW = Vector3.Cross(dir, palm).normalized;
                    list.Add((b, Quaternion.Inverse(b.rotation) * axisW, k == 1 ? 0.8f : 1f));
                }
            Vector3 knuckles = ((i1.position + r1.position) * 0.5f - hand.position).normalized;
            return (Quaternion.Inverse(hand.rotation) * palm, Quaternion.Inverse(hand.rotation) * knuckles);
        }

        void SetViewmodelVisible(bool v)
        {
            foreach (var r in vmRenderers) if (r) r.enabled = v;
            foreach (var r in worldBagRenderers) if (r) r.enabled = !v;
        }

        // ------------------------------------------------------------ Trigger
        public bool Play(ActionType action, int attributeIndex = -1)
        {
            if (!initialized) Init();
            if (playing || !enabled) return false;
            if (requireStanding && player && !player.IsIdleOnGround) return false;
            type = action;
            attribute = attributeIndex >= 0 ? attributeIndex : lastCopiedAttribute;
            if (action == ActionType.Copy) lastCopiedAttribute = attribute;
            t = 0; playing = true; needsRestore = true;
            if (player) player.inputLocked = true;
            SetViewmodelVisible(true);
            return true;
        }

        void LateUpdate() => Step(Time.deltaTime);

        void Step(float dt)
        {
            if (!playing && !needsRestore) return;
            foreach (var kv in rest) kv.Key.localRotation = kv.Value;
            if (bag) { bag.localPosition = bagRestLocalPos; bag.localRotation = bagRestLocalRot; }

            if (!playing)
            {
                if (bagFront) bagFront.localRotation = bagFrontRest;
                SetViewmodelVisible(false);
                if (player) player.inputLocked = false;
                needsRestore = false;
                return;
            }

            t += dt * speed;
            float T = duration;
            // The hands come up from below the screen and go back down at the end
            float w = Smooth(Ramp(t, 0f, 0.35f)) * (1f - Smooth(Ramp(t, T - 0.35f, T)));

            // ---- Right hand: holds the bag ----
            Vector3 rTarget = cam.TransformPoint(rightHandHold);
            SolveArm(rArm, rFore, rHand, rTarget, cam.TransformPoint(rightElbowHint), null, w);
            OrientHand(rHand, rFore, rPalmInHand, rFingersInHand, cam.rotation * rightPalmDir.normalized, w);
            Curl(rCurl, rightGrip * w, fingerCurlAngle);

            // Bag: hangs naturally (slightly tilted toward the camera), with the handle kept in the right hand
            // Order: first rotate around the vertical axis by bagYaw, then tilt toward the camera, then swing inward
            Quaternion orient = Quaternion.AngleAxis(-bagSwingInward, Vector3.forward)
                              * Quaternion.AngleAxis(-bagTiltTowardCamera, Vector3.right)
                              * Quaternion.AngleAxis(-bagYaw, Vector3.up);
            Vector3 upW = cam.rotation * (orient * Vector3.up);
            Vector3 frontW = cam.rotation * (orient * Vector3.forward);
            if (bag)
            {
                Quaternion want = Quaternion.LookRotation(frontW, upW) * Quaternion.Inverse(Quaternion.LookRotation(bagFrontInBag, bagUpInBag));
                bag.rotation = Quaternion.Slerp(bag.rotation, want, w);
                Vector3 grip = rHand.position
                    + rHand.rotation * rFingersInHand * gripAlongFingers
                    + rHand.rotation * rPalmInHand * gripTowardPalm;
                bag.position += Vector3.Lerp(rHand.position, grip, w) - bag.TransformPoint(bagHandleInBag);
            }

            // ---- Opening and closing the bag ----
            float open = Smooth(Ramp(t, 0.3f, 0.5f)) * (1f - Smooth(Ramp(t, 1.3f, 1.5f)));
            if (bagFront) bagFront.localRotation = bagFrontRest * Quaternion.Euler(openSign * bagOpenAngle * open, 0, 0);

            // ---- Left hand ----
            Vector3 mouth;
            if (bagBackR && bagFrontR)
            {
                Bounds bb = bagBackR.bounds; bb.Encapsulate(bagFrontR.bounds);
                float ext = Mathf.Abs(upW.x) * bb.extents.x + Mathf.Abs(upW.y) * bb.extents.y + Mathf.Abs(upW.z) * bb.extents.z;
                // Bag opening: upper part of the bag, toward the open side (the gap when the bag is open)
                Vector3 top = bb.center + upW * (ext * 0.35f) + frontW * mouthTowardOpening;
                mouth = cam.InverseTransformPoint(top) + Vector3.left * mouthLeftOffset;
            }
            else
                mouth = cam.InverseTransformPoint(rHand.position) + cam.InverseTransformDirection(-upW) * mouthBelowHand;
            Vector3 mouthIn = mouth + cam.InverseTransformDirection(-upW) * mouthDip;
            Vector3 ready = leftHandReady, reach = leftHandReach;
            Vector3 lLocal; float curl;
            if (type == ActionType.Copy)
            {
                lLocal = Keys(t, (0f, ready), (0.35f, ready), (0.65f, reach), (0.85f, reach), (1.05f, mouth), (1.15f, mouthIn), (1.3f, mouthIn), (1.45f, ready));
                curl = Smooth(Ramp(t, 0.72f, 0.84f)) * (1f - Smooth(Ramp(t, 1.18f, 1.3f)));
            }
            else
            {
                lLocal = Keys(t, (0f, ready), (0.35f, ready), (0.5f, mouth), (0.6f, mouthIn), (0.75f, mouthIn), (0.85f, mouth), (1.1f, reach), (1.3f, reach), (1.45f, ready));
                curl = Smooth(Ramp(t, 0.62f, 0.74f)) * (1f - Smooth(Ramp(t, 1.12f, 1.22f)));
            }
            SolveArm(lArm, lFore, lHand, cam.TransformPoint(lLocal), cam.TransformPoint(leftElbowHint), null, w);
            float inBag = type == ActionType.Copy
                ? Smooth(Ramp(t, 0.85f, 1.05f)) * (1f - Smooth(Ramp(t, 1.3f, 1.45f)))
                : Smooth(Ramp(t, 0.35f, 0.5f)) * (1f - Smooth(Ramp(t, 0.85f, 1.05f)));
            Vector3 lPalm = Vector3.Slerp(leftPalmDir.normalized, leftPalmDirInBag.normalized, inBag);
            OrientHand(lHand, lFore, lPalmInHand, lFingersInHand, cam.rotation * lPalm, w);
            Curl(lCurl, curl * w, fingerCurlAngle);

            if (t >= T)
            {
                playing = false;
                OnActionFinished?.Invoke(type, attribute);
            }
        }

        void OnDisable()
        {
            Unsubscribe();
            if (player) player.inputLocked = false;
            if (initialized) SetViewmodelVisible(false);
        }

#if UNITY_EDITOR
        /// <summary>Poses the animation at second "time" without entering Play mode (for checking).</summary>
        public void EditorPreview(ActionType action, float time)
        {
            Init();
            foreach (var kv in rest) kv.Key.localRotation = kv.Value;
            type = action; t = time; playing = true; needsRestore = true;
            SetViewmodelVisible(true);
            var p = player; player = null;
            Step(0f);
            player = p; playing = false; needsRestore = false;
        }
#endif

        // ------------------------------------------------------------ Helpers
        static float Ramp(float x, float a, float b) => Mathf.Clamp01((x - a) / (b - a));
        static float Smooth(float x) => x * x * (3f - 2f * x);

        static Vector3 Keys(float x, params (float time, Vector3 pos)[] k)
        {
            if (x <= k[0].time) return k[0].pos;
            for (int i = 0; i < k.Length - 1; i++)
                if (x <= k[i + 1].time)
                    return Vector3.Lerp(k[i].pos, k[i + 1].pos, Smooth(Ramp(x, k[i].time, k[i + 1].time)));
            return k[k.Length - 1].pos;
        }

        static void Curl(List<(Transform bone, Vector3 axis, float scale)> list, float amount, float maxAngle)
        {
            foreach (var c in list)
                c.bone.localRotation = c.bone.localRotation * Quaternion.AngleAxis(amount * maxAngle * c.scale, c.axis);
        }

        /// <summary>Makes the fingers follow the forearm, with the palm facing palmWorld.</summary>
        static void OrientHand(Transform hand, Transform fore, Vector3 palmLocal, Vector3 fingersLocal, Vector3 palmWorld, float weight)
        {
            if (weight <= 0.0001f) return;
            Vector3 fingersWorld = (hand.position - fore.position).normalized;
            Vector3 p = Vector3.ProjectOnPlane(palmWorld, fingersWorld);
            if (p.sqrMagnitude < 1e-6f) return;
            Vector3 pl = Vector3.ProjectOnPlane(palmLocal, fingersLocal);
            Quaternion want = Quaternion.LookRotation(fingersWorld, p.normalized) * Quaternion.Inverse(Quaternion.LookRotation(fingersLocal, pl.normalized));
            hand.rotation = Quaternion.Slerp(hand.rotation, want, weight);
        }

        /// <summary>Two-bone IK: A = upper arm, B = forearm, C = hand.</summary>
        static void SolveArm(Transform A, Transform B, Transform C, Vector3 target, Vector3 hint, Quaternion? handRot, float weight)
        {
            if (weight <= 0.0001f) return;
            Quaternion handRest = C.rotation;
            Vector3 goal = Vector3.Lerp(C.position, target, weight);
            float lab = Vector3.Distance(A.position, B.position);
            float lbc = Vector3.Distance(B.position, C.position);
            float lat = Mathf.Clamp(Vector3.Distance(A.position, goal), Mathf.Abs(lab - lbc) + 0.001f, lab + lbc - 0.001f);

            Vector3 ba = A.position - B.position, bc = C.position - B.position;
            Vector3 n = Vector3.Cross(ba, bc);
            if (n.sqrMagnitude < 1e-8f) n = Vector3.Cross(ba, hint - B.position);
            n.Normalize();
            float cur = Vector3.Angle(ba, bc);
            float want = Mathf.Acos(Mathf.Clamp((lab * lab + lbc * lbc - lat * lat) / (2 * lab * lbc), -1f, 1f)) * Mathf.Rad2Deg;
            B.rotation = Quaternion.AngleAxis(want - cur, n) * B.rotation;

            A.rotation = Quaternion.FromToRotation(C.position - A.position, goal - A.position) * A.rotation;

            Vector3 axis = (goal - A.position).normalized;
            Vector3 e = Vector3.ProjectOnPlane(B.position - A.position, axis);
            Vector3 h = Vector3.ProjectOnPlane(hint - A.position, axis);
            if (e.sqrMagnitude > 1e-6f && h.sqrMagnitude > 1e-6f)
                A.rotation = Quaternion.AngleAxis(Vector3.SignedAngle(e, h, axis), axis) * A.rotation;

            if (handRot.HasValue) C.rotation = Quaternion.Slerp(handRest, handRot.Value, weight);
        }
    }
}
