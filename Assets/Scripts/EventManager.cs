using System;
using UnityEngine;

public class EventManager : MonoBehaviour
{
    public static EventManager Instance { get; private set; }

    // Events
    public event Action OnLevelStart;
    public event Action OnLevelRestart;
    public event Action OnWin;
    public event Action OnFail;

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

    public void TriggerWin()
    {
        OnWin?.Invoke();
    }

    public void TriggerFail()
    {
        OnFail?.Invoke();
    }
}
