using UnityEngine;
using UnityEngine.EventSystems;

public class OptionScript : MonoBehaviour
{
    [Header("最初に選択するボタン")]
    [SerializeField, Tooltip("生成時にフォーカスを合わせたいButtonをセット")]
    private GameObject FirstSelectedButton;

    private void Start()
    {
        FocusFirstButton();
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

    }

    public void AReturnToTitleButton()
    {

    }

    public void ARestartButton()
    {

    }
}