using UnityEngine;

[System.Serializable]
public struct PropertySoundEntry
{
    public AudioClip clip;
    public Vector2 pitchRange;
}

[System.Serializable]
public struct MusicTrackEntry
{
    public AudioClip track;
    public float crossfadeDuration;
}

[CreateAssetMenu(fileName = "AudioLibrary", menuName = "PhysicsThief/AudioLibrary")]
public class AudioLibrarySO : ScriptableObject
{
    [Header("Sound Effects")]
    public string[] sfxKeys;
    public AudioClip[] sfxClips;

    [Header("Property Scans/Reactions")]
    public string[] propertySoundsKeys;
    public PropertySoundEntry[] propertySounds;

    [Header("Music Tracks")]
    public string[] musicTracksKeys;
    public MusicTrackEntry[] musicTracks;

    public AudioClip GetSFX(string key)
    {
        // Implementation here
        return null;
    }

    public AudioClip GetPropertyAudio(AttributeType type)
    {
        // Implementation here
        return null;
    }
}
