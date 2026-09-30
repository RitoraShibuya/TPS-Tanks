using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StageSelectNavigation : MonoBehaviour
{
    [SerializeField] public Selectable[] stageButtons;
    [SerializeField] private RectTransform selectImage;

    [Header("クリック時に表示する画像")]
    [SerializeField] private RectTransform clickImage;

    [Header("進行状況によるロック制御")]
    [Tooltip("チュートリアルボタンを設定してください")]
    [SerializeField] private Selectable tutorialButton;

    private const string SaveKeyPrefix = "StageCleared_";
    private bool[] _cleared;

    // ▼▼▼ 追加 ▼▼▼
    // Inspectorで元々設定されている(ロックが無い前提の)ナビゲーション構成を保持しておく
    private Navigation[] _originalStageNavigations;
    private Navigation _originalTutorialNavigation;
    // ▲▲▲ 追加 ▲▲▲

    private Selectable _lastStageButton;
    private EventSystem _eventSystem;

    private void Awake()
    {
        if (stageButtons.Length > 0)
        {
            _lastStageButton = stageButtons[0];
        }

        TLoadProgress();

        // ▼▼▼ 追加:ロック処理で書き換える前に、元のナビゲーション設定を保存しておく ▼▼▼
        TCaptureOriginalNavigations();
        // ▲▲▲ 追加 ▲▲▲
    }

    private void Start()
    {
        _eventSystem = EventSystem.current;

        if (_eventSystem == null)
        {
            Debug.LogWarning($"[{nameof(StageSelectNavigation)}] EventSystem が見つかりません。", this);
            return;
        }

        TRefreshInteractable();

        Selectable defaultSelection = TGetDefaultSelection();
        if (defaultSelection != null)
        {
            _eventSystem.SetSelectedGameObject(defaultSelection.gameObject);
        }

        if (selectImage != null)
        {
            selectImage.SetAsLastSibling();
        }

        if (clickImage != null)
        {
            clickImage.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (_eventSystem == null) return;

        var current = _eventSystem.currentSelectedGameObject;
        if (current == null) return;

        if (tutorialButton != null && current == tutorialButton.gameObject)
        {
            // 何もしない
        }
        else
        {
            foreach (var stage in stageButtons)
            {
                if (stage != null && stage.gameObject == current)
                {
                    _lastStageButton = stage;
                    break;
                }
            }
        }

        if (selectImage != null)
        {
            if (!selectImage.gameObject.activeSelf)
            {
                selectImage.gameObject.SetActive(true);
            }

            RectTransform buttonRect = current.GetComponent<RectTransform>();

            if (buttonRect != null)
            {
                selectImage.position = buttonRect.position;
            }
        }
    }

    public Selectable TGetLastStageButton()
    {
        return _lastStageButton;
    }

    public void TShowClickImage(RectTransform targetButtonRect)
    {
        if (clickImage == null || targetButtonRect == null) return;

        clickImage.gameObject.SetActive(true);
        clickImage.position = targetButtonRect.position;
        clickImage.SetAsLastSibling();
    }

    public void THideClickImage()
    {
        if (clickImage != null)
        {
            clickImage.gameObject.SetActive(false);
        }
    }

    public void THideSelectImage()
    {
        if (selectImage != null)
        {
            selectImage.gameObject.SetActive(false);
        }
    }

    private void TLoadProgress()
    {
        _cleared = new bool[1 + stageButtons.Length];

        for (int i = 0; i < _cleared.Length; i++)
        {
            _cleared[i] = PlayerPrefs.GetInt(SaveKeyPrefix + i, 0) == 1;
        }
    }

    private void TRefreshInteractable()
    {
        if (_cleared == null) return;

        if (tutorialButton != null)
        {
            tutorialButton.interactable = true;
        }

        for (int i = 0; i < stageButtons.Length; i++)
        {
            if (stageButtons[i] == null) continue;
            stageButtons[i].interactable = _cleared[i];

            // ★追加
            Debug.Log($"[StageSelectNavigation] stageButtons[{i}] ({stageButtons[i].name}) interactable = {stageButtons[i].interactable}");
        }

        TApplyNavigationLocks();
    }  // ▲▲▲ 追加 ▲▲▲

    private Selectable TGetDefaultSelection()
    {
        if (tutorialButton != null && tutorialButton.interactable)
        {
            return tutorialButton;
        }

        foreach (var stage in stageButtons)
        {
            if (stage != null && stage.interactable)
            {
                return stage;
            }
        }

        return null;
    }

public void TSetStageCleared(int stageIndex)
{
    if (_cleared == null || stageIndex < 0 || stageIndex >= _cleared.Length) return;

    _cleared[stageIndex] = true;
    PlayerPrefs.SetInt(SaveKeyPrefix + stageIndex, 1);
    PlayerPrefs.Save();

    // ★追加
    Debug.Log($"[StageSelectNavigation] TSetStageCleared({stageIndex}) 呼び出し完了。_cleared[{stageIndex}] = {_cleared[stageIndex]}");

    TRefreshInteractable();
}

// ▼▼▼ ここから追加 ▼▼▼

/// <summary>
/// Inspectorで設定されている「本来あるべきナビゲーション構成」を控えておく。
/// これが無いと、一度Noneにしたリンクを後で解放するとき元に戻せなくなる。
/// </summary>
private void TCaptureOriginalNavigations()
    {
        _originalStageNavigations = new Navigation[stageButtons.Length];
        for (int i = 0; i < stageButtons.Length; i++)
        {
            if (stageButtons[i] != null)
            {
                _originalStageNavigations[i] = stageButtons[i].navigation;
            }
        }

        if (tutorialButton != null)
        {
            _originalTutorialNavigation = tutorialButton.navigation;
        }
    }

    /// <summary>
    /// 「元のナビゲーション構成」を基準にしつつ、リンク先がロック中(interactable = false)の
    /// ボタンを指している場合はNone(移動不可)に上書きする。
    /// これにより、ロック中のボタンへはカーソル自体が絶対に移動できなくなる。
    /// </summary>
    private void TApplyNavigationLocks()
    {
        for (int i = 0; i < stageButtons.Length; i++)
        {
            if (stageButtons[i] == null) continue;

            Navigation nav = _originalStageNavigations[i];
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnUp = TFilterLocked(nav.selectOnUp);
            nav.selectOnDown = TFilterLocked(nav.selectOnDown);
            nav.selectOnLeft = TFilterLocked(nav.selectOnLeft);
            nav.selectOnRight = TFilterLocked(nav.selectOnRight);

            stageButtons[i].navigation = nav;
        }

        if (tutorialButton != null)
        {
            Navigation nav = _originalTutorialNavigation;
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnUp = TFilterLocked(nav.selectOnUp);
            nav.selectOnDown = TFilterLocked(nav.selectOnDown);
            nav.selectOnLeft = TFilterLocked(nav.selectOnLeft);
            nav.selectOnRight = TFilterLocked(nav.selectOnRight);

            tutorialButton.navigation = nav;
        }
    }

    /// <summary>
    /// リンク先がロック中(interactable = false)なら None(null)を返し、
    /// 解放済みならそのままのリンクを返す。
    /// </summary>
    private Selectable TFilterLocked(Selectable target)
    {
        if (target == null) return null;
        return target.interactable ? target : null;
    }

    // ▲▲▲ ここまで追加 ▲▲▲
}