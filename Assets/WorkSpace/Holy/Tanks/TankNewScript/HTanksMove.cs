using UnityEngine;
using UnityEngine.InputSystem;

public class HTanksMove : MonoBehaviour 
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


    private void Awake()
    {
        ApplyTuningConfig();

        rb = GetComponent<Rigidbody>();
    }

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

    private void Update()
    {
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
