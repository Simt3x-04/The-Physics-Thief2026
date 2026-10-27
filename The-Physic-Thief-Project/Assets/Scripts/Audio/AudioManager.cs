using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using PhysicsThief.Core;
using PhysicsThief.Properties;

namespace PhysicsThief.Audio
{
    /// <summary>Singleton persistente: SFX, voci TTS di Gino, musica con cross-fade e suoni spaziali.</summary>
    public class AudioManager : Singleton<AudioManager>
    {
        [SerializeField] private AudioLibrarySO audioLibrary;
        [SerializeField] private AudioMixer masterMixer;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource voiceSource;
        [SerializeField] private AudioSource musicSourceA;
        [SerializeField] private AudioSource musicSourceB;

        private MusicTrackId currentTrack = MusicTrackId.None;

        protected override void Awake() { }

        public void PlaySFX(string clipKey, float pitchVariation = 0f) { }
        public void PlaySpatialSFX(string clipKey, Vector3 position) { }
        public void PlayVoiceTTS(string cueKey) { }
        public void PlayPropertySFX(AttributeType type, PropertyAudioEvent audioEvent) { }
        public void SetMusicTrack(MusicTrackId track) { }

        /// <summary>TODO: sfuma da una sorgente musicale all'altra in crossFadeDuration secondi.</summary>
        private IEnumerator CrossFadeRoutine(MusicTrackEntry next) { yield break; }
    }
}
