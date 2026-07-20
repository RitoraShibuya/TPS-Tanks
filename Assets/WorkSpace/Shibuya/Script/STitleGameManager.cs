using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems; 
using UnityEngine.SceneManagement;

public class STitleGameManager : SGameManagerBase
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
        if (!SProgressManager.SInstance.IsStageCleared(0))
        {
            LoadSceneWithDelay("TutorialScene",1.0f);
        }
        else
        {
            LoadSceneWithDelay("Stage1Scene", 1.0f);
        }
        SUIManager.SInstance.SPlayWipeOut(1.0f);
    }
}
