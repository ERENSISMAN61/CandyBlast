using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;

/// <summary>
/// Manages game UI display
/// Shows score, moves, and game state
/// </summary>
public class GameUI : MonoBehaviour
{
    [Title("UI References")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI movesText;
    [SerializeField] private TextMeshProUGUI groupInfoText;
    [SerializeField] private GameObject deadlockPanel;
    [SerializeField] private Button shuffleButton;
    [SerializeField] private Button restartButton;

    [Title("Settings")]
    [SerializeField] private Board board;
    [SerializeField] private LevelManager levelManager;

    private int currentScore = 0;
    private int currentMoves = 0;

    private void Start()
    {
        // Subscribe to events
        if (board != null)
        {
            board.OnBlocksBlasted += OnBlocksBlasted;
            board.OnDeadlock += OnDeadlock;
            board.OnBoardStable += OnBoardStable;
        }

        // Setup buttons
        if (shuffleButton != null)
            shuffleButton.onClick.AddListener(OnShuffleClicked);

        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);

        if (deadlockPanel != null)
            deadlockPanel.SetActive(false);

        UpdateUI();
    }

    private void OnBlocksBlasted(int count)
    {
        currentMoves++;
        currentScore += count * 10;
        UpdateUI();
    }

    private void OnDeadlock()
    {
        if (deadlockPanel != null)
            deadlockPanel.SetActive(true);
    }

    private void OnBoardStable()
    {
        if (deadlockPanel != null)
            deadlockPanel.SetActive(false);
    }

    private void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = $"Score: {currentScore}";

        if (movesText != null)
            movesText.text = $"Moves: {currentMoves}";

        if (groupInfoText != null && levelManager != null)
        {
            groupInfoText.text = $"Level: {levelManager.Rows}x{levelManager.Columns}\n" +
                               $"Colors: {levelManager.ColorCount}\n" +
                               $"A:{levelManager.ThresholdA} B:{levelManager.ThresholdB} C:{levelManager.ThresholdC}";
        }
    }

    private void OnShuffleClicked()
    {
        if (board != null && !board.IsAnimating)
        {
            board.ShuffleBoard();
            if (deadlockPanel != null)
                deadlockPanel.SetActive(false);
        }
    }

    private void OnRestartClicked()
    {
        currentScore = 0;
        currentMoves = 0;

        if (board != null)
        {
            board.InitializeBoard();
        }

        if (deadlockPanel != null)
            deadlockPanel.SetActive(false);

        UpdateUI();
    }

    private void OnDestroy()
    {
        if (board != null)
        {
            board.OnBlocksBlasted -= OnBlocksBlasted;
            board.OnDeadlock -= OnDeadlock;
            board.OnBoardStable -= OnBoardStable;
        }

        if (shuffleButton != null)
            shuffleButton.onClick.RemoveListener(OnShuffleClicked);

        if (restartButton != null)
            restartButton.onClick.RemoveListener(OnRestartClicked);
    }
}
