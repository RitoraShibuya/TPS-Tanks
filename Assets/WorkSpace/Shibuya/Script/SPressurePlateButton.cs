
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SPressurePlateButton : MonoBehaviour
{
    /// <summary>
    /// ボタンの状態定義
    /// </summary>
    public enum ButtonState
    {
        Hidden,     // 非表示・非活性（敵が残っている）
        Appeared,   // 出現・待機中（敵全滅、アクター接触待ち）
        Pressed     // 押し込み完了（壁消去済み・非表示・判定オフ）
    }

    [Header("1. 出現条件設定")]
    [Tooltip("このボタンを出現させるために倒す必要がある敵のリスト（全て倒すとボタン出現）")]
    [SerializeField] private List<EnemyBase> m_targetEnemies = new();

    [Header("2. 消去対象設定")]
    [Tooltip("ボタンが押された時に消去（Destroy）する壁オブジェクトの配列")]
    [SerializeField] private GameObject[] m_targetWalls;

    public ButtonState CurrentState => m_currentState;

    // 内部状態
    private ButtonState m_currentState = ButtonState.Hidden;

    // プレイヤーオブジェクト
    private GameObject m_targetActor;

    private Collider m_myCollider;
    private Renderer[] m_myRenderers;

    // 敵の全滅確認コルーチンの重複実行防止
    private Coroutine m_enemyCheckCoroutine;

    private void Awake()
    {
        m_myCollider = GetComponent<Collider>();
        m_myRenderers = GetComponentsInChildren<Renderer>();
    }

    private void Start()
    {
        // プレイヤーを検索
        FindPlayerActor();

        // 初期状態を設定
        SetState(ButtonState.Hidden);

        // 敵の破棄監視を登録
        SetupEnemyObservers();
    }

    /// <summary>
    /// シーン上から TankMovement を持つプレイヤーを検索
    /// </summary>
    private void FindPlayerActor()
    {
        var playerTank = FindFirstObjectByType<TankMovement>();

        if (playerTank != null)
        {
            m_targetActor = playerTank.gameObject;
        }
        else
        {
            Debug.LogWarning(
                "[SPressurePlateButton] シーン内に TankMovement (プレイヤー) が見つかりませんでした。");
        }
    }

    /// <summary>
    /// 状態の変更と見た目・判定の一括制御
    /// </summary>
    private void SetState(ButtonState newState)
    {
        // 押し込み完了後は状態を戻さない
        if (m_currentState == ButtonState.Pressed &&
            newState != ButtonState.Pressed)
        {
            return;
        }

        m_currentState = newState;

        switch (m_currentState)
        {
            case ButtonState.Hidden:
                SetVisibility(false);
                SetColliderEnabled(false);
                break;

            case ButtonState.Appeared:
                SetVisibility(true);
                SetColliderEnabled(true);

                Debug.Log(
                    "[SPressurePlateButton] 敵が全滅したため、ボタンが出現しました。");
                break;

            case ButtonState.Pressed:
                SetVisibility(false);
                SetColliderEnabled(false);
                DestroyWalls();

                Debug.Log(
                    "[SPressurePlateButton] ボタンが踏まれました！ 壁を消去し、ボタンを非表示にしました。");
                break;
        }
    }

    /// <summary>
    /// 自身および子オブジェクトの Renderer を制御
    /// </summary>
    private void SetVisibility(bool isVisible)
    {
        if (m_myRenderers == null) return;

        foreach (var rendererComponent in m_myRenderers)
        {
            if (rendererComponent != null)
            {
                rendererComponent.enabled = isVisible;
            }
        }
    }

    /// <summary>
    /// コライダーの有効・無効を切り替え
    /// </summary>
    private void SetColliderEnabled(bool isEnabled)
    {
        if (m_myCollider != null)
        {
            m_myCollider.enabled = isEnabled;
        }
    }

    /// <summary>
    /// 監視対象の敵へ破棄検知コンポーネントを追加
    /// </summary>
    private void SetupEnemyObservers()
    {
        if (m_targetEnemies == null || m_targetEnemies.Count == 0)
        {
            // 対象の敵がいない場合は最初から出現
            SetState(ButtonState.Appeared);
            return;
        }

        // Inspector の参照が Missing になっている敵を除去
        m_targetEnemies.RemoveAll(enemy => enemy == null);

        if (m_targetEnemies.Count == 0)
        {
            SetState(ButtonState.Appeared);
            return;
        }

        foreach (var enemy in m_targetEnemies)
        {
            if (enemy == null) continue;

            // 既に監視コンポーネントがある場合は再利用
            EnemyDeathObserver observer =
                enemy.GetComponent<EnemyDeathObserver>();

            if (observer == null)
            {
                observer = enemy.gameObject.AddComponent<EnemyDeathObserver>();
            }

            // このボタンへの通知を登録
            observer.Register(this);
        }

        // 登録時点ですでに全滅していないか確認
        CheckRemainingEnemiesImmediately();
    }

    /// <summary>
    /// 敵の破棄通知を受け取ったときに呼び出される
    /// </summary>
    private void OnEnemyDied()
    {
        Debug.Log(
            "[SPressurePlateButton] 敵の破棄通知を受信しました。");

        // 同じフレーム内で複数回実行しない
        if (m_enemyCheckCoroutine == null)
        {
            m_enemyCheckCoroutine =
                StartCoroutine(CheckRemainingEnemies());
        }
    }

    /// <summary>
    /// Destroy の反映後に残りの敵を確認
    /// </summary>
    private IEnumerator CheckRemainingEnemies()
    {
        // Destroy の反映後に確認する
        yield return null;

        m_enemyCheckCoroutine = null;

        CheckRemainingEnemiesImmediately();
    }

    /// <summary>
    /// 残っている敵を確認し、全滅していればボタンを出現させる
    /// </summary>
    private void CheckRemainingEnemiesImmediately()
    {
        if (m_currentState != ButtonState.Hidden) return;

        if (m_targetEnemies == null)
        {
            SetState(ButtonState.Appeared);
            return;
        }

        // Unity の Destroy 済みオブジェクト参照を除去
        m_targetEnemies.RemoveAll(enemy => enemy == null);

        Debug.Log(
            $"[SPressurePlateButton] 残りの対象敵数: {m_targetEnemies.Count}");

        if (m_targetEnemies.Count == 0)
        {
            SetState(ButtonState.Appeared);
        }
    }

    // --- 接触判定 ---

    private void OnTriggerEnter(Collider other)
    {
        CheckAndPress(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        CheckAndPress(collision.gameObject);
    }

    private void CheckAndPress(GameObject hitObject)
    {
        // 出現状態以外は判定しない
        if (m_currentState != ButtonState.Appeared) return;

        if (IsTargetActor(hitObject))
        {
            SetState(ButtonState.Pressed);
        }
    }

    private bool IsTargetActor(GameObject obj)
    {
        if (m_targetActor == null || obj == null) return false;

        return obj == m_targetActor ||
               obj.transform.root == m_targetActor.transform.root;
    }

    /// <summary>
    /// 対象の壁オブジェクトを一括破棄
    /// </summary>
    private void DestroyWalls()
    {
        if (m_targetWalls == null) return;

        foreach (var wall in m_targetWalls)
        {
            if (wall != null)
            {
                Destroy(wall);
            }
        }
    }

    // ==========================================
    // デバッグ用処理
    // ==========================================

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void OnGUI()
    {
        float areaWidth = 220f;
        float areaHeight = 140f;

        Rect areaRect = new Rect(
            Screen.width - areaWidth - 10f,
            10f,
            areaWidth,
            areaHeight);

        GUILayout.BeginArea(areaRect, GUI.skin.box);

        GUILayout.Label("[ Button Debug ]");

        GUILayout.Label(
            $"Actor Found: {(m_targetActor != null ? "Yes" : "No")}");

        GUILayout.Label($"State: {m_currentState}");

        GUILayout.Space(5);

        // 敵全滅の疑似発火
        GUI.enabled = m_currentState == ButtonState.Hidden;

        if (GUILayout.Button(
                "【DEBUG】Kill All Enemies",
                GUILayout.Height(30)))
        {
            ForceKillEnemies();
        }

        GUILayout.Space(5);

        // ボタン押下の疑似発火
        GUI.enabled = m_currentState == ButtonState.Appeared;

        if (GUILayout.Button(
                "【DEBUG】Press Button",
                GUILayout.Height(30)))
        {
            ForcePressButton();
        }

        GUI.enabled = true;

        GUILayout.EndArea();
    }

    /// <summary>
    /// デバッグ用：登録された敵を全て破棄
    /// </summary>
    [ContextMenu("Debug: Force Kill All Enemies")]
    public void ForceKillEnemies()
    {
        Debug.Log("[Debug] 強制敵全滅を実行しました。");

        if (m_targetEnemies != null)
        {
            foreach (var enemy in m_targetEnemies.ToArray())
            {
                if (enemy != null)
                {
                    // Destroy により EnemyDeathObserver も通知される
                    Destroy(enemy.gameObject);
                }
            }
        }

        // 破棄待ちを考慮し、次のフレームに全滅確認
        if (m_enemyCheckCoroutine == null)
        {
            m_enemyCheckCoroutine =
                StartCoroutine(CheckRemainingEnemies());
        }
    }

    /// <summary>
    /// デバッグ用：ボタンを強制的に押して壁を消す
    /// </summary>
    [ContextMenu("Debug: Force Press Button")]
    public void ForcePressButton()
    {
        if (m_currentState != ButtonState.Appeared) return;

        Debug.Log("[Debug] 強制ボタン押し込みを実行しました。");

        SetState(ButtonState.Pressed);
    }
#endif

    // ==========================================
    // 敵の破棄を検知する内部ヘルパークラス
    // ==========================================

    private class EnemyDeathObserver : MonoBehaviour
    {
        private readonly List<SPressurePlateButton> m_listeners = new();

        private bool m_destroyNotified;

        /// <summary>
        /// 監視先のボタンを登録
        /// </summary>
        public void Register(SPressurePlateButton listener)
        {
            if (listener == null) return;

            if (!m_listeners.Contains(listener))
            {
                m_listeners.Add(listener);
            }

            // すでに破棄通知済みなら再通知
            if (m_destroyNotified)
            {
                listener.OnEnemyDied();
            }
        }

        private void OnDestroy()
        {
            m_destroyNotified = true;

            Debug.Log(
                $"[EnemyDeathObserver] {gameObject.name} の破棄を検知しました。");

            // 通知先が複数ある場合も全て通知
            foreach (var listener in m_listeners)
            {
                if (listener != null)
                {
                    listener.OnEnemyDied();
                }
            }

            m_listeners.Clear();
        }
    }
}
