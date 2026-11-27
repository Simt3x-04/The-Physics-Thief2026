using System;
using UnityEngine;

namespace PhysicsThief.Player
{
    /// <summary>
    /// MonoBehaviour shell for the briefcase: it holds a BriefcaseModel (the rules) and forwards its 'Changed' event.
    /// Components that perform scanning or pasting interact with the Model (e.g., inventory.Model.TryAdd(...), inventory.Model.ConsumeSelected()).
    /// </summary>
    public class BriefcaseInventory : MonoBehaviour
    {
        [SerializeField] private int startingSlots = 2;

        private BriefcaseModel model;

        /// <summary>Created upon first access: safe even if the HUD registers before this object's Awake [method].</summary>
        public BriefcaseModel Model => model ??= new BriefcaseModel(startingSlots);

        public event Action Changed
        {
            add { Model.Changed += value; }
            remove { Model.Changed -= value; }
        }
    }
}
