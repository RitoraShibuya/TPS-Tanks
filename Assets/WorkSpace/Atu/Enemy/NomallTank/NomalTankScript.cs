using UnityEngine;

public class EnemyTankController : EnemyBase
{
    private enum PatrolState { Moving, Rotating }

    [Header("ステータスデータ設定")]
    [SerializeField] private EnemyTankData statsData;

    [Header("パーツ参照")]
    [SerializeField] private Transform bodyTransform;          // 車体 (Body)
    [SerializeField] private Transform turretTransform;        // 砲塔 (Turret)
    [SerializeField] private Transform gunBarrelTransform;     // 砲身 (上下Turret)
    [SerializeField] private Transform muzzleTransform;        // 砲口 (発射位置)

    [Header("弾の発射設定")]
    [SerializeField] private GameObject bulletPrefab;          // 弾のプレハブ
    [SerializeField] private float bulletSpeed = 20f;         // 弾速

    [Header("モデルの正面軸補正設定")]
    [Tooltip("モデルの見た目の正面がどのローカル軸を向いているか指定（標準はForward、横を向いているならRight）")]
    [SerializeField] private ModelForwardAxis modelForwardAxis = ModelForwardAxis.Forward;

    [Header("AI・巡回設定")]
    [Tooltip("移動巡回する地点のリスト")]
    [SerializeField] private Transform[] waypoints;
    [Tooltip("地点に到着したとみなす距離")]
    [SerializeField] private float waypointThreshold = 0.5f;

    [Header("砲身上下制限")]
    [SerializeField] private float minGunPitch = -10f;
    [SerializeField] private float maxGunPitch = 30f;

    private float lastAttackTime = 0f;
    private int currentWaypointIndex = 0;
    private PatrolState currentPatrolState = PatrolState.Moving;

    public enum ModelForwardAxis { Forward, Right, Left, Back }

    protected override void Awake()
    {
        base.Awake();

        if (statsData != null)
        {
            currentHealth = statsData.maxHp;
        }
    }

    private void Update()
    {
        if (statsData == null) return;

        // ターゲット視界判定 & 戦闘・追従制御
        if (target != null && IsTargetInSight())
        {
            // ターゲットが視界に入っている場合：照準・攻撃
            HandleCombatAndTracking();
        }
        else
        {
            // ターゲットが設定されていない／視界外の場合：通常巡回
            if (waypoints != null && waypoints.Length > 0)
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
        if (distance > statsData.sightDistance) return false;

        direction.Normalize();

        Transform body = bodyTransform != null ? bodyTransform : transform;
        Vector3 forward = GetModelForward(body);

        float angle = Vector3.Angle(forward, direction);
        return angle <= (statsData.sightAngle / 2f);
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
    /// ターゲットへ車体・砲塔・砲身を向け、射撃を行う
    /// </summary>
    private void HandleCombatAndTracking()
    {
        if (target == null) return;

        Vector3 targetPosition = target.position;
        targetPosition.y = transform.position.y; // XZ平面での旋回

        Vector3 direction = (targetPosition - transform.position);
        direction.y = 0f;
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

        // --- 2. 水平砲塔（Turret）照準 ---
        if (turretTransform != null)
        {
            Vector3 turretTargetDir = target.position - turretTransform.position;
            turretTargetDir.y = 0f;

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

        // --- 3. 上下砲身（GunBarrel）照準 ---
        if (gunBarrelTransform != null && turretTransform != null)
        {
            Vector3 localTargetPos = turretTransform.InverseTransformPoint(target.position);
            float targetAngle = -Mathf.Atan2(localTargetPos.y, localTargetPos.z) * Mathf.Rad2Deg;
            targetAngle = Mathf.Clamp(targetAngle, minGunPitch, maxGunPitch);

            Quaternion targetBarrelRot = Quaternion.Euler(targetAngle, 0f, 0f);
            gunBarrelTransform.localRotation = Quaternion.RotateTowards(
                gunBarrelTransform.localRotation,
                targetBarrelRot,
                statsData.pitchTurretRotationSpeed * Time.deltaTime
            );
        }

        // --- 4. 射撃判定 ---
        if (Time.time >= lastAttackTime + statsData.attackCooldown)
        {
            if (IsAimingAtTarget())
            {
                lastAttackTime = Time.time;
                ExecuteShoot();
            }
        }
    }

    /// <summary>
    /// 照準がターゲットを概ね捉えているか判定
    /// </summary>
    private bool IsAimingAtTarget()
    {
        if (target == null) return false;

        Transform checkTransform = turretTransform != null ? turretTransform : transform;
        Vector3 direction = (target.position - checkTransform.position).normalized;

        // 砲塔の向きとターゲットへの方向の角度差で判定
        Vector3 forward = GetModelForward(checkTransform);
        float angle = Vector3.Angle(forward, direction);

        return angle <= 10f; // 10度以内なら許可
    }

    /// <summary>
    /// ウェイポイント移動・旋回巡回処理
    /// </summary>
    private void HandlePatrolMovement()
    {
        Transform targetWaypoint = waypoints[currentWaypointIndex];
        if (targetWaypoint == null) return;

        Vector3 destination = targetWaypoint.position;
        destination.y = transform.position.y;

        Vector3 direction = (destination - transform.position);
        direction.y = 0f;
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

                body.rotation = Quaternion.RotateTowards(
                    body.rotation,
                    targetYawRotation,
                    statsData.bodyRotationSpeed * Time.deltaTime
                );

                transform.position = Vector3.MoveTowards(
                    transform.position,
                    destination,
                    statsData.moveSpeed * Time.deltaTime
                );
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
            Vector3 nextDirection = (nextDestination - transform.position);
            nextDirection.y = 0f;
            nextDirection.Normalize();

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
        if (muzzleTransform == null)
        {
            Debug.LogWarning("EnemyTankController: muzzleTransform が設定されていません。", this);
            return;
        }

        if (bulletPrefab == null)
        {
            Debug.LogWarning("EnemyTankController: bulletPrefab が設定されていません。", this);
            return;
        }

        if (target == null) return;

        // 砲口からターゲットへの方向ベクトルを正確に計算
        Vector3 shootDirection = (target.position - muzzleTransform.position).normalized;

        // 1. 砲口の位置・方向を計算して弾を生成
        Quaternion bulletRotation = Quaternion.LookRotation(shootDirection);
        GameObject bulletObj = Instantiate(
            bulletPrefab,
            muzzleTransform.position,
            bulletRotation
        );

        // 2. 計算した射撃方向（shootDirection）を直接渡して発射
        if (bulletObj.TryGetComponent(out EnemyBullet bullet))
        {
            bullet.Launch(
                shootDirection,
                bulletSpeed,
                statsData.attackPower
            );
        }

        Debug.Log($"Enemy Tank Shoot! Attack Power: {statsData.attackPower}");
    }

    protected override void Die()
    {
        Debug.Log("Enemy Tank Destroyed!");
        base.Die();
    }

    private void OnDrawGizmosSelected()
    {
        if (statsData == null) return;

        // 視界描画（黄色）
        Gizmos.color = Color.yellow;
        Transform body = bodyTransform != null ? bodyTransform : transform;

        Vector3 modelForward = GetModelForward(body);
        Vector3 leftRay = Quaternion.Euler(0, -statsData.sightAngle / 2f, 0) * modelForward;
        Vector3 rightRay = Quaternion.Euler(0, statsData.sightAngle / 2f, 0) * modelForward;

        Gizmos.DrawRay(transform.position, leftRay * statsData.sightDistance);
        Gizmos.DrawRay(transform.position, rightRay * statsData.sightDistance);

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