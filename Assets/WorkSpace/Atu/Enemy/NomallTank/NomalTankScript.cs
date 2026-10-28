using System.Collections.Generic;
using UnityEngine;

public class EnemyTankController : EnemyBase
{
    public enum ModelForwardAxis { Forward, Right, Left, Back }
    public enum PatrolType { Loop, PingPong, Random }

    [Header("パーツ設定")]
    [SerializeField] private Transform turretTransform;
    [SerializeField] private Transform barrelTransform;
    [SerializeField] private Transform muzzleTransform;

    [Header("モデルの正面軸補正設定")]
    [Tooltip("戦車本体（モデルの正面）が向いているローカル軸を指定")]
    [SerializeField] private ModelForwardAxis modelForwardAxis = ModelForwardAxis.Forward;

    [Header("移動・旋回ステータス")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float bodyRotationSpeed = 120f;
    [SerializeField] private float turretRotationSpeed = 90f;

    [Header("WayPoint（巡回）設定")]
    [SerializeField] private List<Transform> waypoints = new List<Transform>();
    [SerializeField] private PatrolType patrolType = PatrolType.Loop;
    [SerializeField] private float waypointArriveDistance = 1.5f;
    [SerializeField] private float waypointWaitTime = 2f;

    [Header("索敵・前方視界設定")]
    [SerializeField] private float sightDistance = 15f; // 正面視界の届く距離
    [SerializeField] private float sightAngle = 120f;   // 視野角（例: 120度なら正面から左右60度ずつ）

    [Header("射撃パラメータ")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 40f;
    [SerializeField] private float attackRange = 20f;   // 攻撃に移行する距離
    [SerializeField] private float fireRate = 2f;
    [SerializeField] private LayerMask obstacleLayer = ~0;

    private float nextFireTime;

    // WayPoint巡回用
    private int currentWaypointIndex = 0;
    private bool isPatrolReverse = false;
    private float waitTimer = 0f;
    private bool isWaiting = false;

    // 検知フラグ
    private bool isPlayerDetected = false;

    private void Update()
    {
        // 1. ターゲットが視界に入っているかチェック（未検知時のみ）
        if (!isPlayerDetected && IsTargetInSight())
        {
            isPlayerDetected = true; // 正面視界に入ったのでプレイヤーを検知！
        }

        // --- 行動分岐 ---
        if (isPlayerDetected && target != null)
        {
            float distance = Vector3.Distance(transform.position, target.position);

            // 攻撃距離内 ＆ 射線が通っている場合：停止して砲撃
            if (distance <= attackRange && HasLineOfSight())
            {
                AimTurret();

                if (Time.time >= nextFireTime)
                {
                    nextFireTime = Time.time + fireRate;
                    ExecuteShoot();
                }
            }
            else
            {
                // 検知済みだが攻撃距離外：プレイヤーの方向へ直接移動
                MoveTowardsPosition(target.position);
                AimTurret();
            }
        }
        else
        {
            // --- 未検知状態：WayPoint巡回 ---
            if (HasValidWaypoints())
            {
                PatrolWaypoints();
            }
        }
    }

    /// <summary>
    /// 指定位置へ車体を旋回させながら直進移動（NavMeshを使わない基本物理/Transform移動）
    /// </summary>
    private void MoveTowardsPosition(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position);
        direction.y = 0f; // 水平移動のみ

        if (direction != Vector3.zero)
        {
            // 車体の進行方向への旋回
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            // モデル正面軸補正
            switch (modelForwardAxis)
            {
                case ModelForwardAxis.Right: targetRotation *= Quaternion.Euler(0, -90, 0); break;
                case ModelForwardAxis.Left: targetRotation *= Quaternion.Euler(0, 90, 0); break;
                case ModelForwardAxis.Back: targetRotation *= Quaternion.Euler(0, 180, 0); break;
            }

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                bodyRotationSpeed * Time.deltaTime
            );

            // 直進移動
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                moveSpeed * Time.deltaTime
            );
        }
    }

    /// <summary>
    /// FixedTurretを踏襲した視界判定（距離・モデル正面軸の視野角・壁の裏でないか）
    /// </summary>
    private bool IsTargetInSight()
    {
        if (target == null) return false;

        // 1. 距離判定
        Vector3 directionToTarget = target.position - transform.position;
        float distance = directionToTarget.magnitude;
        if (distance > sightDistance) return false;

        // 2. 視野角判定（水平面で計算）
        Vector3 horizontalDir = directionToTarget;
        horizontalDir.y = 0f;
        horizontalDir.Normalize();

        Vector3 forward = GetModelForward(transform);

        float angle = Vector3.Angle(forward, horizontalDir);
        if (angle > (sightAngle / 2f)) return false;

        // 3. 壁（障害物）の遮蔽チェック (Raycast)
        return HasLineOfSight();
    }

    /// <summary>
    /// 射線（Raycast）遮蔽チェック
    /// </summary>
    private bool HasLineOfSight()
    {
        if (target == null) return false;

        Vector3 rayStartPos = turretTransform != null ? turretTransform.position : transform.position + Vector3.up * 1f;
        Vector3 rayDir = (target.position - rayStartPos).normalized;
        float dist = Vector3.Distance(rayStartPos, target.position);

        if (Physics.Raycast(rayStartPos, rayDir, out RaycastHit hit, dist, obstacleLayer))
        {
            if (!hit.transform.IsChildOf(target) && hit.transform != target && !hit.transform.CompareTag(playerTag))
            {
                return false; // 壁に遮られている
            }
        }
        return true;
    }

    private Vector3 GetModelForward(Transform t)
    {
        switch (modelForwardAxis)
        {
            case ModelForwardAxis.Right: return t.right;
            case ModelForwardAxis.Left: return -t.right;
            case ModelForwardAxis.Back: return -t.forward;
            default: return t.forward;
        }
    }

    private bool HasValidWaypoints()
    {
        if (waypoints == null || waypoints.Count == 0) return false;
        foreach (var wp in waypoints)
        {
            if (wp != null) return true;
        }
        return false;
    }

    private void PatrolWaypoints()
    {
        if (isWaiting)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waypointWaitTime)
            {
                isWaiting = false;
                waitTimer = 0f;
                SetNextWaypoint();
            }
            return;
        }

        Transform currentPoint = waypoints[currentWaypointIndex];
        if (currentPoint == null)
        {
            SetNextWaypoint();
            return;
        }

        MoveTowardsPosition(currentPoint.position);

        if (Vector3.Distance(transform.position, currentPoint.position) <= waypointArriveDistance)
        {
            isWaiting = true;
        }
    }

    private void SetNextWaypoint()
    {
        if (waypoints == null || waypoints.Count <= 1) return;

        switch (patrolType)
        {
            case PatrolType.Loop:
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;
                break;

            case PatrolType.PingPong:
                if (isPatrolReverse)
                {
                    currentWaypointIndex--;
                    if (currentWaypointIndex <= 0)
                    {
                        currentWaypointIndex = 0;
                        isPatrolReverse = false;
                    }
                }
                else
                {
                    currentWaypointIndex++;
                    if (currentWaypointIndex >= waypoints.Count - 1)
                    {
                        currentWaypointIndex = waypoints.Count - 1;
                        isPatrolReverse = true;
                    }
                }
                break;

            case PatrolType.Random:
                currentWaypointIndex = Random.Range(0, waypoints.Count);
                break;
        }
    }

    private void AimTurret()
    {
        if (turretTransform == null || target == null) return;

        Vector3 targetDir = (target.position - turretTransform.position).normalized;
        targetDir.y = 0;

        if (targetDir != Vector3.zero)
        {
            Quaternion lookRot = Quaternion.LookRotation(targetDir);

            switch (modelForwardAxis)
            {
                case ModelForwardAxis.Right: lookRot *= Quaternion.Euler(0, -90, 0); break;
                case ModelForwardAxis.Left: lookRot *= Quaternion.Euler(0, 90, 0); break;
                case ModelForwardAxis.Back: lookRot *= Quaternion.Euler(0, 180, 0); break;
            }

            turretTransform.rotation = Quaternion.Slerp(turretTransform.rotation, lookRot, Time.deltaTime * 5f);
        }
    }

    private void ExecuteShoot()
    {
        if (muzzleTransform == null || bulletPrefab == null || target == null) return;

        Vector3 shootDirection = (GetTargetAimPoint() - muzzleTransform.position).normalized;
        Quaternion bulletRotation = Quaternion.LookRotation(shootDirection);

        GameObject bulletObj = Instantiate(bulletPrefab, muzzleTransform.position, bulletRotation);

        if (bulletObj.TryGetComponent(out SharedBullet bullet))
        {
            bullet.Launch(shootDirection, bulletSpeed, gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 1. 正面軸補正を考慮した視界範囲（黄色）を描画
        Gizmos.color = Color.yellow;
        Vector3 forward = GetModelForward(transform);
        Vector3 leftRay = Quaternion.Euler(0, -sightAngle / 2f, 0) * forward;
        Vector3 rightRay = Quaternion.Euler(0, sightAngle / 2f, 0) * forward;

        Gizmos.DrawRay(transform.position, leftRay * sightDistance);
        Gizmos.DrawRay(transform.position, rightRay * sightDistance);
        Gizmos.DrawRay(transform.position, forward * sightDistance);

        // 2. 攻撃距離（赤色）
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // 3. WayPointの描画（水色）
        if (waypoints != null && waypoints.Count > 0)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < waypoints.Count; i++)
            {
                if (waypoints[i] != null)
                {
                    Gizmos.DrawWireSphere(waypoints[i].position, 0.5f);
                    int next = (i + 1) % waypoints.Count;
                    if (waypoints[next] != null)
                    {
                        Gizmos.DrawLine(waypoints[i].position, waypoints[next].position);
                    }
                }
            }
        }
    }
}