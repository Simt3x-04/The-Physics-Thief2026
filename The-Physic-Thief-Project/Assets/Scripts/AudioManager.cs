using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    
    public AudioSource sfxSource;
    public AudioSource musicSource;
    public AudioSource voiceSource;
    public AudioMixer masterMixer;

    public AudioLibrarySO audioLibrary;

    private void Awake()
    {
        // Implementation here
    }

    public void PlaySFX(string clipKey, float pitchVar = 1f)
    {
        // Implementation here
    }

    public void PlayVoiceTTS(string clipKey)
    {
        // Implementation here
    }

    public void PlayPropertySFX(AttributeType type)
    {
        // Implementation here
    }

    public void SetMusicTrack(string trackName)
    {
        // Implementation here
    }

    public void PlaySpatialSFX(string clipKey, Vector3 pos)
    {
        // Implementation here
    }
}
