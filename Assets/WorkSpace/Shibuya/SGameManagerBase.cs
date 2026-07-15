using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SGameManagerBase : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void LoadSceneWithDelay(string sceneName,float duration = 1.0f)
    {
        StartCoroutine(LoadSceneCoroutine(sceneName,duration));
    }

    private IEnumerator LoadSceneCoroutine(string sceneName,float duration)
    {
        yield return new WaitForSeconds(duration);
        SceneManager.LoadScene(sceneName);
    }
}
