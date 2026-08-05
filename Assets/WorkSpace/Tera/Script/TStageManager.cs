using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StageSelectNavigation : MonoBehaviour
{
    [SerializeField] private Selectable[] stageButtons;
    [SerializeField] private RectTransform selectImage;

    [Header("クリック時に表示する画像")]
    [SerializeField] private RectTransform clickImage;

    private Selectable _lastStageButton;
    private EventSystem _eventSystem;

    private void Awake()
    {
        if (stageButtons.Length > 0)
        {
            _lastStageButton = stageButtons[0];
        }
    }

    private void Start()
    {
        _eventSystem = EventSystem.current;

        if (_eventSystem == null)
        {
            Debug.LogWarning($"[{nameof(StageSelectNavigation)}] EventSystem が見つかりません。", this);
            return;
        }

        if (stageButtons.Length > 0)
        {
            _eventSystem.SetSelectedGameObject(stageButtons[0].gameObject);
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

        foreach (var stage in stageButtons)
        {
            if (stage != null && stage.gameObject == current)
            {
                _lastStageButton = stage;
                break;
            }
        }

        if (selectImage != null)
        {
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
}