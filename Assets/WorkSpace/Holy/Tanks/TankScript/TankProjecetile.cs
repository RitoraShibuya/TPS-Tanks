using UnityEngine;

/// <summary>
/// 弾(Projectile)の挙動を担当するスクリプト。
///
/// 仕様:
/// - 発射された弾は、狙った方向へ直線で飛ぶ。
/// - 「直線で飛ぶ距離」はInspectorで指定した固定値(Straight Flight Distance)。
///   その距離を超えたら、重力の影響を受けて落下を開始する。
/// - 直線飛行中・落下中を問わず、何かに衝突すればそこで着弾して消える。
/// - 落下中は、弾を進行方向(速度ベクトル)に向けて回転させる。
///
/// セットアップ:
/// - 弾のPrefabにこのスクリプトと Rigidbody, Collider(IsTrigger推奨) をアタッチする。
/// - TankWeapon からこのスクリプトの Launch() を呼び出して発射する。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class TankProjectile : MonoBehaviour
{
    [Header("飛行設定")]
    [Tooltip("直線飛行時の速度 (m/s)。仕様書の数値が確定するまでの暫定値。")]
    [SerializeField]
    private float flightSpeed = 30f;

    [Tooltip("直線で飛ぶ距離(m)。この距離を超えると落下(重力)を開始する。")]
    [SerializeField]
    private float straightFlightDistance = 6f;

    [Tooltip("発射から何秒後に自動で消えるか(何にも当たらなかった場合の保険)")]
    [SerializeField]
    private float lifeTime = 10f;

    private Rigidbody rb;

    private bool isFalling;
    private Vector3 startPosition;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    /// <summary>
    /// 弾を発射する。
    /// </summary>
    /// <param name="aimPoint">狙点(ワールド座標)。飛んでいく方向の計算にのみ使用する。</param>
    public void Launch(Vector3 aimPoint)
    {
        startPosition = transform.position;
        isFalling = false;

        Vector3 direction = (aimPoint - startPosition).normalized;
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = transform.forward;
        }

        transform.rotation = Quaternion.LookRotation(direction);
        rb.linearVelocity = direction * flightSpeed;

        Destroy(gameObject, lifeTime);
    }

    private void FixedUpdate()
    {
        if (isFalling)
        {
            // 落下中は、速度ベクトルの方向に弾を向ける(進行方向に回転)
            if (rb.linearVelocity.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(rb.linearVelocity);
            }
            return;
        }

        // まだ直線飛行中。Inspectorで指定した距離に到達したかどうかを判定する。
        float traveledDistance = Vector3.Distance(startPosition, transform.position);
        if (traveledDistance >= straightFlightDistance)
        {
            // 直線飛行距離を超えた → 落下フェーズへ移行(重力ON、速度はそのまま引き継ぐ)
            isFalling = true;
            rb.useGravity = true;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 何かに衝突したら着弾処理(見た目のエフェクトなどはここに追加する)
        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Colliderをトリガーにしている場合はこちらで着弾処理
        Destroy(gameObject);
    }
}