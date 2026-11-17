using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using PhysicsThief.Core;
using PhysicsThief.Properties;

namespace PhysicsThief.Audio
{
    /// <summary>Persistent singleton: SFX, Gino’s TTS voices, music with cross-fade, and spatial sounds.</summary>
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

        ///<summary>TODO: crossfade from one music source to another over a duration of crossFadeDuration seconds.</summary>
        private IEnumerator CrossFadeRoutine(MusicTrackEntry next) { yield break; }
    }
}
