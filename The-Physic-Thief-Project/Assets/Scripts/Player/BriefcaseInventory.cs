using System;
using UnityEngine;

namespace PhysicsThief.Player
{
    /// <summary>
    /// Guscio MonoBehaviour della valigetta: tiene un BriefcaseModel (le regole) e ne inoltra l'evento Changed.
    /// Chi scansiona/incolla usa Model (es. inventory.Model.TryAdd(...), inventory.Model.ConsumeSelected()).
    /// </summary>
    public class BriefcaseInventory : MonoBehaviour
    {
        [SerializeField] private int startingSlots = 2;

        private BriefcaseModel model;

        /// <summary>Creato al primo accesso: sicuro anche se l'HUD si iscrive prima dell'Awake di questo oggetto.</summary>
        public BriefcaseModel Model => model ??= new BriefcaseModel(startingSlots);

        public event Action Changed
        {
            add { Model.Changed += value; }
            remove { Model.Changed -= value; }
        }
    }
}
