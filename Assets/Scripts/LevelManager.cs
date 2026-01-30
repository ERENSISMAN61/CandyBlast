using UnityEngine;
using Sirenix.OdinInspector;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Title("Level Configuration")]

    [BoxGroup("Level Data")]
    [SerializeField, LabelText("Current Level Data")]
    [InlineEditor(InlineEditorModes.GUIOnly)]
    private LevelData currentLevelData;

    [BoxGroup("Level Data")]
    [SerializeField, LabelText("All Levels")]
    [ListDrawerSettings(ShowIndexLabels = true)]
    private LevelData[] allLevels;

    [BoxGroup("Level Data")]
    [SerializeField, LabelText("Starting Level Index")]
    private int startingLevelIndex = 0;



    [Title("References")]
    [SerializeField] private Board board;

    // properties
    public int Rows => GetCurrentRows();
    public int Columns => GetCurrentColumns();
    public int ColorCount => GetCurrentColorCount();
    public int ThresholdA => GetCurrentThresholdA();
    public int ThresholdB => GetCurrentThresholdB();
    public int ThresholdC => GetCurrentThresholdC();
    public int MinGroupSize => GetCurrentMinGroupSize();
    public LevelData CurrentLevel => currentLevelData;
    public int CurrentLevelIndex { get; private set; }


    private int totalMoves = 0;
    private int totalBlocksBlasted = 0;
    private int remainingTargetScore = 0;
    private int remainingMoves = 0;



    // Public property for UI access
    public int RemainingTargetScore => remainingTargetScore;
    public int RemainingMoves => remainingMoves;

    // helper methods to get current level data
    private int GetCurrentRows() => currentLevelData.Rows;
    private int GetCurrentColumns() => currentLevelData.Columns;
    private int GetCurrentColorCount() => currentLevelData.ColorCount;
    private int GetCurrentThresholdA() => currentLevelData.ThresholdA;
    private int GetCurrentThresholdB() => currentLevelData.ThresholdB;
    private int GetCurrentThresholdC() => currentLevelData.ThresholdC;
    private int GetCurrentMinGroupSize() => currentLevelData.MinGroupSize;
    private bool GetCurrentAutoShuffle() => currentLevelData.AutoShuffleOnDeadlock;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

    }

    public void InitializeGame()
    {
        // load starting level if available
        if (allLevels != null && allLevels.Length > 0 && startingLevelIndex < allLevels.Length)
        {
            LoadLevel(startingLevelIndex);
        }
    }
    private void InitializeLevel()
    {
        if (board == null)
        {
            Debug.LogError("Board reference is missing!");
            return;
        }

        // reset stats
        totalMoves = 0;
        totalBlocksBlasted = 0;
        remainingTargetScore = currentLevelData != null ? currentLevelData.TargetScore : 1000;
        remainingMoves = currentLevelData != null ? currentLevelData.MaxMoves : 0;

        // configure board with current settings
        board.SetBoardParameters(GetCurrentRows(), GetCurrentColumns(), GetCurrentColorCount());

        // CRITICAL: Always unsubscribe first to prevent memory leaks
        UnsubscribeFromEvents();
        
        // Then subscribe
        EventManager.Instance.OnBlocksBlasted += OnBlocksBlasted;
        EventManager.Instance.OnDeadlock += OnDeadlock;
        // EventManager.Instance.OnBoardStable += OnBoardStable;
        
        // initialize board
        board.InitializeBoard();

        string levelInfo = currentLevelData != null ? currentLevelData.GetLevelSummary() : "Manual Configuration";
        Debug.Log($"Level initialized: {levelInfo}");

        EventManager.Instance.TriggerLevelStart();
    }

    public void LoadLevel(int levelIndex)
    {
        if (allLevels == null || levelIndex < 0 || levelIndex >= allLevels.Length)
        {
            Debug.LogError($"Invalid level index: {levelIndex}");
            return;
        }

        // cleanup before loading new level
        if (board != null)
        {
            board.ClearBoard();
        }

        currentLevelData = allLevels[levelIndex];
        CurrentLevelIndex = levelIndex;
        InitializeLevel();
    }

    [Button("Load Next Level", ButtonSizes.Medium)]
    public void LoadNextLevel()
    {
        if (allLevels == null || allLevels.Length == 0)
        {
            Debug.LogWarning("No levels available!");
            return;
        }

        int nextIndex = (CurrentLevelIndex + 1) % allLevels.Length;
        LoadLevel(nextIndex);
    }

    [Button("Restart Level", ButtonSizes.Medium)]
    public void RestartLevel()
    {

        if (board != null)
        {
            board.ClearBoard();
        }

        InitializeLevel();
    }

    private void OnBlocksBlasted(int count)
    {
        totalMoves++;
        totalBlocksBlasted += count;
        int scoreGained = count * 10; // 10 points per block
        remainingTargetScore -= scoreGained;

        // decrease remaining moves if limit is set
        if (currentLevelData != null && currentLevelData.MaxMoves > 0)
        {
            remainingMoves--;

            // check for fail condition
            if (remainingMoves <= 0 && remainingTargetScore > 0)
            {
                remainingMoves = 0;
                EventManager.Instance.TriggerUpdateUITexts();
                board.StopLevel(); // stop spawning new blocks
                OnLevelFailed();
                return;
            }
        }

        // Debug.Log($"Blasted {count} blocks! Score gained: {scoreGained}, Remaining target: {remainingTargetScore}, Remaining moves: {remainingMoves}");

        // check if level is complete
        if (remainingTargetScore <= 0)
        {
            remainingTargetScore = 0;
            EventManager.Instance.TriggerUpdateUITexts(); // update UI before stopping
            board.StopLevel(); // stop spawning new blocks
            OnLevelComplete();
            return;
        }

        EventManager.Instance.TriggerUpdateUITexts();

    }

    private void OnLevelComplete()
    {
        Debug.Log("Level Complete! Target score reached!");
        // EventManager.Instance.TriggerLevelComplete();
        EventManager.Instance.TriggerWin();
    }

    private void OnLevelFailed()
    {
        Debug.Log("Level Failed! No moves left!");
        EventManager.Instance.TriggerFail();
    }

    // private void OnBoardStable()
    // {
    //     Debug.Log("Board is stable");
    // }

    private void OnDeadlock()
    {
        // Debug.Log("Deadlock detected! No valid moves available.");

        if (GetCurrentAutoShuffle())
        {
            Debug.Log("Deadlock detected!Auto-shuffling board...");
            board.ShuffleBoard();
        }
    }


    private void UnsubscribeFromEvents()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnBlocksBlasted -= OnBlocksBlasted;
            EventManager.Instance.OnDeadlock -= OnDeadlock;
            // EventManager.Instance.OnBoardStable -= OnBoardStable;
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
        
        // Cleanup board
        if (board != null)
        {
            board.ClearBoard();
        }
    }
}
