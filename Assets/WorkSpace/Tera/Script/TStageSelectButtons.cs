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
        TDebugUnlockNext(1); // ★追加:動作確認用
        // ステージ1用の処理
    }

    public void TOnStage2Click()
    {
        OnStageSelectedEvent?.Invoke(2);
        OnStageButtonClicked();
        TDebugUnlockNext(2); // ★追加:動作確認用
        // ステージ2用の処理
    }

    public void TOnStage3Click()
    {
        OnStageSelectedEvent?.Invoke(3);
        OnStageButtonClicked();
        TDebugUnlockNext(3); // ★追加:動作確認用
        // ステージ3用の処理
    }

    public void TOnStage4Click()
    {
        OnStageSelectedEvent?.Invoke(4);
        OnStageButtonClicked();
        TDebugUnlockNext(4); // ★追加:動作確認用
        // ステージ4用の処理
    }

    public void TOnTutorialClick()
    {
        OnStageSelectedEvent?.Invoke(0);
        OnStageButtonClicked();
        TDebugUnlockNext(0); // ★追加:動作確認用
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

    // ▼▼▼ ここから追加 ▼▼▼
    /// <summary>
    /// 【動作確認用】クリックしたステージをクリア扱いにし、次のステージを解放する。
    /// stageIndexは 0:チュートリアル, 1〜4:ステージ1〜4。
    /// 本実装では、実際のクリア判定(ゴール到達など)が完成し次第この呼び出しは削除し、
    /// クリア成立時のみ TSetStageCleared を呼ぶ形に置き換えてください。
    /// </summary>
    private void TDebugUnlockNext(int stageIndex)
    {
        if (stageSelectNavigation == null) return;

        stageSelectNavigation.TSetStageCleared(stageIndex);
    }
    // ▲▲▲ ここまでクロードコードでの追加 ▲▲▲
}