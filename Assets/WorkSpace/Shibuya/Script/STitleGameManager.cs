using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems; 
using UnityEngine.SceneManagement;

public class STitleGameManager : SGameManagerBase
{
    [Header("UI Settings")]
    [SerializeField] private GameObject STitleUIPrefab;
    [SerializeField] private GameObject SStageSelectUIPrefab;

    private GameObject STitleUIInstance;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        SUIManager.SInstance.SPlayFadeIn(0.4f);
        STitleUIInstance = SUIManager.SInstance.SShowUI(STitleUIPrefab);

        //生成されたUIのコンポーネントを取得し、イベントを紐づける
        TCallingSelect selectUI = STitleUIInstance.GetComponent<TCallingSelect>();
        if (selectUI != null)
        {
            // UIから「ステージが選ばれた」という通知が来たら、LoadStage を実行するよう予約
            selectUI.OnCallStageSelect += OnMainButtonClick;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnMainButtonClick()
    {
        if (!SProgressManager.SInstance.IsStageCleared(0))
        {
            LoadStage(0);
        }
        else
        {
            SUIManager.SInstance.SHideUI(STitleUIInstance);

            SUIManager.SInstance.SShowUI(SStageSelectUIPrefab);
            //UIManagerにUIを出してもらう
            GameObject uiInstance = SUIManager.SInstance.SShowUI(SStageSelectUIPrefab);

            //生成されたUIのコンポーネントを取得し、イベントを紐づける
            StageSelectButtons selectUI = uiInstance.GetComponent<StageSelectButtons>();
            if (selectUI != null)
            {
                // UIから「ステージが選ばれた」という通知が来たら、LoadStage を実行するよう予約
                selectUI.OnStageSelectedEvent += LoadStage;
            }
        }
       
    }

    private void LoadStage(int stageID)
    {
        SUIManager.SInstance.SPlayWipeOut(1.0f);

        switch (stageID)
        {
            default:
                break;
            case 0:
                LoadSceneWithDelay("TutorialScene", 1.0f);
                break;
            case 1:
                LoadSceneWithDelay("Stage1Scene", 1.0f);
                break;
        }
    }
}
