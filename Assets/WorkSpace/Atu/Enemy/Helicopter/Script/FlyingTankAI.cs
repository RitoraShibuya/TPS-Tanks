using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 空中を飛ぶ敵（ヘリ）
/// WayPoint を巡回し、プレイヤーを見つけたら接近して上空から射撃する
/// </summary>
public class FlyingTankAI : PatrolEnemyBase
{
    [Header("パーツ設定")]
    [SerializeField] private Transform barrelTransform;     // 上下に回転させたい砲身パーツ

    [Header("プロペラ・ローター設定")]
    [Tooltip("回転させたいプロペラやローターのTransformを指定")]
    [SerializeField] private List<Transform> propellerTransforms = new List<Transform>();
    [Tooltip("プロペラの回転軸（標準はY軸）")]
    [SerializeField] private Vector3 propellerRotateAxis = Vector3.up;
    [Tooltip("プロペラの回転速度")]
    [SerializeField] private float propellerSpeed = 1000f;

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
    [SerializeField] private float maxBarrelPitch = 60f;

    private bool isTargetInAngleLimit = true;

    protected override float WaypointGizmoHeight => flyHeight;

    protected override void Awake()
    {
        base.Awake();

        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.angularSpeed = rotationSpeed * 50f;
            agent.baseOffset = flyHeight;
            agent.updateRotation = false;
            agent.stoppingDistance = 0.5f;
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
            Vector3 selfPosFlat = new Vector3(transform.position.x, 0f, transform.position.z);
            Vector3 targetPosFlat = new Vector3(target.position.x, 0f, target.position.z);
            float horizontalDistance = Vector3.Distance(selfPosFlat, targetPosFlat);

            // ターゲット捕捉時は常に照準を合わせ続ける
            AimAtTarget();

            if (IsAgentReady)
            {
                if (horizontalDistance > attackRange)
                {
                    // 攻撃範囲外ならターゲットへ向かって移動
                    agent.isStopped = false;
                    agent.SetDestination(target.position);
                }
                else
                {
                    // 攻撃範囲内なら停止
                    agent.isStopped = true;
                    if (agent.hasPath) agent.ResetPath();
                }
            }

            // 攻撃範囲内・視線が通っている・砲身の角度制限内なら射撃
            if (horizontalDistance <= attackRange && isTargetInAngleLimit && HasLineOfSight())
            {
                TryFire();
            }
        }
        else if (HasValidWaypoints())
        {
            PatrolWaypoints();
        }
    }

    /// <summary>
    /// 巡回時：砲身を水平に戻しつつ、進行方向へ機体を向ける
    /// </summary>
    protected override void MoveToPatrolPoint(Vector3 destination)
    {
        if (barrelTransform != null)
        {
            barrelTransform.localRotation = Quaternion.RotateTowards(
                barrelTransform.localRotation,
                Quaternion.identity,
                barrelRotationSpeed * Time.deltaTime
            );
        }

        base.MoveToPatrolPoint(destination);

        Vector3 moveDirection = agent.velocity;
        moveDirection.y = 0f;

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDirection.normalized) * GetAxisCorrection();
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
        }
    }

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

            Quaternion targetBodyRot = yawRotation * pitchRotation * GetAxisCorrection();
            transform.rotation = Quaternion.Slerp(transform.rotation, targetBodyRot, Time.deltaTime * rotationSpeed);
        }

        // --- 2. 砲身（Barrel）の上下照準（角度制限） ---
        if (barrelTransform != null)
        {
            Vector3 aimPos = muzzleTransform != null ? muzzleTransform.position : barrelTransform.position;
            Vector3 relativeTargetDir = (target.position - aimPos).normalized;

            Vector3 localDir = transform.InverseTransformDirection(relativeTargetDir);
            float targetBarrelPitch = -Mathf.Atan2(localDir.y, Mathf.Sqrt(localDir.x * localDir.x + localDir.z * localDir.z)) * Mathf.Rad2Deg;

            isTargetInAngleLimit = targetBarrelPitch >= minBarrelPitch && targetBarrelPitch <= maxBarrelPitch;

            float clampedBarrelPitch = Mathf.Clamp(targetBarrelPitch, minBarrelPitch, maxBarrelPitch);

            barrelTransform.localRotation = Quaternion.RotateTowards(
                barrelTransform.localRotation,
                Quaternion.Euler(clampedBarrelPitch, 0f, 0f),
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

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        if (target != null)
        {
            Vector3 origin = muzzleTransform != null ? muzzleTransform.position : transform.position;
            Gizmos.color = HasLineOfSight() ? Color.green : Color.red;
            Gizmos.DrawLine(origin, GetTargetAimPoint());
        }
    }
}
