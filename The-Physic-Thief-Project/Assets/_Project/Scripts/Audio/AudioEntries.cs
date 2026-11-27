using System;
using UnityEngine;
using UnityEngine.Audio;
using PhysicsThief.Properties;

namespace PhysicsThief.Audio
{
    public enum MusicTrackId { None, Menu, Ambient, Boss }

    public enum PropertyAudioEvent { Scan, Paste, Expire, Reaction }

    [Serializable]
    public struct SfxEntry
    {
        public string key;
        public AudioClip[] clips;
        public Vector2 pitchRange;
        [Range(0f, 1f)] public float volume;
    }

    [Serializable]
    public struct VoiceLineEntry
    {
        public string cueKey;
        public AudioClip clip;
        [TextArea] public string subtitle;
    }

    [Serializable]
    public struct PropertySoundEntry
    {
        public AttributeType type;
        public AudioClip scanClip;
        public AudioClip pasteClip;
        public AudioClip expireClip;
        public AudioClip reactionClip;
        public AnimationCurve pitchCurve;
    }

    [Serializable]
    public struct MusicTrackEntry
    {
        public MusicTrackId track;
        public AudioClip clip;
        public float crossFadeDuration;
        public AudioMixerSnapshot snapshot;
    }
}
