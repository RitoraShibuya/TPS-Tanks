using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Selectable))]
public class TutorialUpNavigation : MonoBehaviour, IMoveHandler
{
    [Tooltip("必ずInspectorで直接アサインしてください")]
    [SerializeField] private StageSelectNavigation stageSelectNavigation;

    private void Awake()
    {
        if (stageSelectNavigation == null)
        {
            stageSelectNavigation = GetComponentInParent<StageSelectNavigation>();
        }

        // ★追加
        if (stageSelectNavigation != null)
        {
            Debug.Log($"[StageSelectButtons] 参照OK: {stageSelectNavigation.gameObject.name} (InstanceID: {stageSelectNavigation.GetInstanceID()})", this);
        }
        else
        {
            Debug.LogWarning("[StageSelectButtons] StageSelectNavigation が見つかりません。", this);
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

        // ★追加:ロックされているボタンへは移動させない
        if (!target.interactable) return;

        EventSystem.current.SetSelectedGameObject(target.gameObject);
        eventData.Use();
    }
}