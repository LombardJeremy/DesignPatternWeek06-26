using UnityEngine;

public class RoundManager : MonoBehaviour, ISnapshotProvider
{
    // Instance of the Manager
    private static RoundManager s_instance;
    private IActionCommand m_lastActionDone;

    // Round Counter

    // Inject UIManager to lock and unlock UI
    [DependencyInjection] private UiManager m_uiManager;

    // Memento + Action
    public ISnapshot MLastSnapshotToUse { get; set; }
    public Historic MHistoric { get; set; }

    public int MRoundCounter { get; set; }

    #region Character

    // Characters Fields
    [SerializeField] private Character m_player;

    [SerializeField] private Character m_enemy;

    public Character CurrentTarget { get; set; }

    private int m_currentCharacterPlayin = 1;

    #endregion

    #region Main FCT

    private class RoundManagerSnapshot : ISnapshot
    {
        // Round Action
        private readonly IActionCommand m_action;

        private readonly int m_enemyHealth;

        // Master 
        private readonly RoundManager m_master;

        // Character Health
        private readonly int m_playerHealth;

        // Round Count
        private readonly int m_roundCount;

        // Who's player turn it is
        private readonly int m_turnCharacterPlaying;

        public RoundManagerSnapshot(RoundManager master)
        {
            m_master = master;

            m_playerHealth = master.m_player.CurrentHealth;
            m_enemyHealth = master.m_enemy.CurrentHealth;

            m_turnCharacterPlaying = master.m_currentCharacterPlayin;

            m_action = master.m_lastActionDone;

            m_roundCount = master.MRoundCounter;
        }

        public void Apply()
        {
            m_master.m_player.CurrentHealth = m_playerHealth;
            m_master.m_enemy.CurrentHealth = m_enemyHealth;

            m_master.m_currentCharacterPlayin = 0; // Need Player to be first

            m_master.m_lastActionDone = m_action;

            m_master.MRoundCounter = m_roundCount;
        }
    }

    // Get Snapshot at current time
    public ISnapshot GetSnapshot()
    {
        return new RoundManagerSnapshot(this);
    }

    // Apply Snapshot at current Time
    void ISnapshotProvider.ApplySnapshot(ISnapshot snapshot)
    {
        snapshot.Apply();
    }

    private void Awake() //Only 1 copy
    {
        if (s_instance != null && s_instance != this)
            Destroy(gameObject);
        else
            s_instance = this;

        MHistoric = new Historic();
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
        ChangeSides();
    }

    //Simple Update between each round
    public void UpdateRound(bool isLoaded)
    {
        if (m_currentCharacterPlayin == 0) // Begin of player turn
        {
            
            if (m_player.IsDead)
            {
                m_uiManager.SetWinLooseText(false);
                UnlockUI(false);
            }
            UnlockUI(true);
            if( !isLoaded) ChangeRound();
        }
        else // Begin of enemy turn
        {
            if (m_enemy.IsDead)
            {
                m_uiManager.SetWinLooseText(true);
                return;
            }
            UnlockUI(false);
        }
    }

    public void ChangeSides()
    {
        if (m_currentCharacterPlayin == 0)
        {
            // End of player turn
            m_currentCharacterPlayin = 1;
            UpdateRound(false);
            m_enemy.InitializeCharacter();
        }
        else 
        {
            // End of enemy turn
            m_currentCharacterPlayin = 0;
            UpdateRound(false);
            m_player.InitializeCharacter();
        }
    }

    public void ChangeRound()
    {
        MRoundCounter += 1;
        
        m_uiManager.UpdateRound(MRoundCounter);
        
        var command = new CommandSave(MHistoric, GetSnapshot());
        command.Do();
    }

    public void GoBackOneRound()
    {
        var command = new CommandLoad(MHistoric, this);
        command.Do();

        MLastSnapshotToUse.Apply();

        UpdateRound(true);
        m_uiManager.UpdateRound(MRoundCounter);
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