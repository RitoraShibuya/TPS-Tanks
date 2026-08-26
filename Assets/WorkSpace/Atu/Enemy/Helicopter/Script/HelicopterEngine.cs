using UnityEngine;

public class FlyingTankController : MonoBehaviour
{
    // =========================================================
    // データ
    // =========================================================

    [Header("ステータスデータ")]
    [SerializeField] private FlyingTankData statsData;


    // =========================================================
    // パーツ参照
    // =========================================================

    [Header("パーツ参照")]
    [SerializeField] private Transform bodyTransform;
    [SerializeField] private Transform turretTransform;
    [SerializeField] private Transform gunBarrelTransform;
    [SerializeField] private Transform mainRotor;
    [SerializeField] private Transform tailRotor;


    // =========================================================
    // ローター設定
    // =========================================================

    [Header("ローター設定")]
    [SerializeField] private float maxRotorSpeed = 1500f;
    [SerializeField] private float rotorAcceleration = 300f;
    [SerializeField] private float rotorDeceleration = 150f;
    [SerializeField] private float tailRotorMultiplier = 1.2f;

    private float currentRotorSpeed;
    private bool isEngineOn = true;


    // =========================================================
    // 砲身設定
    // =========================================================

    [Header("砲身設定")]
    [SerializeField] private float minGunPitch = -10f;
    [SerializeField] private float maxGunPitch = 30f;

    private float currentGunPitch;


    // =========================================================
    // 戦闘関連
    // =========================================================

    [Header("戦闘設定")]
    [SerializeField] private Transform muzzleTransform;

    private float lastAttackTime;
    private int currentHp;


    // =========================================================
    // Unity Events
    // =========================================================

    private void Start()
    {
        Initialize();
    }

    private void Update()
    {
        UpdateRotor();

        if (statsData == null)
            return;

        UpdateFlight();

        if (statsData.canMove)
        {
            HandleMovement();
        }
    }


    // =========================================================
    // 初期化
    // =========================================================

    private void Initialize()
    {
        if (statsData == null)
            return;

        currentHp = statsData.maxHp;

        // 現在の砲身角度を初期値として取得
        if (gunBarrelTransform != null)
        {
            currentGunPitch = NormalizeAngle(
                gunBarrelTransform.localEulerAngles.x
            );
        }
    }


    // =========================================================
    // ローター
    // =========================================================

    private void UpdateRotor()
    {
        float targetSpeed = isEngineOn ? maxRotorSpeed : 0f;
        float speedChange = isEngineOn
            ? rotorAcceleration
            : rotorDeceleration;

        currentRotorSpeed = Mathf.MoveTowards(
            currentRotorSpeed,
            targetSpeed,
            speedChange * Time.deltaTime
        );

        RotateMainRotor();
        RotateTailRotor();
    }

    private void RotateMainRotor()
    {
        if (mainRotor == null)
            return;

        mainRotor.Rotate(
            Vector3.up * currentRotorSpeed * Time.deltaTime,
            Space.Self
        );
    }

    private void RotateTailRotor()
    {
        if (tailRotor == null)
            return;

        tailRotor.Rotate(
            Vector3.forward *
            currentRotorSpeed *
            tailRotorMultiplier *
            Time.deltaTime,
            Space.Self
        );
    }


    // =========================================================
    // 飛行
    // =========================================================

    private void UpdateFlight()
    {
        MaintainAltitude();
    }

    private void MaintainAltitude()
    {
        Vector3 position = transform.position;

        float targetHeight = statsData.flightAltitude;

        position.y = Mathf.Lerp(
            position.y,
            targetHeight,
            Time.deltaTime * 2f
        );

        transform.position = position;
    }


    // =========================================================
    // 移動
    // =========================================================

    private void HandleMovement()
    {
        float moveInput = Input.GetAxis("Vertical");
        float turnInput = Input.GetAxis("Horizontal");

        HandleBodyRotation(turnInput);
        HandleForwardMovement(moveInput);
    }

    private void HandleBodyRotation(float turnInput)
    {
        if (bodyTransform == null)
            return;

        if (Mathf.Approximately(turnInput, 0f))
            return;

        float rotationAmount =
            turnInput *
            statsData.bodyRotationSpeed *
            Time.deltaTime;

        bodyTransform.Rotate(
            Vector3.up * rotationAmount,
            Space.Self
        );
    }

    private void HandleForwardMovement(float moveInput)
    {
        if (Mathf.Approximately(moveInput, 0f))
            return;

        Transform moveReference =
            bodyTransform != null
                ? bodyTransform
                : transform;

        Vector3 direction =
            moveReference.forward * moveInput;

        transform.position +=
            direction *
            statsData.moveSpeed *
            Time.deltaTime;
    }


    // =========================================================
    // 砲塔照準
    // =========================================================

    /// <summary>
    /// 砲塔の左右・砲身の上下を操作する。
    /// yawInput / pitchInput は -1 ～ 1。
    /// </summary>
    public void AimTurret(float yawInput, float pitchInput)
    {
        if (statsData == null)
            return;

        RotateTurret(yawInput);
        RotateGunBarrel(pitchInput);
    }

    private void RotateTurret(float yawInput)
    {
        if (turretTransform == null)
            return;

        if (Mathf.Approximately(yawInput, 0f))
            return;

        float rotationAmount =
            yawInput *
            statsData.turretRotationSpeed *
            Time.deltaTime;

        turretTransform.Rotate(
            Vector3.up * rotationAmount,
            Space.Self
        );
    }

    private void RotateGunBarrel(float pitchInput)
    {
        if (gunBarrelTransform == null)
            return;

        if (Mathf.Approximately(pitchInput, 0f))
            return;

        currentGunPitch +=
            pitchInput *
            statsData.pitchTurretRotationSpeed *
            Time.deltaTime;

        currentGunPitch = Mathf.Clamp(
            currentGunPitch,
            minGunPitch,
            maxGunPitch
        );

        Vector3 localRotation =
            gunBarrelTransform.localEulerAngles;

        localRotation.x = currentGunPitch;

        gunBarrelTransform.localEulerAngles =
            localRotation;
    }


    // =========================================================
    // 射撃
    // =========================================================

    /// <summary>
    /// 射撃を試みる。
    /// クールダウン中なら発射しない。
    /// </summary>
    public void TryShoot()
    {
        if (statsData == null)
            return;

        if (!CanShoot())
            return;

        lastAttackTime = Time.time;

        ExecuteShoot();
    }

    private bool CanShoot()
    {
        return Time.time >=
               lastAttackTime +
               statsData.attackCooldown;
    }

    private void ExecuteShoot()
    {
        if (muzzleTransform == null)
        {
            Debug.LogWarning(
                "FlyingTankController: muzzleTransform が設定されていません。",
                this
            );

            return;
        }

        // =====================================================
        // ここで弾を生成する
        //
        // 例:
        // GameObject bullet = Instantiate(
        //     bulletPrefab,
        //     muzzleTransform.position,
        //     muzzleTransform.rotation
        // );
        //
        // Bullet bulletComponent =
        //     bullet.GetComponent<Bullet>();
        //
        // bulletComponent.Initialize(
        //     statsData.attackPower
        // );
        // =====================================================

        Debug.Log(
            $"Flying Tank Shoot! Power: {statsData.attackPower}"
        );
    }


    // =========================================================
    // エンジン
    // =========================================================

    public void ToggleEngine()
    {
        isEngineOn = !isEngineOn;
    }

    public void SetEngine(bool isOn)
    {
        isEngineOn = isOn;
    }

    public bool IsEngineOn()
    {
        return isEngineOn;
    }


    // =========================================================
    // HP
    // =========================================================

    public int GetCurrentHp()
    {
        return currentHp;
    }

    public int GetMaxHp()
    {
        return statsData != null
            ? statsData.maxHp
            : 0;
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0)
            return;

        currentHp -= damage;

        currentHp = Mathf.Max(
            currentHp,
            0
        );

        if (currentHp <= 0)
        {
            OnDestroyed();
        }
    }

    private void OnDestroyed()
    {
        Debug.Log("Flying Tank Destroyed!");

        // TODO:
        // 爆発エフェクト
        // 撃破処理
        // リスポーン
        // ゲームオーバー処理など
    }


    // =========================================================
    // Utility
    // =========================================================

    private float NormalizeAngle(float angle)
    {
        if (angle > 180f)
            angle -= 360f;

        return angle;
    }
}
