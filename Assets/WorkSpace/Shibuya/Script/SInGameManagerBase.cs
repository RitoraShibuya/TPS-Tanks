using UnityEngine;

public class SInGameManagerBase : SGameManagerBase
{
    public int SStageID = -1;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected virtual void Start()
    {
        SUIManager.SInstance.SPlayFadeIn(0.4f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public virtual void OnGameClear()
    {
        SStageData data = new SStageData();
        data.SStageID = SStageID;
        data.SIsCleared = true;
        SProgressManager.SInstance.AddStageData(data);
        OnGameEnd();
    }

    public virtual void OnGameOver()
    {
        OnGameEnd();
    }

    private void OnGameEnd()
    {
        LoadSceneWithDelay("TitleScene", 1.6f);
        SUIManager.SInstance.SPlayFadeOut(1.6f);
    }
}
