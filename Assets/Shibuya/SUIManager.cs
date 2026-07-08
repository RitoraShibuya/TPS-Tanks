using UnityEngine;

public class SUIManager : MonoBehaviour
{
    public static SUIManager SInstance { get; private set; }

    [SerializeField] private GameObject SFadePrefab;

    private Transform SCanvasTran;
    private GameObject SCurrentFadeInstance;

    private void Awake()
    {
        if (SInstance == null)
        {
            SInstance = this;
            DontDestroyOnLoad(gameObject);

            Canvas childCanvas = GetComponentInChildren<Canvas>();
            if (childCanvas != null)
            {
                SCanvasTran = childCanvas.transform;
            }
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

        GameObject spawn_ui = Instantiate(ui_prefub, SCanvasTran);

        RectTransform rect = spawn_ui.GetComponent<RectTransform>();

        return spawn_ui;
    }

    public void SHideUI(GameObject ui_object)
    {
        if (ui_object != null)
        {
            Destroy(ui_object);
        }
    }

    public void SPlayFadeIn()
    {
        if (SFadePrefab != null && SCanvasTran != null)
        {
            if (this.SCurrentFadeInstance == null)
            {
                this.SCurrentFadeInstance = Instantiate(SFadePrefab, SCanvasTran);
            }

            SCurrentFadeInstance.transform.SetAsLastSibling();

            Animator animator = SCurrentFadeInstance.GetComponent<Animator>();
            if (animator != null)
            {
                animator.Play("FadeIn");
            }


            Destroy(SCurrentFadeInstance, 1.5f);
        }
        else
        {
            Debug.LogError("プレハブまたはCanvasTransformが設定されていません！");
        }
    }

    public void SPlayFadeOut()
    {
        if (SFadePrefab != null && SCanvasTran != null)
        {
            if (SCurrentFadeInstance == null)
            {
                SCurrentFadeInstance = Instantiate(SFadePrefab, SCanvasTran);
            }

            SCurrentFadeInstance.transform.SetAsLastSibling();

            Animator animator = SCurrentFadeInstance.GetComponent<Animator>();
            if (animator != null)
            {
                animator.Play("FadeOut");
            }
        }
        else
        {
            Debug.LogError("プレハブまたはCanvasTransformが設定されていません！");
        }
    }
}
