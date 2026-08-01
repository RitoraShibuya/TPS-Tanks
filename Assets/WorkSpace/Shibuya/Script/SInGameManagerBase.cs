using UnityEngine;
using UnityEngine.InputSystem;

public class SInGameManagerBase : SGameManagerBase
{
    public int SStageID = -1;

    [Header("Input Settings")]
    [SerializeField] private InputActionReference SPauseAction;

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
            Debug.Log($"[{GetType().Name}] ポーズアクションの入力を有効化しました。");
        }
    }

    protected virtual void OnDisable()
    {
        if (SPauseAction != null)
        {
            SPauseAction.action.performed -= OnPauseInput;
            SPauseAction.action.Disable();
            Debug.Log($"[{GetType().Name}] ポーズアクションの入力を無効化しました。");
        }
    }

    private void OnPauseInput(InputAction.CallbackContext context)
    {
        Debug.Log($"[{GetType().Name}] 🎮 ポーズ入力検知！");
        TogglePause();
    }

    // ==========================================
    // ポーズ制御処理（フェードと同じ方式に統一！）
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
        Debug.Log($"[{GetType().Name}] 時間を {(IsPaused ? "停止" : "再開")} しました。");

        // 2. UIManagerに直接「UIを出せ/消せ」と命令する（フェードと同じ！）
        if (SUIManager.SInstance != null)
        {
            if (IsPaused)
            {
                SUIManager.SInstance.SShowPauseUI();
            }
            else
            {
                SUIManager.SInstance.SHidePauseUI();
            }
        }
    }

    // ==========================================
    // 既存のゲーム進行処理
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

    private void OnGameEnd()
    {
        LoadSceneWithDelay("TitleScene", 1.6f);
        SUIManager.SInstance.SPlayFadeOut(1.6f);
    }
}