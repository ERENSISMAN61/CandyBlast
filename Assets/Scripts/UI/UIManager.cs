using UnityEngine;
using DG.Tweening;
public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject gamePanel;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject failPanel;
    public void PlayButton()
    {
        LevelManager.Instance.InitializeGame();

        mainMenuPanel.transform.DOMoveY(-2000, 0.7f).SetEase(Ease.InBack).OnComplete(() =>
        {
            mainMenuPanel.SetActive(false);

        });
    }
}
