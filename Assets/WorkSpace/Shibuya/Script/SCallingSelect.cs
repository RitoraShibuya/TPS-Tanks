
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

    // ステージ選択要求イベント
    public event Action OnCallStageSelect;

    /// <summary>
    /// 最初のボタンにフォーカスを設定
    /// </summary>
    public void FocusFirstButton()
    {
        StartCoroutine(SetInitialFocus());
    }

    /// <summary>
    /// 初期フォーカス設定
    /// </summary>
    private IEnumerator SetInitialFocus()
    {
        // UIの生成・有効化を待つ
        yield return null;

        if (SMainButton == null)
        {
            Debug.LogWarning(
                "[SCallingSelect] MainButton が設定されていません。");
            yield break;
        }

        // Canvasのレイアウトを更新
        Canvas.ForceUpdateCanvases();

        // EventSystemを取得
        EventSystem es = EventSystem.current;

        if (es == null)
        {
            es = FindFirstObjectByType<EventSystem>();
        }

        if (es == null)
        {
            Debug.LogWarning(
                "[SCallingSelect] EventSystem が見つかりません。");
            yield break;
        }

        // ボタンの操作可能状態を確認
        if (!SMainButton.gameObject.activeInHierarchy ||
            !SMainButton.interactable)
        {
            Debug.LogWarning(
                "[SCallingSelect] メインボタンが操作できません。");
            yield break;
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
