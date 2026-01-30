using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "Level_", menuName = "CandyBlast/Level Data", order = 1)]
public class LevelData : ScriptableObject
{



    [Title("Grid Configuration")]

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

    private bool autoShuffleOnDeadlock = true;

    [Title("Level Goals (Optional)")]
    [BoxGroup("Goals")]
    [SerializeField, LabelText("Target Score")]
    private int targetScore = 1000;

    [BoxGroup("Goals")]
    [SerializeField, LabelText("Max Moves (0 = unlimited)")]
    private int maxMoves = 0;





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


    private void OnValidate()
    {
        ValidateThresholds();

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
    }

    public string GetLevelSummary()
    {
        return $"Grid: {rows}x{columns}, {colorCount} colors\n" +
               $"Thresholds: A={thresholdA}, B={thresholdB}, C={thresholdC}\n" +
               $"Goal: {targetScore} points" +
               (maxMoves > 0 ? $", {maxMoves} moves" : "");
    }


}
