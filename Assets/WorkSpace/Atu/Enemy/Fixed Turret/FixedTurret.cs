using UnityEngine;

/// <summary>
/// 移動しない固定砲台のAIスクリプト
/// </summary>
public class FixedTurret : EnemyBase
{
    public enum ModelForwardAxis { Forward, Right, Left, Back }

    [Header("パーツ設定")]
    [SerializeField] private Transform turretTransform;         // 砲塔（水平旋回）
    [SerializeField] private Transform barrelTransform;         // 砲身（上下旋回）
    [SerializeField] private Transform muzzleTransform;         // 砲口（発射位置）

    [Header("モデルの正面軸補正設定")]
    [Tooltip("砲台（モデルの正面）が向いているローカル軸を指定")]
    [SerializeField] private ModelForwardAxis modelForwardAxis = ModelForwardAxis.Forward;

    [Header("旋回ステータス")]
    [SerializeField] private float turretRotationSpeed = 90f;   // 砲塔旋回速度
    [SerializeField] private float pitchRotationSpeed = 60f;    // 砲身上下速度
    [SerializeField] private float minGunPitch = -10f;          // 砲身上限下限
    [SerializeField] private float maxGunPitch = 30f;

    [Header("索敵・前方視界設定")]
    [SerializeField] private float sightDistance = 20f;     // 正面視界の届く距離
    [SerializeField] private float sightAngle = 120f;       // 視野角（例: 120度なら正面から左右60度ずつ）

    [Header("射撃パラメータ")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 30f;
    [SerializeField] private float attackRange = 20f;       // 射撃を開始する距離
    [SerializeField] private float fireRate = 1.5f;         // 発射間隔（秒）
    [SerializeField] private LayerMask obstacleLayer = ~0;

    private float nextFireTime;

    private void Update()
    {
        if (target == null) return;

        // 視界（距離・モデル正面軸の視野角・壁遮蔽）内に入っているか判定
        if (IsTargetInSight())
        {
            float distanceToPlayer = Vector3.Distance(transform.position, target.position);

            // 攻撃距離内の場合のみ照準＆射撃
            if (distanceToPlayer <= attackRange)
            {
                AimTurret();

                if (Time.time >= nextFireTime)
                {
                    if (IsAimingAtTarget()) // 照準が概ね合っていたら発射
                    {
                        nextFireTime = Time.time + fireRate;
                        ExecuteShoot();
                    }
                }
            }
        }
    }

    /// <summary>
    /// 全エネミー共通の視界判定（距離・モデル正面軸の視野角・壁遮蔽）
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

        Vector3 rayStartPos = muzzleTransform != null ? muzzleTransform.position : transform.position + Vector3.up * 1f;
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

    /// <summary>
    /// 砲塔・砲身の旋回処理（モデル軸補正対応）
    /// </summary>
    private void AimTurret()
    {
        if (target == null) return;

        // 1. 砲塔（Turret）の水平旋回（Yaw）
        if (turretTransform != null)
        {
            Vector3 targetDir = (target.position - turretTransform.position).normalized;
            targetDir.y = 0f;

            if (targetDir != Vector3.zero)
            {
                Quaternion lookRot = Quaternion.LookRotation(targetDir);

                // モデル軸補正
                switch (modelForwardAxis)
                {
                    case ModelForwardAxis.Right: lookRot *= Quaternion.Euler(0, -90, 0); break;
                    case ModelForwardAxis.Left: lookRot *= Quaternion.Euler(0, 90, 0); break;
                    case ModelForwardAxis.Back: lookRot *= Quaternion.Euler(0, 180, 0); break;
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

            float targetAngle = 0f;
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
            barrelTransform.localRotation = Quaternion.RotateTowards(
                barrelTransform.localRotation,
                targetBarrelRot,
                pitchRotationSpeed * Time.deltaTime
            );
        }
    }

    /// <summary>
    /// 照準がターゲットを捉えているか判定（誤差20度以内）
    /// </summary>
    private bool IsAimingAtTarget()
    {
        if (target == null) return false;

        Transform checkTransform = muzzleTransform != null ? muzzleTransform :
                                  (turretTransform != null ? turretTransform : transform);

        Vector3 direction = (target.position - checkTransform.position);
        direction.y = 0f;
        direction.Normalize();

        Vector3 forward = GetModelForward(checkTransform);
        forward.y = 0f;
        forward.Normalize();

        float angle = Vector3.Angle(forward, direction);
        return angle <= 20f;
    }

    /// <summary>
    /// 指定トランスフォームの実際の見た目の正面方向を取得
    /// </summary>
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

    /// <summary>
    /// 弾の発射実行（SharedBullet対応）
    /// </summary>
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
    }
}