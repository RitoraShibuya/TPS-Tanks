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
/// </summary>
public class TankWeapon : MonoBehaviour
{
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

    private InputAction fireAction;
    private float fireCooldownRemaining;
    private Collider[] ownerColliders;

    private void Awake()
    {
        if (aimSystem == null)
        {
            aimSystem = GetComponent<TankAimSystem>();
        }

        if (muzzlePoint == null)
        {
            muzzlePoint = transform;
        }

        // 戦車自身(Body/Head/UDRotater/MuzullArm/Muzullなど)のColliderを事前に集めておく。
        // 発射した弾がこれらに当たって即座に消えてしまう(自己衝突)のを防ぐために使用する。
        ownerColliders = GetComponentsInParent<Collider>(true);

        // 発射用のInputActionをコード上で定義
        fireAction = new InputAction(name: "Fire", type: InputActionType.Button);
        fireAction.AddBinding("<Gamepad>/leftShoulder"); // L1/LBボタン
        fireAction.AddBinding("<Mouse>/leftButton");     // マウス左クリック
    }

    private void OnEnable()
    {
        fireAction.Enable();
    }

    private void OnDisable()
    {
        fireAction.Disable();
    }

    private void Update()
    {
        if (fireCooldownRemaining > 0f)
        {
            fireCooldownRemaining -= Time.deltaTime;
        }

        if (fireAction.WasPressedThisFrame() && fireCooldownRemaining <= 0f)
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
            projectile.Launch(aimPoint);
        }
        else
        {
            Debug.LogWarning("[TankWeapon] Projectile Prefab に TankProjectile が付いていません。", this);
        }
    }
}