using UnityEngine;

namespace PhysicsThief.Properties.Reactors
{
    /// <summary>
    /// Base for components that "react" to a change in properties (Strategy/Observer).
    /// To add a new effect, you simply need a new Reactor, without having to modify PhysicalObject.
    /// </summary>
    public abstract class ReactorBase : MonoBehaviour
    {
        [SerializeField] protected PhysicalObject physObj;

        /// <summary>TODO: if physObj is null, retrieve it using GetComponent.</summary>
        protected virtual void Awake() { }

        /// <summary>TODO: Subscribe to physObj.OnPropertyValueChanged.</summary>
        protected virtual void OnEnable() { }

        /// <summary>TODO: Unsubscribe to physObj.OnPropertyValueChanged.</summary>
        protected virtual void OnDisable() { }

        protected abstract void HandlePropertyChange(AttributeType type, float newValue);
    }
}
