using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "Level_", menuName = "CandyBlast/Level Data", order = 1)]
public class LevelData : ScriptableObject
{
    [Title("Level Info")]
    [SerializeField] private string levelName = "Level 1";
    [SerializeField, TextArea(2, 4)] private string description = "Complete description of this level";
    [SerializeField] private int levelNumber = 1;
    [SerializeField] private Sprite levelIcon;

    [Title("Grid Configuration")]
    [InfoBox("Grid dimensions and color setup")]

    [BoxGroup("Grid")]
    [SerializeField, Range(2, 10), LabelText("M (Rows)")]
    private int rows = 10;

    [BoxGroup("Grid")]
    [SerializeField, Range(2, 10), LabelText("N (Columns)")]
    private int columns = 10;

    [BoxGroup("Grid")]
    [SerializeField, Range(1, 6), LabelText("K (Colors)")]
    private int colorCount = 6;

    [Title("Group Size Thresholds")]
    [InfoBox("Icon variants based on group size\nA < B < C\n" +
             "Default: size ≤ A\n" +
             "Icon A: A < size ≤ B\n" +
             "Icon B: B < size ≤ C\n" +
             "Icon C: size > C")]

    [BoxGroup("Thresholds")]
    [SerializeField, Range(2, 20), LabelText("A Threshold")]
    private int thresholdA = 4;

    [BoxGroup("Thresholds")]
    [SerializeField, Range(2, 20), LabelText("B Threshold")]
    private int thresholdB = 7;

    [BoxGroup("Thresholds")]
    [SerializeField, Range(2, 20), LabelText("C Threshold")]
    private int thresholdC = 9;

    [Title("Game Rules")]
    [BoxGroup("Rules")]
    [SerializeField, Range(2, 10), LabelText("Min Group Size")]
    private int minGroupSize = 2;

    [BoxGroup("Rules")]
    [SerializeField, LabelText("Auto Shuffle on Deadlock")]
    private bool autoShuffleOnDeadlock = true;

    [Title("Level Goals (Optional)")]
    [BoxGroup("Goals")]
    [SerializeField, LabelText("Target Score")]
    private int targetScore = 1000;

    [BoxGroup("Goals")]
    [SerializeField, LabelText("Max Moves (0 = unlimited)")]
    private int maxMoves = 0;

    [BoxGroup("Goals")]
    [SerializeField, LabelText("Time Limit (0 = unlimited)")]
    private float timeLimit = 0f;

    [Title("Difficulty")]
    [SerializeField, Range(1, 5)]
    [PropertyRange(1, 5)]
    [OnValueChanged("UpdateDifficultyColor")]
    private int difficulty = 1;

    [ShowInInspector, ReadOnly, HideLabel]
    [PropertyOrder(100)]
    private string difficultyDisplay => GetDifficultyString();

    // properties for external access
    public string LevelName => levelName;
    public string Description => description;
    public int LevelNumber => levelNumber;
    public Sprite LevelIcon => levelIcon;

    public int Rows => rows;
    public int Columns => columns;
    public int ColorCount => colorCount;

    public int ThresholdA => thresholdA;
    public int ThresholdB => thresholdB;
    public int ThresholdC => thresholdC;

    public int MinGroupSize => minGroupSize;
    public bool AutoShuffleOnDeadlock => autoShuffleOnDeadlock;

    public int TargetScore => targetScore;
    public int MaxMoves => maxMoves;
    public float TimeLimit => timeLimit;
    public int Difficulty => difficulty;

    private void OnValidate()
    {
        ValidateThresholds();
        ValidateLevelNumber();
    }

    private void ValidateThresholds()
    {
        if (thresholdA >= thresholdB)
        {
            thresholdB = thresholdA + 1;
        }

        if (thresholdB >= thresholdC)
        {
            thresholdC = thresholdB + 1;
        }
    }

    private void ValidateLevelNumber()
    {
        if (levelNumber < 1)
            levelNumber = 1;
    }

    private string GetDifficultyString()
    {
        return difficulty switch
        {
            1 => "⭐ Easy",
            2 => "⭐⭐ Normal",
            3 => "⭐⭐⭐ Medium",
            4 => "⭐⭐⭐⭐ Hard",
            5 => "⭐⭐⭐⭐⭐ Expert",
            _ => "Unknown"
        };
    }



    [Button("Copy From Example 1", ButtonSizes.Medium)]
    [BoxGroup("Quick Setup")]
    private void LoadExample1()
    {
        rows = 10;
        columns = 10;
        colorCount = 6;
        thresholdA = 4;
        thresholdB = 7;
        thresholdC = 9;
        targetScore = 1000;
        difficulty = 2;
    }

    [Button("Copy From Example 2", ButtonSizes.Medium)]
    [BoxGroup("Quick Setup")]
    private void LoadExample2()
    {
        rows = 5;
        columns = 8;
        colorCount = 4;
        thresholdA = 4;
        thresholdB = 6;
        thresholdC = 8;
        targetScore = 800;
        difficulty = 3;
    }

    public string GetLevelSummary()
    {
        return $"{levelName} (#{levelNumber})\n" +
               $"Grid: {rows}x{columns}, {colorCount} colors\n" +
               $"Thresholds: A={thresholdA}, B={thresholdB}, C={thresholdC}\n" +
               $"Goal: {targetScore} points" +
               (maxMoves > 0 ? $", {maxMoves} moves" : "") +
               (timeLimit > 0 ? $", {timeLimit}s" : "");
    }

#if UNITY_EDITOR
    [Button("Print Level Summary", ButtonSizes.Large)]
    [PropertyOrder(101)]
    private void PrintSummary()
    {
        Debug.Log(GetLevelSummary());
    }
#endif
}
