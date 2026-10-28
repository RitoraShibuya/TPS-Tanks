using System.Collections.Generic;
using UnityEngine;

public class FlyingTankAI : EnemyBase
{
    public enum ModelForwardAxis { Forward, Right, Left, Back }
    public enum PatrolType { Loop, PingPong, Random }

    [Header("パーツ設定")]
    [SerializeField] private Transform barrelTransform;     // 上下に回転させたい砲身パーツ
    [SerializeField] private Transform muzzleTransform;     // 発射位置

    [Header("プロペラ・ローター設定")]
    [Tooltip("回転させたいプロペラやローターのTransformを指定")]
    [SerializeField] private List<Transform> propellerTransforms = new List<Transform>();
    [Tooltip("プロペラの回転軸（標準はY軸）")]
    [SerializeField] private Vector3 propellerRotateAxis = Vector3.up;
    [Tooltip("プロペラの回転速度")]
    [SerializeField] private float propellerSpeed = 1000f;

    [Header("モデルの正面軸補正設定")]
    [Tooltip("飛行体の正面（カメラ・モデルの向いている方向）のローカル軸を指定")]
    [SerializeField] private ModelForwardAxis modelForwardAxis = ModelForwardAxis.Forward;

    [Header("飛行・移動設定")]
    [SerializeField] private float flyHeight = 8f;           // 飛行高度
    [SerializeField] private float moveSpeed = 5f;           // 移動速度
    [SerializeField] private float rotationSpeed = 3f;       // 旋回速度

    [Header("機体（本体）俯仰角制限")]
    [Tooltip("ヘリ本体が前におじぎできる最大角度（例: 20度）")]
    [SerializeField] private float maxBodyPitchAngle = 20f;
    [SerializeField] private float minBodyPitchAngle = -15f;

    [Header("砲身（Barrel）仰俯角設定")]
    [SerializeField] private float barrelRotationSpeed = 90f; // 砲身の上下旋回速度
    [SerializeField] private float minBarrelPitch = -10f;     // 砲身の上限角度（上向き）
    [Tooltip("砲身が下に向ける最大角度（例: 60度に設定すると、本体20度+砲身60度＝最大80度まで下を向く）")]
    [SerializeField] private float maxBarrelPitch = 60f;      // ★角度制限を緩和

    [Header("WayPoint（巡回）設定")]
    [SerializeField] private List<Transform> waypoints = new List<Transform>();
    [SerializeField] private PatrolType patrolType = PatrolType.Loop;
    [SerializeField] private float waypointArriveDistance = 2f;
    [SerializeField] private float waypointWaitTime = 2f;

    [Header("索敵・前方視界設定")]
    [SerializeField] private float sightDistance = 25f;
    [SerializeField] private float sightAngle = 120f;

    [Header("射撃パラメータ")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 35f;
    [SerializeField] private float attackRange = 25f;       // ★射撃範囲を広めに設定推奨
    [SerializeField] private float fireRate = 1.2f;

    [Header("遮蔽判定設定")]
    [Tooltip("壁や障害物のみ（Default, Environment等）を選択。EverythingやEnemy/Playerは含めないでください。")]
    [SerializeField] private LayerMask obstacleLayer;

    private float nextFireTime;
    private bool isTargetInAngleLimit = true;

    // WayPoint巡回用
    private int currentWaypointIndex = 0;
    private bool isPatrolReverse = false;
    private float waitTimer = 0f;
    private bool isWaiting = false;

    // 検知フラグ
    private bool isPlayerDetected = false;

    protected override void Awake()
    {
        base.Awake();

        if (obstacleLayer.value == 0)
        {
            obstacleLayer = LayerMask.GetMask("Default");
        }
    }

    private void Update()
    {
        RotatePropellers();

        // 1. ターゲットが正面視界に入っているかチェック
        if (!isPlayerDetected && IsTargetInSight())
        {
            isPlayerDetected = true;
            Debug.Log("[FlyingTankAI] ターゲットを捕捉しました！");
        }

        // --- 行動分岐 ---
        if (isPlayerDetected && target != null)
        {
            // ★ 高度（Y軸）を除外した「水平距離」で計算
            Vector3 selfPosFlat = new Vector3(transform.position.x, 0f, transform.position.z);
            Vector3 targetPosFlat = new Vector3(target.position.x, 0f, target.position.z);
            float horizontalDistance = Vector3.Distance(selfPosFlat, targetPosFlat);

            // ターゲット捕捉時は【常に照準（AimAtTarget）を合わせ続ける】
            AimAtTarget();

            // 攻撃範囲外ならターゲット上空へ移動（照準しながら移動）
            if (horizontalDistance > attackRange)
            {
                Vector3 targetPosition = target.position + Vector3.up * flyHeight;
                MoveToPositionOnly(targetPosition);
            }

            // 攻撃範囲内かつ視線（Raycast）が通っているなら射撃チェック
            if (horizontalDistance <= attackRange && HasLineOfSight())
            {
                if (isTargetInAngleLimit && Time.time >= nextFireTime)
                {
                    nextFireTime = Time.time + fireRate;
                    ExecuteShoot();
                }
            }
        }
        else
        {
            if (HasValidWaypoints())
            {
                PatrolWaypoints();
            }
        }
    }

    /// <summary>
    /// 移動処理専用（向き変更はAimAtTargetに任せる）
    /// </summary>
    private void MoveToPositionOnly(Vector3 destination)
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            destination,
            moveSpeed * Time.deltaTime
        );
    }

    /// <summary>
    /// 巡回用移動（進行方向に向く）
    /// </summary>
    private void MoveToPatrolPosition(Vector3 destination)
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            destination,
            moveSpeed * Time.deltaTime
        );

        if (barrelTransform != null)
        {
            barrelTransform.localRotation = Quaternion.RotateTowards(
                barrelTransform.localRotation,
                Quaternion.identity,
                barrelRotationSpeed * Time.deltaTime
            );
        }

        Vector3 moveDirection = (destination - transform.position).normalized;
        moveDirection.y = 0f;

        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDirection);

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
    /// 機体本体（前傾Pitch）と砲身（Barrel）の角度制限付き連動照準
    /// </summary>
    private void AimAtTarget()
    {
        if (target == null) return;

        // --- 1. 機体本体（親）の旋回（Yaw）＆ 前傾（Pitch） ---
        Vector3 dirToTarget = (target.position - transform.position).normalized;
        Vector3 horizontalDir = dirToTarget;
        horizontalDir.y = 0f;

        if (horizontalDir != Vector3.zero)
        {
            Quaternion yawRotation = Quaternion.LookRotation(horizontalDir.normalized);

            float rawPitch = -Mathf.Atan2(dirToTarget.y, horizontalDir.magnitude) * Mathf.Rad2Deg;
            float clampedPitch = Mathf.Clamp(rawPitch, minBodyPitchAngle, maxBodyPitchAngle);

            Quaternion pitchRotation = Quaternion.Euler(clampedPitch, 0f, 0f);

            Quaternion axisCorrection = Quaternion.identity;
            switch (modelForwardAxis)
            {
                case ModelForwardAxis.Right: axisCorrection = Quaternion.Euler(0, -90, 0); break;
                case ModelForwardAxis.Left: axisCorrection = Quaternion.Euler(0, 90, 0); break;
                case ModelForwardAxis.Back: axisCorrection = Quaternion.Euler(0, 180, 0); break;
            }

            Quaternion targetBodyRot = yawRotation * pitchRotation * axisCorrection;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetBodyRot, Time.deltaTime * rotationSpeed);
        }

        // --- 2. 砲身（Barrel）の上下照準（角度制限） ---
        if (barrelTransform != null)
        {
            Vector3 aimPos = muzzleTransform != null ? muzzleTransform.position : barrelTransform.position;
            Vector3 relativeTargetDir = (target.position - aimPos).normalized;

            Vector3 localDir = transform.InverseTransformDirection(relativeTargetDir);
            float targetBarrelPitch = -Mathf.Atan2(localDir.y, Mathf.Abs(localDir.z)) * Mathf.Rad2Deg;

            if (targetBarrelPitch > maxBarrelPitch || targetBarrelPitch < minBarrelPitch)
            {
                isTargetInAngleLimit = false;
            }
            else
            {
                isTargetInAngleLimit = true;
            }

            float clampedBarrelPitch = Mathf.Clamp(targetBarrelPitch, minBarrelPitch, maxBarrelPitch);

            Quaternion targetBarrelRot = Quaternion.Euler(clampedBarrelPitch, 0f, 0f);
            barrelTransform.localRotation = Quaternion.RotateTowards(
                barrelTransform.localRotation,
                targetBarrelRot,
                barrelRotationSpeed * Time.deltaTime
            );
        }
        else
        {
            isTargetInAngleLimit = true;
        }
    }

    private void RotatePropellers()
    {
        if (propellerTransforms == null || propellerTransforms.Count == 0) return;

        float rotateAmount = propellerSpeed * Time.deltaTime;
        foreach (var prop in propellerTransforms)
        {
            if (prop != null)
            {
                prop.Rotate(propellerRotateAxis * rotateAmount, Space.Self);
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
        forward.y = 0f;
        forward.Normalize();

        float angle = Vector3.Angle(forward, horizontalDir);
        if (angle > (sightAngle / 2f)) return false;

        return HasLineOfSight();
    }

    private bool HasLineOfSight()
    {
        if (target == null) return false;

        Vector3 origin = muzzleTransform != null ? muzzleTransform.position : transform.position;
        Vector3 targetCenterPos = target.position + Vector3.up * 0.5f;
        Vector3 dir = (targetCenterPos - origin).normalized;
        float dist = Vector3.Distance(origin, targetCenterPos);

        RaycastHit[] hits = Physics.RaycastAll(origin, dir, dist, obstacleLayer);

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

        Vector3 destination = currentPoint.position + Vector3.up * flyHeight;
        MoveToPatrolPosition(destination);

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

        Vector3 shootDirection = (GetTargetAimPoint() - muzzleTransform.position).normalized;
        Quaternion bulletRotation = Quaternion.LookRotation(shootDirection);

        GameObject bulletObj = Instantiate(bulletPrefab, muzzleTransform.position, bulletRotation);
        Debug.Log($"[FlyingTankAI] ★弾の生成に成功しました: {bulletObj.name}");

        if (bulletObj.TryGetComponent(out SharedBullet bullet))
        {
            bullet.Launch(shootDirection, bulletSpeed, gameObject);
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

        if (target != null)
        {
            Vector3 origin = muzzleTransform != null ? muzzleTransform.position : transform.position;
            Vector3 targetCenterPos = target.position + Vector3.up * 0.5f;
            Gizmos.color = HasLineOfSight() ? Color.green : Color.red;
            Gizmos.DrawLine(origin, targetCenterPos);
        }

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