
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SStageSelectButtons : MonoBehaviour
{
    [Header("Stage Buttons")]
    [SerializeField] private Button SStage1Button;
    [SerializeField] private Button SStage2Button;
    [SerializeField] private Button SStage3Button;
    [SerializeField] private Button SStage4Button;

    private Button[] m_stageButtons;

    // ステージ選択イベント
    public event Action<int> OnStageSelectedEvent;

    private void Awake()
    {
        m_stageButtons = new Button[]
        {
            SStage1Button,
            SStage2Button,
            SStage3Button,
            SStage4Button
        };
    }

    private void Start()
    {
        // ステージの解放状況を反映
        if (SProgressManager.SInstance != null)
        {
            SetupButtons(
                SProgressManager.SInstance.GetStageStatusList()
            );
        }
    }

    /// <summary>
    /// 最初に解放されているステージボタンへフォーカス
    /// </summary>
    public void FocusFirstButton()
    {
        StartCoroutine(SetInitialFocus());
    }

    /// <summary>
    /// 初期フォーカス設定
    /// </summary>
    private IEnumerator SetInitialFocus()
    {
        // UI の有効化やレイアウト更新を待つ
        yield return null;

        // EventSystem を取得
        EventSystem es = EventSystem.current;

        if (es == null)
        {
            es = FindFirstObjectByType<EventSystem>();
        }

        if (es == null)
        {
            Debug.LogWarning(
                "[SStageSelectButtons] EventSystem が見つかりません。");
            yield break;
        }

        // 最初に操作可能なボタンを検索
        Button targetButton = null;

        foreach (Button button in m_stageButtons)
        {
            if (button == null)
            {
                continue;
            }

            if (!button.gameObject.activeInHierarchy ||
                !button.interactable)
            {
                continue;
            }

            targetButton = button;
            break;
        }

        if (targetButton == null)
        {
            Debug.LogWarning(
                "[SStageSelectButtons] フォーカス可能なステージボタンがありません。");
            yield break;
        }

        // 既存の選択を解除
        es.SetSelectedGameObject(null);

        // ステージボタンにフォーカスを設定
        es.SetSelectedGameObject(targetButton.gameObject);

        // 明示的に選択状態を更新
        targetButton.Select();

        Debug.Log(
            "[SStageSelectButtons] Selected: " +
            es.currentSelectedGameObject);
    }

    /// <summary>
    /// ステージの解放状況に応じてボタンを設定
    /// </summary>
    public void SetupButtons(List<StageStatusInfo> stageStatusList)
    {
        if (stageStatusList == null) return;

        foreach (StageStatusInfo status in stageStatusList)
        {
            int index = status.StageID - 1;

            // Stage 1～4 以外は対象外
            if (index < 0 || index >= m_stageButtons.Length)
            {
                continue;
            }

            Button button = m_stageButtons[index];

            if (button == null)
            {
                continue;
            }

            // 未解放ステージ・シーンがまだ無いステージは操作不可
            bool hasScene = SGameManagerBase.CanLoadScene(
                SGameManagerBase.GetStageSceneName(status.StageID));

            button.interactable = status.IsUnlocked && hasScene;
        }
    }

    /// <summary>
    /// ステージボタン押下時の共通処理
    /// </summary>
    private void SelectStage(int stageID)
    {
        // 未解放ステージは選択不可
        if (SProgressManager.SInstance == null)
        {
            return;
        }

        if (!SProgressManager.SInstance.IsStageUnlocked(stageID))
        {
            return;
        }

        // 決定音を再生
        if (SSoundManager.Instance != null)
        {
            SSoundManager.Instance.PlaySE_Decide();
        }

        // ゲームマネージャーへステージIDを通知
        OnStageSelectedEvent?.Invoke(stageID);
    }

    public void OnStage1ButtonClick()
    {
        SelectStage(1);
    }

    public void OnStage2ButtonClick()
    {
        SelectStage(2);
    }

    public void OnStage3ButtonClick()
    {
        SelectStage(3);
    }

    public void OnStage4ButtonClick()
    {
        SelectStage(4);
    }
}
