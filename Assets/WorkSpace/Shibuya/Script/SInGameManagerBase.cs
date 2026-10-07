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

    // ゲーム終了（クリアまたはオーバー）処理が既に走ったかのガードフラグ
    protected bool m_isGameEnded = false;

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
        // ★【連打・不正操作防止】シーン遷移中やゲーム終了後はポーズ操作を受け付けない
        if (m_isLoading || m_isGameEnded) return;

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
        // ★【連打防止】シーン遷移中はポーズ操作を弾く
        if (m_isLoading) return;

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
    // ゲームクリア / ゲームオーバー処理
    // ==========================================

    public virtual void OnGameClear()
    {
        // ★【多重発火防止】既にゲーム終了済み、またはシーン遷移中なら何もしない
        if (m_isGameEnded || m_isLoading) return;
        m_isGameEnded = true;

        SStageData data = SProgressManager.SInstance.GetStageData(SStageID);
        if (data == null)
        {
            data = new SStageData { SStageID = SStageID, SIsCleared = true };
            SProgressManager.SInstance.AddStageData(data);
        }
        else
        {
            data.SIsCleared = true;
        }

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
        // ★【多重発火防止】既にゲーム終了済み、またはシーン遷移中なら何もしない
        if (m_isGameEnded || m_isLoading) return;
        m_isGameEnded = true;

        OnGameEnd();
        GameObject overUI = SUIManager.SInstance.SShowUI(SGameOverPrefab);

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
        // ★【連打防止】遷移中の連打（FadeOutの重ね掛け）をブロック
        if (m_isLoading) return;

        SetPause(false);

        string nextSceneName = "Stage" + (SStageID + 1) + "Scene";
        LoadSceneWithDelay(nextSceneName, 1.6f);

        if (SUIManager.SInstance != null)
        {
            SUIManager.SInstance.SPlayFadeOut(1.6f);
        }
    }

    private void OnBackTitle()
    {
        // ★【連打防止】遷移中の連打（FadeOutの重ね掛け）をブロック
        if (m_isLoading) return;

        SetPause(false);
        LoadSceneWithDelay("TitleScene", 1.6f);

        if (SUIManager.SInstance != null)
        {
            SUIManager.SInstance.SPlayFadeOut(1.6f);
        }
    }

    private void OnRestart()
    {
        // ★【連打防止】遷移中の連打（FadeOutの重ね掛け）をブロック
        if (m_isLoading) return;

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

    // ==========================================
    // デバッグ処理
    // ==========================================

    private void OnGUI()
    {
        // ★【連打・誤操作防止】遷移中やゲーム終了後はデバッグボタンを操作不可（グレーアウト）に
        GUI.enabled = !m_isLoading && !m_isGameEnded;

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

        GUI.enabled = true;
    }

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
}