using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class TankMovement : MonoBehaviour
{
    [Header("入力設定 (Input System)")]
    public InputAction moveAction = new InputAction("Move");
    public InputAction aimAction = new InputAction("Aim");
    public InputAction fireAction = new InputAction("Fire");

    [Header("入力設定Keyborad (Input System)")]
    private InputActionReference moveActionKeyboard;
    private InputActionReference aimActionKeyboard;

    [Header("移動・回転設定 (Lスティック)")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 360f;
    public float deadZone = 0.3f;

    [Header("砲台・カメラ設定 (Rスティック)")]
    public float aimHorizontalSpeed = 120f;
    public float aimVerticalSpeed = 90f;
    public float maxElevation = 30f;
    public float minElevation = -10f;
    public float autoResetSpeed = 5f;

    [Header("カメラのオフセット位置")]
    public Vector3 cameraOffset = new Vector3(0f, 3f, -8f);

    [Header("参照オブジェクト (同階層)")]
    public Transform tankBody;
    public Transform tankTurret;
    public Transform mainCamera;

    [Header("エイム・砲身パーツ (同階層)")]
    public Transform udRotator;
    public Transform[] barrelParts;
    public Transform firePosition;

    [Header("発射する球")]
    public GameObject Bullet;

    [Header("弾の飛行設定")]
    [SerializeField]
    private float bulletFlightSpeed = 20f;

    [SerializeField]
    private float bulletStraightDistance = 30f;

    [Header("坂道判定")]
    public LayerMask groundLayer = -1;
    public float rayLength = 2.0f;
    public float rayOffsetHeight = 1.0f;

    private Rigidbody rb;
    private Vector3 currentTurretForward;
    private float currentPitch = 0f;


    [Header("着弾レティクル")]
    [SerializeField]
    private RectTransform landingReticle;

    [SerializeField]
    private Canvas reticleCanvas;


    [Header("落下開始地点マーカー")]
    [SerializeField]
    private Transform fallPointMarker;


    [Header("落下地点の予測")]
    [SerializeField]
    private float predictionMaxTime = 5f;

    [SerializeField]
    private float predictionStep = 0.05f;

    [SerializeField]
    private LayerMask predictionLayerMask = -1;

    [SerializeField]
    private HHelth health;

    [SerializeField]
    private float fireCooldown = 0.3f;

    private float nextFireTime = 0f;

    private Camera aimCamera;

    // レイが当たったワールド座標
    public Vector3 landingWorldPosition;

    // 変換後のスクリーン座標
    public Vector3 landingScreenPosition;

    // 有効な着弾地点が見つかったか
    public bool hasLandingPoint;


    [Header("値チェック")]
    public  Vector2 Val1= Vector2.zero;


    private void Awake()
    {
        moveAction.AddBinding("<Gamepad>/leftStick");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        aimAction.AddBinding("<Gamepad>/rightStick");
        aimAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");

        fireAction.AddBinding("<Gamepad>/leftShoulder");
        fireAction.AddBinding("<Keyboard>/r");

        if (health == null)
        {
            health = GetComponent<HHelth>();
        }

        // ステージにHHelthが付いていない場合でも被弾できるように追加する
        if (health == null)
        {
            health = gameObject.AddComponent<HHelth>();
        }
    }

    private void OnEnable()
    {
        moveAction.Enable();
        aimAction.Enable();
        fireAction.Enable();
        fireAction.performed += OnFirePerformd;

        if (health != null)
        {
            health.OnDie += Die;
        }
    }

    private void OnDisable()
    {
        moveAction.Disable();
        aimAction.Disable();
        fireAction.performed -= OnFirePerformd;
        fireAction.Disable();

        if (health != null)
        {
            health.OnDie -= Die;
        }
    }

    private void Die()
    {
        Debug.Log("Playerが死亡した！");

        // ステージのゲームマネージャーにゲームオーバーを通知
        SInGameManagerBase gameManager = FindFirstObjectByType<SInGameManagerBase>();
        if (gameManager != null)
        {
            gameManager.OnGameOver();
        }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (mainCamera == null && Camera.main != null)
        {
            mainCamera = Camera.main.transform;
        }

        if (mainCamera != null)
        {
            aimCamera = mainCamera.GetComponent<Camera>();
        }

        LoadTankParameters();

        rb.constraints = RigidbodyConstraints.FreezeRotation;

        if (mainCamera == null)
        {
            mainCamera = Camera.main.transform;
        }

        // 初期方向の取得（安全対策として値がゼロにならないよう保証）
        if (tankTurret != null)
        {
            Vector3 projected = Vector3.ProjectOnPlane(tankTurret.forward, Vector3.up);
            if (projected.sqrMagnitude > 0.01f)
                currentTurretForward = -projected.normalized; // 180度反転モデルなのでマイナス
            else
                currentTurretForward = transform.forward;
        }
        else
        {
            currentTurretForward = transform.forward;
        }
    }

    void Update()
    {
        // ============================================
        // Rスティック処理 (エイム計算)
        // ============================================
        Vector2 rightInput = aimAction.ReadValue<Vector2>();
        Val1 = rightInput;
        if (rightInput.magnitude >= deadZone)
        {
            // 左右：論理的な「向いている方向」を回転
            Quaternion yawRot = Quaternion.AngleAxis(rightInput.x * aimHorizontalSpeed * Time.deltaTime, Vector3.up);
            currentTurretForward = yawRot * currentTurretForward;

            // 上下：ピッチ角度の更新

            //自動ピッチ角度更新
            //currentPitch -= rightInput.y * aimVerticalSpeed * Time.deltaTime;
            //currentPitch = Mathf.Clamp(currentPitch, minElevation, maxElevation);

            // 各角度止まる
            float radiusChange = 0.5f;

            if (Mathf.Abs(rightInput.y) <= deadZone)
            {
                currentPitch = 0.0f;
            }
            else
            {
                float pitchInput = Mathf.Sign(rightInput.y) *
                    (Mathf.Abs(rightInput.y) - deadZone) / (1.0f - deadZone);

                if (pitchInput <= radiusChange)
                {
                    currentPitch = maxElevation * pitchInput;
                }
                else
                {
                    currentPitch = maxElevation * (radiusChange + (pitchInput - radiusChange) * 0.5f);
                }
            }
        }

        else
        {
            // ニュートラル時に水平へ戻る
            currentPitch = Mathf.Lerp(currentPitch, 0f, autoResetSpeed * Time.deltaTime);
        }
        UpdateLandingPoint();
    }

    void FixedUpdate()
    {
        // ============================================
        // Lスティック処理 (移動・車体回転)
        // ============================================
        Vector2 leftInput = moveAction.ReadValue<Vector2>();
        bool isMoving = leftInput.magnitude >= deadZone;

        // ブレーキ処理（移動していない時はXとZをロック）
        if (!isMoving)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
        }
        else
        {
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }


        // ============================================
        // 坂道判定 (停止中も砲台の傾きのために常に計算する)
        // ============================================
        Vector3 groundNormal = Vector3.up;
        Vector3 rayStart = transform.position + Vector3.up * rayOffsetHeight;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, rayLength, groundLayer))
        {
            groundNormal = hit.normal;
        }

        //坂道に入ったら重力を無効化
        float dot = Vector3.Dot(hit.normal, Vector3.up);
        if (dot > 0.99f || dot <= 0)
        {
            rb.useGravity = true;
        }
        else
        {
            rb.useGravity = false;
        }

        Quaternion tiltRotation = Quaternion.FromToRotation(Vector3.up, groundNormal);
        Quaternion offsetRot = Quaternion.Euler(0, 180, 0); // モデルの180度反転オフセット

        // ============================================
        // 移動と車体の回転 (Lスティック入力時のみ実行)
        // ============================================
        if (isMoving)
        {
            Vector2 moveInput = leftInput.normalized;
            Vector3 camForward = mainCamera.forward;
            Vector3 camRight = mainCamera.right;

            camForward.y = 0;
            camRight.y = 0;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 baseMoveDir = camForward * moveInput.y + camRight * moveInput.x;

            // 車体の回転
            if (baseMoveDir != Vector3.zero && tankBody != null)
            {
                Quaternion bodyYaw = Quaternion.LookRotation(baseMoveDir, Vector3.up);
                Quaternion bodyTargetRot = tiltRotation * bodyYaw * offsetRot;

                tankBody.rotation = Quaternion.RotateTowards(
                    tankBody.rotation,
                    bodyTargetRot,
                    rotationSpeed * Time.fixedDeltaTime
                );

                float angle = Quaternion.Angle(
                    tankBody.rotation,
                    bodyTargetRot
                );

                if (angle < 1.0f)
                {
                    Vector3 moveDir = Vector3.ProjectOnPlane(
                        baseMoveDir,
                        groundNormal
                    ).normalized;

                    Vector3 nextPosition =
                        rb.position + moveDir * moveSpeed * Time.fixedDeltaTime;

                    rb.MovePosition(nextPosition);
                }
            }
        }

        // ============================================
        // 砲台・砲身の回転 (停止中も含め、毎フレーム常に実行)
        // ============================================

        // ★砲台（Turret）の回転
        Quaternion logicalYawRot = Quaternion.LookRotation(currentTurretForward, Vector3.up);
        Quaternion turretBaseRot = tiltRotation * logicalYawRot;

        if (tankTurret != null)
        {
            tankTurret.rotation = turretBaseRot * offsetRot;
        }

        // ★UDRotator と 砲身パーツの回転 UDRotatorの回転はModelの問題であっていないかも
        if (udRotator != null)
        {
            Quaternion udTargetRot = turretBaseRot * Quaternion.Euler(currentPitch, 0, 0) * offsetRot;
            udRotator.rotation = udTargetRot;

            if (barrelParts != null)
            {
                foreach (var part in barrelParts)
                {
                    if (part != null)
                    {
                        part.rotation = udTargetRot;
                    }
                }
            }
        }

        // ============================================
        // カメラの追従処理
        // ============================================
        if (mainCamera != null && tankTurret != null)
        {
            // ★坂道の傾き（tiltRotation）を一切使わず、Y軸（Yaw）と ピッチ（上下）のみで計算
            Quaternion camBaseYaw = Quaternion.LookRotation(currentTurretForward, Vector3.up);
            Quaternion camTargetRot = camBaseYaw * Quaternion.Euler(currentPitch, 0, 0);

            mainCamera.rotation = camTargetRot;
            mainCamera.position = tankTurret.position + camTargetRot * cameraOffset;
        }
    }

    void LateUpdate()
    {

    }

    void OnFirePerformd(InputAction.CallbackContext context)
    {
        if (Time.time < nextFireTime)
        {
            return;
        }

        nextFireTime = Time.time + fireCooldown;

        float spawnDistance = 1.0f;

        Vector3 bulletPosition =
            firePosition.position + firePosition.forward * spawnDistance;

        GameObject bullet = Instantiate(
            Bullet,
            bulletPosition,
            firePosition.rotation
        );

        TankProjectile projectile = bullet.GetComponent<TankProjectile>();

        if (projectile != null)
        {
            Vector3 aimPoint =
                bulletPosition + firePosition.forward * 50f;

            projectile.Launch(
                aimPoint,
                bulletStraightDistance,
                bulletFlightSpeed,
                transform
            );
        }
        else
        {
            Debug.LogError("BulletにTankProjectileがありません！");
        }
    }
    private void UpdateLandingPoint()
    {
        hasLandingPoint = false;

        if (landingReticle != null)
        {
            landingReticle.gameObject.SetActive(false);
        }

        if (firePosition == null ||
            aimCamera == null ||
            bulletFlightSpeed <= 0f ||
            bulletStraightDistance < 0f ||
            predictionStep <= 0f)
        {
            return;
        }

        // 弾の発射位置と初速
        float spawnDistance = 1.0f;

        Vector3 startPosition =
            firePosition.position
            + firePosition.forward * spawnDistance;

        Vector3 velocity =
            firePosition.forward * bulletFlightSpeed;

        // 直進終了までの時間と位置
        float straightTime =
            bulletStraightDistance / bulletFlightSpeed;

        Vector3 straightEndPosition =
            startPosition + velocity * straightTime;

        // 直進中に壁や坂などへ衝突するか調べる
        float elapsedTime = 0f;
        Vector3 previousPosition = startPosition;

        while (elapsedTime < straightTime)
        {
            float nextTime = Mathf.Min(
                elapsedTime + predictionStep,
                straightTime
            );

            Vector3 currentPosition =
                startPosition + velocity * nextTime;

            Vector3 segment =
                currentPosition - previousPosition;

            float segmentLength = segment.magnitude;

            if (segmentLength > 0f)
            {
                RaycastHit[] hits = Physics.RaycastAll(
                    previousPosition,
                    segment.normalized,
                    segmentLength,
                    predictionLayerMask,
                    QueryTriggerInteraction.Ignore
                );

                System.Array.Sort(
                    hits,
                    (a, b) => a.distance.CompareTo(b.distance)
                );

                foreach (RaycastHit hit in hits)
                {
                    // 自分の戦車は無視
                    if (hit.transform == transform ||
                        hit.transform.IsChildOf(transform))
                    {
                        continue;
                    }

                    // 衝突地点をレティクルに表示
                    SetLandingReticle(hit.point);
                    return;
                }
            }

            previousPosition = currentPosition;
            elapsedTime = nextTime;
        }

        // 直進終了地点に障害物がない場合、
        // 空中の落下開始地点をレティクルに表示
        SetLandingReticle(straightEndPosition);
    }

    private void SetLandingReticle(Vector3 worldPosition)
    {
        landingWorldPosition = worldPosition;

        landingScreenPosition =
            aimCamera.WorldToScreenPoint(worldPosition);

        hasLandingPoint = landingScreenPosition.z > 0f;

        if (landingReticle != null && hasLandingPoint)
        {
            landingReticle.gameObject.SetActive(true);
            landingReticle.position = landingScreenPosition;
        }
    }



    private void LoadTankParameters()
    {
        TankParameterManager manager = TankParameterManager.Instance;

        if (manager == null)
        {
            Debug.LogError("TankParameterManager が見つかりません！");
            return;
        }

        moveSpeed = manager.GetFloat("moveSpeed", moveSpeed);
        rotationSpeed = manager.GetFloat("rotationSpeed", rotationSpeed);
        deadZone = manager.GetFloat("deadZone", deadZone);

        aimHorizontalSpeed = manager.GetFloat(
            "aimHorizontalSpeed", aimHorizontalSpeed);

        aimVerticalSpeed = manager.GetFloat(
            "aimVerticalSpeed", aimVerticalSpeed);

        autoResetSpeed = manager.GetFloat(
            "autoResetSpeed", autoResetSpeed);

        maxElevation = manager.GetFloat("maxElevation", maxElevation);
        minElevation = manager.GetFloat("minElevation", minElevation);

        bulletFlightSpeed = manager.GetFloat(
            "bulletFlightSpeed", bulletFlightSpeed);

        bulletStraightDistance = manager.GetFloat(
            "bulletStraightDistance", bulletStraightDistance);

        cameraOffset = new Vector3(
            0f,
            manager.GetFloat("cameraHeight", 1.5f),
            -manager.GetFloat("cameraDistance", 6f)
        );

        Debug.Log($"移動速度: {moveSpeed}");
    }


}
