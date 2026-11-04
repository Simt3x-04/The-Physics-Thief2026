using UnityEngine;

namespace PhysicsThief.Core
{
    /// <summary>
    /// Base generica per i manager globali (GameManager, AudioManager).
    /// Garantisce una sola istanza e, se richiesto, la fa sopravvivere ai cambi di scena.
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        [SerializeField] private bool persistAcrossScenes = true;

        public static T Instance { get; private set; }

        /// <summary>
        /// Le classi figlie che ridefiniscono Awake devono chiamare base.Awake() come prima istruzione
        /// e fermarsi se l'oggetto e' un duplicato.
        /// </summary>
        protected virtual void Awake()
        {
            UnityEngine.Object existing = Instance;   // confronto "alla Unity" (gestisce gli oggetti distrutti)
            if (existing != null && existing != this)
            {
                Destroy(gameObject);                  // esiste gia' un'istanza: elimina il duplicato
                return;
            }

            Instance = this as T;

            if (persistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);        // funziona solo su GameObject root della scena
            }
        }
    }
}
