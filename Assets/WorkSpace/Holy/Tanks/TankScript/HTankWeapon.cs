using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 弾の発射制御を担当するスクリプト。
/// Input Systemを使用し、ゲームパッドのL1ボタン、またはマウス左クリックで発射する。
///
/// セットアップ:
/// - Muzzle(砲口)のGameObjectにアタッチする(TankAimSystemと同じオブジェクトを推奨)。
/// - Inspector上の「Aim System」に、同じMuzzleに付いている TankAimSystem を登録する。
/// - 「Projectile Prefab」に、TankProjectile が付いた弾のPrefabを登録する。
/// - 「Muzzle Point」に、弾の発射位置・向きの基準にするTransform
///   (未設定ならこのスクリプト自身のTransformを使用)を登録する。
///
/// 自己衝突回避について:
/// - 発射した弾が戦車自身(Body/Turret/UDRotaterなど)に当たって即座に消えないよう、
///   Awake時に transform.root(戦車プレハブの一番上のGameObject)配下の
///   全Colliderを集めて衝突を無視する設定にしている。
///   そのため、BodyやTurretがバラバラの階層に配置されていても、
///   「戦車プレハブの一番上のGameObjectの下に全部まとまっている」限り正しく動作する。
/// </summary>
public class TankWeapon : MonoBehaviour
{
    [Header("Input Action")]
    [Tooltip("発射入力に使用するInputAction(Button)。TankControls.inputactionsの「Fire」を割り当てる。" +
             "Unity上でバインドを変更すれば、コードを変更せず発射ボタンを変えられる。")]
    [SerializeField]
    private InputActionReference fireActionReference;

    [Header("参照")]
    [Tooltip("狙点の計算に使用する TankAimSystem。未設定の場合は自身から取得を試みる。")]
    [SerializeField]
    private TankAimSystem aimSystem;

    [Tooltip("発射する弾のPrefab(TankProjectileが付いているもの)")]
    [SerializeField]
    private GameObject projectilePrefab;

    [Tooltip("弾の発射位置・向きの基準にするTransform。未設定ならこのオブジェクト自身。")]
    [SerializeField]
    private Transform muzzlePoint;

    [Header("発射設定")]
    [Tooltip("発射間隔(秒)。仕様書の数値(「まとめ」シート)が確定するまでの暫定値。")]
    [SerializeField]
    private float fireInterval = 0.5f;

    [Header("エクセル連携")]
    [Tooltip("CSV(Excel)からインポートした調整値を反映するためのTankTuningConfig。" +
             "設定すると、起動時にこのアセットの値でFire Intervalが上書きされる。未設定ならこのInspectorの値をそのまま使用する。")]
    [SerializeField]
    private TankTuningConfig tuningConfig;

    private float fireCooldownRemaining;
    private Collider[] ownerColliders;

    private void Awake()
    {
        ApplyTuningConfig();

        if (aimSystem == null)
        {
            aimSystem = GetComponent<TankAimSystem>();
        }

        if (muzzlePoint == null)
        {
            muzzlePoint = transform;
        }

        // 戦車自身(Body/Turret/Muzzle/UDRotaterなど)のColliderを事前に集めておく。
        // 発射した弾がこれらに当たって即座に消えてしまう(自己衝突)のを防ぐために使用する。
        //
        // 【重要】GetComponentsInParentではなく、必ず transform.root
        // (シーン階層の一番上=戦車プレハブの一番上のGameObject)から
        // GetComponentsInChildren で集める。
        // BodyがMuzzleの祖先(親・祖父母…)でない階層構成
        // (例: TS_PCTank の直下に Body と Turret が兄弟として並んでいる場合)では、
        // GetComponentsInParentだとBody側のColliderを取得できず、
        // 発射直後の弾がBodyに衝突して消えてしまう不具合が起こるため。
        ownerColliders = transform.root.GetComponentsInChildren<Collider>(true);
    }

    /// <summary>
    /// TankTuningConfig(CSV/Excelからインポートされた値)が設定されている場合、
    /// そちらの値でこのスクリプトのパラメーターを上書きする。
    /// </summary>
    private void ApplyTuningConfig()
    {
        if (tuningConfig == null)
        {
            return;
        }

        fireInterval = tuningConfig.weapon_FireInterval;
    }

    private void OnEnable()
    {
        if (fireActionReference != null)
        {
            fireActionReference.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (fireActionReference != null)
        {
            fireActionReference.action.Disable();
        }
    }

    private void Update()
    {
        if (fireCooldownRemaining > 0f)
        {
            fireCooldownRemaining -= Time.deltaTime;
        }

        bool firePressed = fireActionReference != null && fireActionReference.action.WasPressedThisFrame();

        if (firePressed && fireCooldownRemaining <= 0f)
        {
            Fire();
            fireCooldownRemaining = fireInterval;
        }
    }

    private void Fire()
    {
        if (projectilePrefab == null || aimSystem == null)
        {
            Debug.LogWarning("[TankWeapon] Projectile Prefab または Aim System が設定されていません。", this);
            return;
        }

        Vector3 aimPoint = aimSystem.GetAimWorldPoint(out _);

        GameObject projectileObj = Instantiate(projectilePrefab, muzzlePoint.position, muzzlePoint.rotation);

        // 弾が戦車自身(Body/Head/UDRotater/Muzullなど)に当たって
        // 即座に消えてしまわないよう、あらかじめ衝突を無視しておく
        Collider[] projectileColliders = projectileObj.GetComponentsInChildren<Collider>();
        foreach (Collider ownerCollider in ownerColliders)
        {
            if (ownerCollider == null)
            {
                continue;
            }

            foreach (Collider projectileCollider in projectileColliders)
            {
                if (projectileCollider == null)
                {
                    continue;
                }

                Physics.IgnoreCollision(projectileCollider, ownerCollider, true);
            }
        }

        TankProjectile projectile = projectileObj.GetComponent<TankProjectile>();
        if (projectile != null)
        {
            // 照準(レティクル)と同じ距離(aimSystem.MaxAimDistance)を渡すことで、
            // 「照準の表示位置」と「弾が落下し始める距離」を常に一致させる
            projectile.Launch(aimPoint, aimSystem.MaxAimDistance);
        }
        else
        {
            Debug.LogWarning("[TankWeapon] Projectile Prefab に TankProjectile が付いていません。", this);
        }
    }
}