using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SStageSelectButtons : MonoBehaviour
{
    [Header("Stage Buttons")]
    [SerializeField] private Button SStage1Button;
    [SerializeField] private Button SStage2Button;
    [SerializeField] private Button SStage3Button;
    [SerializeField] private Button SStage4Button;

    // ステージ選択イベント
    public event Action<int> OnStageSelectedEvent;

    private Button[] m_stageButtons;

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
                SProgressManager.SInstance.GetStageStatusList(5)
            );
        }
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
                continue;

            Button button = m_stageButtons[index];

            if (button == null)
                continue;

            // 未解放ステージはグレーアウトして操作不可
            button.interactable = status.IsUnlocked;
        }
    }

    /// <summary>
    /// ステージボタン押下時の共通処理
    /// </summary>
    private void SelectStage(int stageID)
    {
        // 未解放ステージは選択不可
        if (SProgressManager.SInstance == null)
            return;

        if (!SProgressManager.SInstance.IsStageUnlocked(stageID))
            return;

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