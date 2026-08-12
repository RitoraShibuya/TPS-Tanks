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
/// 階層構成A(推奨・Bodyの子にできる場合):
///   Body (TankBody)
///    └ Head/Turret (TankHead)  ※Bodyの子のままでOK
///        └ UDRotater
///            └ Camera
///
/// 階層構成B(BodyとHead/Turretが兄弟関係など、親子にできない場合):
///   TS_PCTank
///    ├ Body (TankBody)
///    └ Turret (TankHead)      ※Bodyの子ではない
///        └ UDRotater
///            └ Camera
///   この場合、下記「Body Transform」欄にBodyのTransformを登録すると、
///   このスクリプトが毎フレームBodyの位置(と水平回転)を基準に自分の位置を追従させる
///   (詳細は動作仕様を参照)。
///
/// 動作仕様(重要):
/// - 【階層構成Aの場合】HeadはBodyの子のままでよい。位置はBodyの子であることで自動的に追従する。
///   「Body Transform」欄は空のままでよい。
/// - 【階層構成Bの場合】「Body Transform」にBodyを設定すると、Awake時点での
///   Bodyから見た自分のローカル位置(オフセット)を記憶し、毎フレームそのオフセットを
///   Bodyの現在位置・向きに変換して自分のtransform.positionに適用する。
///   これにより、親子関係が無くても「Bodyに乗っている」ような位置追従を再現する。
/// - 回転は(どちらの階層構成でも共通)「ワールド回転を直接指定する」方式で管理する。
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
/// 1. Bodyの子にできるなら、Headは届いたモデルの構造通り、Bodyの子のままでよい(切り離す必要はない)。
///    Bodyの子にできない場合は、下記「Body Transform」にBodyを設定する。
/// 2. Headの子として「UDRotater」用のGameObjectを配置する。
/// 3. カメラ(Camera)はさらにUDRotaterの子として配置する。
/// 4. このスクリプトをHead(Turret)のGameObjectにアタッチする。
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

    [Header("階層構成が異なる場合の位置追従設定")]
    [Tooltip("BodyがこのGameObjectの親ではない場合(兄弟関係など)に、BodyのTransformをここへ設定する。" +
             "設定すると、Awake時点のBodyから見た相対位置を記憶し、毎フレームBodyの現在位置・水平回転を" +
             "基準に自分の位置を追従させる(擬似的な親子関係)。" +
             "Bodyの子として配置している通常の構成では、この欄は空のままでよい。")]
    [SerializeField]
    private Transform bodyTransform;

    [Header("エクセル連携")]
    [Tooltip("CSV(Excel)からインポートした調整値を反映するためのTankTuningConfig。" +
             "設定すると、起動時にこのアセットの値で上記のパラメーターが上書きされる。未設定ならこのInspectorの値をそのまま使用する。")]
    [SerializeField]
    private TankTuningConfig tuningConfig;

    // ワールド基準のヨー角度(Bodyの回転とは無関係に、プレイヤー操作のみで変化する)
    private float worldYaw;

    // bodyTransform設定時: Awake時点でのBodyから見た自分のローカル位置(擬似親子オフセット)
    private Vector3 localOffsetFromBody;

    private void Awake()
    {
        ApplyTuningConfig();

        // 初期回転をworldYawに反映しておく(Headに最初から向きが付いている場合のズレ防止)
        worldYaw = transform.eulerAngles.y;

        if (bodyTransform != null)
        {
            // Bodyから見た現在の相対位置を記憶しておく(以降、この関係を維持し続ける)
            localOffsetFromBody = bodyTransform.InverseTransformPoint(transform.position);
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

        mouseSensitivity = tuningConfig.head_MouseSensitivity;
        gamepadSensitivity = tuningConfig.head_GamepadSensitivity;
        stickDeadzone = tuningConfig.head_StickDeadzone;
        horizontalDeadzoneBand = tuningConfig.head_HorizontalDeadzoneBand;
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
        // bodyTransformが設定されている場合(Bodyが親でない階層構成)は、
        // Bodyの現在位置・回転を基準に、Awake時点で記憶したオフセット分だけ離れた位置へ
        // 自分の位置を再計算する。TransformPointはBodyの回転も考慮するため、
        // Bodyが旋回しても、擬似的に「Bodyに乗っている」位置関係を維持できる。
        // (LateUpdateで行うことで、Bodyの物理演算=Rigidbody.MovePositionの結果が
        //  確定した後に位置を合わせることになり、1フレームの遅延やズレを防げる)
        if (bodyTransform != null)
        {
            transform.position = bodyTransform.TransformPoint(localOffsetFromBody);
        }

        // ワールド回転を直接指定する。Bodyの子であっても、
        // transform.rotation(ワールド)を直接設定すればBodyの回転の影響を受けない。
        // LateUpdateで行うことで、Bodyの物理演算(補間含む)が全て確定した後に
        // 上書きすることになり、タイミングのズレによる「跳ね」を防げる。
        transform.rotation = Quaternion.Euler(0f, worldYaw, 0f);
    }
}