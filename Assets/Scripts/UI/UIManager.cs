using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Sirenix.OdinInspector;
public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject mainMenuPanel;
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

    private void OnEnable()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnWin += ShowWinPanel;
            EventManager.Instance.OnFail += ShowFailPanel;
        }
    }

    private void OnDisable()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnWin -= ShowWinPanel;
            EventManager.Instance.OnFail -= ShowFailPanel;
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
        if (winPanel != null)
        {
            winPanel.transform.DOScale(0f, 0.3f).SetEase(Ease.InBack).OnComplete(() =>
            {
                winPanel.SetActive(false);
            });
        }

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
}
