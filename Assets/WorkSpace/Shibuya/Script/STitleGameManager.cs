using UnityEngine;

public class STitleGameManager : SGameManagerBase
{
    [Header("UI Settings")]
    [SerializeField] private GameObject STitleUIPrefab;
    [SerializeField] private GameObject SStageSelectUIPrefab;

    private GameObject STitleUIInstance;

    private void Start()
    {
        // メニュー用BGMを再生
        if (SSoundManager.Instance != null)
        {
            SSoundManager.Instance.PlayBGM_Menu();
        }

        if (SUIManager.SInstance == null)
        {
            Debug.LogError("[STitleGameManager] SUIManager がシーンにありません。");
            return;
        }

        SUIManager.SInstance.SPlayTransitionIn(0.4f);
        STitleUIInstance = SUIManager.SInstance.SShowUI(STitleUIPrefab);

        if (STitleUIInstance != null &&
            STitleUIInstance.TryGetComponent(out SCallingSelect selectUI))
        {
            selectUI.OnCallStageSelect -= OnMainButtonClick;
            selectUI.OnCallStageSelect += OnMainButtonClick;
        }
    }

    public void OnMainButtonClick()
    {
        if (m_isLoading) return;

        // ステージ1未クリアならステージ選択を出さずにステージ1へ
        if (SProgressManager.SInstance == null || !SProgressManager.SInstance.IsStageCleared(1))
        {
            LoadStage(1);
            return;
        }

        GameObject uiInstance = SUIManager.SInstance.SShowUI(SStageSelectUIPrefab);

        if (uiInstance != null &&
            uiInstance.TryGetComponent(out SStageSelectButtons selectUI))
        {
            selectUI.OnStageSelectedEvent -= LoadStage;
            selectUI.OnStageSelectedEvent += LoadStage;
        }
    }

    private void LoadStage(int stageID)
    {
        if (m_isLoading) return;
        if (stageID < 1 || stageID > SProgressManager.SStageCount) return;

        // 未解放ステージは弾く
        if (SProgressManager.SInstance != null &&
            !SProgressManager.SInstance.IsStageUnlocked(stageID))
        {
            Debug.LogWarning($"Stage {stageID} はまだ解放されていません。");
            return;
        }

        string sceneName = GetStageSceneName(stageID);

        // シーンが Build Settings にあるときだけ演出して遷移する
        if (LoadSceneWithDelay(sceneName, 1.0f) && SUIManager.SInstance != null)
        {
            SUIManager.SInstance.SPlayWipeOut(1.0f);
        }
    }

    // --- デバッグ用ボタン描画 ---
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void OnGUI()
    {
        if (SProgressManager.SInstance == null) return;

        GUILayout.BeginArea(new Rect(10, 10, 200, 280));
        GUILayout.Label("<b>[ Debug Stage Load ]</b>");

        for (int i = 1; i <= SProgressManager.SStageCount; i++)
        {
            bool isUnlocked = SProgressManager.SInstance.IsStageUnlocked(i);
            bool isCleared = SProgressManager.SInstance.IsStageCleared(i);
            bool hasScene = CanLoadScene(GetStageSceneName(i));

            string status = !hasScene
                ? "[No Scene]"
                : isCleared ? "[Cleared]" : (isUnlocked ? "[Unlocked]" : "[Locked]");

            GUI.enabled = hasScene && isUnlocked && !m_isLoading;

            if (GUILayout.Button($"Stage {i} {status}", GUILayout.Height(35)))
            {
                LoadStage(i);
            }
        }

        GUI.enabled = true;
        GUILayout.EndArea();
    }
#endif
}
