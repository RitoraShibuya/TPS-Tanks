using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Selectable))]
public class TutorialUpNavigation : MonoBehaviour, IMoveHandler
{
    [Tooltip("未設定の場合、親階層から自動検索します(同一プレハブ内にある想定)")]
    [SerializeField] private StageSelectNavigation stageSelectNavigation;

    private void Awake()
    {
        if (stageSelectNavigation == null)
        {
            stageSelectNavigation = GetComponentInParent<StageSelectNavigation>();
        }

        if (stageSelectNavigation == null)
        {
            Debug.LogWarning($"[{nameof(TutorialUpNavigation)}] StageSelectNavigation が見つかりません。", this);
        }
    }

    public void OnMove(AxisEventData eventData)
    {
        TOnMove(eventData);
    }

    private void TOnMove(AxisEventData eventData)
    {
        if (eventData.moveDir != MoveDirection.Up) return;
        if (stageSelectNavigation == null) return;

        var target = stageSelectNavigation.TGetLastStageButton();
        if (target == null) return;

        EventSystem.current.SetSelectedGameObject(target.gameObject);
        eventData.Use();
    }
}