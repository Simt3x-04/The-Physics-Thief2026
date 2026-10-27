using System;
using UnityEngine;
using PhysicsThief.Properties;

namespace PhysicsThief.Player
{
    /// <summary>Mira un PhysicalObject, lo scansiona e copia/incolla le sue proprieta' tramite la valigetta.</summary>
    public class PlayerScanner : MonoBehaviour
    {
        [SerializeField] private Transform rayOrigin;
        [SerializeField] private float rayDistance = 15f;
        [SerializeField] private LayerMask scanLayer;
        [SerializeField] private BriefcaseInventory inventory;
        [SerializeField] private PlayerInputHandler input;

        public PhysicalObject CurrentTarget { get; private set; }

        /// <summary>L'HUD si iscrive qui per mostrare/nascondere l'overlay dello scanner.</summary>
        public event Action<PhysicalObject> OnTargetChanged;

        /// <summary>TODO: aggiorna CurrentTarget con ScanTarget() e invoca OnTargetChanged se cambia.</summary>
        private void Update() { }

        /// <summary>TODO: Physics.Raycast da rayOrigin su scanLayer, ritorna il PhysicalObject colpito (o null).</summary>
        public PhysicalObject ScanTarget() { return default; }

        /// <summary>TODO: prende la proprieta' dal bersaglio e la mette nella valigetta. Ritorna true se riuscito.</summary>
        public bool CopyProperty(AttributeType type) { return default; }

        /// <summary>TODO: consuma lo slot attivo e chiama CurrentTarget.ApplyProperty(...). Ritorna true se riuscito.</summary>
        public bool PasteProperty() { return default; }

        /// <summary>TODO: incolla in sequenza tutte le proprieta' presenti nella valigetta.</summary>
        public void SequencePaste() { }
    }
}
