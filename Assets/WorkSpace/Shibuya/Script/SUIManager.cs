using System;
using UnityEngine;

public class SUIManager : MonoBehaviour
{
    public static SUIManager SInstance { get; private set; }

    [Header("Transition UI Prefabs")]
    [SerializeField] private GameObject SFadePrefab;
    [SerializeField] private GameObject SWipePrefab;

    [Header("Pause UI Prefab")]
    [SerializeField] private GameObject SPauseUIPrefab;

    private GameObject SCurrentFadeInstance;
    private GameObject SCurrentWipeInstance;
    private GameObject SCurrentPauseInstance;

    public event Action OnPauseEvent;
    public event Action OnResumeEvent;

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

    // ==========================================
    // 基本UI機能
    // ==========================================

    /// <summary>
    /// Canvasが含まれたUIプレハブを生成して表示します。
    /// </summary>
    public GameObject SShowUI(GameObject ui_prefab)
    {
        if (ui_prefab == null)
        {
            Debug.LogError("生成するUIプレハブが指定されていません！");
            return null;
        }

        GameObject spawn_ui = Instantiate(ui_prefab);
        return spawn_ui;
    }

    /// <summary>
    /// 表示中のUIを削除（非表示）します。
    /// </summary>
    public void SHideUI(GameObject ui_object)
    {
        if (ui_object != null)
        {
            Destroy(ui_object);
        }
    }

    // ==========================================
    // ポーズUI機能とイベントバインド
    // ==========================================

    /// <summary>
    /// ポーズイベントとレジュームイベントをバインドします。
    /// </summary>
    public void SBindPauseEvent(Action onPause, Action onResume)
    {
        if (onPause != null) OnPauseEvent += onPause;
        if (onResume != null) OnResumeEvent += onResume;
    }

    /// <summary>
    /// ポーズイベントとレジュームイベントのバインドを解除します。
    /// </summary>
    public void SUnbindPauseEvent(Action onPause, Action onResume)
    {
        if (onPause != null) OnPauseEvent -= onPause;
        if (onResume != null) OnResumeEvent -= onResume;
    }

    /// <summary>
    /// ポーズUIを表示し、バインドされたポーズイベントを発火します。
    /// </summary>
    public void SShowPauseUI()
    {
        if (SCurrentPauseInstance == null && SPauseUIPrefab != null)
        {
            SCurrentPauseInstance = Instantiate(SPauseUIPrefab);
            OnPauseEvent?.Invoke(); // イベント発火
        }
    }

    /// <summary>
    /// ポーズUIを削除（非表示）し、バインドされたレジュームイベントを発火します。
    /// </summary>
    public void SHidePauseUI()
    {
        if (SCurrentPauseInstance != null)
        {
            Destroy(SCurrentPauseInstance);
            SCurrentPauseInstance = null;
            OnResumeEvent?.Invoke(); // イベント発火
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
        if (SFadePrefab != null)
        {
            ResetTransitions();

            SCurrentFadeInstance = Instantiate(SFadePrefab);

            Animator animator = SCurrentFadeInstance.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.speed = 1.0f / duration;
                animator.Play("FadeIn");
            }

            Destroy(SCurrentFadeInstance, duration + 0.1f);
        }
    }

    public void SPlayFadeOut(float duration = 1.0f)
    {
        if (SFadePrefab != null)
        {
            ResetTransitions();

            SCurrentFadeInstance = Instantiate(SFadePrefab);

            Animator animator = SCurrentFadeInstance.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.speed = 1.0f / duration;
                animator.Play("FadeOut");
            }
        }
    }

    // ==========================================
    // ワイプ演出
    // ==========================================
    public void SPlayWipeIn(float duration = 1.0f)
    {
        if (SWipePrefab != null)
        {
            ResetTransitions();

            SCurrentWipeInstance = Instantiate(SWipePrefab);

            Animator animator = SCurrentWipeInstance.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.speed = 1.0f / duration;
                animator.Play("WipeIn");
            }

            Destroy(SCurrentWipeInstance, duration + 0.1f);
        }
    }

    public void SPlayWipeOut(float duration = 1.0f)
    {
        if (SWipePrefab != null)
        {
            ResetTransitions();

            SCurrentWipeInstance = Instantiate(SWipePrefab);

            Animator animator = SCurrentWipeInstance.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.speed = 1.0f / duration;
                animator.Play("WipeOut");
            }
        }
    }
}