using UnityEngine;

public class FlyingTankAI : EnemyBase
{
    private enum PatrolState { Moving, Rotating }

    [Header("ステータスデータ設定")]
    [SerializeField] private FlyingTankData statsData;

    [Header("パーツ参照")]
    [SerializeField] private Transform bodyTransform;          // 車体 (Body)
    [SerializeField] private Transform turretTransform;        // 砲塔 (Turret)
    [SerializeField] private Transform gunBarrelTransform;     // 砲身 (上下Turret)
    [SerializeField] private Transform muzzleTransform;        // 砲口 (発射位置)
    [SerializeField] private Transform mainRotor;              // メインローター
    [SerializeField] private Transform tailRotor;              // テールローター

    [Header("弾の発射設定")]
    [SerializeField] private GameObject bulletPrefab;          // 弾のプレハブ
    [SerializeField] private float bulletSpeed = 100f;         // 弾速

    [Header("モデルの正面軸補正設定")]
    [Tooltip("モデルの見た目の正面がどのローカル軸を向いているか指定（標準はForward、画像のように横を向いているならRight）")]
    [SerializeField] private ModelForwardAxis modelForwardAxis = ModelForwardAxis.Right;

    [Header("AI・巡回設定")]
    [Tooltip("移動巡回する地点のリスト")]
    [SerializeField] private Transform[] waypoints;
    [Tooltip("地点に到着したとみなす距離")]
    [SerializeField] private float waypointThreshold = 1.0f;
    [Tooltip("ターゲットに接近を試みる最小距離（これ以上近ければ停止して攻撃）")]
    [SerializeField] private float attackApproachDistance = 8.0f;

    [Header("ローターエンジン設定")]
    [SerializeField] private float maxRotorSpeed = 1500f;
    [SerializeField] private float acceleration = 300f;
    [SerializeField] private float deceleration = 150f;
    [SerializeField] private float tailRotorMultiplier = 1.2f;

    private float currentRotorSpeed = 0f;
    private bool isEngineOn = true;
    private float lastAttackTime = 0f;
    private int currentWaypointIndex = 0;
    private PatrolState currentPatrolState = PatrolState.Moving;

    public enum ModelForwardAxis { Forward, Right, Left, Back }

    protected override void Awake()
    {
        base.Awake();

        // FlyingTankDataの最大HPを自身の初期HPとしてセット
        if (statsData != null)
        {
            currentHealth = statsData.maxHealth;
        }
    }

    private void Update()
    {
        // 1. ローター回転処理
        HandleRotorEngine();

        if (statsData == null) return;

        // 2. 飛行高度維持
        HandleFlightAltitude();

        // 3. ターゲット視界判定 & 戦闘・追従制御（targetは親クラスEnemyBaseのものを使用）
        if (target != null && IsTargetInSight())
        {
            // ターゲットが視界に入っている場合：追従・照準・攻撃
            HandleCombatAndTracking();
        }
        else
        {
            // ターゲットが設定されていない／視界外の場合：通常巡回
            if (statsData.canMove && waypoints != null && waypoints.Length > 0)
            {
                HandlePatrolMovement();
            }
        }
    }

    /// <summary>
    /// ターゲットが視界（距離・視野角）に入っているか判定する
    /// </summary>
    private bool IsTargetInSight()
    {
        if (target == null) return false;

        Vector3 direction = target.position - transform.position;
        direction.y = 0f; // 水平距離で判定

        float distance = direction.magnitude;
        if (distance > statsData.visionDistance) return false;

        direction.Normalize();

        Transform body = bodyTransform != null ? bodyTransform : transform;
        Vector3 forward = GetModelForward(body);

        float angle = Vector3.Angle(forward, direction);
        return angle <= (statsData.visionAngle / 2f);
    }

    /// <summary>
    /// モデルの向きに合わせた正面方向ベクトルを取得する
    /// </summary>
    private Vector3 GetModelForward(Transform t)
    {
        Transform targetTransform = t != null ? t : transform;
        switch (modelForwardAxis)
        {
            case ModelForwardAxis.Right: return targetTransform.right;
            case ModelForwardAxis.Left: return -targetTransform.right;
            case ModelForwardAxis.Back: return -targetTransform.forward;
            default: return targetTransform.forward;
        }
    }

    /// <summary>
    /// ターゲットを追従しながら向きを変更し、照準・攻撃を行う
    /// </summary>
    private void HandleCombatAndTracking()
    {
        if (target == null) return;

        Vector3 targetPosition = target.position;
        targetPosition.y = transform.position.y; // 旋回・追従はXZ平面

        Vector3 direction = (targetPosition - transform.position);
        float distanceToTarget = direction.magnitude;
        direction.Normalize();

        Transform body = bodyTransform != null ? bodyTransform : transform;

        // --- 1. 車体（Body）をターゲットに向ける ---
        if (direction != Vector3.zero)
        {
            Quaternion targetYawRotation = Quaternion.LookRotation(direction);
            if (modelForwardAxis == ModelForwardAxis.Right)
            {
                targetYawRotation *= Quaternion.Euler(0, -90, 0);
            }

            body.rotation = Quaternion.RotateTowards(
                body.rotation,
                targetYawRotation,
                statsData.bodyRotationSpeed * Time.deltaTime
            );
        }

        // --- 2. ターゲットとの距離に応じて追従移動 ---
        if (statsData.canMove && distanceToTarget > attackApproachDistance)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                statsData.moveSpeed * Time.deltaTime
            );
        }

        // --- 3. 水平砲塔（Turret）照準 ---
        if (turretTransform != null)
        {
            Vector3 turretTargetDir = target.position - turretTransform.position;
            turretTargetDir.y = 0;

            if (turretTargetDir != Vector3.zero)
            {
                Quaternion targetTurretRot = Quaternion.LookRotation(turretTargetDir);
                if (modelForwardAxis == ModelForwardAxis.Right) targetTurretRot *= Quaternion.Euler(0, -90, 0);

                turretTransform.rotation = Quaternion.RotateTowards(
                    turretTransform.rotation,
                    targetTurretRot,
                    statsData.turretRotationSpeed * Time.deltaTime
                );
            }
        }

        // --- 4. 上下砲身（GunBarrel）照準 ---
        if (gunBarrelTransform != null && turretTransform != null)
        {
            Vector3 localTargetPos = turretTransform.InverseTransformPoint(target.position);
            float targetAngle = -Mathf.Atan2(localTargetPos.y, localTargetPos.z) * Mathf.Rad2Deg;

            Quaternion targetBarrelRot = Quaternion.Euler(targetAngle, 0f, 0f);
            gunBarrelTransform.localRotation = Quaternion.RotateTowards(
                gunBarrelTransform.localRotation,
                targetBarrelRot,
                statsData.pitchTurretRotationSpeed * Time.deltaTime
            );
        }

        // --- 5. 射撃判定 ---
        if (Time.time >= lastAttackTime + statsData.attackCooldown)
        {
            lastAttackTime = Time.time;
            ExecuteShoot();
        }
    }

    /// <summary>
    /// ウェイポイントの真上まで移動後、その場で旋回を行ってから次の地点へ移動する
    /// </summary>
    private void HandlePatrolMovement()
    {
        Transform targetWaypoint = waypoints[currentWaypointIndex];
        if (targetWaypoint == null) return;

        Vector3 destination = targetWaypoint.position;
        destination.y = transform.position.y;

        Vector3 direction = (destination - transform.position);
        float distanceToDest = direction.magnitude;
        direction.Normalize();

        Transform body = bodyTransform != null ? bodyTransform : transform;

        if (currentPatrolState == PatrolState.Moving)
        {
            if (direction != Vector3.zero)
            {
                Quaternion targetYawRotation = Quaternion.LookRotation(direction);
                if (modelForwardAxis == ModelForwardAxis.Right)
                {
                    targetYawRotation *= Quaternion.Euler(0, -90, 0);
                }

                float speedFactor = Mathf.Clamp01(distanceToDest / waypointThreshold);
                float pitchAngle = 0f * speedFactor;
                float rollAngle = -20f * speedFactor;

                Quaternion tiltRotation = Quaternion.Euler(pitchAngle, 0f, rollAngle);
                Quaternion finalTargetRotation = targetYawRotation * tiltRotation;

                body.rotation = Quaternion.RotateTowards(
                    body.rotation,
                    finalTargetRotation,
                    statsData.bodyRotationSpeed * Time.deltaTime
                );

                if (distanceToDest > 0.01f)
                {
                    transform.position = Vector3.MoveTowards(
                        transform.position,
                        destination,
                        statsData.moveSpeed * Time.deltaTime
                    );
                }
            }

            if (distanceToDest <= waypointThreshold)
            {
                currentPatrolState = PatrolState.Rotating;
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            }
        }
        else if (currentPatrolState == PatrolState.Rotating)
        {
            Transform nextWaypoint = waypoints[currentWaypointIndex];
            if (nextWaypoint == null) return;

            Vector3 nextDestination = nextWaypoint.position;
            nextDestination.y = transform.position.y;
            Vector3 nextDirection = (nextDestination - transform.position).normalized;

            if (nextDirection != Vector3.zero)
            {
                Quaternion targetYawRotation = Quaternion.LookRotation(nextDirection);
                if (modelForwardAxis == ModelForwardAxis.Right)
                {
                    targetYawRotation *= Quaternion.Euler(0, -90, 0);
                }

                body.rotation = Quaternion.RotateTowards(
                    body.rotation,
                    targetYawRotation,
                    statsData.bodyRotationSpeed * Time.deltaTime
                );

                if (Quaternion.Angle(body.rotation, targetYawRotation) < 2.0f)
                {
                    currentPatrolState = PatrolState.Moving;
                }
            }
        }
    }

    /// <summary>
    /// 弾を発射する
    /// </summary>
    private void ExecuteShoot()
    {
        if (muzzleTransform == null || bulletPrefab == null || target == null) return;

        // 1. 砲口からターゲットへの方向ベクトルを計算
        Vector3 shootDirection = (target.position - muzzleTransform.position).normalized;

        // 2. ターゲット方向を基準にした回転を作成
        Quaternion bulletRotation = Quaternion.LookRotation(shootDirection);

        // ★重要: モデルの正面軸が Right の場合、弾の生成角度も90度補正する
        if (modelForwardAxis == ModelForwardAxis.Right)
        {
            bulletRotation *= Quaternion.Euler(0, -90, 0);
        }
        else if (modelForwardAxis == ModelForwardAxis.Left)
        {
            bulletRotation *= Quaternion.Euler(0, 90, 0);
        }

        // 3. 補正した角度で弾を生成
        GameObject bulletObj = Instantiate(
            bulletPrefab,
            muzzleTransform.position,
            bulletRotation
        );

        // 4. 弾の飛翔処理を開始（飛翔方向は純粋な shootDirection を渡す）
        if (bulletObj.TryGetComponent(out EnemyBullet bullet))
        {
            bullet.Launch(
                shootDirection,
                bulletSpeed,
                statsData.attackPower
            );
        }

        Debug.Log($"Helicopter Shoot! Attack Power: {statsData.attackPower}");
    }

    private void HandleFlightAltitude()
    {
        Vector3 pos = transform.position;
        pos.y = Mathf.Lerp(pos.y, statsData.flightAltitude, Time.deltaTime * 2.0f);
        transform.position = pos;
    }

    private void HandleRotorEngine()
    {
        float targetSpeed = isEngineOn ? maxRotorSpeed : 0f;
        float rate = isEngineOn ? acceleration : deceleration;
        currentRotorSpeed = Mathf.MoveTowards(currentRotorSpeed, targetSpeed, rate * Time.deltaTime);

        if (mainRotor != null) mainRotor.Rotate(Vector3.up * currentRotorSpeed * Time.deltaTime, Space.Self);
        if (tailRotor != null) tailRotor.Rotate(Vector3.forward * (currentRotorSpeed * tailRotorMultiplier) * Time.deltaTime, Space.Self);
    }

    private void OnDrawGizmosSelected()
    {
        if (statsData == null) return;

        // 視界描画（黄色）
        Gizmos.color = Color.yellow;
        Transform body = bodyTransform != null ? bodyTransform : transform;

        Vector3 modelForward = GetModelForward(body);
        Vector3 leftRay = Quaternion.Euler(0, -statsData.visionAngle / 2f, 0) * modelForward;
        Vector3 rightRay = Quaternion.Euler(0, statsData.visionAngle / 2f, 0) * modelForward;

        Gizmos.DrawRay(transform.position, leftRay * statsData.visionDistance);
        Gizmos.DrawRay(transform.position, rightRay * statsData.visionDistance);

        // 巡回ルート描画（青色）
        if (waypoints != null && waypoints.Length > 1)
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] != null)
                {
                    Vector3 nextPos = waypoints[(i + 1) % waypoints.Length].position;
                    Gizmos.DrawLine(waypoints[i].position, nextPos);
                }
            }
        }
    }
}