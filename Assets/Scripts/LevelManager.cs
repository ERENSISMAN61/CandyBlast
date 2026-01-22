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
    [InfoBox("Example 1: M=10, N=10, K=6, A=4, B=7, C=9\n" +
             "Example 2: M=5, N=8, K=4, A=4, B=6, C=8")]

    [BoxGroup("Grid Settings")]
    [SerializeField, Range(2, 10), LabelText("M (Rows)")]
    private int rows = 10;

    [BoxGroup("Grid Settings")]
    [SerializeField, Range(2, 10), LabelText("N (Columns)")]
    private int columns = 10;

    [BoxGroup("Grid Settings")]
    [SerializeField, Range(1, 6), LabelText("K (Color Count)")]
    private int colorCount = 6;

    [Title("Icon Thresholds")]
    [InfoBox("A < B < C\n" +
             "Default: group size ≤ A\n" +
             "Icon A: A < group size ≤ B\n" +
             "Icon B: B < group size ≤ C\n" +
             "Icon C: group size > C")]

    [BoxGroup("Thresholds")]
    [SerializeField, Range(2, 20), LabelText("A (First Threshold)")]
    private int thresholdA = 4;

    [BoxGroup("Thresholds")]
    [SerializeField, Range(2, 20), LabelText("B (Second Threshold)")]
    private int thresholdB = 7;

    [BoxGroup("Thresholds")]
    [SerializeField, Range(2, 20), LabelText("C (Third Threshold)")]
    private int thresholdC = 9;

    [Title("Game Rules")]
    [BoxGroup("Rules")]
    [SerializeField, LabelText("Min Group Size")]
    private int minGroupSize = 2;

    [BoxGroup("Rules")]
    [SerializeField, LabelText("Auto Shuffle On Deadlock")]
    private bool autoShuffleOnDeadlock = true;

    [Title("References")]
    [SerializeField] private Board board;

    // Properties
    public int Rows => rows;
    public int Columns => columns;
    public int ColorCount => colorCount;
    public int ThresholdA => thresholdA;
    public int ThresholdB => thresholdB;
    public int ThresholdC => thresholdC;
    public int MinGroupSize => minGroupSize;

    // Game stats
    [Title("Game Stats"), ReadOnly]
    [ShowInInspector] private int totalMoves = 0;
    [ShowInInspector] private int totalBlocksBlasted = 0;
    [ShowInInspector] private int score = 0;

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

    private void Start()
    {
        InitializeLevel();
    }

    /// <summary>
    /// Initialize level with current parameters
    /// </summary>
    [Button("Initialize Level", ButtonSizes.Large)]
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

        // Configure board
        board.SetBoardParameters(rows, columns, colorCount);

        // Subscribe to board events
        board.OnBlocksBlasted += OnBlocksBlasted;
        board.OnDeadlock += OnDeadlock;
        board.OnBoardStable += OnBoardStable;

        // Initialize board
        board.InitializeBoard();

        Debug.Log($"Level initialized: {rows}x{columns} grid, {colorCount} colors, Thresholds: A={thresholdA}, B={thresholdB}, C={thresholdC}");
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

        if (autoShuffleOnDeadlock)
        {
            Debug.Log("Auto-shuffling board...");
            board.ShuffleBoard();
        }
    }

    /// <summary>
    /// Load a preset example configuration
    /// </summary>
    [Button("Load Example 1 (10x10, 6 colors)")]
    private void LoadExample1()
    {
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
    private void LoadExample2()
    {
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
