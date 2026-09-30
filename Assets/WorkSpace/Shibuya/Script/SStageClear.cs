using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class SStageClear : MonoBehaviour
{
    [Header("最初に選択するボタン")]
    [SerializeField, Tooltip("生成時にフォーカスを合わせたいButtonをセット")]
    private GameObject FirstSelectedButton;

    // SInGameManagerBase から受け取るコールバック関数
    private Action SOnNextAction;
    private Action SOnRestartAction;
    private Action SOnReturnAction;

    private void OnEnable()
    {
        // UIが生成・アクティブになったら、1フレーム待機して確実フォーカスを当てる
        StartCoroutine(FocusNextFrame());
    }

    private IEnumerator FocusNextFrame()
    {
        yield return null; // 1フレーム待機してUnity内部のUI更新を終わらせる
        FocusFirstButton();
    }

    // GameManagerからUI表示時に呼ばれる初期化関数
    public void Setup(Action onNext, Action onRestart, Action onReturn)
    {
        SOnNextAction = onNext;
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

    public void ANextButton()
    {
        if (SOnNextAction != null)
        {
            SOnNextAction.Invoke();
        }
    }

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