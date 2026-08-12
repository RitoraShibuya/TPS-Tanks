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

    private void Update()
    {
        // 1. ローター回転
        HandleRotorEngine();

        if (statsData == null) return;

        // 2. 飛行高度維持 (仕様: 6m)
        HandleFlightAltitude();

        // 3. 巡回移動 (仕様: canMove ON時)
        if (statsData.canMove && waypoints != null && waypoints.Length > 0)
        {
            HandlePatrolMovement();
        }

        // 4. 索敵および砲塔の自動旋回・攻撃
        HandleTargetDetectionAndAttack();
    }

    /// <summary>
    /// 指定されたポイント（Waypoints）へ順番に移動・旋回
    /// </summary>
    private void HandlePatrolMovement()
    {
        Transform targetWaypoint = waypoints[currentWaypointIndex];
        if (targetWaypoint == null) return;

        // 同一高度（XZ平面）での方向計算
        Vector3 destination = targetWaypoint.position;
        destination.y = transform.position.y;

        Vector3 direction = (destination - transform.position).normalized;

        if (direction != Vector3.zero)
        {
            // Bodyの回転（仕様: 180度/s）
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            Transform body = bodyTransform != null ? bodyTransform : transform;
            body.rotation = Quaternion.RotateTowards(body.rotation, targetRotation, statsData.bodyRotationSpeed * Time.deltaTime);

            // 前方移動（仕様: 5m/s）
            transform.position = Vector3.MoveTowards(transform.position, destination, statsData.moveSpeed * Time.deltaTime);
        }

        // 地点到達判定 -> 次のウェイポイントへ
        if (Vector3.Distance(transform.position, destination) <= waypointThreshold)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        }
    }

    /// <summary>
    /// 視界判定（距離7m / 角度20度）と自動照準・攻撃
    /// </summary>
    private void HandleTargetDetectionAndAttack()
    {
        if (target == null) return;

        Vector3 targetDirection = (target.position - transform.position).normalized;
        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        Transform body = bodyTransform != null ? bodyTransform : transform;
        float angleToTarget = Vector3.Angle(body.forward, targetDirection);

        // 視界（角度20度以内かつ距離7m以内）に入っているかチェック
        bool isInVision = (distanceToTarget <= statsData.visionDistance) && (angleToTarget <= statsData.visionAngle / 2f);

        if (isInVision)
        {
            // --- Turret（水平60度/s）をターゲットに向ける ---
            if (turretTransform != null)
            {
                Vector3 turretTargetDir = target.position - turretTransform.position;
                turretTargetDir.y = 0; // 水平のみ
                if (turretTargetDir != Vector3.zero)
                {
                    Quaternion targetTurretRot = Quaternion.LookRotation(turretTargetDir);
                    turretTransform.rotation = Quaternion.RotateTowards(turretTransform.rotation, targetTurretRot, statsData.turretRotationSpeed * Time.deltaTime);
                }
            }

            // --- 上下Turret（俯仰60度/s）をターゲットに向ける ---
            if (gunBarrelTransform != null)
            {
                Vector3 barrelTargetDir = target.position - gunBarrelTransform.position;
                if (barrelTargetDir != Vector3.zero)
                {
                    Quaternion targetBarrelRot = Quaternion.LookRotation(barrelTargetDir);
                    gunBarrelTransform.rotation = Quaternion.RotateTowards(gunBarrelTransform.rotation, targetBarrelRot, statsData.pitchTurretRotationSpeed * Time.deltaTime);
                }
            }

            // --- 砲撃実行（クールダウン: 0.2s） ---
            if (Time.time >= lastAttackTime + statsData.attackCooldown)
            {
                lastAttackTime = Time.time;
                ExecuteShoot();
            }
        }
    }

    private void ExecuteShoot()
    {
        // 弾の生成や攻撃音などのロジックをここに実装
        // Debug.Log($"敵が攻撃! 攻撃力: {statsData.attackPower}");
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

    // シーンビューで視界範囲と巡回ルートを可視化（デバッグ用）
    private void OnDrawGizmosSelected()
    {
        if (statsData == null) return;

        // 視界範囲（黄色）
        Gizmos.color = Color.yellow;
        Transform body = bodyTransform != null ? bodyTransform : transform;
        Vector3 leftRay = Quaternion.Euler(0, -statsData.visionAngle / 2f, 0) * body.forward;
        Vector3 rightRay = Quaternion.Euler(0, statsData.visionAngle / 2f, 0) * body.forward;

        Gizmos.DrawRay(transform.position, leftRay * statsData.visionDistance);
        Gizmos.DrawRay(transform.position, rightRay * statsData.visionDistance);

        // 巡回ルート（青ライン）
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