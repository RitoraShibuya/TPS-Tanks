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

    [Header("デバッグ")]
    [Tooltip("画面右上にデバッグ用ボタンを表示する（エディタ / Development Build のみ）")]
    [SerializeField] private bool m_showDebugGUI = false;

    public ButtonState CurrentState => m_currentState;

    // 内部状態
    private ButtonState m_currentState = ButtonState.Hidden;

    // プレイヤーオブジェクト
    private GameObject m_targetActor;

    private Collider m_myCollider;
    private Renderer[] m_myRenderers;

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

        // 敵の撃破監視を登録
        SetupEnemyObservers();
    }

    private void OnDestroy()
    {
        foreach (var enemy in m_targetEnemies)
        {
            if (enemy != null) enemy.OnDied -= OnEnemyDied;
        }
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

                // ボタン出現音
                if (SSoundManager.Instance != null) SSoundManager.Instance.PlaySE_SwitchAppear();

                Debug.Log(
                    "[SPressurePlateButton] 敵が全滅したため、ボタンが出現しました。");
                break;

            case ButtonState.Pressed:
                SetVisibility(false);
                SetColliderEnabled(false);

                // ボタン押下音
                if (SSoundManager.Instance != null) SSoundManager.Instance.PlaySE_SwitchPressed();

                // 対象の壁を破壊
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
    /// 監視対象の敵の撃破イベントを購読する
    /// </summary>
    private void SetupEnemyObservers()
    {
        // Inspector の参照が Missing になっている敵を除去
        m_targetEnemies.RemoveAll(enemy => enemy == null);

        foreach (var enemy in m_targetEnemies)
        {
            enemy.OnDied += OnEnemyDied;
        }

        // 対象の敵がいない場合は最初から出現
        CheckRemainingEnemies();
    }

    /// <summary>
    /// 敵が倒されたときに呼び出される（シーン切り替えによる破棄では呼ばれない）
    /// </summary>
    private void OnEnemyDied(EnemyBase deadEnemy)
    {
        deadEnemy.OnDied -= OnEnemyDied;
        m_targetEnemies.Remove(deadEnemy);

        CheckRemainingEnemies();
    }

    /// <summary>
    /// 残っている敵を確認し、全滅していればボタンを出現させる
    /// </summary>
    private void CheckRemainingEnemies()
    {
        if (m_currentState != ButtonState.Hidden) return;

        // Destroy 済みの参照を除去
        m_targetEnemies.RemoveAll(enemy => enemy == null || enemy.IsDead);

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

    /// <summary>
    /// 触れたのがプレイヤー（またはその子オブジェクト）か
    /// ※transform.root で比較すると、プレイヤーと同じ親の下にある敵や弾でも押せてしまうため IsChildOf で判定する
    /// </summary>
    private bool IsTargetActor(GameObject obj)
    {
        if (m_targetActor == null || obj == null) return false;

        return obj == m_targetActor ||
               obj.transform.IsChildOf(m_targetActor.transform);
    }

    /// <summary>
    /// 対象の壁オブジェクトを一括破棄
    /// </summary>
    private void DestroyWalls()
    {
        if (m_targetWalls == null) return;

        bool hasWall = false;

        foreach (var wall in m_targetWalls)
        {
            if (wall != null)
            {
                Destroy(wall);
                hasWall = true;
            }
        }

        // 実際に壁を破壊した場合だけ再生
        if (hasWall && SSoundManager.Instance != null)
        {
            SSoundManager.Instance.PlaySE_WallVanish();
        }
    }

    // ==========================================
    // デバッグ用処理
    // ==========================================

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void OnGUI()
    {
        if (!m_showDebugGUI) return;

        float areaWidth = 220f;
        float areaHeight = 140f;

        Rect areaRect = new Rect(
            Screen.width - areaWidth - 10f,
            10f,
            areaWidth,
            areaHeight);

        GUILayout.BeginArea(areaRect, GUI.skin.box);

        GUILayout.Label($"[ Button Debug: {gameObject.name} ]");

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
    /// デバッグ用：登録された敵を全て倒す
    /// </summary>
    [ContextMenu("Debug: Force Kill All Enemies")]
    public void ForceKillEnemies()
    {
        Debug.Log("[Debug] 強制敵全滅を実行しました。");

        foreach (var enemy in m_targetEnemies.ToArray())
        {
            if (enemy != null)
            {
                // Kill() で OnDied が発火し、ボタン・ゲームマネージャーの両方に通知される
                enemy.Kill();
            }
        }

        CheckRemainingEnemies();
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
}
