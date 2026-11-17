using System.Collections.Generic;
using UnityEngine;
using PhysicsThief.Properties;

namespace PhysicsThief.Audio
{
    /// <summary>
    /// ScriptableObject containing all the clips. Unity does not serialize Dictionaries; instead, use Lists
    /// and build lookup dictionaries at runtime.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioLibrary", menuName = "Physics Thief/Audio Library")]
    public class AudioLibrarySO : ScriptableObject
    {
        [SerializeField] private List<SfxEntry> sfxClips = new List<SfxEntry>();
        [SerializeField] private List<VoiceLineEntry> voiceLines = new List<VoiceLineEntry>();
        [SerializeField] private List<PropertySoundEntry> propertySounds = new List<PropertySoundEntry>();
        [SerializeField] private List<MusicTrackEntry> musicTracks = new List<MusicTrackEntry>();

        /// <summary>TODO: build lookup dictionaries from the lists.</summary>
        private void OnEnable() { }

        public bool TryGetSfx(string key, out SfxEntry entry) { entry = default; return default; }
        public bool TryGetVoiceLine(string cueKey, out VoiceLineEntry entry) { entry = default; return default; }
        public bool TryGetPropertySound(AttributeType type, out PropertySoundEntry entry) { entry = default; return default; }
        public bool TryGetMusicTrack(MusicTrackId track, out MusicTrackEntry entry) { entry = default; return default; }
    }
}
