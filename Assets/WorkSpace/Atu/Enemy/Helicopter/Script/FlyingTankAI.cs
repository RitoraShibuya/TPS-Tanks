using UnityEngine;

public class FlyingTankAI : EnemyBase
{
    [Header("飛行・追従設定")]
    [SerializeField] private float flyHeight = 8f;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float attackRange = 25f;

    [Header("砲身・発射口パーツ")]
    [SerializeField] private Transform muzzleTransform;

    [Header("射撃パラメータ")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 35f;
    [SerializeField] private float fireRate = 1.2f;
    [SerializeField] private LayerMask obstacleLayer;

    private float nextFireTime;

    private void Update()
    {
        if (target == null) return;

        // 目標位置（ターゲットの上空高度 flyHeight）
        Vector3 targetPosition = target.position + Vector3.up * flyHeight;
        float distance = Vector3.Distance(transform.position, targetPosition);

        // 追従移動
        if (distance > 3f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        }

        // ターゲット方向を向く
        Vector3 lookDir = (target.position - transform.position).normalized;
        if (lookDir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 3f);
        }

        // 攻撃可能距離 ＆ 壁の遮蔽がなければ射撃
        if (Vector3.Distance(transform.position, target.position) <= attackRange && HasLineOfSight())
        {
            if (Time.time >= nextFireTime)
            {
                nextFireTime = Time.time + fireRate;
                ExecuteShoot();
            }
        }
    }

    private bool HasLineOfSight()
    {
        Vector3 origin = muzzleTransform != null ? muzzleTransform.position : transform.position;
        Vector3 dir = (target.position - origin).normalized;
        float dist = Vector3.Distance(origin, target.position);

        if (Physics.Raycast(origin, dir, out RaycastHit hit, dist, obstacleLayer))
        {
            if (!hit.transform.IsChildOf(target) && !hit.transform.CompareTag(playerTag))
            {
                return false;
            }
        }
        return true;
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
}