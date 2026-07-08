using UnityEngine;
using UnityEngine.SceneManagement;

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

    protected void LoadNextScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
