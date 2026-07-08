using UnityEditor;
using UnityEngine;
using System.Collections;

public class STitleGameManager : SGameManagerBase
{
    bool SIsTutorialCleared = false;
    public GameObject test_objyect;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnSceneButtonClick()
    {
        if (!SIsTutorialCleared)
        {
            GameObject ui = SUIManager.SInstance.SShowUI(test_objyect);
            StartCoroutine(DelayCoroutine());
        }
        else
        {
            
        }
    }
    private IEnumerator DelayCoroutine()
    {
        yield return new WaitForSeconds(2f);
        LoadNextScene("TutorialScene");
    }
}
