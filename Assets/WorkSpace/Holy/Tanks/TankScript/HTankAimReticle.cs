using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 照準UI(レティクル)を画面に表示するスクリプト。
///
/// 仕様:
/// - 狙点(TankAimSystemが計算するワールド座標)を画面上の2D座標に変換し、
///   UIレイヤーで表示する。
/// - Y軸オフセットを加えて表示する。
/// - Rスティックの上下操作(仰角)に対し、係数を持って照準も上下に追加移動する。
///
/// セットアップ:
/// - Canvas(Screen Space - Overlay 推奨)の下にUI Imageを配置し、
///   そのUI ImageのGameObjectにこのスクリプトをアタッチする。
/// - 「Aim System」に、Muzzleに付いている TankAimSystem を登録する。
/// - 「Render Camera」に、実際に描画しているカメラ(UDRotaterの子のCameraなど)を登録する。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class TankAimReticle : MonoBehaviour
{
    [Header("Input Action")]
    [Tooltip("ゲームパッド右スティックに使用するInputAction(Vector2)。TankControls.inputactionsの「StickLook」を割り当てる。" +
             "TankHead/UDRotaterと同じアクションを指定してよい。")]
    [SerializeField]
    private InputActionReference stickLookActionReference;

    [Header("参照")]
    [Tooltip("狙点の計算に使用する TankAimSystem")]
    [SerializeField]
    private TankAimSystem aimSystem;

    [Tooltip("狙点をスクリーン座標に変換する際に使用するカメラ")]
    [SerializeField]
    private Camera renderCamera;

    [Header("表示調整")]
    [Tooltip("スクリーン座標へのY軸オフセット(ピクセル)")]
    [SerializeField]
    private float yScreenOffset = 0f;

    [Tooltip("Rスティック上下操作(仰角)に対する追加の上下移動係数(ピクセル)")]
    [SerializeField]
    private float verticalStickCoefficient = 50f;

    [Tooltip("この大きさ未満の右スティック入力は無視する(遊び・誤差吸収用)")]
    [SerializeField]
    private float stickDeadzone = 0.1f;

    [Header("エクセル連携")]
    [Tooltip("CSV(Excel)からインポートした調整値を反映するためのTankTuningConfig。" +
             "設定すると、起動時にこのアセットの値で上記のパラメーターが上書きされる。未設定ならこのInspectorの値をそのまま使用する。")]
    [SerializeField]
    private TankTuningConfig tuningConfig;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        ApplyTuningConfig();

        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (renderCamera == null)
        {
            renderCamera = Camera.main;
        }
    }

    /// <summary>
    /// TankTuningConfig(CSV/Excelからインポートされた値)が設定されている場合、
    /// そちらの値でこのスクリプトのパラメーターを上書きする。
    /// </summary>
    private void ApplyTuningConfig()
    {
        if (tuningConfig == null)
        {
            return;
        }

        yScreenOffset = tuningConfig.reticle_YScreenOffset;
        verticalStickCoefficient = tuningConfig.reticle_VerticalStickCoefficient;
        stickDeadzone = tuningConfig.reticle_StickDeadzone;
    }

    private void OnEnable()
    {
        // 【重要】StickLookはTankHead(Turret)・UDRotaterと同じ実体を共有していることが多い。
        // InputActionのEnable/Disableは「参照カウント」ではなく単純なON/OFFの状態切り替えなので、
        // 複数のスクリプトが同じアクションに対して個別にDisable()を呼ぶと、
        // 他のスクリプトが使っている分まで一緒に止まってしまう。
        // そのため、このスクリプトではEnableだけ行い、Disableは呼ばない
        // (TankHead/UDRotater側が管理しているEnable状態に相乗りするだけにする)。
        if (stickLookActionReference != null)
        {
            stickLookActionReference.action.Enable();
        }
    }

    private void OnDisable()
    {
        // 意図的に何もしない。
        // このGameObjectが非表示(SetActive(false))になった際に、共有しているStickLookアクション
        // まで一緒にDisableしてしまうと、TankHead/UDRotater側の視点操作まで止まってしまうため
        // (実際にこれが原因で「スティックでの視点操作が効かなくなる」不具合が起きたことがある)。
        // StickLookの有効/無効の管理は、常時アクティブなTankHead/UDRotater側に任せる。
    }

    private void LateUpdate()
    {
        if (aimSystem == null || renderCamera == null)
        {
            return;
        }

        Vector3 aimWorldPoint = aimSystem.GetAimWorldPoint(out _);
        Vector3 screenPoint = renderCamera.WorldToScreenPoint(aimWorldPoint);

        // カメラの後ろ(画面に映らない位置)にある場合は非表示にする
        bool isBehindCamera = screenPoint.z <= 0f;
        SetVisible(!isBehindCamera);
        if (isBehindCamera)
        {
            return;
        }

        // Rスティック上下入力を読み取り、係数を掛けて追加のオフセットにする
        Vector2 stickInput = stickLookActionReference != null
            ? stickLookActionReference.action.ReadValue<Vector2>()
            : Vector2.zero;

        if (stickInput.sqrMagnitude < stickDeadzone * stickDeadzone)
        {
            stickInput = Vector2.zero;
        }

        screenPoint.y += yScreenOffset + (stickInput.y * verticalStickCoefficient);

        rectTransform.position = new Vector3(screenPoint.x, screenPoint.y, 0f);
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
        }
        else
        {
            // 【注意】CanvasGroupが無いと、非表示にする際にGameObject自体をSetActive(false)する。
            // これによりOnDisable()が呼ばれるが、上記の通りOnDisable()では何もしないよう変更済みなので
            // 現在は問題ないはず。ただし、このGameObjectにアタッチされた他のコンポーネントの
            // 動作(Update等)も一緒に止まってしまう点は変わらないため、
            // 可能であればこのGameObjectにCanvasGroupコンポーネントを追加し、
            // こちらのSetActive経路を使わないようにすることを推奨する。
            gameObject.SetActive(visible);
        }
    }
}