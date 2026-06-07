using UnityEngine;

public class SoundComponent : MonoBehaviour
{
    [DependencyInjection] private SoundManager m_soundManager;
    
    [SerializeField] private AudioClip m_clip;
    [SerializeField, Range(0.0f, 1.0f)] private float m_volume = 1.0f;
    [SerializeField, Range(-3.0f, 3.0f)] private float m_pitch = 1.0f;
    [SerializeField] private bool m_randomPitch = false;
    [SerializeField, Range(-3.0f, 3.0f)] private float m_lowPitchBound = 0.7f;
    [SerializeField, Range(-3.0f, 3.0f)] private float m_highPitchBound = 1.3f;

    private void Start()
    {
        m_lowPitchBound = Mathf.Clamp(m_lowPitchBound, -3.0f, 3.0f);
        m_highPitchBound = Mathf.Clamp(m_highPitchBound, -3.0f, 3.0f);
    }

    public void PlayMusic()
    {
        if (!m_clip || !m_soundManager) return;
        
        m_soundManager.PlayMusic(m_clip);
    }
    
    public void PlaySFX2D()
    {
        if (!m_clip || !m_soundManager) return;

        float pitch = m_randomPitch ? Random.Range(m_lowPitchBound, m_highPitchBound) : m_pitch;
        
        m_soundManager.PlaySFX2D(m_clip, m_volume, pitch);
    }
    
    public void PlaySFX3D()
    {
        if (!m_clip || !m_soundManager) return;

        float pitch = m_randomPitch ? Random.Range(m_lowPitchBound, m_highPitchBound) : m_pitch;
        
        m_soundManager.PlaySFX3D(m_clip, transform.position, m_volume, pitch);
    }
    
    public void SetClip(AudioClip clip)
    {
        m_clip = clip;
    }
}
