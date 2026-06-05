using UnityEngine;
using UnityEngine.UI;

public class RoundManager : MonoBehaviour, ISnapshotProvider
{
    // Instance of the Manager
    private static RoundManager s_instance;

    // Inject UIManager to lock and unlock UI
    [DependencyInjection] private UiManager m_uiManager;

    // Memento + Action
    public ISnapshot MLastSnapshotToUse { get; set; }
    public Historic MHistoric { get; set; }
    private IActionCommand m_lastActionDone;

    #region Character

    // Characters Fields
    [SerializeField] private Character m_player;
    [SerializeField] private Character m_enemy;

    public Character CurrentTarget { get; set; }

    private int m_currentCharacterPlayin = 1;

    #endregion

    #region Main FCT

    class RoundManagerSnapshot : ISnapshot
    {
        // Master 
        private RoundManager m_master;

        // Character Health
        private int m_playerHealth;
        private int m_enemyHealth;

        // Who's player turn it is
        private int m_turnCharacterPlaying;
        
        // Round Action
        private IActionCommand m_action;
        
        public RoundManagerSnapshot(RoundManager master)
        {
            m_master = master;
            
            m_playerHealth = master.m_player.CurrentHealth;
            m_enemyHealth = master.m_enemy.CurrentHealth;

            m_turnCharacterPlaying = master.m_currentCharacterPlayin;

            m_action = master.m_lastActionDone;
        }
        
        public void Apply()
        {
            m_master.m_player.CurrentHealth = m_playerHealth;
            m_master.m_enemy.CurrentHealth = m_enemyHealth;
            
            m_master.m_currentCharacterPlayin = m_turnCharacterPlaying;
            
            m_master.m_lastActionDone = m_action;
        }
    }
    
    // Get Snapshot at current time
    public ISnapshot GetSnapshot()
    {
        return new RoundManagerSnapshot(this);
    }
    
    // Apply Snapshot at current Time
    void ISnapshotProvider.ApplySnapshot(ISnapshot snapshot) => snapshot.Apply();
    

    private void Awake() //Only 1 copy
    {
        if (s_instance != null && s_instance != this)
            Destroy(gameObject);
        else
            s_instance = this;
        
        MHistoric =  new Historic();
    }

    private void Start()
    {
        m_player.ActionDeclaration += EndOfActionDeclaration;
        m_enemy.ActionDeclaration += EndOfActionDeclaration;
    }

    private void OnDestroy()
    {
        m_player.ActionDeclaration -= EndOfActionDeclaration;
        m_enemy.ActionDeclaration -= EndOfActionDeclaration;
    }

    #endregion

    #region GameLoop FCT

    //Fct depending on character event to update round
    private void EndOfActionDeclaration(IActionCommand action, bool castOnSelf)
    {
        DoAction(action, castOnSelf);
        UpdateRound();
    }

    //Simple Update between each round
    public void UpdateRound()
    {
        if (m_currentCharacterPlayin == 0)
        {
            m_currentCharacterPlayin = 1;

            if (m_enemy.IsDead)
            {
                
                m_uiManager.SetWinLooseText(true);
                UnlockUI(false);

                return;
            }

            UnlockUI(false);

            CommandSave command = new CommandSave(MHistoric, GetSnapshot());

            m_enemy.InitializeCharacter();
        }
        else
        {
            m_currentCharacterPlayin = 0;
            if (m_player.IsDead)
            {
                m_uiManager.SetWinLooseText(false);
                UnlockUI(false);
                return;
            }

            UnlockUI(true);
            m_player.InitializeCharacter();
        }
    }

    //DoAction depending on the player playing & if it's on himself
    public void DoAction(IActionCommand action, bool castOnSelf)
    {
        m_lastActionDone = action;
        if (m_currentCharacterPlayin == 0)
            action.Execute(m_player, castOnSelf ? m_player : m_enemy);
        else
            action.Execute(m_enemy, castOnSelf ? m_enemy : m_player);
    }

    public void UnlockUI(bool isUnlocked) //Lock / Unlock UI
    {
        if (m_uiManager == null) return;
        m_uiManager.SetPlayerUIEnabled(isUnlocked);
    }

    #endregion


}