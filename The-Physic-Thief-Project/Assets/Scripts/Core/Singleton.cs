using UnityEngine;

namespace PhysicsThief.Core
{
    /// <summary>
    /// Base generica per i manager globali (GameManager, AudioManager).
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        [SerializeField] private bool persistAcrossScenes = true;

        public static T Instance { get; private set; }

        /// <summary>TODO: assegna Instance, distruggi i duplicati, DontDestroyOnLoad se persistAcrossScenes.</summary>
        protected virtual void Awake() { }
    }
}
