using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class TankMovement : MonoBehaviour
{
    [Header("入力設定 (Input System)")]
    public InputAction moveAction = new InputAction("Move");
    public InputAction aimAction = new InputAction("Aim");
    public InputAction fireAction = new InputAction("Fire");

    [Header("移動・回転設定 (Lスティック)")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 360f;
    public float deadZone = 0.3f;

    [Header("砲台・カメラ設定 (Rスティック)")]
    public float aimHorizontalSpeed = 120f;
    [Tooltip("※現在は未使用。仰角はスティックの倒し量に応じて直接決まる")]
    public float aimVerticalSpeed = 90f;
    public float maxElevation = 30f;
    public float minElevation = -10f;
    [Tooltip("スティックを離した時に仰角を0度へ戻す速度 (度/秒)")]
    public float autoResetSpeed = 90f;

    [Tooltip("Rスティックを水平からこの角度(度)以内に倒している時は上下入力を無視する (左右回転中に砲身が上がるのを防ぐ)")]
    [Range(0f, 80f)]
    public float pitchSnapAngle = 30f;

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

    [Tooltip("足元からこの距離以内に地面があれば接地とみなす (m)")]
    public float groundCheckDistance = 0.3f;

    [Tooltip("坂に合わせて車体・砲台を傾ける速さ (度/秒)。坂の境目でのガタつきを抑える")]
    public float tiltSmoothSpeed = 120f;

    [Tooltip("入力方向にこの角度(度)以内まで向いたら前進を開始する")]
    public float moveAngleThreshold = 1.0f;

    private Rigidbody rb;
    private Vector3 currentTurretForward;
    private float currentPitch = 0f;

    // 坂道の傾き (FixedUpdate で計算し、LateUpdate で砲台に使う)
    private Quaternion currentTiltRotation = Quaternion.identity;

    // 車体の水平方向の向き (傾きとは分けて管理する)
    private Quaternion currentBodyYaw = Quaternion.identity;

    // モデルの180度反転オフセット
    private static readonly Quaternion offsetRot = Quaternion.Euler(0, 180, 0);

    [Header("着弾レティクル")]
    [SerializeField]
    private RectTransform landingReticle;

    [SerializeField]
    private Canvas reticleCanvas;

    [Header("落下開始地点マーカー")]
    [SerializeField]
    private Transform fallPointMarker;

    //[Header("落下地点の予測")]
    //[SerializeField]
    //private float predictionMaxTime = 5f;

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
    public Vector2 Val1 = Vector2.zero;

    [Header("サウンド (SSoundManager の SE を使用)")]
    [Tooltip("壁などにぶつかった時の衝突音の最短間隔(秒)。連続で鳴りすぎないようにする")]
    [SerializeField]
    private float collisionSoundInterval = 0.3f;

    [Tooltip("接触面の法線の上向き成分がこの値未満なら「壁」とみなして衝突音を鳴らす (地面や坂では鳴らさない)")]
    [SerializeField, Range(0f, 1f)]
    private float wallNormalThreshold = 0.5f;

    // ループSEの再生状態 (SSoundManager の Start/End を必ず対にするため)
    private bool isEngineSoundPlaying = false;
    private bool isTurretSoundPlaying = false;

    // 砲塔が今フレーム旋回したか (Update で判定し、サウンド制御に使う)
    private bool isTurretRotatingThisFrame = false;

    private float nextCollisionSoundTime = 0f;

    private void Awake()
    {
        // Inspector でバインドが設定されていない場合だけ既定のバインドを追加する
        // (Inspector側にも設定があると二重登録になるため)
        if (moveAction.bindings.Count == 0)
        {
            moveAction.AddBinding("<Gamepad>/leftStick");
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
        }

        if (aimAction.bindings.Count == 0)
        {
            aimAction.AddBinding("<Gamepad>/rightStick");
            aimAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
        }

        if (fireAction.bindings.Count == 0)
        {
            fireAction.AddBinding("<Gamepad>/leftShoulder");
            fireAction.AddBinding("<Keyboard>/r");
        }

        if (health == null)
        {
            health = GetComponent<HHelth>();
        }

        // ステージにHHelthが付いていない場合でも被弾できるように追加する
        if (health == null)
        {
            health = gameObject.AddComponent<HHelth>();
        }

        // 被弾・死亡の音はポーズ中やゲーム終了時(TankMovement 無効中)でも鳴らすため、
        // OnEnable/OnDisable ではなく Awake/OnDestroy で購読する
        health.OnDamaged += OnDamagedSound;
        health.OnDie += OnDieSound;
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.OnDamaged -= OnDamagedSound;
            health.OnDie -= OnDieSound;
        }
    }

    private void OnEnable()
    {
        moveAction.Enable();
        aimAction.Enable();
        fireAction.Enable();
        fireAction.performed += OnFirePerformed;
    }

    private void OnDisable()
    {
        moveAction.Disable();
        aimAction.Disable();
        fireAction.performed -= OnFirePerformed;
        fireAction.Disable();

        // ポーズ・開始時の操作ロック・ゲーム終了で無効化されたらループ音を止める
        SetEngineSound(false);
        SetTurretSound(false);
    }

    // ==========================================
    // サウンド
    // ==========================================

    private void SetEngineSound(bool play)
    {
        if (play == isEngineSoundPlaying) return;
        if (SSoundManager.Instance == null) { isEngineSoundPlaying = false; return; }

        if (play) SSoundManager.Instance.PlaySE_TankMoveEngine_Start();
        else SSoundManager.Instance.PlaySE_TankMoveEngine_End();

        isEngineSoundPlaying = play;
    }

    private void SetTurretSound(bool play)
    {
        if (play == isTurretSoundPlaying) return;
        if (SSoundManager.Instance == null) { isTurretSoundPlaying = false; return; }

        if (play) SSoundManager.Instance.PlaySE_TurretRotate_Start();
        else SSoundManager.Instance.PlaySE_TurretRotate_End();

        isTurretSoundPlaying = play;
    }

    /// <summary>
    /// 被弾音（とどめの1発は破壊音だけを鳴らす）
    /// </summary>
    private void OnDamagedSound(int remainingHealth)
    {
        if (remainingHealth <= 0) return;

        if (SSoundManager.Instance != null)
        {
            SSoundManager.Instance.PlaySE_Impact();
        }
    }

    /// <summary>
    /// 破壊音
    /// </summary>
    private void OnDieSound()
    {
        SetEngineSound(false);
        SetTurretSound(false);

        if (SSoundManager.Instance != null)
        {
            SSoundManager.Instance.PlaySE_TankDestroy();
        }
    }

    /// <summary>
    /// 壁や障害物にぶつかった時の衝突音（地面・坂への接地では鳴らさない）
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        if (!isActiveAndEnabled) return;
        if (Time.time < nextCollisionSoundTime) return;
        if (collision.contactCount == 0) return;

        bool hitWall = false;
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y < wallNormalThreshold)
            {
                hitWall = true;
                break;
            }
        }

        if (!hitWall) return;

        nextCollisionSoundTime = Time.time + collisionSoundInterval;

        if (SSoundManager.Instance != null)
        {
            SSoundManager.Instance.PlaySE_Collision();
        }
    }

    // ※プレイヤー死亡時のゲームオーバー通知は SInGameManagerBase が HHelth.OnDie を監視して行う。
    //   (TankMovement はポーズ中などに無効化されるため、ここで購読すると通知を取りこぼす)

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

        // 重力は常にON (坂道では接地中に速度を消して滑りを止める)
        rb.useGravity = true;

        if (mainCamera == null)
        {
            Debug.LogWarning("[TankMovement] カメラが見つかりません。移動はワールド座標基準になります。");
        }

        // 車体の初期の向き (180度反転モデルなのでマイナス)
        Vector3 bodyForward = tankBody != null
            ? Vector3.ProjectOnPlane(-tankBody.forward, Vector3.up)
            : Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        currentBodyYaw = bodyForward.sqrMagnitude > 0.01f
            ? Quaternion.LookRotation(bodyForward.normalized, Vector3.up)
            : Quaternion.identity;

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

        // 旋回音の判定用に、今フレームの変化前の向き・仰角を控えておく
        Vector3 previousTurretForward = currentTurretForward;
        float previousPitch = currentPitch;
        if (rightInput.magnitude >= deadZone)
        {
            // 左右：論理的な「向いている方向」を回転
            Quaternion yawRot = Quaternion.AngleAxis(rightInput.x * aimHorizontalSpeed * Time.deltaTime, Vector3.up);
            currentTurretForward = yawRot * currentTurretForward;

            // 各角度止まる
            float radiusChange = 0.5f;

            // スティックの傾き方向 (水平から何度か)。
            // 左右に倒した時は親指の弧で少し上下にも入ってしまうので、
            // 水平に近い入力では上下を無視して砲身が勝手に上がらないようにする
            float stickAngle = Mathf.Atan2(
                Mathf.Abs(rightInput.y),
                Mathf.Abs(rightInput.x)
            ) * Mathf.Rad2Deg;

            if (Mathf.Abs(rightInput.y) <= deadZone || stickAngle < pitchSnapAngle)
            {
                // 上下の入力なし → なめらかに水平へ戻す
                currentPitch = Mathf.MoveTowards(currentPitch, 0f, autoResetSpeed * Time.deltaTime);
            }
            else
            {
                // 入力の強さ (0〜1) と方向 (上=+ / 下=-)
                float pitchSign = Mathf.Sign(rightInput.y);
                float pitchInput =
                    (Mathf.Abs(rightInput.y) - deadZone) / (1.0f - deadZone);

                float pitchRate;
                if (pitchInput <= radiusChange)
                {
                    pitchRate = pitchInput;
                }
                else
                {
                    pitchRate = radiusChange + (pitchInput - radiusChange) * 0.5f;
                }

                // 上方向は仰角(maxElevation)、下方向は俯角(minElevation)を上限にする
                float pitchLimit = pitchSign > 0f ? maxElevation : -minElevation;
                currentPitch = pitchSign * pitchLimit * pitchRate;
            }
        }
        else
        {
            // ニュートラル時に水平へ戻る (autoResetSpeed = 度/秒)
            currentPitch = Mathf.MoveTowards(currentPitch, 0f, autoResetSpeed * Time.deltaTime);
        }

        currentPitch = Mathf.Clamp(currentPitch, minElevation, maxElevation);

        // 砲塔が左右に回った・砲身が上下した場合だけ旋回音を鳴らす
        isTurretRotatingThisFrame =
            Vector3.Angle(previousTurretForward, currentTurretForward) > 0.01f ||
            Mathf.Abs(currentPitch - previousPitch) > 0.01f;

        SetTurretSound(isTurretRotatingThisFrame);
    }

    void FixedUpdate()
    {
        // ============================================
        // Lスティック処理 (移動・車体回転)
        // ============================================
        Vector2 leftInput = moveAction.ReadValue<Vector2>();
        bool isMoving = leftInput.magnitude >= deadZone;

        // Lスティック入力中（前進・その場旋回）はエンジン音を鳴らす
        SetEngineSound(isMoving);

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
        bool isGrounded = false;
        Vector3 rayStart = transform.position + Vector3.up * rayOffsetHeight;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, rayLength, groundLayer,
                QueryTriggerInteraction.Ignore))
        {
            // 下向きの面 (天井など) は地面として扱わない
            if (Vector3.Dot(hit.normal, Vector3.up) > 0f)
            {
                groundNormal = hit.normal;
            }

            isGrounded = hit.distance <= rayOffsetHeight + groundCheckDistance;
        }

        // 接地中は速度を消す。
        // 移動は MovePosition で行っているので速度は使っておらず、
        // 残すと坂で重力により滑り落ちたり、押し戻されてガタついたりする。
        // (以前は坂で useGravity をOFFにしていたが、坂の境目で毎フレームON/OFFが切り替わり
        //  ガクガクする原因になっていたので、重力は常にONにしている)
        if (isGrounded)
        {
            rb.linearVelocity = Vector3.zero;
        }

        // 傾きはなめらかに追従させる (坂の境目で判定がばたついても車体が跳ねないように)
        Quaternion targetTilt = Quaternion.FromToRotation(Vector3.up, groundNormal);
        currentTiltRotation = Quaternion.RotateTowards(
            currentTiltRotation,
            targetTilt,
            tiltSmoothSpeed * Time.fixedDeltaTime
        );

        // ============================================
        // 移動と車体の回転 (Lスティック入力時のみ実行)
        // ============================================
        if (isMoving)
        {
            Vector2 moveInput = leftInput.normalized;
            Vector3 camForward = mainCamera != null ? mainCamera.forward : Vector3.forward;
            Vector3 camRight = mainCamera != null ? mainCamera.right : Vector3.right;

            camForward.y = 0;
            camRight.y = 0;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 baseMoveDir = camForward * moveInput.y + camRight * moveInput.x;

            // 車体の回転 (水平方向の向きだけで判定する)
            // 以前は坂の傾きも含めた角度で判定していたため、坂の境目で
            // 目標角度が跳ねるたびに 1度以内 を満たせず前進できなくなっていた
            if (baseMoveDir != Vector3.zero && tankBody != null)
            {
                Quaternion targetYaw = Quaternion.LookRotation(baseMoveDir, Vector3.up);

                currentBodyYaw = Quaternion.RotateTowards(
                    currentBodyYaw,
                    targetYaw,
                    rotationSpeed * Time.fixedDeltaTime
                );

                float angle = Quaternion.Angle(currentBodyYaw, targetYaw);

                if (angle < moveAngleThreshold)
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

        // 車体の姿勢 = 坂の傾き × 水平方向の向き (停止中も坂に合わせて傾ける)
        if (tankBody != null)
        {
            tankBody.rotation = currentTiltRotation * currentBodyYaw * offsetRot;
        }
    }

    void LateUpdate()
    {
        // 砲台とカメラは入力(Update)と同じく毎フレーム更新する。
        // FixedUpdate で動かすとフレームレートと物理更新のズレでカクつくため。

        // ============================================
        // 砲台・砲身の回転 (停止中も含め、毎フレーム常に実行)
        // ============================================

        // ★砲台（Turret）の回転
        Quaternion logicalYawRot = Quaternion.LookRotation(currentTurretForward, Vector3.up);
        Quaternion turretBaseRot = currentTiltRotation * logicalYawRot;

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

        // 砲台の向きが決まった後に着弾予測を計算する
        UpdateLandingPoint();
    }

    void OnFirePerformed(InputAction.CallbackContext context)
    {
        if (Time.time < nextFireTime)
        {
            return;
        }

        if (firePosition == null || Bullet == null)
        {
            Debug.LogError("[TankMovement] firePosition または Bullet が設定されていません！");
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

        if (SSoundManager.Instance != null)
        {
            SSoundManager.Instance.PlaySE_Shoot();
        }

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
        else if (bullet.TryGetComponent(out SharedBullet sharedBullet))
        {
            // Bullet プレハブが SharedBullet の場合はまっすぐ撃つ
            sharedBullet.Launch(
                firePosition.forward,
                bulletFlightSpeed,
                gameObject
            );
        }
        else
        {
            Debug.LogError("Bullet に TankProjectile / SharedBullet がありません！");
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

        fireCooldown = manager.GetFloat("fireCooldown", fireCooldown);

        // CSVに無い場合は Inspector の cameraOffset をそのまま使う
        cameraOffset = new Vector3(
            cameraOffset.x,
            manager.GetFloat("cameraHeight", cameraOffset.y),
            -manager.GetFloat("cameraDistance", -cameraOffset.z)
        );

        Debug.Log($"移動速度: {moveSpeed}");
    }
}