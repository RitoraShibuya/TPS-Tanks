using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
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
    [SerializeField] private float sightDistance = 15f;
    [SerializeField] private float sightAngle = 120f;

    [Header("射撃パラメータ")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 40f;
    [SerializeField] private float attackRange = 20f;
    [SerializeField] private float fireRate = 2f;

    [Header("遮蔽判定設定")]
    [SerializeField] private LayerMask obstacleLayer;

    private NavMeshAgent agent;
    private float nextFireTime;

    private int currentWaypointIndex = 0;
    private bool isPatrolReverse = false;
    private float waitTimer = 0f;
    private bool isWaiting = false;

    private bool isPlayerDetected = false;

    protected override void Awake()
    {
        base.Awake();
        agent = GetComponent<NavMeshAgent>();

        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.angularSpeed = bodyRotationSpeed;
            agent.stoppingDistance = 0.5f;
            // 自前でモデルの軸補正回転を行うため、Agentの自動回転をオフにする
            agent.updateRotation = false;
        }
    }

    private void Update()
    {
        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        // NavMeshAgentの移動方向に合わせて、軸補正を行いながら滑らかに回転させる
        UpdateBodyRotation();

        // 1. ターゲットが視界に入っているかチェック
        if (!isPlayerDetected && IsTargetInSight())
        {
            isPlayerDetected = true;
        }

        // --- 行動分岐 ---
        if (isPlayerDetected && target != null)
        {
            float distance = Vector3.Distance(transform.position, target.position);

            if (distance <= attackRange && HasLineOfSight())
            {
                agent.isStopped = true;
                AimTurret();

                if (Time.time >= nextFireTime)
                {
                    nextFireTime = Time.time + fireRate;
                    ExecuteShoot();
                }
            }
            else
            {
                agent.isStopped = false;
                agent.SetDestination(target.position);
                AimTurret();
            }
        }
        else
        {
            if (HasValidWaypoints())
            {
                agent.isStopped = false;
                PatrolWaypoints();
            }
            else
            {
                agent.isStopped = true;
            }
        }
    }

    /// <summary>
    /// NavMeshAgentの進行方向（desiredVelocity）に対してモデル軸補正をかけて回転させる
    /// </summary>
    private void UpdateBodyRotation()
    {
        // 移動中の場合（移動速度ベクトルが存在する場合）
        if (agent.desiredVelocity.sqrMagnitude > 0.1f)
        {
            Vector3 moveDir = agent.desiredVelocity.normalized;
            moveDir.y = 0f;

            if (moveDir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir);

                // モデルの正面軸に合わせて回転補正
                switch (modelForwardAxis)
                {
                    case ModelForwardAxis.Right: targetRot *= Quaternion.Euler(0, -90, 0); break;
                    case ModelForwardAxis.Left: targetRot *= Quaternion.Euler(0, 90, 0); break;
                    case ModelForwardAxis.Back: targetRot *= Quaternion.Euler(0, 180, 0); break;
                }

                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRot,
                    bodyRotationSpeed * Time.deltaTime
                );
            }
        }
    }

    private bool IsTargetInSight()
    {
        if (target == null) return false;

        Vector3 directionToTarget = target.position - transform.position;
        float distance = directionToTarget.magnitude;
        if (distance > sightDistance) return false;

        Vector3 horizontalDir = directionToTarget;
        horizontalDir.y = 0f;
        horizontalDir.Normalize();

        Vector3 forward = GetModelForward(transform);

        float angle = Vector3.Angle(forward, horizontalDir);
        if (angle > (sightAngle / 2f)) return false;

        return HasLineOfSight();
    }

    private bool HasLineOfSight()
    {
        if (target == null) return false;

        Vector3 rayStartPos = muzzleTransform != null ? muzzleTransform.position : transform.position + Vector3.up * 1f;
        Vector3 targetCenterPos = target.position + Vector3.up * 0.5f;
        Vector3 rayDir = (targetCenterPos - rayStartPos).normalized;
        float dist = Vector3.Distance(rayStartPos, targetCenterPos);

        RaycastHit[] hits = Physics.RaycastAll(rayStartPos, rayDir, dist, obstacleLayer);

        foreach (var hit in hits)
        {
            if (hit.transform.IsChildOf(transform) || hit.transform == transform) continue;

            if (!hit.transform.IsChildOf(target) && hit.transform != target && !hit.transform.CompareTag(playerTag))
            {
                return false;
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

        agent.SetDestination(currentPoint.position);

        if (!agent.pathPending && agent.remainingDistance <= waypointArriveDistance)
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
        Gizmos.color = Color.yellow;
        Vector3 forward = GetModelForward(transform);
        Vector3 leftRay = Quaternion.Euler(0, -sightAngle / 2f, 0) * forward;
        Vector3 rightRay = Quaternion.Euler(0, sightAngle / 2f, 0) * forward;

        Gizmos.DrawRay(transform.position, leftRay * sightDistance);
        Gizmos.DrawRay(transform.position, rightRay * sightDistance);
        Gizmos.DrawRay(transform.position, forward * sightDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

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