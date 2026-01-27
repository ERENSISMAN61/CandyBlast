using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// Manages level configuration and game rules
/// Singleton pattern for easy access
/// </summary>
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

    // Properties
    public int Rows => GetCurrentRows();
    public int Columns => GetCurrentColumns();
    public int ColorCount => GetCurrentColorCount();
    public int ThresholdA => GetCurrentThresholdA();
    public int ThresholdB => GetCurrentThresholdB();
    public int ThresholdC => GetCurrentThresholdC();
    public int MinGroupSize => GetCurrentMinGroupSize();
    public LevelData CurrentLevel => currentLevelData;
    public int CurrentLevelIndex { get; private set; }

    // Game stats
    [Title("Game Stats"), ReadOnly]
    [ShowInInspector] private int totalMoves = 0;
    [ShowInInspector] private int totalBlocksBlasted = 0;
    [ShowInInspector] private int score = 0;
    [ShowInInspector] private float elapsedTime = 0f;
    [ShowInInspector] private string currentLevelName = "No Level";

    // Helper methods to get current level data
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
        // Load starting level if available
        if (allLevels != null && allLevels.Length > 0 && startingLevelIndex < allLevels.Length)
        {
            LoadLevel(startingLevelIndex);
        }
    }
    /// <summary>
    /// Initialize level with current parameters
    /// </summary>
    private void InitializeLevel()
    {
        if (board == null)
        {
            Debug.LogError("Board reference is missing!");
            return;
        }

        // Reset stats
        totalMoves = 0;
        totalBlocksBlasted = 0;
        score = 0;
        elapsedTime = 0f;

        // Update current level name
        currentLevelName = currentLevelData != null ? currentLevelData.LevelName : "Manual Config";

        // Configure board with current settings
        board.SetBoardParameters(GetCurrentRows(), GetCurrentColumns(), GetCurrentColorCount());

        // Subscribe to board events
        board.OnBlocksBlasted -= OnBlocksBlasted; // Unsubscribe first to avoid duplicates
        board.OnDeadlock -= OnDeadlock;
        board.OnBoardStable -= OnBoardStable;

        board.OnBlocksBlasted += OnBlocksBlasted;
        board.OnDeadlock += OnDeadlock;
        board.OnBoardStable += OnBoardStable;

        // Initialize board
        board.InitializeBoard();

        string levelInfo = currentLevelData != null ? currentLevelData.GetLevelSummary() : "Manual Configuration";
        Debug.Log($"Level initialized: {levelInfo}");

        EventManager.Instance.TriggerLevelStart();
    }

    /// <summary>
    /// Load a specific level by index
    /// </summary>
    public void LoadLevel(int levelIndex)
    {
        if (allLevels == null || levelIndex < 0 || levelIndex >= allLevels.Length)
        {
            Debug.LogError($"Invalid level index: {levelIndex}");
            return;
        }

        currentLevelData = allLevels[levelIndex];
        CurrentLevelIndex = levelIndex;
        InitializeLevel();
    }

    /// <summary>
    /// Load next level
    /// </summary>
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

    /// <summary>
    /// Restart current level
    /// </summary>
    [Button("Restart Level", ButtonSizes.Medium)]
    public void RestartLevel()
    {
        InitializeLevel();
    }

    /// <summary>
    /// Validate that thresholds are in correct order
    /// </summary>
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

    /// <summary>
    /// Called when blocks are blasted
    /// </summary>
    private void OnBlocksBlasted(int count)
    {
        totalBlocksBlasted += count;
        score += count * 10; // 10 points per block

        Debug.Log($"Blasted {count} blocks! Total score: {score}");
    }

    /// <summary>
    /// Called when board is stable (no more animations)
    /// </summary>
    private void OnBoardStable()
    {
        Debug.Log("Board is stable");
    }

    /// <summary>
    /// Called when deadlock is detected
    /// </summary>
    private void OnDeadlock()
    {
        Debug.LogWarning("Deadlock detected! No valid moves available.");

        if (GetCurrentAutoShuffle())
        {
            Debug.Log("Auto-shuffling board...");
            board.ShuffleBoard();
        }
    }

    private void Update()
    {
        // Track elapsed time
        if (board != null && !board.IsAnimating)
        {
            elapsedTime += Time.deltaTime;
        }
    }

    /// <summary>
    /// Load a preset example configuration
    /// </summary>
    [Button("Load Example 1 (10x10, 6 colors)")]
    [FoldoutGroup("Manual Settings")]
    private void LoadExample1()
    {
        currentLevelData = null; // Clear level data to use manual settings
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
        currentLevelData = null; // Clear level data to use manual settings
        rows = 5;
        columns = 8;
        colorCount = 4;
        thresholdA = 4;
        thresholdB = 6;
        thresholdC = 8;

        if (Application.isPlaying)
            InitializeLevel();
    }

    /// <summary>
    /// Get icon variant description for UI
    /// </summary>
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
            board.OnBlocksBlasted -= OnBlocksBlasted;
            board.OnDeadlock -= OnDeadlock;
            board.OnBoardStable -= OnBoardStable;
        }
    }
}
