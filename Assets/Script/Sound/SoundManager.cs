using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

[Service]
public class SoundManager : MonoBehaviour
{
    private AudioSource m_musicSource;

    private float m_musicFadeOutSpeed = 1.0f;
    private float m_musicFadeInSpeed = 1.0f;
    private AudioMixerGroup m_sfxMixerGroup;

    private Coroutine m_changeMusicCoroutine;

    #region Singleton

    private static SoundManager m_instance;

    private void InitSingleton()
    {
        if (m_instance != null && m_instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            m_instance = this;
        }
    }

    #endregion

    private void Awake()
    {
        InitSingleton();
    }

    [DependencyInjection]
    private void Init(SoundSettings soundSettings)
    {
        m_musicSource = gameObject.AddComponent<AudioSource>();
        m_musicSource.loop = true;
        
        if (!soundSettings) return;
        
        m_musicFadeOutSpeed = soundSettings.musicFadeOutSpeed;
        m_musicFadeInSpeed = soundSettings.musicFadeInSpeed;
        m_sfxMixerGroup = soundSettings.sfxMixerGroup;
        m_musicSource.volume = soundSettings.musicVolume;
        m_musicSource.outputAudioMixerGroup = soundSettings.musicMixerGroup;
    }

    private void OnDestroy()
    {
        StopChangeMusic();
    }

    #region Music

    public void PlayMusic(AudioClip newMusic)
    {
        if (!m_musicSource) return;
        if (m_musicSource.clip == newMusic) return;

        StartChangeMusic(newMusic);
    }

    public void StopMusic()
    {
        if (!m_musicSource || !m_musicSource.clip) return;

        StartChangeMusic(null);
    }

    private void StartChangeMusic(AudioClip newMusic)
    {
        StopChangeMusic();

        m_changeMusicCoroutine = StartCoroutine(ChangeMusic(newMusic));
    }

    private IEnumerator ChangeMusic(AudioClip newMusic)
    {
        if (m_musicSource.clip)
        {
            while (m_musicSource.volume > 0.0f)
            {
                m_musicSource.volume = Math.Max(m_musicSource.volume - Time.deltaTime / m_musicFadeOutSpeed, 0.0f);
                yield return null;
            }

            m_musicSource.Stop();
            m_musicSource.clip = null;
        }

        if (newMusic)
        {
            m_musicSource.clip = newMusic;
            m_musicSource.Play();

            while (m_musicSource.volume < 1.0f)
            {
                m_musicSource.volume = Math.Min(m_musicSource.volume + Time.deltaTime / m_musicFadeInSpeed, 1.0f);
                yield return null;
            }
        }
    }

    private void StopChangeMusic()
    {
        if (m_changeMusicCoroutine != null)
        {
            StopCoroutine(m_changeMusicCoroutine);
            m_changeMusicCoroutine = null;
        }
    }

    #endregion

    #region SFX

    public void PlaySFX2D(AudioClip sfx, float volume = 1.0f, float pitch = 1.0f)
    {
        PlaySFX(sfx, transform.position, volume, pitch, 0.0f);
    }

    public void PlaySFX3D(AudioClip sfx, Vector3 position, float volume = 1.0f, float pitch = 1.0f)
    {
        PlaySFX(sfx, position, volume, pitch, 1.0f);
    }

    private void PlaySFX(AudioClip sfx, Vector3 position, float volume = 1.0f, float pitch = 1.0f,
        float spatialBlend = 1.0f)
    {
        GameObject go = new GameObject("SFX");
        go.transform.parent = transform;
        go.transform.position = position;

        AudioSource source = go.AddComponent<AudioSource>();

        source.clip = sfx;
        source.outputAudioMixerGroup = m_sfxMixerGroup;
        source.volume = volume;
        source.pitch = pitch;
        source.spatialBlend = spatialBlend;
        source.rolloffMode = AudioRolloffMode.Linear;

        Destroy(go, sfx.length);
        source.Play();
    }

    #endregion
}