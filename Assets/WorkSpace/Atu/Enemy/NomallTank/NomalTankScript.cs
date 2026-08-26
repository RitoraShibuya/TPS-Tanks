using UnityEngine;

public class EnemyTankController : MonoBehaviour
{
    // =========================================================
    // ステータス
    // =========================================================

    [Header("ステータス")]
    [SerializeField] private EnemyTankData statsData;


    // =========================================================
    // ターゲット
    // =========================================================

    [Header("ターゲット")]
    [SerializeField] private Transform target;

    [SerializeField] private string targetTag = "Player";


    // =========================================================
    // 巡回設定
    // =========================================================

    [Header("巡回地点")]
    [Tooltip("Enemy戦車がプレイヤーを発見するまで移動する地点")]
    [SerializeField] private Transform[] patrolPoints;

    [Tooltip("巡回地点に到着したと判断する距離")]
    [SerializeField] private float patrolArrivalDistance = 0.2f;

    private int currentPatrolIndex = 0;


    // =========================================================
    // パーツ
    // =========================================================

    [Header("パーツ参照")]
    [SerializeField] private Transform bodyTransform;
    [SerializeField] private Transform turretTransform;
    [SerializeField] private Transform gunBarrelTransform;
    [SerializeField] private Transform muzzleTransform;


    // =========================================================
    // 砲身設定
    // =========================================================

    [Header("砲身設定")]
    [SerializeField] private float minGunPitch = -10f;
    [SerializeField] private float maxGunPitch = 30f;

    private float currentGunPitch;


    // =========================================================
    // 戦闘
    // =========================================================

    private float lastAttackTime;
    private int currentHp;


    // =========================================================
    // Unity
    // =========================================================

    private void Start()
    {
        Initialize();
    }

    private void Update()
    {
        if (statsData == null)
            return;

        FindTarget();

        // プレイヤーが見つかった場合
        if (target != null && IsTargetInSight())
        {
            HandleCombat();
            return;
        }

        // プレイヤーが見つかっていない場合
        HandlePatrol();
    }


    // =========================================================
    // 初期化
    // =========================================================

    private void Initialize()
    {
        if (statsData == null)
            return;

        currentHp = statsData.maxHp;

        if (gunBarrelTransform != null)
        {
            currentGunPitch = NormalizeAngle(
                gunBarrelTransform.localEulerAngles.x
            );
        }
    }


    // =========================================================
    // ターゲット検索
    // =========================================================

    private void FindTarget()
    {
        if (target != null)
            return;

        GameObject player =
            GameObject.FindGameObjectWithTag(targetTag);

        if (player != null)
        {
            target = player.transform;
        }
    }


    // =========================================================
    // 巡回
    // =========================================================

    private void HandlePatrol()
    {
        // 巡回地点が設定されていない場合
        if (patrolPoints == null ||
            patrolPoints.Length == 0)
        {
            return;
        }

        Transform patrolPoint =
            patrolPoints[currentPatrolIndex];

        if (patrolPoint == null)
        {
            MoveToNextPatrolPoint();
            return;
        }

        Vector3 targetPosition =
            patrolPoint.position;

        Vector3 direction =
            targetPosition - transform.position;

        // 地上戦車なので高さは無視
        direction.y = 0f;

        float distance =
            direction.magnitude;

        // 巡回地点に到着
        if (distance <= patrolArrivalDistance)
        {
            MoveToNextPatrolPoint();
            return;
        }

        direction.Normalize();

        // Bodyを巡回地点へ向ける
        RotateBody(direction);

        // 前進
        MoveForward();
    }


    // =========================================================
    // 次の巡回地点へ
    // =========================================================

    private void MoveToNextPatrolPoint()
    {
        currentPatrolIndex++;

        if (currentPatrolIndex >= patrolPoints.Length)
        {
            currentPatrolIndex = 0;
        }
    }


    // =========================================================
    // プレイヤー視界判定
    // =========================================================

    private bool IsTargetInSight()
    {
        if (target == null)
            return false;

        Vector3 direction =
            target.position - transform.position;

        // 地上戦車なので高さを無視
        direction.y = 0f;

        float distance =
            direction.magnitude;

        // 視界距離
        if (distance > statsData.sightDistance)
            return false;

        if (distance <= 0.001f)
            return true;

        direction.Normalize();

        Transform reference =
            bodyTransform != null
                ? bodyTransform
                : transform;

        float angle =
            Vector3.Angle(
                reference.forward,
                direction
            );

        // 視界角度
        return angle <= statsData.sightAngle;
    }


    // =========================================================
    // 戦闘
    // =========================================================

    private void HandleCombat()
    {
        if (target == null)
            return;

        Vector3 targetPosition =
            target.position;

        // Bodyをプレイヤー方向へ向ける
        Vector3 direction =
            targetPosition - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude > 0.01f)
        {
            RotateBody(direction.normalized);
        }

        // Turretをプレイヤーへ向ける
        AimTurretAtTarget(targetPosition);

        // 射撃
        TryShoot();
    }


    // =========================================================
    // Body旋回
    // =========================================================

    private void RotateBody(Vector3 direction)
    {
        if (bodyTransform == null)
            return;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction);

        bodyTransform.rotation =
            Quaternion.RotateTowards(
                bodyTransform.rotation,
                targetRotation,
                statsData.bodyRotationSpeed *
                Time.deltaTime
            );
    }


    // =========================================================
    // 前進
    // =========================================================

    private void MoveForward()
    {
        Transform moveReference =
            bodyTransform != null
                ? bodyTransform
                : transform;

        transform.position +=
            moveReference.forward *
            statsData.moveSpeed *
            Time.deltaTime;
    }


    // =========================================================
    // Turret照準
    // =========================================================

    private void AimTurretAtTarget(
        Vector3 targetPosition)
    {
        if (turretTransform == null)
            return;

        Vector3 direction =
            targetPosition -
            turretTransform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction);

        turretTransform.rotation =
            Quaternion.RotateTowards(
                turretTransform.rotation,
                targetRotation,
                statsData.turretRotationSpeed *
                Time.deltaTime
            );

        AimGunBarrel(targetPosition);
    }


    // =========================================================
    // 砲身上下
    // =========================================================

    private void AimGunBarrel(
        Vector3 targetPosition)
    {
        if (gunBarrelTransform == null)
            return;

        if (gunBarrelTransform.parent == null)
            return;

        Vector3 localTarget =
            gunBarrelTransform.parent
                .InverseTransformPoint(targetPosition);

        float targetPitch =
            Mathf.Atan2(
                localTarget.y,
                localTarget.z
            ) * Mathf.Rad2Deg;

        targetPitch =
            Mathf.Clamp(
                targetPitch,
                minGunPitch,
                maxGunPitch
            );

        currentGunPitch =
            Mathf.MoveTowards(
                currentGunPitch,
                targetPitch,
                statsData.pitchTurretRotationSpeed *
                Time.deltaTime
            );

        Vector3 rotation =
            gunBarrelTransform.localEulerAngles;

        rotation.x = currentGunPitch;

        gunBarrelTransform.localEulerAngles =
            rotation;
    }


    // =========================================================
    // 射撃
    // =========================================================

    private void TryShoot()
    {
        if (Time.time <
            lastAttackTime +
            statsData.attackCooldown)
        {
            return;
        }

        if (!IsAimingAtTarget())
            return;

        lastAttackTime = Time.time;

        ExecuteShoot();
    }


    private bool IsAimingAtTarget()
    {
        if (muzzleTransform == null)
            return false;

        if (target == null)
            return false;

        Vector3 direction =
            target.position -
            muzzleTransform.position;

        if (direction.sqrMagnitude <= 0.001f)
            return true;

        direction.Normalize();

        float angle =
            Vector3.Angle(
                muzzleTransform.forward,
                direction
            );

        // 5度以内なら射撃
        return angle <= 5f;
    }


    private void ExecuteShoot()
    {
        if (muzzleTransform == null)
        {
            Debug.LogWarning(
                "EnemyTankController: " +
                "muzzleTransform が設定されていません。",
                this
            );

            return;
        }

        // =====================================================
        // 弾生成処理
        // =====================================================

        Debug.Log(
            $"Enemy Tank Shoot! " +
            $"Attack Power: {statsData.attackPower}"
        );

        /*
        GameObject bullet = Instantiate(
            bulletPrefab,
            muzzleTransform.position,
            muzzleTransform.rotation
        );

        bullet.GetComponent<Projectile>()
            .Initialize(statsData.attackPower);
        */
    }


    // =========================================================
    // ダメージ
    // =========================================================

    public void TakeDamage(int damage)
    {
        if (damage <= 0)
            return;

        currentHp -= damage;

        currentHp =
            Mathf.Max(currentHp, 0);

        if (currentHp <= 0)
        {
            OnDestroyed();
        }
    }


    private void OnDestroyed()
    {
        Debug.Log("Enemy Tank Destroyed!");

        // TODO:
        // 爆発
        // 撃破エフェクト
        // Destroy(gameObject);
    }


    // =========================================================
    // HP
    // =========================================================

    public int GetCurrentHp()
    {
        return currentHp;
    }


    public int GetMaxHp()
    {
        return statsData != null
            ? statsData.maxHp
            : 0;
    }


    // =========================================================
    // Utility
    // =========================================================

    private float NormalizeAngle(float angle)
    {
        if (angle > 180f)
        {
            angle -= 360f;
        }

        return angle;
    }
}
