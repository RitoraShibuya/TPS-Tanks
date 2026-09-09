using UnityEngine;

public class FlyingTankAI : MonoBehaviour
{
    private enum PatrolState { Moving, Rotating }

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
    private PatrolState currentPatrolState = PatrolState.Moving;

    public enum ModelForwardAxis { Forward, Right, Left, Back }

    private void Update()
    {
        // 1. ローター回転処理
        HandleRotorEngine();

        if (statsData == null) return;

        // 2. 飛行高度維持
        HandleFlightAltitude();

        // 3. 巡回移動（到着後にその場旋回して次へ移動）
        if (statsData.canMove && waypoints != null && waypoints.Length > 0)
        {
            HandlePatrolMovement();
        }

        // 4. 索敵・砲塔制御・攻撃
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
    /// ウェイポイントの真上まで移動後、その場で旋回を行ってから次の地点へ移動する
    /// </summary>
    private void HandlePatrolMovement()
    {
        Transform targetWaypoint = waypoints[currentWaypointIndex];
        if (targetWaypoint == null) return;

        // 同一高度（XZ平面）での目的地設定
        Vector3 destination = targetWaypoint.position;
        destination.y = transform.position.y;

        Vector3 direction = (destination - transform.position);
        float distanceToDest = direction.magnitude;
        direction.Normalize();

        Transform body = bodyTransform != null ? bodyTransform : transform;

        // ----------------------------------------------------
        // ステート1: 目的地へ直進移動（移動中の傾き処理も適用）
        // ----------------------------------------------------
        if (currentPatrolState == PatrolState.Moving)
        {
            if (direction != Vector3.zero)
            {
                // 目標方向への回転を作成（モデル補正付き）
                Quaternion targetYawRotation = Quaternion.LookRotation(direction);
                if (modelForwardAxis == ModelForwardAxis.Right)
                {
                    targetYawRotation *= Quaternion.Euler(0, -90, 0);
                }

                // 移動中の前傾・ロール角度の計算
                float speedFactor = Mathf.Clamp01(distanceToDest / waypointThreshold);
                float pitchAngle = 0f * speedFactor;   // ピッチ（前後）
                float rollAngle = -20f * speedFactor;  // ロール（左右）

                Quaternion tiltRotation = Quaternion.Euler(pitchAngle, 0f, rollAngle);
                Quaternion finalTargetRotation = targetYawRotation * tiltRotation;

                // 向きの更新
                body.rotation = Quaternion.RotateTowards(
                    body.rotation,
                    finalTargetRotation,
                    statsData.bodyRotationSpeed * Time.deltaTime
                );

                // 位置の移動
                if (distanceToDest > 0.01f)
                {
                    transform.position = Vector3.MoveTowards(
                        transform.position,
                        destination,
                        statsData.moveSpeed * Time.deltaTime
                    );
                }
            }

            // 到達判定：真上（しきい値以内）に到達したら「その場旋回」モードへ移行
            if (distanceToDest <= waypointThreshold)
            {
                currentPatrolState = PatrolState.Rotating;
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            }
        }
        // ----------------------------------------------------
        // ステート2: 到達後、移動を停止してその場で次の目的地へ向く
        // ----------------------------------------------------
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

                // その場で向きを変更（傾きなし）
                body.rotation = Quaternion.RotateTowards(
                    body.rotation,
                    targetYawRotation,
                    statsData.bodyRotationSpeed * Time.deltaTime
                );

                // 旋回完了判定（正面との角度差が2度未満になったら移動再開）
                if (Quaternion.Angle(body.rotation, targetYawRotation) < 2.0f)
                {
                    currentPatrolState = PatrolState.Moving;
                }
            }
        }
    }

    /// <summary>
    /// 索敵および砲塔の自動旋回・攻撃処理
    /// </summary>
    private void HandleTargetDetectionAndAttack()
    {
        if (target == null) return;

        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        if (distanceToTarget > statsData.visionDistance) return;

        // 1. 水平砲塔（Turret）旋回
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

        // 2. 上下砲身（GunBarrel）旋回
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

        // 3. 視界判定・攻撃実行
        Transform body = bodyTransform != null ? bodyTransform : transform;
        Vector3 targetDirection = (target.position - transform.position).normalized;

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
        // 攻撃ロジック（弾生成など）
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