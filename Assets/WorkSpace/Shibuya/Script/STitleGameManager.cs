
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

        // タイトルUIを表示
        STitleUIInstance = SUIManager.SInstance.SShowUI(STitleUIPrefab);

        if (STitleUIInstance != null &&
            STitleUIInstance.TryGetComponent(out SCallingSelect selectUI))
        {
            selectUI.OnCallStageSelect -= OnMainButtonClick;
            selectUI.OnCallStageSelect += OnMainButtonClick;

            // タイトルUIにフォーカスを設定
            selectUI.FocusFirstButton();
        }
    }

    /// <summary>
    /// メインボタン押下時
    /// </summary>
    public void OnMainButtonClick()
    {
        if (m_isLoading) return;

        // ステージ1未クリアならステージ選択を出さずにステージ1へ
        if (SProgressManager.SInstance == null ||
            !SProgressManager.SInstance.IsStageCleared(1))
        {
            LoadStage(1);
            return;
        }

        // ステージ選択UIを表示
        GameObject uiInstance =
            SUIManager.SInstance.SShowUI(SStageSelectUIPrefab);

        if (uiInstance != null &&
            uiInstance.TryGetComponent(out SStageSelectButtons selectUI))
        {
            selectUI.OnStageSelectedEvent -= LoadStage;
            selectUI.OnStageSelectedEvent += LoadStage;

            // ステージ選択UIにフォーカスを設定
            selectUI.FocusFirstButton();
        }
    }

    /// <summary>
    /// ステージ読み込み
    /// </summary>
    private void LoadStage(int stageID)
    {
        if (m_isLoading) return;

        if (stageID < 1 || stageID > SProgressManager.SStageCount)
        {
            return;
        }

        // 未解放ステージは弾く
        if (SProgressManager.SInstance != null &&
            !SProgressManager.SInstance.IsStageUnlocked(stageID))
        {
            Debug.LogWarning(
                $"Stage {stageID} はまだ解放されていません。");
            return;
        }

        string sceneName = GetStageSceneName(stageID);

        // シーンが Build Settings にあるときだけ演出して遷移する
        if (LoadSceneWithDelay(sceneName, 1.0f) &&
            SUIManager.SInstance != null)
        {
            SUIManager.SInstance.SPlayWipeOut(1.0f);
        }
    }
}
