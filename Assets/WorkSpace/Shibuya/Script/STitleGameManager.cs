using System.Collections.Generic;
using UnityEngine;

public class STitleGameManager : SGameManagerBase
{
    [Header("UI Settings")]
    [SerializeField] private GameObject STitleUIPrefab;
    [SerializeField] private GameObject SStageSelectUIPrefab;

    private GameObject STitleUIInstance;
    private List<StageStatusInfo> m_stageStatusList = new();

    private void Start()
    {
        SUIManager.SInstance.SPlayFadeIn(0.4f);
        STitleUIInstance = SUIManager.SInstance.SShowUI(STitleUIPrefab);

        TCallingSelect selectUI = STitleUIInstance.GetComponent<TCallingSelect>();
        if (selectUI != null)
        {
            selectUI.OnCallStageSelect -= OnMainButtonClick;
            selectUI.OnCallStageSelect += OnMainButtonClick;
        }

        UpdateStageStatusList();
    }

    public void UpdateStageStatusList()
    {
        if (SProgressManager.SInstance != null)
        {
            m_stageStatusList = SProgressManager.SInstance.GetStageStatusList(5);
        }
    }

    public void OnMainButtonClick()
    {
        if (m_isLoading) return;

        UpdateStageStatusList();

        if (!SProgressManager.SInstance.IsStageCleared(0))
        {
            LoadStage(0);
        }
        else
        {
            SUIManager.SInstance.SHideUI(STitleUIInstance);

            // 重複生成を排除し1回のみ生成
            GameObject uiInstance = SUIManager.SInstance.SShowUI(SStageSelectUIPrefab);

            StageSelectButtons selectUI = uiInstance.GetComponent<StageSelectButtons>();
            if (selectUI != null)
            {
                selectUI.OnStageSelectedEvent -= LoadStage;
                selectUI.OnStageSelectedEvent += LoadStage;

                // 今後 StageSelectButtons 側でボタン初期化処理を実装した際にコメント解除する
                // selectUI.SetupButtons(m_stageStatusList);
            }
        }
    }

    private void LoadStage(int stageID)
    {
        if (m_isLoading) return;
        if (stageID < 0 || stageID > 4) return;

        // 未解放ステージは弾く
        if (!SProgressManager.SInstance.IsStageUnlocked(stageID))
        {
            Debug.LogWarning($"Stage {stageID} はまだ解放されていません。");
            return;
        }

        SUIManager.SInstance.SPlayWipeOut(1.0f);

        string sceneName = (stageID == 0) ? "TutorialScene" : $"Stage{stageID}Scene";

        LoadSceneWithDelay(sceneName, 1.0f);
    }

    // --- デバッグ用ボタン描画 ---
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 200, 280));
        GUILayout.Label("<b>[ Debug Stage Load ]</b>");

        for (int i = 0; i <= 4; i++)
        {
            bool isUnlocked = SProgressManager.SInstance.IsStageUnlocked(i);
            bool isCleared = SProgressManager.SInstance.IsStageCleared(i);

            string status = isCleared ? "[Cleared]" : (isUnlocked ? "[Unlocked]" : "[Locked]");
            string label = (i == 0) ? $"0: Tutorial {status}" : $"Stage {i} {status}";

            GUI.enabled = isUnlocked && !m_isLoading;

            if (GUILayout.Button(label, GUILayout.Height(35)))
            {
                LoadStage(i);
            }
        }

        GUI.enabled = true;
        GUILayout.EndArea();
    }
#endif
}