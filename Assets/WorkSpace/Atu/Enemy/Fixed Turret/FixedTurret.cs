using UnityEngine;

/// <summary>
/// 移動しない固定砲台のAIスクリプト
/// </summary>
public class FixedTurret : EnemyBase
{
    [Header("パーツ設定")]
    [SerializeField] private Transform turretTransform;         // 砲塔（水平旋回）
    [SerializeField] private Transform barrelTransform;         // 砲身（上下旋回）

    [Header("旋回ステータス")]
    [SerializeField] private float turretRotationSpeed = 90f;   // 砲塔旋回速度
    [SerializeField] private float pitchRotationSpeed = 60f;    // 砲身上下速度
    [SerializeField] private float minGunPitch = -10f;          // 砲身上限下限
    [SerializeField] private float maxGunPitch = 30f;

    [Tooltip("照準がこの角度(度)以内に合ったら発射する")]
    [SerializeField] private float fireAngleTolerance = 20f;

    // 旋回音（SSoundManager のループSE）を鳴らしているか
    private bool isTurretSoundPlaying = false;

    // ※Awake を書く場合は必ず protected override にして base.Awake() を呼ぶこと
    //   (呼ばないと HP が 0 のままになり、倒せなくなる)

    private void Update()
    {
        if (target == null || !IsTargetInSight())
        {
            ControlTurretSound(false);
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, target.position);

        // 攻撃距離内の場合のみ照準＆射撃
        if (distanceToPlayer > attackRange)
        {
            ControlTurretSound(false);
            return;
        }

        AimTurret();

        // 照準が概ね合っていたら発射（発射音は TryFire 内で再生）
        if (IsAimingAtTarget())
        {
            TryFire();
        }
    }

    /// <summary>
    /// 砲塔・砲身の旋回処理（モデル軸補正対応）
    /// </summary>
    private void AimTurret()
    {
        bool isTurretRotating = false;

        // 1. 砲塔（Turret）の水平旋回（Yaw）
        if (turretTransform != null)
        {
            Vector3 targetDir = target.position - turretTransform.position;
            targetDir.y = 0f;

            if (targetDir.sqrMagnitude > 0.0001f)
            {
                Quaternion lookRot = Quaternion.LookRotation(targetDir) * GetAxisCorrection();

                if (Quaternion.Angle(turretTransform.rotation, lookRot) > 0.1f)
                {
                    isTurretRotating = true;
                }

                turretTransform.rotation = Quaternion.RotateTowards(
                    turretTransform.rotation,
                    lookRot,
                    turretRotationSpeed * Time.deltaTime
                );
            }
        }

        // 2. 砲身（Barrel）の上下俯仰（Pitch）
        if (barrelTransform != null && turretTransform != null)
        {
            Vector3 localTargetPos = turretTransform.InverseTransformPoint(target.position);

            float targetAngle;
            if (modelForwardAxis == ModelForwardAxis.Left || modelForwardAxis == ModelForwardAxis.Right)
            {
                targetAngle = -Mathf.Atan2(localTargetPos.y, Mathf.Abs(localTargetPos.x)) * Mathf.Rad2Deg;
            }
            else if (modelForwardAxis == ModelForwardAxis.Back)
            {
                targetAngle = -Mathf.Atan2(localTargetPos.y, -localTargetPos.z) * Mathf.Rad2Deg;
            }
            else
            {
                targetAngle = -Mathf.Atan2(localTargetPos.y, localTargetPos.z) * Mathf.Rad2Deg;
            }

            targetAngle = Mathf.Clamp(targetAngle, minGunPitch, maxGunPitch);

            Quaternion targetBarrelRot = Quaternion.Euler(targetAngle, 0f, 0f);

            if (Quaternion.Angle(barrelTransform.localRotation, targetBarrelRot) > 0.1f)
            {
                isTurretRotating = true;
            }

            barrelTransform.localRotation = Quaternion.RotateTowards(
                barrelTransform.localRotation,
                targetBarrelRot,
                pitchRotationSpeed * Time.deltaTime
            );
        }

        // 旋回中のみサウンドを再生
        ControlTurretSound(isTurretRotating);
    }

    /// <summary>
    /// 砲塔旋回音の再生制御（SSoundManager の旋回音ループを使う）
    /// </summary>
    private void ControlTurretSound(bool isRotating)
    {
        if (isRotating == isTurretSoundPlaying) return;

        if (SSoundManager.Instance == null)
        {
            isTurretSoundPlaying = false;
            return;
        }

        if (isRotating) SSoundManager.Instance.PlaySE_TurretRotate_Start();
        else SSoundManager.Instance.PlaySE_TurretRotate_End();

        isTurretSoundPlaying = isRotating;
    }

    /// <summary>
    /// 照準がターゲットを捉えているか判定（水平方向の誤差 fireAngleTolerance 度以内）
    /// </summary>
    private bool IsAimingAtTarget()
    {
        Transform checkTransform = muzzleTransform != null ? muzzleTransform :
                                   (turretTransform != null ? turretTransform : transform);

        Vector3 direction = target.position - checkTransform.position;
        direction.y = 0f;

        Vector3 forward = GetModelForward(checkTransform);
        forward.y = 0f;

        if (direction.sqrMagnitude < 0.0001f || forward.sqrMagnitude < 0.0001f) return true;

        return Vector3.Angle(forward, direction) <= fireAngleTolerance;
    }

    private void OnDisable()
    {
        // 破壊SEは EnemyBase.Die() で再生する（シーン切り替え時に鳴らないように OnDestroy では鳴らさない）
        ControlTurretSound(false);
    }
}
