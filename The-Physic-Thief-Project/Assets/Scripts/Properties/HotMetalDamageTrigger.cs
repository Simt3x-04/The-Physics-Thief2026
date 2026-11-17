using UnityEngine;
using PhysicsThief.Core;

namespace PhysicsThief.Properties
{
    /// <summary>
    /// Minimal contact-damage trigger for the HUDtest scene.
    ///
    /// Place this on any Collider (set Is Trigger = true) that should deal
    /// heat damage on contact.  Represents the "hot-metal contact damage"
    /// from Davide's temperature work while TemperatureReactor.HandlePropertyChange
    /// is still a stub.
    ///
    /// Settings
    /// ────────
    ///   damagePerSecond — HP per second while the player overlaps the trigger.
    ///   playerTag       — tag used to identify the player collider.
    ///
    /// Remove this component and use TemperatureReactor directly once
    /// Davide wires HandlePropertyChange to deal contact damage.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class HotMetalDamageTrigger : MonoBehaviour
    {
        [SerializeField] private float damagePerSecond = 20f;
        [SerializeField] private string playerTag = "Player";

        private HealthComponent _playerHealth;
        private bool _inContact;

        // ── Trigger callbacks ────────────────────────────────────────────────
        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;
            _playerHealth = other.GetComponentInParent<HealthComponent>()
                         ?? other.GetComponent<HealthComponent>();
            _inContact = _playerHealth != null;
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;
            _playerHealth = null;
            _inContact = false;
        }

        private void Update()
        {
            if (!_inContact || _playerHealth == null || _playerHealth.IsDead) return;
            _playerHealth.TakeDamage(damagePerSecond * Time.deltaTime);
        }

        // ── Gizmo ────────────────────────────────────────────────────────────
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.3f, 0f, 0.4f);
            var col = GetComponent<Collider>();
            if (col is BoxCollider bc)
                Gizmos.DrawCube(bc.bounds.center, bc.bounds.size);
            else if (col is SphereCollider sc)
                Gizmos.DrawSphere(sc.bounds.center, sc.radius * transform.lossyScale.x);
        }
    }
}
