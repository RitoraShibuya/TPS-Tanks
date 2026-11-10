
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
    private EventSystem m_eventSystem;

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

        // ワールド内の EventSystem を取得
        FindEventSystem();
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

        // UI の初期化後にフォーカスを設定
        StartCoroutine(SetInitialFocus());
    }

    /// <summary>
    /// シーン内の EventSystem を取得
    /// </summary>
    private void FindEventSystem()
    {
        m_eventSystem = EventSystem.current;

        if (m_eventSystem == null)
        {
            m_eventSystem = FindFirstObjectByType<EventSystem>();
        }

        if (m_eventSystem == null)
        {
            Debug.LogWarning(
                "[SStageSelectButtons] シーン内に EventSystem が見つかりません。");
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
            {
                continue;
            }

            Button button = m_stageButtons[index];

            if (button == null)
            {
                continue;
            }

            // 未解放ステージはグレーアウトして操作不可
            button.interactable = status.IsUnlocked;
        }
    }

    /// <summary>
    /// 最初に解放されているステージボタンへフォーカス
    /// </summary>
    private IEnumerator SetInitialFocus()
    {
        // UI の有効化やレイアウト更新を待つ
        yield return null;

        if (m_eventSystem == null)
        {
            FindEventSystem();
        }

        if (m_eventSystem == null)
        {
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

            if (!button.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (!button.interactable)
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

        // 既存の選択を解除してからフォーカスを割り当てる
        m_eventSystem.SetSelectedGameObject(null);
        m_eventSystem.SetSelectedGameObject(targetButton.gameObject);
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
