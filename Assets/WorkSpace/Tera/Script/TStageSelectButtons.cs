using System;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StageSelectButtons : MonoBehaviour
{
    [Tooltip("同一プレハブ内にある想定。未設定なら自動検索します。")]
    [SerializeField] private StageSelectNavigation stageSelectNavigation;

    public event Action<int> OnStageSelectedEvent;

    private void Awake()
    {
        if (stageSelectNavigation == null)
        {
            stageSelectNavigation = GetComponentInParent<StageSelectNavigation>();
        }
    }

    public void TOnStage1Click()
    {
        OnStageSelectedEvent?.Invoke(1);
        OnStageButtonClicked();
        // ステージ1用の処理
    }

    public void TOnStage2Click()
    {
        OnStageSelectedEvent?.Invoke(2);
        OnStageButtonClicked();
        // ステージ2用の処理
    }

    public void TOnStage3Click()
    {
        OnStageSelectedEvent?.Invoke(3);
        OnStageButtonClicked();
        // ステージ3用の処理
    }

    public void TOnStage4Click()
    {
        OnStageSelectedEvent?.Invoke(4);
        OnStageButtonClicked();
        // ステージ4用の処理
    }

    public void TOnTutorialClick()
    {
        OnStageSelectedEvent?.Invoke(0);
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