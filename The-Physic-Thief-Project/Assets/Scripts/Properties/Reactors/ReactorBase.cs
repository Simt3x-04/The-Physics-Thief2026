using UnityEngine;

namespace PhysicsThief.Properties.Reactors
{
    /// <summary>
    /// Base class of the components that "react" to a property change (Observer pattern).
    /// PhysicalObject only holds numbers and timers; a reactor decides what a number MEANS
    /// (e.g. Mass -> Rigidbody mass). To add a new effect, write a new reactor: PhysicalObject never changes.
    /// A reactor never needs timers: when a pasted value expires, PhysicalObject fires the same event with the
    /// original value, so the reactor simply reverts.
    /// </summary>
    [RequireComponent(typeof(PhysicalObject))]
    public abstract class ReactorBase : MonoBehaviour
    {
        [SerializeField] protected PhysicalObject physObj;

        protected virtual void Awake()
        {
            if (physObj == null) physObj = GetComponent<PhysicalObject>();
        }

        protected virtual void OnEnable()
        {
            physObj.OnPropertyValueChanged += HandlePropertyChange;
        }

        protected virtual void OnDisable()
        {
            physObj.OnPropertyValueChanged -= HandlePropertyChange;
        }

        /// <summary>Called for EVERY attribute change on this object: each reactor acts only on the attributes it cares about.</summary>
        protected abstract void HandlePropertyChange(AttributeType type, float newValue);
    }
}
