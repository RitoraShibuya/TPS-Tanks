using UnityEngine;

/// <summary>
/// 移動しない固定砲台のAIスクリプト
/// </summary>
public class FixedTurret : EnemyBase
{
    [Header("ステータス設定")]
    [SerializeField] private float maxHp = 80f;
    [SerializeField] private float sightDistance = 20f;         // 索敵距離
    [SerializeField] private float sightAngle = 120f;           // 視野角
    [SerializeField] private float turretRotationSpeed = 90f;   // 砲塔旋回速度
    [SerializeField] private float pitchRotationSpeed = 60f;    // 砲身上下速度
    [SerializeField] private float attackCooldown = 1.5f;       // 発射間隔（秒）
    [SerializeField] private float attackPower = 15f;           // 攻撃力

    [Header("パーツ参照")]
    [SerializeField] private Transform turretTransform;         // 砲塔（水平旋回）
    [SerializeField] private Transform gunBarrelTransform;      // 砲身（上下旋回）
    [SerializeField] private Transform muzzleTransform;         // 砲口（発射位置）

    [Header("弾の発射設定")]
    [SerializeField] private GameObject bulletPrefab;           // 敵用弾プレハブ
    [SerializeField] private float bulletSpeed = 20f;           // 弾速

    [Header("砲身上下制限")]
    [SerializeField] private float minGunPitch = -10f;
    [SerializeField] private float maxGunPitch = 30f;

    [Header("モデルの正面軸補正設定")]
    [Tooltip("砲口（モデルの正面）が向いているローカル軸を指定")]
    [SerializeField] private ModelForwardAxis modelForwardAxis = ModelForwardAxis.Left;

    public enum ModelForwardAxis { Forward, Right, Left, Back }

    private float lastAttackTime = 0f;

    protected override void Awake()
    {
        base.Awake();
        currentHealth = maxHp;
    }

    private void Update()
    {
        // ターゲットが視界内に入っているかチェック
        if (target != null && IsTargetInSight())
        {
            HandleAimAndShoot();
        }
    }

    /// <summary>
    /// ターゲットが視界（距離・視野角）内に入っているか判定
    /// </summary>
    private bool IsTargetInSight()
    {
        if (target == null) return false;

        Vector3 direction = target.position - transform.position;
        direction.y = 0f; // 水平距離で判定

        float distance = direction.magnitude;
        if (distance > sightDistance) return false;

        direction.Normalize();
        Vector3 forward = GetModelForward(transform);

        float angle = Vector3.Angle(forward, direction);
        return angle <= (sightAngle / 2f);
    }

    /// <summary>
    /// 照準と射撃制御
    /// </summary>
    private void HandleAimAndShoot()
    {
        if (target == null) return;

        // 1. 水平砲塔（Turret）の旋回
        if (turretTransform != null)
        {
            Vector3 turretTargetDir = target.position - turretTransform.position;
            turretTargetDir.y = 0f;

            if (turretTargetDir != Vector3.zero)
            {
                Quaternion targetTurretRot = Quaternion.LookRotation(turretTargetDir);

                // 軸の向きに合わせて回転補正値を加算
                switch (modelForwardAxis)
                {
                    case ModelForwardAxis.Right:
                        targetTurretRot *= Quaternion.Euler(0, -90, 0);
                        break;
                    case ModelForwardAxis.Left:
                        targetTurretRot *= Quaternion.Euler(0, 90, 0);
                        break;
                    case ModelForwardAxis.Back:
                        targetTurretRot *= Quaternion.Euler(0, 180, 0);
                        break;
                }

                turretTransform.rotation = Quaternion.RotateTowards(
                    turretTransform.rotation,
                    targetTurretRot,
                    turretRotationSpeed * Time.deltaTime
                );
            }
        }

        // 2. 上下砲身（GunBarrel）の旋回
        if (gunBarrelTransform != null && turretTransform != null)
        {
            Vector3 localTargetPos = turretTransform.InverseTransformPoint(target.position);

            // X軸方向（Left/Right）が正面の場合のローカル角度計算
            float targetAngle = 0f;
            if (modelForwardAxis == ModelForwardAxis.Left || modelForwardAxis == ModelForwardAxis.Right)
            {
                targetAngle = -Mathf.Atan2(localTargetPos.y, Mathf.Abs(localTargetPos.x)) * Mathf.Rad2Deg;
            }
            else
            {
                targetAngle = -Mathf.Atan2(localTargetPos.y, localTargetPos.z) * Mathf.Rad2Deg;
            }

            targetAngle = Mathf.Clamp(targetAngle, minGunPitch, maxGunPitch);

            Quaternion targetBarrelRot = Quaternion.Euler(targetAngle, 0f, 0f);
            gunBarrelTransform.localRotation = Quaternion.RotateTowards(
                gunBarrelTransform.localRotation,
                targetBarrelRot,
                pitchRotationSpeed * Time.deltaTime
            );
        }

        // 3. 射撃判定
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;
            ExecuteShoot();
        }
    }

    /// <summary>
    /// 弾を発射する
    /// </summary>
    private void ExecuteShoot()
    {
        if (muzzleTransform == null || bulletPrefab == null || target == null) return;

        Vector3 shootDirection = (target.position - muzzleTransform.position).normalized;
        Quaternion bulletRotation = Quaternion.LookRotation(shootDirection);

        GameObject bulletObj = Instantiate(
            bulletPrefab,
            muzzleTransform.position,
            bulletRotation
        );

        if (bulletObj.TryGetComponent(out EnemyBullet bullet))
        {
            bullet.Launch(shootDirection, bulletSpeed, attackPower);
        }
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

    private void OnDrawGizmosSelected()
    {
        // 視界範囲（黄色）を描画
        Gizmos.color = Color.yellow;
        Vector3 forward = GetModelForward(transform);
        Vector3 leftRay = Quaternion.Euler(0, -sightAngle / 2f, 0) * forward;
        Vector3 rightRay = Quaternion.Euler(0, sightAngle / 2f, 0) * forward;

        Gizmos.DrawRay(transform.position, leftRay * sightDistance);
        Gizmos.DrawRay(transform.position, rightRay * sightDistance);
    }
}