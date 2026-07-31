using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StageSelectNavigation : MonoBehaviour
{
    public static StageSelectNavigation Instance { get; private set; }

    [SerializeField] private Selectable[] stageButtons; // ステージ1〜4をInspectorで登録
    private Selectable _lastStageButton;

    private void Awake()
    {
        Instance = this;
        if (stageButtons.Length > 0)
            _lastStageButton = stageButtons[0]; // デフォルトはステージ1
    }

    private void Update()
    {
        var current = EventSystem.current.currentSelectedGameObject;
        if (current == null) return;

        foreach (var stage in stageButtons)
        {
            if (stage != null && stage.gameObject == current)
            {
                _lastStageButton = stage;
                break;
            }
        }
    }

    public Selectable TGetLastStageButton() => _lastStageButton;
}