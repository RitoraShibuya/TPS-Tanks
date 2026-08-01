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

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (renderCamera == null)
        {
            renderCamera = Camera.main;
        }
    }

    private void OnEnable()
    {
        if (stickLookActionReference != null)
        {
            stickLookActionReference.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (stickLookActionReference != null)
        {
            stickLookActionReference.action.Disable();
        }
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
            gameObject.SetActive(visible);
        }
    }
}