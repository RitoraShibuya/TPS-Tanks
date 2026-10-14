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

    [Header("現在の状態")]
    [Tooltip("インスペクター上で現在のステータスを確認できます")]
    [SerializeField] private ButtonState m_currentState = ButtonState.Hidden;

    [Header("1. 出現条件設定")]
    [Tooltip("このボタンを出現させるために倒す必要がある敵のリスト（全て倒すとボタン出現）")]
    [SerializeField] private List<EnemyBase> m_targetEnemies = new();

    [Tooltip("ボタンの見た目・判定オブジェクト（指定しない場合はこのオブジェクト自身の描画/コライダーを制御）")]
    [SerializeField] private GameObject m_buttonVisual;

    [Header("2. 感圧（踏む）設定")]
    [Tooltip("ボタンを踏むことができる対象アクター（プレイヤー等）をインスペクターで指定")]
    [SerializeField] private GameObject m_targetActor;

    [Header("3. 消去対象設定")]
    [Tooltip("ボタンが押された時に消去（Destroy）する壁オブジェクトの配列")]
    [SerializeField] private GameObject[] m_targetWalls;

    public ButtonState CurrentState => m_currentState;

    private Collider m_myCollider;
    private Renderer[] m_myRenderers;

    private void Awake()
    {
        m_myCollider = GetComponent<Collider>();
        m_myRenderers = GetComponentsInChildren<Renderer>();
    }

    private void Start()
    {
        SetState(ButtonState.Hidden);
        SetupEnemyObservers();
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
    /// ボタンの可視/不可視の切り替え
    /// </summary>
    private void SetVisibility(bool isVisible)
    {
        if (m_buttonVisual != null)
        {
            m_buttonVisual.SetActive(isVisible);
        }
        else
        {
            if (m_myRenderers != null)
            {
                foreach (var r in m_myRenderers)
                {
                    if (r != null) r.enabled = isVisible;
                }
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
        // 画面右側にデバッグパネルを配置（他マネージャーのGUIと重ならないように設定）
        float areaWidth = 220f;
        float areaHeight = 160f;
        Rect areaRect = new Rect(Screen.width - areaWidth - 10f, 10f, areaWidth, areaHeight);

        GUILayout.BeginArea(areaRect, GUI.skin.box);
        GUILayout.Label($"<b>[ Button Debug ]</b>");
        GUILayout.Label($"State: <color=yellow>{m_currentState}</color>");
        GUILayout.Space(5);

        // 1. 敵死亡（全滅）の疑似発火ボタン
        GUI.enabled = (m_currentState == ButtonState.Hidden);
        if (GUILayout.Button("【DEBUG】Kill All Enemies", GUILayout.Height(35)))
        {
            ForceKillEnemies();
        }

        GUILayout.Space(5);

        // 2. ボタン押下（壁消去）の疑似発火ボタン
        GUI.enabled = (m_currentState == ButtonState.Appeared);
        if (GUILayout.Button("【DEBUG】Press Button", GUILayout.Height(35)))
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