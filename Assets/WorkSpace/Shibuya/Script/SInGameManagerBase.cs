using System; 
using UnityEngine;
using UnityEngine.InputSystem;

public class SInGameManagerBase : SGameManagerBase
{
    public int SStageID = -1;

    [Header("Input Settings")]
    [SerializeField] private InputActionReference SPauseAction;

    // ポーズ状態が変わったことを通知するイベント 
    public event Action<bool> OnPauseStateChanged;

    // 現在のポーズ状態
    public bool IsPaused { get; private set; } = false;

    // ==========================================
    // 入力イベントの登録・解除
    // ==========================================
    protected virtual void OnEnable()
    {
        if (SPauseAction != null)
        {
            SPauseAction.action.Enable();
            SPauseAction.action.performed += OnPauseInput;
        }
    }

    protected virtual void OnDisable()
    {
        if (SPauseAction != null)
        {
            SPauseAction.action.performed -= OnPauseInput;
            SPauseAction.action.Disable();
        }
    }

    private void OnPauseInput(InputAction.CallbackContext context)
    {
        TogglePause();
    }

    // ==========================================
    // ポーズ制御処理
    // ==========================================
    public void TogglePause()
    {
        SetPause(!IsPaused);
    }

    public void SetPause(bool isPause)
    {
        IsPaused = isPause;

        // 状態に合わせて時間を止めたり動かしたりする
        if (IsPaused)
        {
            PauseGame();
        }
        else
        {
            ResumeGame();
        }

        // 状態が変わったことをSUIManager等に通知
        OnPauseStateChanged?.Invoke(IsPaused);
    }

    private void PauseGame() 
    {
        Time.timeScale = 0f;
    }

    private void ResumeGame()
    {
        Time.timeScale = 1f;
    }

    
    protected virtual void Start()
    {
        SUIManager.SInstance.SPlayFadeIn(0.4f);
    }

    void Update()
    {

    }

    public virtual void OnGameClear()
    {
        SStageData data = new SStageData();
        data.SStageID = SStageID;
        data.SIsCleared = true;
        SProgressManager.SInstance.AddStageData(data);
        OnGameEnd();
    }

    public virtual void OnGameOver()
    {
        OnGameEnd();
    }

    private void OnGameEnd()
    {
        LoadSceneWithDelay("TitleScene", 1.6f);
        SUIManager.SInstance.SPlayFadeOut(1.6f);
    }
}