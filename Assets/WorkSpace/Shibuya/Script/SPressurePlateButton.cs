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

    // 内部状態で管理（インスペクター非表示）
    private ButtonState m_currentState = ButtonState.Hidden;

    // 内部で自動取得するプレイヤー（TankMovement）オブジェクト
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
        // シーン内の TankMovement (プレイヤー) を自動検索
        FindPlayerActor();

        SetState(ButtonState.Hidden);
        SetupEnemyObservers();
    }

    /// <summary>
    /// シーン上から TankMovement コンポーネントを持つプレイヤーを検索
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
            Debug.LogWarning("[SPressurePlateButton] シーン内に TankMovement (プレイヤー) が見つかりませんでした。");
        }
    }

    /// <summary>
    /// 状態の変更とそれに伴う見た目・判定の一括制御
    /// </summary>
    private void SetState(ButtonState newState)
    {
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
                Debug.Log("[SPressurePlateButton] 敵が全滅したため、ボタンが出現しました。");
                break;

            case ButtonState.Pressed:
                SetVisibility(false);
                SetColliderEnabled(false);
                DestroyWalls();
                Debug.Log("[SPressurePlateButton] ボタンが踏まれました！ 壁を消去し、ボタンを非表示にしました。");
                break;
        }
    }

    /// <summary>
    /// ボタンの可視/不可視の切り替え（自身および子オブジェクトの全Rendererを制御）
    /// </summary>
    private void SetVisibility(bool isVisible)
    {
        if (m_myRenderers != null)
        {
            foreach (var r in m_myRenderers)
            {
                if (r != null) r.enabled = isVisible;
            }
        }
    }

    /// <summary>
    /// コライダーの有効/無効の切り替え
    /// </summary>
    private void SetColliderEnabled(bool isEnabled)
    {
        if (m_myCollider != null)
        {
            m_myCollider.enabled = isEnabled;
        }
    }

    /// <summary>
    /// 監視対象の敵へ死亡検知イベントをアタッチ
    /// </summary>
    private void SetupEnemyObservers()
    {
        if (m_targetEnemies == null || m_targetEnemies.Count == 0)
        {
            // 敵が指定されていない場合は最初から出現状態にする
            SetState(ButtonState.Appeared);
            return;
        }

        foreach (var enemy in m_targetEnemies)
        {
            if (enemy == null) continue;

            var observer = enemy.gameObject.AddComponent<EnemyDeathObserver>();
            observer.OnDestroyedAction += OnEnemyDied;
        }
    }

    /// <summary>
    /// 敵が撃破された時に呼ばれる通知
    /// </summary>
    private void OnEnemyDied()
    {
        m_targetEnemies.RemoveAll(enemy => enemy == null);

        // 指定された敵が全滅し、かつまだ非表示状態の場合のみ出現状態へ
        if (m_targetEnemies.Count == 0 && m_currentState == ButtonState.Hidden)
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
        // 出現状態（Appeared）以外（Hidden や Pressed）の時は判定しない
        if (m_currentState != ButtonState.Appeared) return;

        if (IsTargetActor(hitObject))
        {
            SetState(ButtonState.Pressed);
        }
    }

    private bool IsTargetActor(GameObject obj)
    {
        if (m_targetActor == null || obj == null) return false;
        return obj == m_targetActor || obj.transform.root == m_targetActor.transform.root;
    }

    /// <summary>
    /// 壁オブジェクト群の一括破棄
    /// </summary>
    private void DestroyWalls()
    {
        if (m_targetWalls != null)
        {
            foreach (var wall in m_targetWalls)
            {
                if (wall != null)
                {
                    Destroy(wall);
                }
            }
        }
    }

    // ==========================================
    // デバッグ用処理（ゲーム画面上のGUI＆インスペクター）
    // ==========================================

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void OnGUI()
    {
        float areaWidth = 220f;
        float areaHeight = 140f;
        Rect areaRect = new Rect(Screen.width - areaWidth - 10f, 10f, areaWidth, areaHeight);

        GUILayout.BeginArea(areaRect, GUI.skin.box);
        GUILayout.Label($"<b>[ Button Debug ]</b>");
        GUILayout.Label($"Actor Found: {(m_targetActor != null ? "<color=green>Yes</color>" : "<color=red>No</color>")}");
        GUILayout.Space(5);

        // 1. 敵死亡（全滅）の疑似発火ボタン
        GUI.enabled = (m_currentState == ButtonState.Hidden);
        if (GUILayout.Button("【DEBUG】Kill All Enemies", GUILayout.Height(30)))
        {
            ForceKillEnemies();
        }

        GUILayout.Space(5);

        // 2. ボタン押下（壁消去）の疑似発火ボタン
        GUI.enabled = (m_currentState == ButtonState.Appeared);
        if (GUILayout.Button("【DEBUG】Press Button", GUILayout.Height(30)))
        {
            ForcePressButton();
        }

        GUI.enabled = true;
        GUILayout.EndArea();
    }

    /// <summary>
    /// [デバッグ] 登録された敵を全滅させてボタンを出現させる
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
                    Destroy(enemy.gameObject);
                }
            }
            m_targetEnemies.Clear();
        }

        if (m_currentState == ButtonState.Hidden)
        {
            SetState(ButtonState.Appeared);
        }
    }

    /// <summary>
    /// [デバッグ] ボタンを強制的に押し込んで壁を消す
    /// </summary>
    [ContextMenu("Debug: Force Press Button")]
    public void ForcePressButton()
    {
        Debug.Log("[Debug] 強制ボタン押し込みを実行しました。");
        SetState(ButtonState.Pressed);
    }
#endif

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
}