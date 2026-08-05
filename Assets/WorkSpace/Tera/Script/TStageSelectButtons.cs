using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StageSelectButtons : MonoBehaviour
{
    [Tooltip("同一プレハブ内にある想定。未設定なら自動検索します。")]
    [SerializeField] private StageSelectNavigation stageSelectNavigation;

    private void Awake()
    {
        if (stageSelectNavigation == null)
        {
            stageSelectNavigation = GetComponentInParent<StageSelectNavigation>();
        }
    }

    public void TOnStage1Click()
    {
        OnStageButtonClicked();
        // ステージ1用の処理
    }

    public void TOnStage2Click()
    {
        OnStageButtonClicked();
        // ステージ2用の処理
    }

    public void TOnStage3Click()
    {
        OnStageButtonClicked();
        // ステージ3用の処理
    }

    public void TOnStage4Click()
    {
        OnStageButtonClicked();
        // ステージ4用の処理
    }

    public void TOnTutorialClick()
    {
        OnStageButtonClicked();
        // チュートリアル用の処理
    }

    private void OnStageButtonClicked()
    {
        if (stageSelectNavigation == null) return;

        var current = EventSystem.current?.currentSelectedGameObject;
        if (current == null) return;

        RectTransform rect = current.GetComponent<RectTransform>();

        stageSelectNavigation.TShowClickImage(rect);
        stageSelectNavigation.THideSelectImage();
    }
}