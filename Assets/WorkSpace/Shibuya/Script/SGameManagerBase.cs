using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SGameManagerBase : MonoBehaviour
{
    protected bool m_isLoading = false;

    public void LoadSceneWithDelay(string sceneName, float duration = 1.0f)
    {
        if (m_isLoading) return;
        StartCoroutine(LoadSceneCoroutine(sceneName, duration));
    }

    private IEnumerator LoadSceneCoroutine(string sceneName, float duration)
    {
        m_isLoading = true;
        yield return new WaitForSeconds(duration);
        SceneManager.LoadScene(sceneName);
    }
}