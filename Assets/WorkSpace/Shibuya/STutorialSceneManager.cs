using UnityEngine;

public class STutorialSceneManager : SGameManagerBase
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SUIManager.SInstance.SPlayFadeIn(0.4f);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OnSceneButtonClick()
    {
        LoadSceneWithDelay("TitleScene", 1.6f);
        SUIManager.SInstance.SPlayFadeOut(1.6f);
    }
}
