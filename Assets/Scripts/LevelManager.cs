using UnityEngine;
using Sirenix.OdinInspector;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Title("Level Configuration")]
    [InfoBox("Use LevelData ScriptableObjects to configure levels\nLeave empty to use manual settings below")]

    [BoxGroup("Level Data")]
    [SerializeField, LabelText("Current Level Data")]
    [InlineEditor(InlineEditorModes.GUIOnly)]
    private LevelData currentLevelData;

    [BoxGroup("Level Data")]
    [SerializeField, LabelText("All Levels")]
    [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "LevelName")]
    private LevelData[] allLevels;

    [BoxGroup("Level Data")]
    [SerializeField, LabelText("Starting Level Index")]
    private int startingLevelIndex = 0;

    [Title("Manual Configuration (if no LevelData)")]
    [InfoBox("These settings are used only if currentLevelData is null")]
    [FoldoutGroup("Manual Settings")]
    [SerializeField, Range(2, 10), LabelText("M (Rows)")]
    private int rows = 10;

    [FoldoutGroup("Manual Settings")]
    [SerializeField, Range(2, 10), LabelText("N (Columns)")]
    private int columns = 10;

    [FoldoutGroup("Manual Settings")]
    [SerializeField, Range(1, 6), LabelText("K (Color Count)")]
    private int colorCount = 6;

    [FoldoutGroup("Manual Settings")]
    [SerializeField, Range(2, 20), LabelText("A (First Threshold)")]
    private int thresholdA = 4;

    [FoldoutGroup("Manual Settings")]
    [SerializeField, Range(2, 20), LabelText("B (Second Threshold)")]
    private int thresholdB = 7;

    [FoldoutGroup("Manual Settings")]
    [SerializeField, Range(2, 20), LabelText("C (Third Threshold)")]
    private int thresholdC = 9;

    [FoldoutGroup("Manual Settings")]
    [SerializeField, LabelText("Min Group Size")]
    private int minGroupSize = 2;

    [FoldoutGroup("Manual Settings")]
    [SerializeField, LabelText("Auto Shuffle On Deadlock")]
    private bool autoShuffleOnDeadlock = true;

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

    // game stats
    [Title("Game Stats"), ReadOnly]
    [ShowInInspector] private int totalMoves = 0;
    [ShowInInspector] private int totalBlocksBlasted = 0;
    [ShowInInspector] private int remainingTargetScore = 0;
    [ShowInInspector] private int remainingMoves = 0;
    [ShowInInspector] private float elapsedTime = 0f;
    [ShowInInspector] private string currentLevelName = "No Level";

    // Public property for UI access
    public int RemainingTargetScore => remainingTargetScore;
    public int RemainingMoves => remainingMoves;

    // helper methods to get current level data
    private int GetCurrentRows() => currentLevelData != null ? currentLevelData.Rows : rows;
    private int GetCurrentColumns() => currentLevelData != null ? currentLevelData.Columns : columns;
    private int GetCurrentColorCount() => currentLevelData != null ? currentLevelData.ColorCount : colorCount;
    private int GetCurrentThresholdA() => currentLevelData != null ? currentLevelData.ThresholdA : thresholdA;
    private int GetCurrentThresholdB() => currentLevelData != null ? currentLevelData.ThresholdB : thresholdB;
    private int GetCurrentThresholdC() => currentLevelData != null ? currentLevelData.ThresholdC : thresholdC;
    private int GetCurrentMinGroupSize() => currentLevelData != null ? currentLevelData.MinGroupSize : minGroupSize;
    private bool GetCurrentAutoShuffle() => currentLevelData != null ? currentLevelData.AutoShuffleOnDeadlock : autoShuffleOnDeadlock;

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

        ValidateThresholds();
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
        elapsedTime = 0f;

        // update current level name
        currentLevelName = currentLevelData != null ? currentLevelData.LevelName : "Manual Config";

        // configure board with current settings
        board.SetBoardParameters(GetCurrentRows(), GetCurrentColumns(), GetCurrentColorCount());

        // subscribe to board events
        EventManager.Instance.OnBlocksBlasted -= OnBlocksBlasted; // unsubscribe first to avoid duplicates
        EventManager.Instance.OnDeadlock -= OnDeadlock;
        EventManager.Instance.OnBoardStable -= OnBoardStable;

        EventManager.Instance.OnBlocksBlasted += OnBlocksBlasted;
        EventManager.Instance.OnDeadlock += OnDeadlock;
        EventManager.Instance.OnBoardStable += OnBoardStable;
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
        // cleanup before restarting
        if (board != null)
        {
            board.ClearBoard();
        }

        InitializeLevel();
    }

    private void ValidateThresholds()
    {
        if (thresholdA >= thresholdB)
        {
            Debug.LogWarning("Threshold A should be less than B. Auto-correcting...");
            thresholdB = thresholdA + 1;
        }

        if (thresholdB >= thresholdC)
        {
            Debug.LogWarning("Threshold B should be less than C. Auto-correcting...");
            thresholdC = thresholdB + 1;
        }
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

            // check for fail condition (no moves left)
            if (remainingMoves <= 0 && remainingTargetScore > 0)
            {
                remainingMoves = 0;
                EventManager.Instance.TriggerUpdateUITexts();
                board.StopLevel(); // stop spawning new blocks
                OnLevelFailed();
                return;
            }
        }

        Debug.Log($"Blasted {count} blocks! Score gained: {scoreGained}, Remaining target: {remainingTargetScore}, Remaining moves: {remainingMoves}");

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

    private void OnBoardStable()
    {
        Debug.Log("Board is stable");
    }

    private void OnDeadlock()
    {
        Debug.Log("Deadlock detected! No valid moves available.");

        if (GetCurrentAutoShuffle())
        {
            Debug.Log("Auto-shuffling board...");
            board.ShuffleBoard();
        }
    }

    private void Update()
    {
        // track elapsed time
        if (board != null && !board.IsAnimating)
        {
            elapsedTime += Time.deltaTime;
        }
    }

    [Button("Load Example 1 (10x10, 6 colors)")]
    [FoldoutGroup("Manual Settings")]
    private void LoadExample1()
    {
        currentLevelData = null; // clear level data to use manual settings
        rows = 10;
        columns = 10;
        colorCount = 6;
        thresholdA = 4;
        thresholdB = 7;
        thresholdC = 9;

        if (Application.isPlaying)
            InitializeLevel();
    }

    [Button("Load Example 2 (5x8, 4 colors)")]
    [FoldoutGroup("Manual Settings")]
    private void LoadExample2()
    {
        currentLevelData = null; // clear level data to use manual settings
        rows = 5;
        columns = 8;
        colorCount = 4;
        thresholdA = 4;
        thresholdB = 6;
        thresholdC = 8;

        if (Application.isPlaying)
            InitializeLevel();
    }

    public string GetIconVariantDescription(int groupSize)
    {
        if (groupSize > thresholdC)
            return $"Icon C (>{thresholdC})";
        if (groupSize > thresholdB)
            return $"Icon B ({thresholdB + 1}-{thresholdC})";
        if (groupSize > thresholdA)
            return $"Icon A ({thresholdA + 1}-{thresholdB})";

        return $"Default (≤{thresholdA})";
    }

    private void OnValidate()
    {
        ValidateThresholds();
    }

    private void OnDestroy()
    {
        if (board != null)
        {
            EventManager.Instance.OnBlocksBlasted -= OnBlocksBlasted;
            EventManager.Instance.OnDeadlock -= OnDeadlock;
            EventManager.Instance.OnBoardStable -= OnBoardStable;
        }
    }
}
