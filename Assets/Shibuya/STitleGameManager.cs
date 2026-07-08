using UnityEditor;
using UnityEngine;

public class STitleGameManager : SGameManagerBase
{
    bool SIsTutorialCleared = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SUIManager.SInstance.SPlayFadeIn();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnSceneButtonClick()
    {
        if (!SIsTutorialCleared)
        {
            LoadSceneWithDelay("TutorialScene");
        }
        else
        {
            
        }
    }
}
