using UnityEngine;

public class FixedTurret : EnemyBase
{
    public enum ModelForwardAxis { Forward, Back, Left, Right }

    [Header("砲身・モデルパーツ")]
    [SerializeField] private Transform turretBody;
    [SerializeField] private Transform barrel;
    [SerializeField] private Transform muzzleTransform;

    [Header("軸オフセット設定")]
    [SerializeField] private ModelForwardAxis modelForwardAxis = ModelForwardAxis.Forward;

    [Header("射撃・索敵設定")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 30f;
    [SerializeField] private float fireRate = 1.5f;
    [SerializeField] private float attackRange = 25f;
    [SerializeField] private float maxPitchAngle = 30f;
    [SerializeField] private LayerMask obstacleLayer;

    private float nextFireTime;

    private Vector3 GetAdjustedForward(Transform t)
    {
        switch (modelForwardAxis)
        {
            case ModelForwardAxis.Back: return -t.forward;
            case ModelForwardAxis.Left: return -t.right;
            case ModelForwardAxis.Right: return t.right;
            default: return t.forward;
        }
    }

    private void Update()
    {
        if (target == null) return;

        float distance = Vector3.Distance(transform.position, target.position);
        if (distance > attackRange) return;

        // 壁の遮蔽確認
        if (HasLineOfSight())
        {
            AimAtTarget();

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

    private void AimAtTarget()
    {
        if (turretBody != null)
        {
            Vector3 dir = (target.position - turretBody.position).normalized;
            dir.y = 0;
            if (dir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                turretBody.rotation = Quaternion.Slerp(turretBody.rotation, targetRot, Time.deltaTime * 5f);
            }
        }

        if (barrel != null)
        {
            Vector3 dir = (target.position - barrel.position).normalized;
            Quaternion targetRot = Quaternion.LookRotation(dir);
            Vector3 angles = targetRot.eulerAngles;
            float pitch = angles.x > 180 ? angles.x - 360 : angles.x;
            pitch = Mathf.Clamp(pitch, -maxPitchAngle, maxPitchAngle);
            barrel.localRotation = Quaternion.Euler(pitch, 0, 0);
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
}