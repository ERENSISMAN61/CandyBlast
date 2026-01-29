using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;

public class UIManager : MonoBehaviour
{
    [FoldoutGroup("UI References")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [FoldoutGroup("UI References")]
    [SerializeField] private TextMeshProUGUI movesText;
    [FoldoutGroup("UI References")]
    [SerializeField] private TextMeshProUGUI groupInfoText;
    [FoldoutGroup("UI References")]
    [SerializeField] private GameObject deadlockPanel;
    [FoldoutGroup("UI References")]
    [SerializeField] private Button shuffleButton;
    [FoldoutGroup("UI References")]
    [SerializeField] private Button restartButton;

    [FoldoutGroup("Settings")]
    [SerializeField] private Board board;
    [FoldoutGroup("Settings")]
    [SerializeField] private LevelManager levelManager;

    [FoldoutGroup("Main Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [FoldoutGroup("Main Panels")]
    [SerializeField] private GameObject gamePanel;
    [FoldoutGroup("Win Panel")]
    [SerializeField] private GameObject winPanel;
    [FoldoutGroup("Win Panel")]
    [SerializeField] private Image winBgImage;
    [FoldoutGroup("Win Panel")]
    [SerializeField] private GameObject winMidPanel;
    [FoldoutGroup("Win Panel")]
    [SerializeField] private GameObject winText;
    [FoldoutGroup("Fail Panel")]
    [SerializeField] private GameObject failPanel;
    [FoldoutGroup("Fail Panel")]
    [SerializeField] private Image failBgImage;
    [FoldoutGroup("Fail Panel")]
    [SerializeField] private GameObject failMidPanel;
    [FoldoutGroup("Fail Panel")]
    [SerializeField] private GameObject failText;
    [FoldoutGroup("Color Options")]
    [SerializeField] private Color panelOriginalBgColor;
    [FoldoutGroup("Color Options")]
    [SerializeField] private Color panelTransparentBgColor;

    private void Start()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnWin += ShowWinPanel;
            EventManager.Instance.OnFail += ShowFailPanel;

            EventManager.Instance.OnLevelStart += OnLevelStart;
            EventManager.Instance.UpdateUITexts += OnBlocksBlasted;
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

    private void OnDestroy()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnWin -= ShowWinPanel;
            EventManager.Instance.OnFail -= ShowFailPanel;

            EventManager.Instance.OnLevelStart -= OnLevelStart;
            EventManager.Instance.UpdateUITexts -= OnBlocksBlasted;
            EventManager.Instance.OnDeadlock -= OnDeadlock;
            EventManager.Instance.OnBoardStable -= OnBoardStable;
        }

        if (shuffleButton != null)
            shuffleButton.onClick.RemoveListener(OnShuffleClicked);

        if (restartButton != null)
            restartButton.onClick.RemoveListener(OnRestartClicked);
    }

    public void PlayButton()
    {
        LevelManager.Instance.InitializeGame();

        mainMenuPanel.transform.DOMoveY(-2000, 0.7f).SetEase(Ease.InBack).OnComplete(() =>
        {
            mainMenuPanel.SetActive(false);
        });
    }

    private void ShowWinPanel()
    {
        if (winPanel != null)
        {
            winPanel.SetActive(true);
            winMidPanel.transform.localScale = Vector3.zero;
            winText.transform.localScale = Vector3.zero;
            winBgImage.color = panelTransparentBgColor;

            winBgImage.DOColor(panelOriginalBgColor, 0.2f).SetDelay(0.3f);
            winText.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack).SetDelay(0.3f);
            winMidPanel.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack).SetDelay(0.3f);

        }
    }

    private void ShowFailPanel()
    {
        if (failPanel != null)
        {
            failPanel.SetActive(true);
            failMidPanel.transform.localScale = Vector3.zero;
            failText.transform.localScale = Vector3.zero;
            failBgImage.color = panelTransparentBgColor;

            failBgImage.DOColor(panelOriginalBgColor, 0.2f).SetDelay(0.3f);
            failText.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack).SetDelay(0.3f);
            failMidPanel.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack).SetDelay(0.3f);
        }
    }

    public void NextLevelButton()
    {
        // Hide win panel
        if (winPanel != null)
        {

            winBgImage.DOColor(panelTransparentBgColor, 0.4f);
            winText.transform.DOScale(0f, 0.3f).SetEase(Ease.InBack);
            winMidPanel.transform.DOScale(0f, 0.3f).SetEase(Ease.InBack).OnComplete(() =>
            {
                winPanel.SetActive(false);
            });
        }

        // Load next level
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.LoadNextLevel();
        }
    }

    public void RestartButton()
    {
        // Hide all panels
        HideAllPanels();

        // Restart level
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.RestartLevel();
        }
    }

    private void HideAllPanels()
    {


        if (failPanel != null)
        {

            failBgImage.DOColor(panelTransparentBgColor, 0.4f);
            failText.transform.DOScale(0f, 0.3f).SetEase(Ease.InBack);
            failMidPanel.transform.DOScale(0f, 0.3f).SetEase(Ease.InBack).OnComplete(() =>
            {
                failPanel.SetActive(false);
            });
        }
    }

    private void OnLevelStart()
    {
        UpdateUI();
    }

    private void OnBlocksBlasted()
    {
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

        scoreText.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f).SetEase(Ease.OutBack);

        if (movesText != null && levelManager != null)
        {
            // Show remaining moves if limited, otherwise show total moves made
            if (levelManager.CurrentLevel != null && levelManager.CurrentLevel.MaxMoves > 0)
                movesText.text = $"{levelManager.RemainingMoves}";
            else
                movesText.text = "∞"; // Unlimited moves

            movesText.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f).SetEase(Ease.OutBack);
        }

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
        if (levelManager != null)
        {
            levelManager.RestartLevel();
        }

        if (deadlockPanel != null)
            deadlockPanel.SetActive(false);

        UpdateUI();
    }


}
