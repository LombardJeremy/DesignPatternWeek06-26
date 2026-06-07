using UnityEngine;

public class GameManager : MonoBehaviour
{
    private static GameManager s_instance;

    [SerializeField] private RoundManager m_roundManager;
    [DependencyInjection] private SoundManager m_soundManager;
    [SerializeField] private AudioClip m_mainMusicClip;
    
    #region Main FCT

    private void Awake()
    {
        if (s_instance != null && s_instance != this)
            Destroy(gameObject);
        else
            s_instance = this;
    }

    private void Start()
    {
        m_soundManager.PlayMusic(m_mainMusicClip);
        m_roundManager.ChangeSides();
    }

    #endregion
}