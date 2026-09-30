using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class SGameOverUI : MonoBehaviour
{
    [Header("最初に選択するボタン")]
    [SerializeField, Tooltip("生成時にフォーカスを合わせたいButtonをセット")]
    private GameObject FirstSelectedButton;

    // SInGameManagerBase から受け取るコールバック関数
    private Action SOnRestartAction; // Continue用
    private Action SOnReturnAction;  // To Title用

    private void OnEnable()
    {
        // UIが生成・アクティブになったら、1フレーム待機して確実にフォーカスを当てる
        StartCoroutine(FocusNextFrame());
    }

    private IEnumerator FocusNextFrame()
    {
        yield return null; // 1フレーム待機
        FocusFirstButton();
    }

    // GameManagerからUI表示時に呼ばれる初期化関数
    public void Setup(Action onRestart, Action onReturn)
    {
        SOnRestartAction = onRestart;
        SOnReturnAction = onReturn;
    }

    // コントローラー・キーボード操作用のフォーカス設定
    public void FocusFirstButton()
    {
        if (FirstSelectedButton == null) return;

        EventSystem current_event_system = EventSystem.current;
        if (current_event_system == null)
        {
            current_event_system = FindFirstObjectByType<EventSystem>();
        }

        if (current_event_system != null)
        {
            current_event_system.SetSelectedGameObject(null);
            current_event_system.SetSelectedGameObject(FirstSelectedButton);
        }
    }

    // ==========================================
    // UI Buttonの OnClick() に設定するメソッド群
    // ==========================================

    public void AContinueButton()
    {
        if (SOnRestartAction != null)
        {
            SOnRestartAction.Invoke();
        }
    }

    public void AReturnToTitleButton()
    {
        if (SOnReturnAction != null)
        {
            SOnReturnAction.Invoke();
        }
    }
}