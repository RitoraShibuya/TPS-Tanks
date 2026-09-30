using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SInGameManagerBase : SGameManagerBase
{
    public int SStageID = -1;

    [Header("Input Settings")]
    [SerializeField] private InputActionReference SPauseAction;

    [Header("UI Settings")]
    [SerializeField] private GameObject SLevelIntroPrefab;
    [SerializeField] private GameObject SGameOverPrefab;
    [SerializeField] private GameObject SStageClearPrefab;

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
                SUIManager.SInstance.SShowPauseUI(() => SetPause(false), () => OnBackTitle(), () => OnRestart());
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
        if (SLevelIntroPrefab != null)
        {
            GameObject introUI = SUIManager.SInstance.SShowUI(SLevelIntroPrefab);
            var introController = introUI.GetComponent<SLevelIntroUIController>();

            if (introController != null)
            {
                introController.SetupAndPlay(SStageID);
            }
        }
        SUIManager.SInstance.SPlayFadeIn(0.4f);
    }

    // ==========================================
    // 確実なデバッグ処理 (用が済んだらここから下を削除)
    // ==========================================

    // 【方法1】ゲーム画面に強制的にデバッグボタンを表示する
    private void OnGUI()
    {
        // 画面左上にボタンを配置（文字サイズ等を少し大きく）
        GUILayout.BeginArea(new Rect(20, 20, 200, 200));

        if (GUILayout.Button("【DEBUG】Game Clear", GUILayout.Height(50)))
        {
            Debug.Log("[Debug] GUIボタンから GameClear を実行しました");
            ForceGameClear();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("【DEBUG】Game Over", GUILayout.Height(50)))
        {
            Debug.Log("[Debug] GUIボタンから GameOver を実行しました");
            ForceGameOver();
        }

        GUILayout.EndArea();
    }

    // 【方法2】Unityエディタのインスペクター（スクリプト名の右の︙メニュー）から直接実行する
    [ContextMenu("Debug: Force Game Clear")]
    public void ForceGameClear()
    {
        OnGameClear();
    }

    [ContextMenu("Debug: Force Game Over")]
    public void ForceGameOver()
    {
        OnGameOver();
    }

    // ==========================================

    public virtual void OnGameClear()
    {
        SStageData data = new SStageData();
        data.SStageID = SStageID;
        data.SIsCleared = true;
        SProgressManager.SInstance.AddStageData(data);

        OnGameEnd();

        GameObject clearUI = SUIManager.SInstance.SShowUI(SStageClearPrefab);
        if (clearUI != null)
        {
            SStageClear clearScript = clearUI.GetComponent<SStageClear>();
            if (clearScript != null)
            {
                clearScript.Setup(
                    onNext: () => OnNextStage(),
                    onRestart: () => OnRestart(),
                    onReturn: () => OnBackTitle()
                );
            }
        }
    }

    public virtual void OnGameOver()
    {
        OnGameEnd();
        GameObject overUI = SUIManager.SInstance.SShowUI(SGameOverPrefab);

        // 生成したGameOverUIに処理（Action）を渡す
        if (overUI != null)
        {
            SGameOverUI overScript = overUI.GetComponent<SGameOverUI>();
            if (overScript != null)
            {
                overScript.Setup(
                    onRestart: () => OnRestart(),
                    onReturn: () => OnBackTitle()
                );
            }
        }
    }

    // --- 各種ボタンから呼ばれるアクション群 ---

    private void OnNextStage()
    {
        SetPause(false);

        // TODO: 本番では次のステージのシーン名にする
        string nextSceneName = "Stage" + (SStageID + 1) + "Scene";
        LoadSceneWithDelay(nextSceneName, 1.6f);

        if (SUIManager.SInstance != null)
        {
            SUIManager.SInstance.SPlayFadeOut(1.6f);
        }
    }

    private void OnBackTitle()
    {
        SetPause(false);
        LoadSceneWithDelay("TitleScene", 1.6f);

        if (SUIManager.SInstance != null)
        {
            SUIManager.SInstance.SPlayFadeOut(1.6f);
        }
    }

    private void OnRestart()
    {
        SetPause(false);
        LoadSceneWithDelay(SceneManager.GetActiveScene().name, 1.6f);

        if (SUIManager.SInstance != null)
        {
            SUIManager.SInstance.SPlayFadeOut(1.6f);
        }
    }

    private void OnGameEnd()
    {
        // 既存処理
    }

    public int GetStageID()
    {
        return SStageID;
    }
}