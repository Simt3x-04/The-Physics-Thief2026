using System;
using System.Collections.Generic;
using UnityEngine;

namespace PhysicsThief.Level
{
    /// <summary>Stanza-puzzle: sa quali oggetti sono essenziali e li ripristina al reset.</summary>
    public class PuzzleRoom : MonoBehaviour
    {
        [SerializeField] private int roomID;
        [SerializeField] private List<GameObject> essentialProps = new List<GameObject>();

        public int RoomID => roomID;
        public bool IsSolved { get; private set; }
        public event Action<PuzzleRoom> OnSolved;

        /// <summary>TODO: salva posizione/rotazione iniziali degli essentialProps.</summary>
        private void Awake() { }

        /// <summary>TODO: riporta gli essentialProps allo stato iniziale (IResettable, ResetState).</summary>
        public void ResetRoom() { }

        public void MarkSolved() { }
    }
}
