using UnityEngine;
using UnityEngine.Audio;

[Service, CreateAssetMenu(fileName = "SoundSettings", menuName = "Scriptable Objects/SoundSettings")]
public class SoundSettings : ScriptableObject
{
    public AudioMixerGroup musicMixerGroup;
    [Range(0.01f, 1)] public float musicVolume;
    [Range(0.01f, 100f)] public float musicFadeOutSpeed;
    [Range(0.01f, 100f)] public float musicFadeInSpeed;
    
    public AudioMixerGroup sfxMixerGroup;
}
