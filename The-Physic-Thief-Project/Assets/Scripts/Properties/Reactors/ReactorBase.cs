using UnityEngine;

namespace PhysicsThief.Properties.Reactors
{
    /// <summary>
    /// Base dei componenti che "reagiscono" a una variazione di proprieta' (Strategy/Observer).
    /// Per aggiungere un nuovo effetto basta un nuovo Reactor, senza toccare PhysicalObject.
    /// </summary>
    public abstract class ReactorBase : MonoBehaviour
    {
        [SerializeField] protected PhysicalObject physObj;

        /// <summary>TODO: se physObj e' null prendilo con GetComponent.</summary>
        protected virtual void Awake() { }

        /// <summary>TODO: iscriviti a physObj.OnPropertyValueChanged.</summary>
        protected virtual void OnEnable() { }

        /// <summary>TODO: disiscriviti da physObj.OnPropertyValueChanged.</summary>
        protected virtual void OnDisable() { }

        protected abstract void HandlePropertyChange(AttributeType type, float newValue);
    }
}
