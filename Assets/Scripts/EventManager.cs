using System;
using UnityEngine;

public class EventManager : MonoBehaviour
{
    public static EventManager Instance { get; private set; }

    // Events
    public event Action OnLevelStart;
    public event Action OnLevelRestart;
    public event Action OnLevelComplete;
    public event Action OnWin;
    public event Action OnFail;

    // Board Events
    public event Action<int> OnBlocksBlasted;
    public event Action OnBoardStable;
    public event Action OnDeadlock;
    public event Action UpdateUITexts;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Trigger Methods
    public void TriggerLevelStart()
    {
        OnLevelStart?.Invoke();
    }

    public void TriggerLevelRestart()
    {
        OnLevelRestart?.Invoke();
    }

    public void TriggerLevelComplete()
    {
        OnLevelComplete?.Invoke();
    }

    public void TriggerWin()
    {
        OnWin?.Invoke();
    }

    public void TriggerFail()
    {
        OnFail?.Invoke();
    }

    public void TriggerBlocksBlasted(int count)
    {
        OnBlocksBlasted?.Invoke(count);
    }

    public void TriggerBoardStable()
    {
        OnBoardStable?.Invoke();
    }

    public void TriggerDeadlock()
    {
        OnDeadlock?.Invoke();
    }
    public void TriggerUpdateUITexts()
    {
        UpdateUITexts?.Invoke();
    }
}
