using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// タンクのBody(車体)の移動・回転を担当するスクリプト。
/// Input Systemを使用し、キーボード(WASD)とゲームパッド(左スティック)の両方に対応。
///
/// 役割分担:
/// - このスクリプト(TankBody): 移動と車体(Body)自体の回転のみを行う。カメラには関与しない。
/// - TankHead: Bodyの子のまま配置し、ヨー(左右)回転を担当する。ワールド回転を直接指定する
///   ことでBodyの回転(Rigidbodyの補間タイミングも含む)に一切影響されない。
///   詳細は TankHead.cs を参照。
///
/// 動作仕様:
/// - 移動方向は「カメラ絶対」で計算する。つまりスティック/WASDの上入力 = カメラ(Head)が
///   向いている方向へ進む。カメラの向き(水平面のみ、ピッチは無視)を基準に、
///   前後・左右の入力を合成して目標方向を決める。
/// - 決まった目標方向へタンク(Body)の正面を回転させる。
/// - 斜め入力など、現在の向きと目標方向の角度差が大きい場合は
///   まずその場で回転し、正面が目標方向に十分近づいてから前進する。
/// - 角度差が小さい場合(ほぼ正面を向いている)は、微調整の回転をしつつ前進する。
/// - つまり「進みたい方向を向いてから進む」タンクらしい動きになる。
///
/// 事前準備:
/// 1. Edit > Project Settings > Player > Active Input Handling を
///    "Input System Package (New)" または "Both" に設定してください。
/// 2. Package Manager で "Input System" パッケージがインストールされていることを確認してください。
/// 3. このスクリプトをタンクのBody(車体)のGameObjectにアタッチしてください(Rigidbodyが自動で追加されます)。
/// 4. Inspector上の「Lock Height And Tilt」はデフォルトでオン(推奨)。オンの場合、起動時に自動で
///    重力オフ・Y座標固定・X/Z軸回転(転倒)固定を行うため、Rigidbodyの Constraints を手動で
///    いじる必要はない。地形に高低差がある場合のみオフにして、重力・Colliderで挙動を作る。
/// 5. Inspector上の「Camera Transform」に、TankHeadをアタッチしたHead(カメラ)のTransformを
///    ドラッグ&ドロップで登録してください。未設定の場合はワールド座標基準で動作します。
/// 6. Inspector上の「Move Action Reference」に、TankControls.inputactions内の
///    「Tank/Move」アクションを登録してください。バインド(キー割り当て)を変更したい場合は、
///    このスクリプトを触らず、TankControls.inputactionsをUnityで開いて編集してください。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class TankBody : MonoBehaviour
{
    [Header("Input Action")]
    [Tooltip("移動入力に使用するInputAction(Vector2)。TankControls.inputactionsの「Move」を割り当てる。" +
             "Unity上でバインドを変更すれば、コードを変更せず操作方法を変えられる。")]
    [SerializeField]
    private InputActionReference moveActionReference;

    [Header("カメラ基準設定")]
    [Tooltip("移動方向の基準にするカメラ(Head)のTransform。" +
             "スティック/WASD上入力でこのTransformが向いている方向へ進むようになる。" +
             "未設定の場合はワールド座標(常に同じ方向)基準で動作する。")]
    [SerializeField]
    private Transform cameraTransform;

    [Header("移動設定")]
    [Tooltip("前進の速度 (m/s)")]
    [SerializeField]
    private float moveSpeed = 5f;

    [Header("回転設定")]
    [Tooltip("入力方向へ向くまでの旋回速度 (度/秒)")]
    [SerializeField]
    private float rotateSpeed = 180f;

    [Tooltip("この角度(度)以内まで入力方向を向いたら前進を開始する。" +
             "値を大きくすると多少ズレていても前進しやすくなる。")]
    [SerializeField]
    private float moveAngleThreshold = 5f;

    [Tooltip("この大きさ未満のスティック入力は無視する(遊び・誤差吸収用)")]
    [SerializeField]
    private float inputDeadzone = 0.1f;

    [Header("物理設定")]
    [Tooltip("チェックすると、起動時に自動で「重力(Use Gravity)をオフ」「Y座標(高さ)を固定」" +
             "「X軸・Z軸の回転(転倒)を固定」に設定する。地形が平坦な戦車ゲームでは、これをオンにしておくと" +
             "Colliderのすり抜け等による落下トラブルを避けられるため推奨。地形に高低差があり、" +
             "重力で自然に地面に沿わせたい場合のみオフにする。")]
    [SerializeField]
    private bool lockHeightAndTilt = true;

    [Header("エクセル連携")]
    [Tooltip("CSV(Excel)からインポートした調整値を反映するためのTankTuningConfig。" +
             "設定すると、起動時にこのアセットの値で上記のパラメーターが上書きされる。未設定ならこのInspectorの値をそのまま使用する。")]
    [SerializeField]
    private TankTuningConfig tuningConfig;

    private Rigidbody rb;
    private Vector2 moveInput;

    // 最後に入力された方向を保持(入力が無い間は向きを維持するため)
    private Vector3 targetDirection;

    /// <summary>
    /// 移動方向の基準にするカメラ(Head)のTransformを外部から設定する。
    /// モデルの階層構成が実行時に組み替わる場合(TankRigSetupなど)に使用する。
    /// </summary>
    public void SetCameraTransform(Transform camera)
    {
        cameraTransform = camera;
    }

    private void Awake()
    {
        ApplyTuningConfig();

        rb = GetComponent<Rigidbody>();

        // MovePosition/MoveRotationはFixedUpdateのタイミングでしか座標を更新しないため、
        // 補間(Interpolate)を有効にしないと描画フレームレートとのズレでカクつき(跳ね)が発生する。
        // Bodyの子であるHeadなどもこの影響を受けるため、ここで自動設定しておく。
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (lockHeightAndTilt)
        {
            // 重力を切り、Y座標(高さ)・X軸回転・Z軸回転(=転倒)を固定する。
            // このスクリプトは移動も回転もMovePosition/MoveRotationで完全にスクリプト制御しているため、
            // 本来Unityの物理的な重力に頼る必要がない。重力を有効にしたまま
            // MovePosition(非Kinematic Rigidbody)で毎フレーム位置を上書きする組み合わせは、
            // Collider・地形の設定次第で沈み込み/すり抜けが起きやすいため、
            // 平坦な地形を前提に「高さを固定してしまう」ことで根本的に回避する。
            rb.useGravity = false;
            rb.constraints |= RigidbodyConstraints.FreezePositionY
                             | RigidbodyConstraints.FreezeRotationX
                             | RigidbodyConstraints.FreezeRotationZ;
        }

        targetDirection = transform.forward;
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

        moveSpeed = tuningConfig.body_MoveSpeed;
        rotateSpeed = tuningConfig.body_RotateSpeed;
        moveAngleThreshold = tuningConfig.body_MoveAngleThreshold;
        inputDeadzone = tuningConfig.body_InputDeadzone;
    }

    private void OnEnable()
    {
        if (moveActionReference != null)
        {
            moveActionReference.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (moveActionReference != null)
        {
            moveActionReference.action.Disable();
        }
    }

    private void Update()
    {
        // 入力値を毎フレーム取得 (x: 左右, y: 前後)
        moveInput = moveActionReference != null
            ? moveActionReference.action.ReadValue<Vector2>()
            : Vector2.zero;
    }

    private void FixedUpdate()
    {
        bool hasInput = moveInput.sqrMagnitude > (inputDeadzone * inputDeadzone);

        if (hasInput)
        {
            if (cameraTransform != null)
            {
                // カメラの向きを基準に方向を計算する(カメラ絶対 = スティック上でカメラの向いている方向へ進む)
                // カメラのforward/rightを水平面(XZ平面)に投影し、傾き(ピッチ)の影響を除去する
                Vector3 camForward = cameraTransform.forward;
                camForward.y = 0f;
                camForward.Normalize();

                Vector3 camRight = cameraTransform.right;
                camRight.y = 0f;
                camRight.Normalize();

                targetDirection = (camForward * moveInput.y + camRight * moveInput.x).normalized;
            }
            else
            {
                // カメラ未設定時は、入力(x, y)をそのままワールド(XZ平面)上の方向ベクトルとして使用
                targetDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
            }
        }

        // 目標方向をY軸角度(度)に変換
        float targetAngle = Mathf.Atan2(targetDirection.x, targetDirection.z) * Mathf.Rad2Deg;
        float currentAngle = transform.eulerAngles.y;

        // 現在角度と目標角度の差分(-180〜180度に正規化 = 絶対値が小さい方の回転方向)
        // 例: 現在10度、目標350度の場合、単純な引き算だと-340度だが、
        //     DeltaAngleは+20度(=350度側へ少し回る方が近い)を返す。
        float deltaAngle = Mathf.DeltaAngle(currentAngle, targetAngle);

        // 絶対値が小さい方向(deltaAngleの符号)へ、rotateSpeedを超えない範囲で回転
        float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, rotateSpeed * Time.fixedDeltaTime);
        rb.MoveRotation(Quaternion.Euler(0f, newAngle, 0f));

        // 現在の向きと目標方向との角度差(絶対値)
        float angleDifference = Mathf.Abs(deltaAngle);

        // 入力があり、かつ十分に目標方向を向いている場合のみ前進する
        if (hasInput && angleDifference <= moveAngleThreshold)
        {
            float inputMagnitude = Mathf.Clamp01(moveInput.magnitude);
            Vector3 forwardMove = transform.forward * inputMagnitude * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + forwardMove);
        }
    }
}