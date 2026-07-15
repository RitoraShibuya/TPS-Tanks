using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class SUIManager : MonoBehaviour
{
    public static SUIManager SInstance { get; private set; }

    [Header("Transition Prefabs")]
    [SerializeField] private GameObject SFadePrefab;
    [SerializeField] private GameObject SWipePrefab;

    private Transform SCanvasTran;
    private EventSystem SMyEventSystem;

    private GameObject SCurrentFadeInstance;
    private GameObject SCurrentWipeInstance;

    private void Awake()
    {
        if (SInstance == null)
        {
            SInstance = this;
            DontDestroyOnLoad(gameObject);

            SMyEventSystem = GetComponentInChildren<EventSystem>();

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

    // ==========================================
    // イベントシステムの重複防止処理（復活版）
    // ==========================================
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (SInstance == this)
        {
            CleanupExternalEventSystems();
        }
    }

    private void CleanupExternalEventSystems()
    {
        EventSystem[] allEventSystems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);

        if (allEventSystems.Length <= 1)
        {
            return;
        }

        foreach (EventSystem es in allEventSystems)
        {
            if (es != SMyEventSystem)
            {
                Destroy(es.gameObject);
                Debug.Log($"重複している外部のEventSystemを削除しました: {es.gameObject.name}");
            }
        }
    }

    // ==========================================
    // 基本UI機能
    // ==========================================
    void Start() { }
    void Update() { }

    public GameObject SShowUI(GameObject ui_prefub)
    {
        if (ui_prefub == null) return null;
        GameObject spawn_ui = Instantiate(ui_prefub, SCanvasTran);
        return spawn_ui;
    }

    public void SHideUI(GameObject ui_object)
    {
        if (ui_object != null)
        {
            Destroy(ui_object);
        }
    }

    // ==========================================
    // 競合対策：既存のトランジションUIをすべて破棄する
    // ==========================================
    private void ResetTransitions()
    {
        if (SCurrentFadeInstance != null)
        {
            Destroy(SCurrentFadeInstance);
            SCurrentFadeInstance = null;
        }

        if (SCurrentWipeInstance != null)
        {
            Destroy(SCurrentWipeInstance);
            SCurrentWipeInstance = null;
        }
    }

    // ==========================================
    // フェード演出
    // ==========================================
    public void SPlayFadeIn(float duration = 1.0f)
    {
        if (SFadePrefab != null && SCanvasTran != null)
        {
            ResetTransitions();

            SCurrentFadeInstance = Instantiate(SFadePrefab, SCanvasTran);
            SCurrentFadeInstance.transform.SetAsLastSibling();

            Animator animator = SCurrentFadeInstance.GetComponent<Animator>();
            if (animator != null)
            {
                animator.speed = 1.0f / duration;
                animator.Play("FadeIn");
            }

            Destroy(SCurrentFadeInstance, duration + 0.1f);
        }
        else
        {
            Debug.LogError("フェード用のプレハブまたはCanvasTransformが設定されていません！");
        }
    }

    public void SPlayFadeOut(float duration = 1.0f)
    {
        if (SFadePrefab != null && SCanvasTran != null)
        {
            ResetTransitions();

            SCurrentFadeInstance = Instantiate(SFadePrefab, SCanvasTran);
            SCurrentFadeInstance.transform.SetAsLastSibling();

            Animator animator = SCurrentFadeInstance.GetComponent<Animator>();
            if (animator != null)
            {
                animator.speed = 1.0f / duration;
                animator.Play("FadeOut");
            }
        }
        else
        {
            Debug.LogError("フェード用のプレハブまたはCanvasTransformが設定されていません！");
        }
    }

    // ==========================================
    // ワイプ演出
    // ==========================================
    public void SPlayWipeIn(float duration = 1.0f)
    {
        if (SWipePrefab != null && SCanvasTran != null)
        {
            ResetTransitions();

            SCurrentWipeInstance = Instantiate(SWipePrefab, SCanvasTran);
            SCurrentWipeInstance.transform.SetAsLastSibling();

            Animator animator = SCurrentWipeInstance.GetComponent<Animator>();
            if (animator != null)
            {
                animator.speed = 1.0f / duration;
                animator.Play("WipeIn");
            }

            Destroy(SCurrentWipeInstance, duration + 0.1f);
        }
        else
        {
            Debug.LogError("ワイプ用のプレハブまたはCanvasTransformが設定されていません！");
        }
    }

    public void SPlayWipeOut(float duration = 1.0f)
    {
        if (SWipePrefab != null && SCanvasTran != null)
        {
            ResetTransitions();

            SCurrentWipeInstance = Instantiate(SWipePrefab, SCanvasTran);
            SCurrentWipeInstance.transform.SetAsLastSibling();

            Animator animator = SCurrentWipeInstance.GetComponent<Animator>();
            if (animator != null)
            {
                animator.speed = 1.0f / duration;
                animator.Play("WipeOut");
            }
        }
        else
        {
            Debug.LogError("ワイプ用のプレハブまたはCanvasTransformが設定されていません！");
        }
    }
}