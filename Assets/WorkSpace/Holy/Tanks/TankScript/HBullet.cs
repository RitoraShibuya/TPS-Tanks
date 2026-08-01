using UnityEngine;

/// <summary>
/// PC仕様「弾の発射について／弾の軌道について」を担当。
/// ・発射された弾は設定された距離(または命中対象)まで直進する。
/// ・命中対象がある場合はそこに着弾して消える。
/// ・命中対象が無い場合は、直線飛距離の最終地点まで飛んだ後、落下する。
/// ・落下時は弾を進行方向へ回転させる。
/// </summary>
public class Bullet : MonoBehaviour
{
    [Header("パラメータ (仮値)")]
    [SerializeField] private float flightSpeed = 30f;   // 直進フェーズの速度 m/s
    [SerializeField] private float gravity = 9.8f;       // 落下フェーズの重力加速度
    [SerializeField] private float maxLifeTime = 5f;

    private Vector3 targetPoint;
    private bool willHitSomething;
    private bool isFalling;
    private Vector3 fallVelocity;
    private float lifeTimer;

    /// <summary>
    /// TankWeaponから呼び出す初期化。
    /// </summary>
    /// <param name="target">直進先の着弾点、またはヒット対象が無い場合の直線飛距離終端。</param>
    /// <param name="hitSomething">true: targetで着弾して消える。false: targetまで直進後に落下する。</param>
    public void Init(Vector3 target, bool hitSomething)
    {
        targetPoint = target;
        willHitSomething = hitSomething;
    }

    private void Update()
    {
        lifeTimer += Time.deltaTime;
        if (lifeTimer > maxLifeTime)
        {
            Destroy(gameObject);
            return;
        }

        if (!isFalling)
        {
            FlyStraight();
        }
        else
        {
            FallWithGravity();
        }
    }

    private void FlyStraight()
    {
        Vector3 toTarget = targetPoint - transform.position;
        float step = flightSpeed * Time.deltaTime;

        if (toTarget.magnitude <= step)
        {
            transform.position = targetPoint;

            if (willHitSomething)
            {
                // ① 照準内にオブジェクトがあった場合 → 着弾して消える
                Impact();
            }
            else
            {
                // ② 無かった場合 → ここから落下フェーズへ移行
                isFalling = true;
                fallVelocity = transform.forward * flightSpeed;
            }
            return;
        }

        Vector3 dir = toTarget.normalized;
        transform.position += dir * step;
        transform.rotation = Quaternion.LookRotation(dir); // 進行方向へ回転
    }

    private void FallWithGravity()
    {
        fallVelocity += Vector3.down * gravity * Time.deltaTime;
        transform.position += fallVelocity * Time.deltaTime;

        if (fallVelocity.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(fallVelocity.normalized); // 落下中も進行方向へ回転
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Impact();
    }

    private void OnTriggerEnter(Collider other)
    {
        Impact();
    }

    private void Impact()
    {
        // TODO: ヒットエフェクト/ダメージ処理をここに追加する
        Destroy(gameObject);
    }
}