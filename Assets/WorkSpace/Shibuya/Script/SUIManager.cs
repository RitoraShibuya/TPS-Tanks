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

    public GameObject SShowUI(GameObject ui_prefab)
    {
        if (ui_prefab == null) return null;
        return Instantiate(ui_prefab);
    }

    public void SHideUI(GameObject ui_object)
    {
        if (ui_object != null) Destroy(ui_object);
    }

    public void SShowPauseUI(Action onResumeAction, Action onReturnAction, Action onRestartAction)
    {
        if (SCurrentPauseInstance == null && SPauseUIPrefab != null)
        {
            SCurrentPauseInstance = Instantiate(SPauseUIPrefab);
            var option = SCurrentPauseInstance.GetComponent<OptionScript>();
            if (option != null) option.Setup(onResumeAction, onReturnAction, onRestartAction);
        }
    }

    public void SHidePauseUI()
    {
        if (SCurrentPauseInstance != null)
        {
            Destroy(SCurrentPauseInstance);
            SCurrentPauseInstance = null;
        }
    }

    private void ResetTransitions()
    {
        if (SCurrentFadeInstance != null) { Destroy(SCurrentFadeInstance); SCurrentFadeInstance = null; }
        if (SCurrentWipeInstance != null) { Destroy(SCurrentWipeInstance); SCurrentWipeInstance = null; }
    }

    public void SPlayFadeIn(float duration = 1.0f)
    {
        if (SFadePrefab == null) return;
        ResetTransitions();
        SCurrentFadeInstance = Instantiate(SFadePrefab);
        SCurrentFadeInstance.transform.SetAsLastSibling();
        Animator animator = SCurrentFadeInstance.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.speed = 1.0f / duration;
            animator.Play("FadeIn");
        }
        Destroy(SCurrentFadeInstance, duration + 0.1f);
    }

    public void SPlayFadeOut(float duration = 1.0f)
    {
        if (SFadePrefab == null) return;
        ResetTransitions();
        SCurrentFadeInstance = Instantiate(SFadePrefab);
        SCurrentFadeInstance.transform.SetAsLastSibling();
        Animator animator = SCurrentFadeInstance.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.speed = 1.0f / duration;
            animator.Play("FadeOut");
        }
    }

    public void SPlayWipeIn(float duration = 1.0f)
    {
        if (SWipePrefab == null) return;
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

    public void SPlayWipeOut(float duration = 1.0f)
    {
        if (SWipePrefab == null) return;
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