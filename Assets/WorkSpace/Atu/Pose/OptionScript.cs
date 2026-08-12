using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class OptionScript : MonoBehaviour
{
    [Header("最初に選択するボタン")]
    [SerializeField, Tooltip("生成時にフォーカスを合わせたいButtonをセット")]
    private GameObject FirstSelectedButton;

    private Action SOnResumeAction;
    private Action SOnReturnAction;
    private Action SOnRestartAction;

    private void Start()
    {
        FocusFirstButton();
    }

    public void Setup(Action onResume, Action onReturn, Action onRestart)
    {
        SOnResumeAction = onResume;
        SOnReturnAction = onReturn;
        SOnRestartAction = onRestart;
    }

    private void OnEnable()
    {
        FocusFirstButton();
    }

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

    public void AContinueButton()
    {
        if (SOnResumeAction != null)
        {
            SOnResumeAction.Invoke();
        }
    }

    public void AReturnToTitleButton()
    {
        if(SOnReturnAction != null)
        {
            SOnReturnAction.Invoke();
        }
    }

    public void ARestartButton()
    {
        if (SOnRestartAction != null)
        {
            SOnRestartAction.Invoke();
        }
    }
}