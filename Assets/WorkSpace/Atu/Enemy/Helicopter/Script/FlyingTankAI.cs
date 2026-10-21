using System.Collections.Generic;
using UnityEngine;

public class FlyingTankAI : EnemyBase
{
    public enum ModelForwardAxis { Forward, Right, Left, Back }
    public enum PatrolType { Loop, PingPong, Random }

    [Header("パーツ設定")]
    [SerializeField] private Transform muzzleTransform;

    [Header("モデルの正面軸補正設定")]
    [Tooltip("飛行体の正面（カメラ・モデルの向いている方向）のローカル軸を指定")]
    [SerializeField] private ModelForwardAxis modelForwardAxis = ModelForwardAxis.Forward;

    [Header("飛行・移動設定")]
    [SerializeField] private float flyHeight = 8f;           // 飛行高度
    [SerializeField] private float moveSpeed = 5f;           // 移動速度
    [SerializeField] private float rotationSpeed = 3f;       // 旋回速度

    [Header("WayPoint（巡回）設定")]
    [SerializeField] private List<Transform> waypoints = new List<Transform>();
    [SerializeField] private PatrolType patrolType = PatrolType.Loop;
    [SerializeField] private float waypointArriveDistance = 2f;
    [SerializeField] private float waypointWaitTime = 2f;

    [Header("索敵・前方視界設定")]
    [SerializeField] private float sightDistance = 25f;     // 正面視界の届く距離
    [SerializeField] private float sightAngle = 120f;       // 視野角（例: 120度なら正面から左右60度ずつ）

    [Header("射撃パラメータ")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 35f;
    [SerializeField] private float attackRange = 20f;       // 攻撃に移行する距離
    [SerializeField] private float fireRate = 1.2f;
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
        // 1. ターゲットが正面視界に入っているかチェック（未検知時のみ）
        if (!isPlayerDetected && IsTargetInSight())
        {
            isPlayerDetected = true; // 視界に入ったので検知！
        }

        // --- 行動分岐 ---
        if (isPlayerDetected && target != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, target.position);

            // 攻撃距離内 ＆ 視線が通っていれば旋回して砲撃
            if (distanceToPlayer <= attackRange && HasLineOfSight())
            {
                LookAtPosition(target.position);

                if (Time.time >= nextFireTime)
                {
                    nextFireTime = Time.time + fireRate;
                    ExecuteShoot();
                }
            }
            else
            {
                // 検知済みだが攻撃距離外：プレイヤーの上空へ向けて移動
                Vector3 targetPosition = target.position + Vector3.up * flyHeight;
                MoveToPosition(targetPosition);
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
    /// 目標位置へ高度を維持して移動・旋回
    /// </summary>
    private void MoveToPosition(Vector3 destination)
    {
        // 高度を補正した目標地点
        Vector3 targetPosWithHeight = new Vector3(destination.x, destination.y, destination.z);

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosWithHeight,
            moveSpeed * Time.deltaTime
        );

        LookAtPosition(destination);
    }

    /// <summary>
    /// 指定位置の方向へ軸補正を考慮して旋回
    /// </summary>
    private void LookAtPosition(Vector3 lookPosition)
    {
        Vector3 dir = (lookPosition - transform.position).normalized;
        if (dir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);

            // モデル正面軸補正
            switch (modelForwardAxis)
            {
                case ModelForwardAxis.Right: targetRot *= Quaternion.Euler(0, -90, 0); break;
                case ModelForwardAxis.Left: targetRot *= Quaternion.Euler(0, 90, 0); break;
                case ModelForwardAxis.Back: targetRot *= Quaternion.Euler(0, 180, 0); break;
            }

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
        }
    }

    /// <summary>
    /// FixedTurret/EnemyTankControllerを踏襲した視界判定（距離・モデル正面軸の視野角・壁遮蔽）
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
        forward.y = 0f;
        forward.Normalize();

        float angle = Vector3.Angle(forward, horizontalDir);
        if (angle > (sightAngle / 2f)) return false;

        // 3. 壁（障害物）の遮蔽チェック (Raycast)
        return HasLineOfSight();
    }

    /// <summary>
    /// 視線（Raycast）遮蔽チェック
    /// </summary>
    private bool HasLineOfSight()
    {
        if (target == null) return false;

        Vector3 origin = muzzleTransform != null ? muzzleTransform.position : transform.position;
        Vector3 dir = (target.position - origin).normalized;
        float dist = Vector3.Distance(origin, target.position);

        if (Physics.Raycast(origin, dir, out RaycastHit hit, dist, obstacleLayer))
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

        // Waypointの位置に指定飛行高度（flyHeight）を加算して飛行
        Vector3 destination = currentPoint.position + Vector3.up * flyHeight;
        MoveToPosition(destination);

        if (Vector3.Distance(transform.position, destination) <= waypointArriveDistance)
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

    private void ExecuteShoot()
    {
        if (muzzleTransform == null || bulletPrefab == null || target == null) return;

        Vector3 shootDirection = (target.position - muzzleTransform.position).normalized;
        Quaternion bulletRotation = Quaternion.LookRotation(shootDirection);

        GameObject bulletObj = Instantiate(bulletPrefab, muzzleTransform.position, bulletRotation);

        if (bulletObj.TryGetComponent(out SharedBullet bullet))
        {
            bullet.Launch(shootDirection, bulletSpeed, statsData.attackPower, gameObject.tag);
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
                    Vector3 wpPos = waypoints[i].position + Vector3.up * flyHeight;
                    Gizmos.DrawWireSphere(wpPos, 0.5f);

                    int next = (i + 1) % waypoints.Count;
                    if (waypoints[next] != null)
                    {
                        Vector3 nextWpPos = waypoints[next].position + Vector3.up * flyHeight;
                        Gizmos.DrawLine(wpPos, nextWpPos);
                    }
                }
            }
        }
    }
}