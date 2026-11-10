using UnityEngine;

/// <summary>
/// 地上を走る通常の敵戦車
/// WayPoint を巡回し、プレイヤーを見つけたら追跡・射撃する
/// </summary>
public class EnemyTankController : PatrolEnemyBase
{
    [Header("パーツ設定")]
    [SerializeField] private Transform turretTransform;
    [Tooltip("※現在は上下照準を行っていないため未使用")]
    [SerializeField] private Transform barrelTransform;

    [Header("移動・旋回ステータス")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float bodyRotationSpeed = 120f;
    [SerializeField] private float turretRotationSpeed = 90f;

    [Header("音声設定")]
    [SerializeField] private float movementSoundThreshold = 0.2f;
    [SerializeField] private float turretSoundAngleThreshold = 0.5f;

    // 音声再生状態
    private bool isEngineSoundPlaying = false;
    private bool isTurretSoundPlaying = false;

    protected override void Awake()
    {
        base.Awake();

        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.angularSpeed = bodyRotationSpeed;
            agent.stoppingDistance = 0.5f;

            // 自前でモデルの軸補正回転を行う
            agent.updateRotation = false;
        }
    }

    private void Update()
    {
        if (!IsAgentReady)
        {
            StopAllLoopSounds();
            return;
        }

        // 本体の回転
        UpdateBodyRotation();

        // ターゲットの索敵
        if (!isPlayerDetected && IsTargetInSight())
        {
            isPlayerDetected = true;
        }

        // 今フレームの砲塔旋回状態
        bool turretIsRotating = false;

        // 行動分岐
        if (isPlayerDetected && target != null)
        {
            float distance = Vector3.Distance(transform.position, target.position);

            turretIsRotating = AimTurret();

            if (distance <= attackRange && HasLineOfSight())
            {
                agent.isStopped = true;
                TryFire();
            }
            else
            {
                agent.isStopped = false;
                agent.SetDestination(target.position);
            }
        }
        else if (HasValidWaypoints())
        {
            PatrolWaypoints();
        }
        else
        {
            agent.isStopped = true;
        }

        UpdateEngineSound();
        UpdateTurretSound(turretIsRotating);
    }

    /// <summary>
    /// エンジン音の再生・停止
    /// </summary>
    private void UpdateEngineSound()
    {
        bool isMoving =
            !agent.isStopped &&
            agent.velocity.sqrMagnitude > movementSoundThreshold * movementSoundThreshold;

        if (isMoving && !isEngineSoundPlaying)
        {
            if (SSoundManager.Instance != null)
            {
                SSoundManager.Instance.PlaySE_TankMoveEngine_Start();
                isEngineSoundPlaying = true;
            }
        }
        else if (!isMoving && isEngineSoundPlaying)
        {
            if (SSoundManager.Instance != null)
            {
                SSoundManager.Instance.PlaySE_TankMoveEngine_End();
            }

            isEngineSoundPlaying = false;
        }
    }

    /// <summary>
    /// 砲塔旋回音の再生・停止
    /// </summary>
    private void UpdateTurretSound(bool isRotating)
    {
        if (isRotating && !isTurretSoundPlaying)
        {
            if (SSoundManager.Instance != null)
            {
                SSoundManager.Instance.PlaySE_TurretRotate_Start();
                isTurretSoundPlaying = true;
            }
        }
        else if (!isRotating && isTurretSoundPlaying)
        {
            if (SSoundManager.Instance != null)
            {
                SSoundManager.Instance.PlaySE_TurretRotate_End();
            }

            isTurretSoundPlaying = false;
        }
    }

    /// <summary>
    /// すべてのループ音を停止
    /// </summary>
    private void StopAllLoopSounds()
    {
        if (SSoundManager.Instance != null)
        {
            if (isEngineSoundPlaying)
            {
                SSoundManager.Instance.PlaySE_TankMoveEngine_End();
            }

            if (isTurretSoundPlaying)
            {
                SSoundManager.Instance.PlaySE_TurretRotate_End();
            }
        }

        isEngineSoundPlaying = false;
        isTurretSoundPlaying = false;
    }

    private void OnDisable()
    {
        StopAllLoopSounds();
    }

    /// <summary>
    /// NavMeshAgentの進行方向に合わせて回転
    /// </summary>
    private void UpdateBodyRotation()
    {
        Vector3 moveDir = agent.desiredVelocity;
        moveDir.y = 0f;

        if (moveDir.sqrMagnitude <= 0.1f) return;

        Quaternion targetRot = Quaternion.LookRotation(moveDir.normalized) * GetAxisCorrection();

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRot,
            bodyRotationSpeed * Time.deltaTime
        );
    }

    /// <summary>
    /// 砲塔をターゲットに向ける
    /// 戻り値：現在旋回中かどうか
    /// </summary>
    private bool AimTurret()
    {
        if (turretTransform == null || target == null) return false;

        Vector3 targetDir = target.position - turretTransform.position;
        targetDir.y = 0f;

        if (targetDir.sqrMagnitude < 0.0001f) return false;

        Quaternion lookRot = Quaternion.LookRotation(targetDir) * GetAxisCorrection();

        float angle = Quaternion.Angle(turretTransform.rotation, lookRot);

        // Inspector の turretRotationSpeed（度/秒）で旋回する
        turretTransform.rotation = Quaternion.RotateTowards(
            turretTransform.rotation,
            lookRot,
            turretRotationSpeed * Time.deltaTime
        );

        return angle > turretSoundAngleThreshold;
    }
}
