
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SCallingSelect : MonoBehaviour
{
    [Header("UI Focus")]
    [Tooltip("タイトル画面で最初にフォーカスするボタン")]
    [SerializeField] private Button SMainButton;

    private EventSystem m_eventSystem;

    // ステージ選択要求イベント
    public event Action OnCallStageSelect;

    private void Awake()
    {
        // ワールド内の EventSystem を取得
        FindEventSystem();
    }

    private void Start()
    {
        // UI の初期化後にフォーカスを設定
        StartCoroutine(SetInitialFocus());
    }

    /// <summary>
    /// シーン内の EventSystem を取得
    /// </summary>
    private void FindEventSystem()
    {
        m_eventSystem = EventSystem.current;

        if (m_eventSystem == null)
        {
            m_eventSystem = FindFirstObjectByType<EventSystem>();
        }

        if (m_eventSystem == null)
        {
            Debug.LogWarning(
                "[SCallingSelect] シーン内に EventSystem が見つかりません。");
        }
    }

    /// <summary>
    /// 初期フォーカスを設定
    /// </summary>
    private IEnumerator SetInitialFocus()
    {
        // UI の生成・初期化を待つ
        yield return null;

        if (m_eventSystem == null)
        {
            FindEventSystem();
        }

        if (m_eventSystem == null || SMainButton == null)
        {
            yield break;
        }

        if (!SMainButton.gameObject.activeInHierarchy ||
            !SMainButton.interactable)
        {
            yield break;
        }

        m_eventSystem.SetSelectedGameObject(null);
        m_eventSystem.SetSelectedGameObject(SMainButton.gameObject);
    }

    /// <summary>
    /// メインボタン押下時
    /// </summary>
    public void OnMainButtonClick()
    {
        // 決定音を再生
        if (SSoundManager.Instance != null)
        {
            SSoundManager.Instance.PlaySE_Decide();
        }

        // ゲームマネージャーへ通知
        OnCallStageSelect?.Invoke();
    }
}
