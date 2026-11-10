
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

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

    // 前回選択されていたUI
    private GameObject SPreviousSelectedObject;

    // 直前のシーンを抜けるときに使った演出（次のシーンの入り演出をそろえるため）
    private enum TransitionType { None, Fade, Wipe }
    private TransitionType SLastOutTransition = TransitionType.None;

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
            return;
        }
    }

    private void Update()
    {
        if (EventSystem.current == null)
            return;

        GameObject currentSelected =
            EventSystem.current.currentSelectedGameObject;

        if (currentSelected == SPreviousSelectedObject)
            return;

        // UI移動入力があった場合のみカーソル音を再生
        if (currentSelected != null && SIsNavigationInput())
        {
            SPlayCursorSE();
        }

        SPreviousSelectedObject = currentSelected;
    }

    // キーボード・コントローラーのUI移動入力判定
    private bool SIsNavigationInput()
    {
        // キーボード
        if (Keyboard.current != null)
        {
            if (
                Keyboard.current.upArrowKey.isPressed ||
                Keyboard.current.downArrowKey.isPressed ||
                Keyboard.current.leftArrowKey.isPressed ||
                Keyboard.current.rightArrowKey.isPressed ||
                Keyboard.current.wKey.isPressed ||
                Keyboard.current.aKey.isPressed ||
                Keyboard.current.sKey.isPressed ||
                Keyboard.current.dKey.isPressed
            )
            {
                return true;
            }
        }

        // コントローラー
        if (Gamepad.current != null)
        {
            // 十字キー
            if (Gamepad.current.dpad.ReadValue().sqrMagnitude > 0.1f)
            {
                return true;
            }

            // 左スティック
            if (Gamepad.current.leftStick.ReadValue().magnitude > 0.5f)
            {
                return true;
            }
        }

        return false;
    }

    // カーソル音再生
    public void SPlayCursorSE()
    {
        if (SSoundManager.Instance != null)
        {
            SSoundManager.Instance.PlaySE_Cursor();
        }
    }

    public GameObject SShowUI(GameObject ui_prefab)
    {
        if (ui_prefab == null)
        {
            Debug.LogWarning("[SUIManager] UI Prefabが設定されていません。");
            return null;
        }

        return Instantiate(ui_prefab);
    }

    public void SHideUI(GameObject ui_object)
    {
        if (ui_object != null)
        {
            Destroy(ui_object);
        }
    }

    public void SShowPauseUI(
        Action onResumeAction,
        Action onReturnAction,
        Action onRestartAction)
    {
        if (SCurrentPauseInstance == null && SPauseUIPrefab != null)
        {
            SCurrentPauseInstance = Instantiate(SPauseUIPrefab);

            var option = SCurrentPauseInstance.GetComponent<OptionScript>();

            if (option != null)
            {
                option.Setup(
                    onResumeAction,
                    onReturnAction,
                    onRestartAction
                );
            }
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

    /// <summary>
    /// シーン開始時の入り演出。前のシーンをワイプで抜けたならワイプ、それ以外はフェードで入る
    /// </summary>
    public void SPlayTransitionIn(float duration = 1.0f)
    {
        TransitionType last = SLastOutTransition;
        SLastOutTransition = TransitionType.None;

        if (last == TransitionType.Wipe && SWipePrefab != null)
        {
            SPlayWipeIn(duration);
        }
        else
        {
            SPlayFadeIn(duration);
        }
    }

    public void SPlayFadeIn(float duration = 1.0f)
    {
        if (SFadePrefab == null) return;

        ResetTransitions();

        SCurrentFadeInstance = Instantiate(SFadePrefab);
        SCurrentFadeInstance.transform.SetAsLastSibling();

        Animator animator =
            SCurrentFadeInstance.GetComponentInChildren<Animator>();

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
        SLastOutTransition = TransitionType.Fade;

        SCurrentFadeInstance = Instantiate(SFadePrefab);
        SCurrentFadeInstance.transform.SetAsLastSibling();

        Animator animator =
            SCurrentFadeInstance.GetComponentInChildren<Animator>();

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

        Animator animator =
            SCurrentWipeInstance.GetComponentInChildren<Animator>();

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
        SLastOutTransition = TransitionType.Wipe;

        SCurrentWipeInstance = Instantiate(SWipePrefab);

        Animator animator =
            SCurrentWipeInstance.GetComponentInChildren<Animator>();

        if (animator != null)
        {
            animator.speed = 1.0f / duration;
            animator.Play("WipeOut");
        }
    }
}
