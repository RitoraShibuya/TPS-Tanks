
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SCallingSelect : MonoBehaviour
{
    [Header("UI Focus")]
    [Tooltip("タイトル画面で最初にフォーカスするボタン")]
    [SerializeField] private Button SMainButton;

    // ステージ選択要求イベント
    public event Action OnCallStageSelect;

    private void Start()
    {
        FocusFirstButton();
    }

    private void OnEnable()
    {
        FocusFirstButton();
    }

    /// <summary>
    /// 最初のボタンにフォーカスを設定
    /// </summary>
    public void FocusFirstButton()
    {
        if (SMainButton == null) return;

        EventSystem es = EventSystem.current;

        if (es == null)
        {
            es = FindFirstObjectByType<EventSystem>();
        }

        if (es == null) return;

        if (!SMainButton.gameObject.activeInHierarchy ||
            !SMainButton.interactable)
        {
            Debug.LogWarning(
                "[SCallingSelect] メインボタンが操作できません。");
            return;
        }

        // 現在の選択状態を解除
        es.SetSelectedGameObject(null);

        // メインボタンにフォーカスを設定
        es.SetSelectedGameObject(SMainButton.gameObject);

        // 明示的に選択状態を更新
        SMainButton.Select();

        Debug.Log(
            "[SCallingSelect] Selected: " +
            es.currentSelectedGameObject);
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
