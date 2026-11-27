using UnityEngine;

namespace PhysicsThief.Core
{
    /// <summary>
    /// Generic base class for global managers (GameManager, AudioManager).
    /// Ensures a single instance exists and, if requested, allows it to persist across scene changes.
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        [SerializeField] private bool persistAcrossScenes = true;

        public static T Instance { get; private set; }

        /// <summary>
        /// Child classes that override Awake must call base.Awake() as the first instruction
        /// and stop if the object is a duplicate.
        /// </summary>
        protected virtual void Awake()
        {
            UnityEngine.Object existing = Instance;
            if (existing != null && existing != this)
            {
                Destroy(gameObject);                  // an instance already exists: delete the duplicate
                return;
            }

            Instance = this as T;

            if (persistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);        // it only works on root GameObjects in the scene
            }
        }
    }
}
