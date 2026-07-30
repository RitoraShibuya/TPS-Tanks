using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// タンクのHead(カメラ搭載部分)の位置追従・回転を担当するスクリプト。
/// Input Systemを使用し、マウスとゲームパッド右スティックの両方でカメラ(Head)を回転操作できる。
///
/// 役割分担:
/// - TankBody: 移動と車体(Body)自体の回転のみを行う。
/// - このスクリプト(TankHead): Bodyの位置には追従するが、Bodyの回転には一切追従しない。
///   カメラの向き(Head自身の回転)は、マウス/右スティックの入力のみで独立して制御する。
///
/// 動作仕様:
/// - 位置: 毎フレーム、Bodyの位置 + オフセット に追従する。
/// - 回転: マウスの移動量、またはゲームパッドの右スティック入力によって
///   ヨー(左右)・ピッチ(上下)を独立して回転させる。Bodyの回転には一切影響されない。
///
/// セットアップ:
/// 1. Headは、Bodyの子オブジェクトにしない(子にすると回転も自動で追従してしまうため)。
///    独立したGameObjectとして配置すること。
/// 2. カメラ(Camera)はHeadの子として配置する。
/// 3. このスクリプトをHeadのGameObjectにアタッチし、Inspector上の「Body Transform」に
///    Body(車体)のTransformをドラッグ&ドロップで登録する。
/// </summary>
public class TankHead : MonoBehaviour
{
    [Header("追従設定")]
    [Tooltip("位置追従の対象となるBody(車体)のTransform")]
    [SerializeField]
    private Transform bodyTransform;

    [Tooltip("Bodyの位置からHeadをどれだけずらして配置するか(例: 車体からの高さ)")]
    [SerializeField]
    private Vector3 positionOffset = new Vector3(0f, 2f, 0f);

    [Header("回転設定(共通)")]
    [Tooltip("上下(ピッチ)の可動範囲の下限(度)。真下方向に近いほど小さい値。")]
    [SerializeField]
    private float pitchMin = -60f;

    [Tooltip("上下(ピッチ)の可動範囲の上限(度)。真上方向に近いほど大きい値。")]
    [SerializeField]
    private float pitchMax = 60f;

    [Tooltip("チェックすると上下方向の入力を反転する")]
    [SerializeField]
    private bool invertPitch = false;

    [Header("マウス設定")]
    [Tooltip("マウスでの回転感度")]
    [SerializeField]
    private float mouseSensitivity = 0.2f;

    [Header("ゲームパッド設定")]
    [Tooltip("右スティックでの回転感度(度/秒)")]
    [SerializeField]
    private float gamepadSensitivity = 180f;

    [Tooltip("この大きさ未満の右スティック入力は無視する(遊び・誤差吸収用)")]
    [SerializeField]
    private float stickDeadzone = 0.1f;

    private InputAction mouseLookAction;
    private InputAction stickLookAction;

    private float yaw;
    private float pitch;

    private void Awake()
    {
        // 初期回転をyaw/pitchに反映しておく(Headに最初から向きが付いている場合のズレ防止)
        Vector3 startEuler = transform.eulerAngles;
        yaw = startEuler.y;
        pitch = NormalizePitch(startEuler.x);

        // マウス移動量(デルタ)取得用アクション
        mouseLookAction = new InputAction(
            name: "MouseLook",
            type: InputActionType.Value,
            expectedControlType: "Vector2"
        );
        mouseLookAction.AddBinding("<Mouse>/delta");

        // ゲームパッド右スティック取得用アクション
        stickLookAction = new InputAction(
            name: "StickLook",
            type: InputActionType.Value,
            expectedControlType: "Vector2"
        );
        stickLookAction.AddBinding("<Gamepad>/rightStick");
    }

    private void OnEnable()
    {
        mouseLookAction.Enable();
        stickLookAction.Enable();
    }

    private void OnDisable()
    {
        mouseLookAction.Disable();
        stickLookAction.Disable();
    }

    private void Update()
    {
        // マウス入力(1フレームあたりの移動量なので、そのまま感度を掛けて使用)
        Vector2 mouseDelta = mouseLookAction.ReadValue<Vector2>();

        // 右スティック入力(-1〜1の傾き具合なので、度/秒として時間を掛けて使用)
        Vector2 stickInput = stickLookAction.ReadValue<Vector2>();
        if (stickInput.sqrMagnitude < stickDeadzone * stickDeadzone)
        {
            stickInput = Vector2.zero;
        }

        float pitchSign = invertPitch ? 1f : -1f;

        // ヨー(左右)の加算
        yaw += mouseDelta.x * mouseSensitivity;
        yaw += stickInput.x * gamepadSensitivity * Time.deltaTime;

        // ピッチ(上下)の加算
        pitch += mouseDelta.y * mouseSensitivity * pitchSign;
        pitch += stickInput.y * gamepadSensitivity * Time.deltaTime * pitchSign;

        // ピッチのみ可動範囲を制限(ヨーは360度自由に回転可能)
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

        // Bodyの回転には一切影響されず、独立してHeadの回転を適用
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void LateUpdate()
    {
        if (bodyTransform == null)
        {
            return;
        }

        // 位置だけをBodyに追従させる(回転はUpdate側で独立管理しているため触らない)
        transform.position = bodyTransform.position + positionOffset;
    }

    /// <summary>
    /// eulerAngles.x は 0〜360度で返ってくるため、-180〜180度の範囲に変換する。
    /// </summary>
    private float NormalizePitch(float angle)
    {
        if (angle > 180f)
        {
            angle -= 360f;
        }
        return angle;
    }
}