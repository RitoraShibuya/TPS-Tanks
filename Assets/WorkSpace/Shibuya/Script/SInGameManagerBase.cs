using System.Collections;
using System.Collections.Generic;
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

    [Header("Enemy Settings")]
    [Tooltip("倒すとクリアになる敵（EnemyBase派生）をセット。空の場合はシーン上の全ての敵が対象になる")]
    [SerializeField] private List<EnemyBase> m_targetEnemies = new();

    public bool IsPaused { get; private set; } = false;

    // ゲーム終了（クリアまたはオーバー）処理が既に走ったかのガードフラグ
    protected bool m_isGameEnded = false;

    // プレイヤー制御用
    private TankMovement m_playerTank;
    private HHelth m_playerHealth;
    private bool m_isStartLocked = false;

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

    protected virtual void OnDestroy()
    {
        // 監視イベントの解除
        foreach (var enemy in m_targetEnemies)
        {
            if (enemy != null) enemy.OnDied -= OnEnemyDied;
        }

        if (m_playerHealth != null)
        {
            m_playerHealth.OnDie -= OnPlayerDied;
        }

        // ポーズ中にシーンが破棄されても時間が止まったままにならないようにする
        Time.timeScale = 1f;
    }

    private void OnPauseInput(InputAction.CallbackContext context)
    {
        // シーン遷移中やゲーム終了後はポーズ操作を受け付けない
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
        // シーン遷移中はポーズ操作を弾く
        if (m_isLoading) return;

        IsPaused = isPause;

        // 1. 時間を操作する
        Time.timeScale = IsPaused ? 0f : 1f;

        // 2. ポーズ中のプレイヤー制御（ポーズ時は停止、解除時はロック中でなければ再開）
        if (IsPaused)
        {
            SetPlayerControlEnabled(false);
        }
        else if (!m_isStartLocked && !m_isGameEnded)
        {
            SetPlayerControlEnabled(true);
        }

        // 3. UIManagerに直接「UIを出せ/消せ」と命令する
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
        // インゲームBGMの再生
        if (SSoundManager.Instance != null)
        {
            SSoundManager.Instance.PlayBGM_InGame();
        }

        // シーン上の TankMovement を自動検索
        m_playerTank = FindFirstObjectByType<TankMovement>();

        if (SUIManager.SInstance != null)
        {
            if (SLevelIntroPrefab != null)
            {
                GameObject introUI = SUIManager.SInstance.SShowUI(SLevelIntroPrefab);
                var introController = introUI != null ? introUI.GetComponent<SLevelIntroUIController>() : null;

                if (introController != null)
                {
                    introController.SetupAndPlay(SStageID);
                }
            }

            SUIManager.SInstance.SPlayFadeIn(0.4f);
        }
        else
        {
            Debug.LogWarning("[SInGameManagerBase] SUIManager がシーンにありません。");
        }

        // 敵の死亡監視イベントを登録
        SetupEnemyObservers();

        // プレイヤーの死亡監視イベントを登録
        SetupPlayerObserver();

        // ゲーム開始時のカメラ初期化を待ってから1秒間の操作不能制御を開始
        StartCoroutine(StartPlayerLockRoutine());
    }

    /// <summary>
    /// ステージ開始から1秒間プレイヤーを操作不能にするコルーチン
    /// </summary>
    private IEnumerator StartPlayerLockRoutine()
    {
        m_isStartLocked = true;

        // 1フレーム待機することで TankMovement の Start() と最初の LateUpdate() (カメラ初期位置セット) を確実に実行させる
        yield return null;

        // カメラが正常位置についた後に操作をオフにする
        SetPlayerControlEnabled(false);

        // 残りのロック時間（1秒間）待機
        yield return new WaitForSeconds(1.0f);

        m_isStartLocked = false;

        // 1秒経過後、ポーズ中・ゲーム終了後でなければ操作解除
        if (!IsPaused && !m_isGameEnded)
        {
            SetPlayerControlEnabled(true);
        }
    }

    /// <summary>
    /// プレイヤー（TankMovement）の操作スクリプトおよび移動速度の有効/無効化
    /// </summary>
    private void SetPlayerControlEnabled(bool isEnabled)
    {
        if (m_playerTank == null) return;

        m_playerTank.enabled = isEnabled;

        if (!isEnabled && m_playerTank.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void SetupEnemyObservers()
    {
        m_targetEnemies.RemoveAll(enemy => enemy == null);

        // 未設定ならシーン上の全ての敵をクリア条件にする（登録忘れでクリアできなくなるのを防ぐ）
        if (m_targetEnemies.Count == 0)
        {
            m_targetEnemies.AddRange(FindObjectsByType<EnemyBase>(FindObjectsSortMode.None));
        }

        if (m_targetEnemies.Count == 0)
        {
            Debug.LogWarning("[SInGameManagerBase] シーンに敵がいないため、敵全滅によるクリアは発生しません。");
            return;
        }

        foreach (var enemy in m_targetEnemies)
        {
            enemy.OnDied += OnEnemyDied;
        }
    }

    /// <summary>
    /// 敵が倒された瞬間だけ呼ばれる処理（シーン切り替えによる破棄では呼ばれない）
    /// </summary>
    private void OnEnemyDied(EnemyBase deadEnemy)
    {
        deadEnemy.OnDied -= OnEnemyDied;
        m_targetEnemies.Remove(deadEnemy);
        m_targetEnemies.RemoveAll(enemy => enemy == null);

        // 全滅したらクリア（すでにクリア演出中なら OnGameClear 側で弾く）
        if (m_targetEnemies.Count == 0)
        {
            OnGameClear();
        }
    }

    private void SetupPlayerObserver()
    {
        if (m_playerTank != null)
        {
            m_playerHealth = m_playerTank.GetComponent<HHelth>();
        }

        // TankMovement と同じオブジェクトに無ければシーン上から検索
        if (m_playerHealth == null)
        {
            m_playerHealth = FindFirstObjectByType<HHelth>();
        }

        if (m_playerHealth == null)
        {
            Debug.LogWarning("[SInGameManagerBase] プレイヤーの HHelth が見つからないため、ゲームオーバーを検知できません。");
            return;
        }

        m_playerHealth.OnDie += OnPlayerDied;
    }

    /// <summary>
    /// プレイヤーの体力が0になった瞬間だけ呼ばれる処理
    /// </summary>
    private void OnPlayerDied()
    {
        Debug.Log("プレイヤーが死にました");
        OnGameOver();
    }

    // ==========================================
    // ゲームクリア / ゲームオーバー処理
    // ==========================================

    public virtual void OnGameClear()
    {
        // 既にゲーム終了済み、またはシーン遷移中なら何もしない
        if (m_isGameEnded || m_isLoading) return;
        m_isGameEnded = true;

        // BGMを停止し、クリアSEを再生
        if (SSoundManager.Instance != null)
        {
            SSoundManager.Instance.StopBGM();
            SSoundManager.Instance.PlaySE_StageClear();
        }

        // 進行状況を記録
        if (SProgressManager.SInstance != null)
        {
            SStageData data = SProgressManager.SInstance.GetStageData(SStageID);
            if (data == null)
            {
                SProgressManager.SInstance.AddStageData(new SStageData { SStageID = SStageID, SIsCleared = true });
            }
            else
            {
                data.SIsCleared = true;
            }
        }

        OnGameEnd();

        if (SUIManager.SInstance == null) return;

        GameObject clearUI = SUIManager.SInstance.SShowUI(SStageClearPrefab);
        if (clearUI != null && clearUI.TryGetComponent(out SStageClear clearScript))
        {
            clearScript.Setup(
                onNext: () => OnNextStage(),
                onRestart: () => OnRestart(),
                onReturn: () => OnBackTitle()
            );
        }
    }

    public virtual void OnGameOver()
    {
        // 既にゲーム終了済み、またはシーン遷移中なら何もしない
        if (m_isGameEnded || m_isLoading) return;
        m_isGameEnded = true;

        // BGMを停止し、ゲームオーバーSEを再生
        if (SSoundManager.Instance != null)
        {
            SSoundManager.Instance.StopBGM();
            SSoundManager.Instance.PlaySE_GameOver();
        }

        OnGameEnd();

        if (SUIManager.SInstance == null) return;

        GameObject overUI = SUIManager.SInstance.SShowUI(SGameOverPrefab);
        if (overUI != null && overUI.TryGetComponent(out SGameOverUI overScript))
        {
            overScript.Setup(
                onRestart: () => OnRestart(),
                onReturn: () => OnBackTitle()
            );
        }
    }

    // --- 各種ボタンから呼ばれるアクション群 ---

    private void OnNextStage()
    {
        int nextStageID = SStageID + 1;
        string nextSceneName = GetStageSceneName(nextStageID);

        // 最終ステージ、または次のステージのシーンがまだ無い場合はタイトルへ戻る
        if (nextStageID > SProgressManager.SStageCount || !CanLoadScene(nextSceneName))
        {
            Debug.Log($"[SInGameManagerBase] 次のステージ ({nextSceneName}) が無いためタイトルへ戻ります。");
            OnBackTitle();
            return;
        }

        TransitionTo(nextSceneName);
    }

    private void OnBackTitle()
    {
        TransitionTo("TitleScene");
    }

    private void OnRestart()
    {
        TransitionTo(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// フェードアウトしてからシーンを切り替える
    /// </summary>
    private void TransitionTo(string sceneName)
    {
        if (m_isLoading) return;

        SetPause(false);

        if (LoadSceneWithDelay(sceneName, 1.6f) && SUIManager.SInstance != null)
        {
            SUIManager.SInstance.SPlayFadeOut(1.6f);
        }
    }

    private void OnGameEnd()
    {
        // ゲーム終了時にプレイヤー操作を停止
        SetPlayerControlEnabled(false);
    }

    public int GetStageID()
    {
        return SStageID;
    }

    // ==========================================
    // デバッグ処理
    // ==========================================

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
