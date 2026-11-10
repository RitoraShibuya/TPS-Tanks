using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SGameManagerBase : MonoBehaviour
{
    protected bool m_isLoading = false;

    /// <summary>
    /// ステージIDからシーン名を作る（例: 1 → "Stage1Scene"）
    /// </summary>
    public static string GetStageSceneName(int stageID)
    {
        return $"Stage{stageID}Scene";
    }

    /// <summary>
    /// Build Settings に登録されていて読み込めるシーンか
    /// </summary>
    public static bool CanLoadScene(string sceneName)
    {
        return Application.CanStreamedLevelBeLoaded(sceneName);
    }

    /// <summary>
    /// 指定秒数待ってからシーンを読み込む。読み込めないシーンなら false を返して何もしない
    /// </summary>
    public bool LoadSceneWithDelay(string sceneName, float duration = 1.0f)
    {
        if (m_isLoading) return false;

        if (!CanLoadScene(sceneName))
        {
            Debug.LogError($"[SGameManagerBase] シーン '{sceneName}' は Build Settings に登録されていません。");
            return false;
        }

        StartCoroutine(LoadSceneCoroutine(sceneName, duration));
        return true;
    }

    private IEnumerator LoadSceneCoroutine(string sceneName, float duration)
    {
        m_isLoading = true;
        yield return new WaitForSeconds(duration);
        SceneManager.LoadScene(sceneName);
    }
}
