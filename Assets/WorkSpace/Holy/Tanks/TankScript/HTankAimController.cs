using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// タンクのHead(カメラ搭載部分の土台)のヨー(左右)回転を担当するスクリプト。
/// Input Systemを使用し、マウスとゲームパッド右スティックの両方でヨー回転を操作できる。
///
/// 役割分担:
/// - TankBody: 移動と車体(Body)自体の回転のみを行う。
/// - このスクリプト(TankHead): ヨー(左右)の回転のみを、マウス/右スティックの入力で
///   独立して制御する。
/// - UDRotater: ピッチ(上下)の回転を担当する。Headの子オブジェクトとして配置し、
///   ローカルX軸回転のみを行う。詳細は UDRotater.cs を参照。
///
/// 階層構成(Bodyの子のままでよい):
///   Body (TankBody)
///    └ Head (TankHead)  ※Bodyの子のままでOK
///        └ UDRotater
///            └ Camera
///
/// 動作仕様(重要):
/// - HeadはBodyの子のままでよい。位置はBodyの子であることで自動的に追従する。
/// - 回転は「ワールド回転を直接指定する」方式で管理する。
///   ・毎フレーム、プレイヤーの入力(マウス/右スティック)によるヨー角度を蓄積する。
///   ・その蓄積したヨー角度を transform.rotation (ワールド回転) に直接設定する。
///   ・親であるBodyが物理演算(Rigidbody)によって回転していても、
///     ワールド回転を直接指定しているため、Bodyの回転量やタイミング
///     (Rigidbodyの補間の有無など)に一切影響されない。
///   ・以前試した「Bodyの回転量を毎フレーム逆算して引く」方式は、
///     Rigidbodyの補間(Interpolation)による滑らかな見た目と、
///     FixedUpdateタイミングでしか更新されない回転量計算がズレてしまい、
///     見た目が跳ねる原因になっていたため、この方式に変更した。
///
/// セットアップ:
/// 1. Headは、届いたモデルの構造通り、Bodyの子のままでよい(切り離す必要はない)。
/// 2. Headの子として「UDRotater」用のGameObjectを配置し、UDRotater.csをアタッチする。
/// 3. カメラ(Camera)はさらにUDRotaterの子として配置する。
/// 4. このスクリプトをHeadのGameObjectにアタッチするだけでよい(Body側の参照は不要)。
/// </summary>
public class TankHead : MonoBehaviour
{
    [Header("Input Action")]
    [Tooltip("マウス移動量に使用するInputAction(Vector2)。TankControls.inputactionsの「MouseLook」を割り当てる。")]
    [SerializeField]
    private InputActionReference mouseLookActionReference;

    [Tooltip("ゲームパッド右スティックに使用するInputAction(Vector2)。TankControls.inputactionsの「StickLook」を割り当てる。")]
    [SerializeField]
    private InputActionReference stickLookActionReference;

    [Header("ヨー回転設定")]
    [Tooltip("マウスでのヨー回転感度")]
    [SerializeField]
    private float mouseSensitivity = 0.2f;

    [Tooltip("右スティックでのヨー回転感度(度/秒)")]
    [SerializeField]
    private float gamepadSensitivity = 180f;

    [Tooltip("この大きさ未満の右スティック入力は無視する(遊び・誤差吸収用)")]
    [SerializeField]
    private float stickDeadzone = 0.1f;

    [Header("不感帯設定(斜め入力時の軸スナップ)")]
    [Tooltip("スティックの左右(X)成分がこの値以内の場合、左右入力を無視して上下(ピッチ)のみの動作にする。" +
             "仕様書の既定は「ナシ」(0)。まずは0で試し、必要になった場合のみ値を入れる。")]
    [SerializeField]
    private float horizontalDeadzoneBand = 0f;

    // ワールド基準のヨー角度(Bodyの回転とは無関係に、プレイヤー操作のみで変化する)
    private float worldYaw;

    private void Awake()
    {
        // 初期回転をworldYawに反映しておく(Headに最初から向きが付いている場合のズレ防止)
        worldYaw = transform.eulerAngles.y;
    }

    private void OnEnable()
    {
        if (mouseLookActionReference != null)
        {
            mouseLookActionReference.action.Enable();
        }

        if (stickLookActionReference != null)
        {
            stickLookActionReference.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (mouseLookActionReference != null)
        {
            mouseLookActionReference.action.Disable();
        }

        if (stickLookActionReference != null)
        {
            stickLookActionReference.action.Disable();
        }
    }

    private void Update()
    {
        // マウス入力(1フレームあたりの移動量なので、そのまま感度を掛けて使用)
        Vector2 mouseDelta = mouseLookActionReference != null
            ? mouseLookActionReference.action.ReadValue<Vector2>()
            : Vector2.zero;

        // 右スティック入力(-1〜1の傾き具合なので、度/秒として時間を掛けて使用)
        Vector2 stickInput = stickLookActionReference != null
            ? stickLookActionReference.action.ReadValue<Vector2>()
            : Vector2.zero;

        if (stickInput.sqrMagnitude < stickDeadzone * stickDeadzone)
        {
            stickInput = Vector2.zero;
        }

        // 不感帯(軸スナップ): 左右(X)成分が「左右幅」以内なら、
        // 斜め入力とみなさず上下(ピッチ)のみの動作にする(左右を無視する)
        if (Mathf.Abs(stickInput.x) <= horizontalDeadzoneBand)
        {
            stickInput.x = 0f;
        }

        // プレイヤー操作分だけをworldYawに加算する(Bodyの回転は一切関与しない)
        worldYaw += mouseDelta.x * mouseSensitivity;
        worldYaw += stickInput.x * gamepadSensitivity * Time.deltaTime;
    }

    private void LateUpdate()
    {
        // ワールド回転を直接指定する。Bodyの子であっても、
        // transform.rotation(ワールド)を直接設定すればBodyの回転の影響を受けない。
        // LateUpdateで行うことで、Bodyの物理演算(補間含む)が全て確定した後に
        // 上書きすることになり、タイミングのズレによる「跳ね」を防げる。
        transform.rotation = Quaternion.Euler(0f, worldYaw, 0f);
    }
}