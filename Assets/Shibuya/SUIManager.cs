using UnityEngine;

public class SUIManager : MonoBehaviour
{
    public static SUIManager SInstance { get; private set; }
    [SerializeField] private Transform SUIParent;

    private void Awake()
    {
        if (SInstance == null)
        {
            SInstance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public GameObject SShowUI(GameObject ui_prefub)
    {
        if (ui_prefub == null) return null;

        GameObject spawnedUI = Instantiate(ui_prefub, SUIParent);

        RectTransform rect = spawnedUI.GetComponent<RectTransform>();

        return spawnedUI;
    }
    public void SHideUI(GameObject uiObject)
    {
        if (uiObject != null)
        {
            Destroy(uiObject);
        }
    }
}
