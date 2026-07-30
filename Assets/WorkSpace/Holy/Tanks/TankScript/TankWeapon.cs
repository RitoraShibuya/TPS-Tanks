using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// PC仕様「③ 弾発射（および照準・弾軌道）」を担当。
///
/// ・Lボタン押下で発射する。
/// ・照準内(Muzzleの正面方向)に当たり判定付きオブジェクトが存在する場合は、
///   そのオブジェクトの着弾点(照準の中心)へ命中させる。
/// ・存在しない場合は、弾の直線飛距離(bulletRange)の最終地点を照準の中心として、
///   そこまで直進した後、落下させる。
/// ヒット判定はレイヤーマスク方式(hittableLayers)で行う。
/// </summary>
public class TankWeapon : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private Transform muzzle;
    [SerializeField] private Bullet bulletPrefab;

    [Header("照準判定")]
    [Tooltip("弾のヒット判定対象レイヤー。Tank自身や弾自身は含めないこと。")]
    [SerializeField] private LayerMask hittableLayers = ~0;
    [Tooltip("照準内に当たり判定オブジェクトが無い場合の、弾の直線飛距離(=まとめのカメラ半径と同じ6m)。")]
    [SerializeField] private float bulletRange = 6f;

    [Header("発射制御 (仮値)")]
    [SerializeField] private float fireCooldown = 0.5f;
    private float cooldownTimer;

    private void Update()
    {
        cooldownTimer -= Time.deltaTime;

        if (FirePressed() && cooldownTimer <= 0f)
        {
            Fire();
            cooldownTimer = fireCooldown;
        }
    }

    private bool FirePressed()
    {
        var pad = Gamepad.current;
        // 仕様書の「Lボタン」= 左肩ボタン(L1)を想定。プロジェクトの実際の割り当てに合わせて変更すること。
        return pad != null && pad.leftShoulder.wasPressedThisFrame;
    }

    private void Fire()
    {
        if (muzzle == null || bulletPrefab == null) return;

        Vector3 origin = muzzle.position;
        Vector3 direction = muzzle.forward;

        Vector3 targetPoint;
        bool hitSomething;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, Mathf.Infinity, hittableLayers))
        {
            // ① 照準内に当たり判定付きオブジェクトが存在する場合 → その中心(着弾点)に着弾
            targetPoint = hit.point;
            hitSomething = true;
        }
        else
        {
            // ② 存在しない場合 → 直線飛距離の最終地点を照準の中心とし、後で落下させる
            targetPoint = origin + direction * bulletRange;
            hitSomething = false;
        }

        Bullet bullet = Instantiate(bulletPrefab, origin, Quaternion.LookRotation(direction));
        bullet.Init(targetPoint, hitSomething);
    }
}