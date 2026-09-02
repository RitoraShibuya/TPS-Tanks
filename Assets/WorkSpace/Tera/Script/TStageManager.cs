using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StageSelectNavigation : MonoBehaviour
{
    [SerializeField] public Selectable[] stageButtons;
    [SerializeField] private RectTransform selectImage;

    [Header("クリック時に表示する画像")]
    [SerializeField] private RectTransform clickImage;

    // ▼▼▼ ここから追加 ▼▼▼
    [Header("進行状況によるロック制御")]
    [Tooltip("チュートリアルボタンを設定してください")]
    [SerializeField] private Selectable tutorialButton;

    private const string SaveKeyPrefix = "StageCleared_";
    // index 0:チュートリアル, 1〜4:ステージ1〜4
    private bool[] _cleared;
    // ▲▲▲ ここまで追加 ▲▲▲

    private Selectable _lastStageButton;
    private EventSystem _eventSystem;

    private void Awake()
    {
        if (stageButtons.Length > 0)
        {
            _lastStageButton = stageButtons[0];
        }

        // ▼▼▼ 追加 ▼▼▼
        TLoadProgress();
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

        // ▼▼▼ 追加 ▼▼▼
        TRefreshInteractable();
        // ▲▲▲ 追加 ▲▲▲
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

    // ▼▼▼ ここから追加 ▼▼▼

    /// <summary>
    /// PlayerPrefsからクリア状況を読み込む
    /// </summary>
    private void TLoadProgress()
    {
        // 0:チュートリアル + stageButtons.Length分(ステージ1〜4)
        _cleared = new bool[1 + stageButtons.Length];

        for (int i = 0; i < _cleared.Length; i++)
        {
            _cleared[i] = PlayerPrefs.GetInt(SaveKeyPrefix + i, 0) == 1;
        }
    }

    /// <summary>
    /// クリア状況にもとづいて各ボタンのinteractableを更新する
    /// </summary>
    private void TRefreshInteractable()
    {
        if (_cleared == null) return;

        // チュートリアルは常に解放
        if (tutorialButton != null)
        {
            tutorialButton.interactable = true;
        }

        // ステージ1は「チュートリアルクリア済みか(_cleared[0])」で判定
        // ステージ2以降は「一つ前のステージがクリア済みか(_cleared[i])」で判定
        for (int i = 0; i < stageButtons.Length; i++)
        {
            if (stageButtons[i] == null) continue;
            stageButtons[i].interactable = _cleared[i];
        }
    }

    /// <summary>
    /// ステージクリア時に呼び出す。stageIndexは 0:チュートリアル, 1〜4:ステージ1〜4。
    /// </summary>
    public void TSetStageCleared(int stageIndex)
    {
        if (_cleared == null || stageIndex < 0 || stageIndex >= _cleared.Length) return;

        _cleared[stageIndex] = true;
        PlayerPrefs.SetInt(SaveKeyPrefix + stageIndex, 1);
        PlayerPrefs.Save();

        TRefreshInteractable();
    }

    // ▲▲▲ ここまでクロードコードでの追加 ▲▲▲
}