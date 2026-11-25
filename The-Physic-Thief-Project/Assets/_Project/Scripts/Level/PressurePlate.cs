using System;
using System.Collections.Generic;
using UnityEngine;

namespace PhysicsThief.Level
{
    /// <summary>
    /// Pressure plate (GDD 2.4 and 3.8): it is pressed while the objects resting on it weigh at least 'requiredMass'
    /// and it needs that condition continuously: when a pasted mass expires, the plate is released and the gate closes.
    /// It only reads Rigidbody masses, so it works with any object and knows nothing about reactors or the scanner.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PressurePlate : MonoBehaviour
    {
        [SerializeField] private Gate gate;
        [Tooltip("GDD 3.2: 500 kg or more crushes buttons.")]
        [SerializeField] private float requiredMass = 500f;
        [Tooltip("How far above the top of the plate objects are detected, in meters.")]
        [SerializeField] private float detectionHeight = 0.3f;
        [SerializeField] private LayerMask detectionLayers = ~0;

        [Header("Visual (optional)")]
        [Tooltip("Optional child that sinks when the plate is pressed.")]
        [SerializeField] private Transform visual;
        [SerializeField] private float pressDepth = 0.08f;

        private const float Skin = 0.05f;   // the detection box also reaches slightly into the plate, to catch resting objects

        private Collider plateCollider;
        private readonly HashSet<Rigidbody> bodies = new HashSet<Rigidbody>();
        private Vector3 visualStartPosition;

        public bool IsPressed { get; private set; }

        /// <summary>Total mass currently on the plate (kg), updated every physics step.</summary>
        public float CurrentMass { get; private set; }

        public event Action<bool> OnPressedChanged;

        private void Awake()
        {
            plateCollider = GetComponent<Collider>();
            if (visual != null) visualStartPosition = visual.localPosition;
        }

        private void FixedUpdate()
        {
            // Box right above the plate: same footprint as the collider, 'detectionHeight' tall.
            Bounds bounds = plateCollider.bounds;
            Vector3 half = new Vector3(bounds.extents.x, detectionHeight * 0.5f + Skin * 0.5f, bounds.extents.z);
            Vector3 center = new Vector3(bounds.center.x, bounds.max.y + half.y - Skin, bounds.center.z);

            Collider[] hits = Physics.OverlapBox(center, half, Quaternion.identity, detectionLayers, QueryTriggerInteraction.Ignore);

            bodies.Clear();
            float total = 0f;
            foreach (Collider hit in hits)
            {
                Rigidbody body = hit.attachedRigidbody;
                if (body == null || body.isKinematic) continue;   // only free objects weigh on the plate
                if (bodies.Add(body)) total += body.mass;
            }
            CurrentMass = total;

            SetPressed(total >= requiredMass);
        }

        private void SetPressed(bool pressed)
        {
            if (pressed == IsPressed) return;
            IsPressed = pressed;

            if (gate != null) gate.SetOpen(pressed);
            if (visual != null) visual.localPosition = visualStartPosition + (pressed ? Vector3.down * pressDepth : Vector3.zero);
            OnPressedChanged?.Invoke(pressed);
        }
    }
}
