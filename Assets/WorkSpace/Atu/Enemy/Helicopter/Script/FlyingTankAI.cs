using UnityEngine;

public class FlyingTankAI : MonoBehaviour
{
    [Header("ステータスデータ設定")]
    [SerializeField] private FlyingTankData statsData;

    [Header("パーツ参照")]
    [SerializeField] private Transform bodyTransform;          // 車体 (Body)
    [SerializeField] private Transform turretTransform;        // 砲塔 (Turret)
    [SerializeField] private Transform gunBarrelTransform;     // 砲身 (上下Turret)
    [SerializeField] private Transform mainRotor;             // メインローター
    [SerializeField] private Transform tailRotor;             // テールローター

    [Header("モデルの正面軸補正設定")]
    [Tooltip("モデルの見た目の正面がどのローカル軸を向いているか指定（標準はForward、画像のように横を向いているならRight）")]
    [SerializeField] private ModelForwardAxis modelForwardAxis = ModelForwardAxis.Right;

    [Header("AI・巡回設定")]
    [Tooltip("移動巡回する地点のリスト")]
    [SerializeField] private Transform[] waypoints;
    [Tooltip("地点に到着したとみなす距離")]
    [SerializeField] private float waypointThreshold = 1.0f;
    [Tooltip("攻撃対象（プレイヤーなど）")]
    [SerializeField] private Transform target;

    [Header("ローターエンジン設定")]
    [SerializeField] private float maxRotorSpeed = 1500f;
    [SerializeField] private float acceleration = 300f;
    [SerializeField] private float deceleration = 150f;
    [SerializeField] private float tailRotorMultiplier = 1.2f;

    private float currentRotorSpeed = 0f;
    private bool isEngineOn = true;
    private float lastAttackTime = 0f;
    private int currentWaypointIndex = 0;

    public enum ModelForwardAxis { Forward, Right, Left, Back }

    private void Update()
    {
        HandleRotorEngine();

        if (statsData == null) return;

        HandleFlightAltitude();

        if (statsData.canMove && waypoints != null && waypoints.Length > 0)
        {
            HandlePatrolMovement();
        }

        HandleTargetDetectionAndAttack();
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
    /// 指定されたポイント（Waypoints）へ順番に移動・旋回。前進時は前傾姿勢になる。
    /// </summary>
    private void HandlePatrolMovement()
    {
        Transform targetWaypoint = waypoints[currentWaypointIndex];
        if (targetWaypoint == null) return;

        // 同一高度（XZ平面）での方向計算
        Vector3 destination = targetWaypoint.position;
        destination.y = transform.position.y;

        Vector3 direction = (destination - transform.position);
        float distanceToDest = direction.magnitude; // 目的地までの距離
        direction.Normalize(); // 正規化

        Transform body = bodyTransform != null ? bodyTransform : transform;

        if (direction != Vector3.zero)
        {
            Quaternion targetYawRotation = Quaternion.LookRotation(direction);

            if (modelForwardAxis == ModelForwardAxis.Right)
            {
                targetYawRotation *= Quaternion.Euler(0, -90, 0); // 右向きモデルを正面に向ける補正
            }

            float speedFactor = Mathf.Clamp01(distanceToDest / waypointThreshold);

            float maxTiltAngle = 0f * speedFactor;
            float rollAngle = -20f * speedFactor;

            float targetTiltAngle = maxTiltAngle * speedFactor;

            Quaternion tiltRotation = Quaternion.Euler(targetTiltAngle, 0f, rollAngle);

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
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        }
    }

    private void HandleTargetDetectionAndAttack()
    {
        if (target == null) return;

        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        if (distanceToTarget > statsData.visionDistance) return;

        // 1. Turret（水平）の旋回
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

        // 2. GunBarrel（上下ピッチ）の旋回
        if (gunBarrelTransform != null)
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

        // 3. 視界角判定（正しく修正したモデルの正面ベクトルを使用）
        Transform body = bodyTransform != null ? bodyTransform : transform;
        Vector3 targetDirection = (target.position - transform.position).normalized;

        // body.forward ではなく GetModelForward(body) を使用
        float angleToTarget = Vector3.Angle(GetModelForward(body), targetDirection);

        if (angleToTarget <= (statsData.visionAngle / 2f))
        {
            if (Time.time >= lastAttackTime + statsData.attackCooldown)
            {
                lastAttackTime = Time.time;
                ExecuteShoot();
            }
        }
    }

    private void ExecuteShoot()
    {
        // 攻撃ロジック
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

        Gizmos.color = Color.yellow;
        Transform body = bodyTransform != null ? bodyTransform : transform;

        // body.forward ではなく GetModelForward(body) を使用
        Vector3 modelForward = GetModelForward(body);
        Vector3 leftRay = Quaternion.Euler(0, -statsData.visionAngle / 2f, 0) * modelForward;
        Vector3 rightRay = Quaternion.Euler(0, statsData.visionAngle / 2f, 0) * modelForward;

        Gizmos.DrawRay(transform.position, leftRay * statsData.visionDistance);
        Gizmos.DrawRay(transform.position, rightRay * statsData.visionDistance);

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