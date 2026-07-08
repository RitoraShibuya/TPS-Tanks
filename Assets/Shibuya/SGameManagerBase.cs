using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SGameManagerBase : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SUIManager.SInstance.SPlayFadeIn();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void LoadSceneWithDelay(string sceneName)
    {
        StartCoroutine(LoadSceneCoroutine(sceneName));
        SUIManager.SInstance.SPlayFadeOut();
    }

    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        yield return new WaitForSeconds(1.5f);
        SceneManager.LoadScene(sceneName);
    }
}
