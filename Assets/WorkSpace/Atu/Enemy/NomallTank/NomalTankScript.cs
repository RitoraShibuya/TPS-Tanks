using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyTankController : EnemyBase
{
    public enum ModelForwardAxis { Forward, Back, Left, Right }

    [Header("砲塔・発射口パーツ")]
    [SerializeField] private Transform turretTransform;
    [SerializeField] private Transform barrelTransform;
    [SerializeField] private Transform muzzleTransform;

    [Header("軸オフセット設定")]
    [SerializeField] private ModelForwardAxis modelForwardAxis = ModelForwardAxis.Forward;

    [Header("射撃パラメータ")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 40f;
    [SerializeField] private float attackRange = 20f;
    [SerializeField] private float fireRate = 2f;
    [SerializeField] private LayerMask obstacleLayer;

    private NavMeshAgent agent;
    private float nextFireTime;

    protected override void Awake()
    {
        base.Awake();
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        if (target == null) return;

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

    private void AimTurret()
    {
        if (turretTransform == null) return;

        Vector3 targetDir = (target.position - turretTransform.position).normalized;
        targetDir.y = 0;

        if (targetDir != Vector3.zero)
        {
            Quaternion lookRot = Quaternion.LookRotation(targetDir);
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
}