using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SInGameManagerBase : SGameManagerBase
{
    public int SStageID = -1;

    [Header("Input Settings")]
    [SerializeField] private InputActionReference SPauseAction;
    [SerializeField] private InputActionReference SReturnAction;
    [SerializeField] private InputActionReference SRestartAction;

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

        // 1. 時間を操作する
        Time.timeScale = IsPaused ? 0f : 1f;

        // 2. UIManagerに直接「UIを出せ/消せ」と命令する
        if (SUIManager.SInstance != null)
        {
            if (IsPaused)
            {
                SUIManager.SInstance.SShowPauseUI(() => SetPause(false),() => OnBackTitle(),()=>OnRestart());
            }
            else
            {
                SUIManager.SInstance.SHidePauseUI();
            }
        }
    }

    // ==========================================
    // ゲーム進行処理
    // ==========================================
    protected virtual void Start()
    {
        SUIManager.SInstance.SPlayFadeIn(0.4f);
    }

    void Update() { }

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

    private void OnBackTitle()
    {
        SetPause(false);
        OnGameEnd();
    }

    private void OnGameEnd()
    {
        LoadSceneWithDelay("TitleScene", 1.6f);
        SUIManager.SInstance.SPlayFadeOut(1.6f);
    }

    private void OnRestart()
    {
        SetPause(false);
        LoadSceneWithDelay(SceneManager.GetActiveScene().name, 1.6f);
        SUIManager.SInstance.SPlayFadeOut(1.6f);
    }
}