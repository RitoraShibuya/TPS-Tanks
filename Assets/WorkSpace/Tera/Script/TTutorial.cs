using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Selectable))]
public class TutorialUpNavigation : MonoBehaviour, IMoveHandler
{

    public void OnMove(AxisEventData eventData)
    {
        TOnMove(eventData);
    }

    private void TOnMove(AxisEventData eventData)
    {
        if (eventData.moveDir == MoveDirection.Up)
        {
            var target = StageSelectNavigation.Instance.TGetLastStageButton();
            if (target != null)
            {
                EventSystem.current.SetSelectedGameObject(target.gameObject);
                eventData.Use(); // Selectable標準の上移動処理を止める
            }
        }
    }
}