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

    private int currentMoves = 0;

    private void Start()
    {
        // Subscribe to events
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnBlocksBlasted += OnBlocksBlasted;
            EventManager.Instance.OnDeadlock += OnDeadlock;
            EventManager.Instance.OnBoardStable += OnBoardStable;
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
        if (scoreText != null && levelManager != null)
            scoreText.text = $"{levelManager.RemainingTargetScore}";

        if (movesText != null)
            movesText.text = $"{currentMoves}";

        if (groupInfoText != null && levelManager != null)
        {
            groupInfoText.text = $"Level {levelManager.CurrentLevelIndex + 1}";
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
        currentMoves = 0;

        if (levelManager != null)
        {
            levelManager.RestartLevel();
        }

        if (deadlockPanel != null)
            deadlockPanel.SetActive(false);

        UpdateUI();
    }

    private void OnDestroy()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnBlocksBlasted -= OnBlocksBlasted;
            EventManager.Instance.OnDeadlock -= OnDeadlock;
            EventManager.Instance.OnBoardStable -= OnBoardStable;
        }

        if (shuffleButton != null)
            shuffleButton.onClick.RemoveListener(OnShuffleClicked);

        if (restartButton != null)
            restartButton.onClick.RemoveListener(OnRestartClicked);
    }
}
