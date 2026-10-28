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
    [Tooltip("シーン上にあらかじめ配置された敵（EnemyBase派生）をセット")]
    [SerializeField] private List<EnemyBase> m_targetEnemies = new();

    public bool IsPaused { get; private set; } = false;

    // ゲーム終了（クリアまたはオーバー）処理が既に走ったかのガードフラグ
    protected bool m_isGameEnded = false;

    // プレイヤー制御用
    private TankMovement m_playerTank;
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
        else
        {
            if (!m_isStartLocked && !m_isGameEnded)
            {
                SetPlayerControlEnabled(true);
            }
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
        // ★インゲームBGMの再生
        if (SSoundManager.Instance != null)
        {
            SSoundManager.Instance.PlayBGM_InGame();
        }

        // シーン上の TankMovement を自動検索
        m_playerTank = FindFirstObjectByType<TankMovement>();

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

        // ★1フレーム待機することで TankMovement の Start() と最初の LateUpdate() (カメラ初期位置セット) を確実に実行させる
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

        if (!isEnabled)
        {
            if (m_playerTank.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    private void SetupEnemyObservers()
    {
        foreach (var enemy in m_targetEnemies)
        {
            if (enemy == null) continue;

            // EnemyBaseを書き換える代わりに、監視用コンポーネントを動的にアタッチする
            var observer = enemy.gameObject.AddComponent<EnemyDeathObserver>();
            observer.OnDestroyedAction += OnEnemyDied;
        }
    }

    /// <summary>
    /// 敵がDestroyされた瞬間だけ呼ばれる処理
    /// </summary>
    private void OnEnemyDied()
    {
        // 破棄された敵（null）をリストから除外
        m_targetEnemies.RemoveAll(enemy => enemy == null);

        // 全滅したらクリア（すでにクリア演出中なら弾く）
        if (m_targetEnemies.Count == 0 && !m_isGameEnded)
        {
            OnGameClear();
        }
    }

    private void SetupPlayerObserver()
    {
        HHelth playerHealth = null;

        if (m_playerTank != null)
        {
            Debug.Log("m_playerTankがからです");
            playerHealth = m_playerTank.GetComponent<HHelth>();
        }

        // TankMovement と同じオブジェクトに無ければシーン上から検索
        if (playerHealth == null)
        {
            Debug.Log("playerHealthがからです");
            playerHealth = FindFirstObjectByType<HHelth>();
        }

        if (playerHealth == null)
        {
            Debug.Log("playerHealthがからなのでreturnしました");
            return;
        }
        // HHelthを書き換える代わりに、監視用コンポーネントを動的にアタッチする
        var observer = playerHealth.gameObject.AddComponent<PlayerDeathObserver>();
        observer.Setup(playerHealth);
        observer.OnDiedAction += OnPlayerDied;
    }

    /// <summary>
    /// プレイヤーの体力が0になった瞬間だけ呼ばれる処理
    /// </summary>
    private void OnPlayerDied()
    {
        Debug.Log("プレイヤーが死にました");
        // すでにクリア / ゲームオーバー演出中なら弾く
        if (m_isGameEnded) return;
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

        // ★BGMを停止し、クリアSEを再生
        if (SSoundManager.Instance != null)
        {
            SSoundManager.Instance.StopBGM();
            SSoundManager.Instance.PlaySE_StageClear();
        }

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
        // 既にゲーム終了済み、またはシーン遷移中なら何もしない
        if (m_isGameEnded || m_isLoading) return;
        m_isGameEnded = true;

        // ★BGMを停止し、ゲームオーバーSEを再生
        if (SSoundManager.Instance != null)
        {
            SSoundManager.Instance.StopBGM();
            SSoundManager.Instance.PlaySE_GameOver();
        }

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

    private void OnGUI()
    {
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

    // ==========================================
    // 敵の死亡（Destroy）を検知する内部ヘルパークラス
    // ==========================================
    private class EnemyDeathObserver : MonoBehaviour
    {
        public System.Action OnDestroyedAction;

        private void OnDestroy()
        {
            OnDestroyedAction?.Invoke();
        }
    }

    // ==========================================
    // プレイヤーの死亡（HHelth.OnDie）を検知する内部ヘルパークラス
    // ==========================================
    private class PlayerDeathObserver : MonoBehaviour
    {
        public System.Action OnDiedAction;

        private HHelth m_health;

        public void Setup(HHelth health)
        {
            m_health = health;
            m_health.OnDie += HandleDie;
        }

        private void HandleDie()
        {
            OnDiedAction?.Invoke();
        }

        private void OnDestroy()
        {
            if (m_health != null)
            {
                m_health.OnDie -= HandleDie;
            }
        }
    }
}