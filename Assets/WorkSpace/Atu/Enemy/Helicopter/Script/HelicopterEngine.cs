using UnityEngine;

public class FlyingTankController : MonoBehaviour
{
    [Header("ステータスデータ設定")]
    [SerializeField] private FlyingTankData statsData;

    [Header("パーツ参照")]
    [SerializeField] private Transform bodyTransform;          // 車体 (Body)
    [SerializeField] private Transform turretTransform;        // 砲塔 (Turret)
    [SerializeField] private Transform gunBarrelTransform;     // 砲身 (上下Turret)
    [SerializeField] private Transform mainRotor;             // メインローター
    [SerializeField] private Transform tailRotor;             // テールローター

    [Header("ローターエンジン設定")]
    [SerializeField] private float maxRotorSpeed = 1500f;     // 最高速度
    [SerializeField] private float acceleration = 300f;      // 加速度
    [SerializeField] private float deceleration = 150f;      // 減速度
    [SerializeField] private float tailRotorMultiplier = 1.2f;// テールローター速度倍率

    private float currentRotorSpeed = 0f;
    private bool isEngineOn = true;
    private float lastAttackTime = 0f;
    private int currentHp;

    private void Start()
    {
        if (statsData != null)
        {
            currentHp = statsData.maxHp;
        }
    }

    private void Update()
    {
        // 1. ローター回転（エンジン状態に応じて可変）
        HandleRotorEngine();

        if (statsData == null) return;

        // 2. 飛行高度維持（目標高度 6m へ補間移動）
        HandleFlightAltitude();

        // 3. 移動・車体旋回処理（移動可フラグのチェック）
        if (statsData.canMove)
        {
            HandleMovementAndRotation();
        }
    }

    /// <summary>
    /// ローターの加速・減速および回転処理
    /// </summary>
    private void HandleRotorEngine()
    {
        float targetSpeed = isEngineOn ? maxRotorSpeed : 0f;
        float rate = isEngineOn ? acceleration : deceleration;
        currentRotorSpeed = Mathf.MoveTowards(currentRotorSpeed, targetSpeed, rate * Time.deltaTime);

        if (mainRotor != null)
        {
            mainRotor.Rotate(Vector3.up * currentRotorSpeed * Time.deltaTime, Space.Self);
        }

        if (tailRotor != null)
        {
            tailRotor.Rotate(Vector3.forward * (currentRotorSpeed * tailRotorMultiplier) * Time.deltaTime, Space.Self);
        }
    }

    /// <summary>
    /// 設定された飛行高度（飛行高度 6m）を維持する処理
    /// </summary>
    private void HandleFlightAltitude()
    {
        Vector3 currentPos = transform.position;
        float targetY = statsData.flightAltitude;

        // スムーズに目標高度に上昇・維持
        currentPos.y = Mathf.Lerp(currentPos.y, targetY, Time.deltaTime * 2.0f);
        transform.position = currentPos;
    }

    /// <summary>
    /// 入力に応じた移動（5m/s）およびBody回転（180度/s）
    /// </summary>
    private void HandleMovementAndRotation()
    {
        float moveInput = Input.GetAxis("Vertical");    // W/S キー
        float turnInput = Input.GetAxis("Horizontal");  // A/D キー

        // 車体の回転（Body回転速度: 180度/sec）
        if (bodyTransform != null && turnInput != 0f)
        {
            bodyTransform.Rotate(Vector3.up * turnInput * statsData.bodyRotationSpeed * Time.deltaTime, Space.Self);
        }

        // 車体の正面方向へ移動（移動スピード: 5m/sec）
        Transform moveReference = bodyTransform != null ? bodyTransform : transform;
        Vector3 moveDirection = moveReference.forward * moveInput;
        transform.position += moveDirection * statsData.moveSpeed * Time.deltaTime;
    }

    /// <summary>
    /// 砲塔（Turret 60度/s）と砲身（上下Turret 60度/s）の旋回制御
    /// </summary>
    /// <param name="yawInput">左右旋回入力 (-1 ~ 1)</param>
    /// <param name="pitchInput">上下傾けての調整入力 (-1 ~ 1)</param>
    public void AimTurret(float yawInput, float pitchInput)
    {
        if (statsData == null) return;

        // Turret水平回転 (60度/sec)
        if (turretTransform != null && yawInput != 0f)
        {
            turretTransform.Rotate(Vector3.up * yawInput * statsData.turretRotationSpeed * Time.deltaTime, Space.Self);
        }

        // 上下Turret（砲身俯仰）回転 (60度/sec)
        if (gunBarrelTransform != null && pitchInput != 0f)
        {
            gunBarrelTransform.Rotate(Vector3.right * pitchInput * statsData.pitchTurretRotationSpeed * Time.deltaTime, Space.Self);
        }
    }

    /// <summary>
    /// 砲撃判定（クールダウン 0.2sec）
    /// </summary>
    public void TryShoot()
    {
        if (statsData == null) return;

        if (Time.time >= lastAttackTime + statsData.attackCooldown)
        {
            lastAttackTime = Time.time;
            ExecuteShoot();
        }
    }

    private void ExecuteShoot()
    {
        // 攻撃力 (statsData.attackPower) を考慮した弾の生成などの砲撃処理
    }

    public void ToggleEngine() => isEngineOn = !isEngineOn;
    public void SetEngine(bool isOn) => isEngineOn = isOn;
}