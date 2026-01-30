using System.Collections;
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

    private bool nextButtonClicked = false;
    private bool restartButtonClicked = false;

    // Cache values to avoid unnecessary text updates and mesh regeneration
    private int cachedScore = -1;
    private int cachedMoves = -1;
    private int cachedLevel = -1;

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
        if (nextButtonClicked)
            return;
        nextButtonClicked = true;

        StartCoroutine(NextLevelSequence());
    }

    private IEnumerator NextLevelSequence()
    {

        if (LevelManager.Instance != null && LevelManager.Instance.NeedsCleanup())
        {
            yield return LevelManager.Instance.PerformCleanup();
        }


        if (winPanel != null)
        {
            bool animationComplete = false;

            winBgImage.DOColor(panelTransparentBgColor, 0.4f);
            winText.transform.DOScale(0f, 0.3f).SetEase(Ease.InBack);
            winMidPanel.transform.DOScale(0f, 0.3f).SetEase(Ease.InBack).OnComplete(() =>
            {
                winPanel.SetActive(false);
                animationComplete = true;
            });


            yield return new WaitUntil(() => animationComplete);
        }


        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.LoadNextLevel();
        }

        nextButtonClicked = false;
    }

    public void RestartButton()
    {
        if (restartButtonClicked)
            return;
        restartButtonClicked = true;
        // hide all panels
        HideAllPanels();

        // restart level
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
                restartButtonClicked = false;
            });
        }
    }

    private void OnLevelStart()
    {
        // Reset cached values when new level starts
        cachedScore = -1;
        cachedMoves = -1;
        cachedLevel = -1;

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
        {
            int newScore = levelManager.RemainingTargetScore;
            // Int comparison is much faster than string comparison
            if (cachedScore != newScore)
            {
                cachedScore = newScore;
                // SetText avoids boxing and unnecessary allocations
                scoreText.SetText("{0}", newScore);
                scoreText.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f).SetEase(Ease.OutBack);
            }
        }

        if (movesText != null && levelManager != null)
        {
            // show remaining moves if limited, otherwise show -1 for unlimited
            int newMoves = (levelManager.CurrentLevel != null && levelManager.CurrentLevel.MaxMoves > 0)
                ? levelManager.RemainingMoves
                : -1; // Use -1 as sentinel value for unlimited

            if (cachedMoves != newMoves)
            {
                cachedMoves = newMoves;
                // SetText with conditional avoids string allocation
                if (newMoves >= 0)
                    movesText.SetText("{0}", newMoves);
                else
                    movesText.text = "∞"; // Special character, use text property

                movesText.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f).SetEase(Ease.OutBack);
            }
        }

        if (groupInfoText != null && levelManager != null)
        {
            int newLevel = levelManager.CurrentLevelIndex + 1;
            if (cachedLevel != newLevel)
            {
                cachedLevel = newLevel;
                // SetText with format avoids string interpolation allocation
                groupInfoText.SetText("Level {0}", newLevel);
            }
        }
    }




}
