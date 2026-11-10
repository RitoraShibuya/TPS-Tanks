using UnityEngine;

/// <summary>
/// プレイヤー戦車の弾
/// ・設定距離までは重力なしで直進する
/// ・直進距離を過ぎたら gravityMultiplier 倍の重力で落下する
/// ・敵に当たったら HP を1減らして消える
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class TankProjectile : MonoBehaviour
{
    [Header("飛行設定")]
    [SerializeField]
    private float flightSpeed = 20f;

    [SerializeField]
    private float lifeTime = 10f;

    [Tooltip("落下時の重力の倍率（1 = 通常の重力）")]
    [SerializeField]
    private float gravityMultiplier = 12f;

    [Header("着弾エフェクト")]
    [Tooltip("着弾時に再生するパーティクル（敵の弾と同じ CompleteShellExplosion）。音は SSoundManager の着弾音を使う")]
    [SerializeField]
    private ParticleSystem hitEffectPrefab;

    private Rigidbody rb;

    // 発射した戦車（自分の弾で自分に当たらないようにする）
    private Transform owner;

    private float straightFlightTime;
    private float flightElapsedTime;

    private bool isFalling;
    private bool hasHit;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // 重力は自前で加えるので Rigidbody の重力は常にOFF
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        TankParameterManager manager = TankParameterManager.Instance;

        if (manager != null)
        {
            gravityMultiplier = manager.GetFloat("gravityMultiplier", gravityMultiplier);
        }
    }

    public void Launch(
        Vector3 aimPoint,
        float straightDistance,
        float speed,
        Transform shooter = null)
    {
        owner = shooter;
        flightSpeed = speed;

        flightElapsedTime = 0f;
        isFalling = false;

        Vector3 direction = aimPoint - transform.position;

        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = transform.forward;
        }
        else
        {
            direction.Normalize();
        }

        transform.rotation = Quaternion.LookRotation(direction);

        straightFlightTime = flightSpeed > 0f ? straightDistance / flightSpeed : 0f;

        rb.linearVelocity = direction * flightSpeed;

        Destroy(gameObject, lifeTime);
    }

    private void FixedUpdate()
    {
        if (!isFalling)
        {
            flightElapsedTime += Time.fixedDeltaTime;

            if (flightElapsedTime >= straightFlightTime)
            {
                isFalling = true;
            }
        }

        if (isFalling)
        {
            // useGravity は OFF なので、重力の全量（gravityMultiplier 倍）をここで加える
            rb.AddForce(Physics.gravity * gravityMultiplier, ForceMode.Acceleration);

            if (rb.linearVelocity.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(rb.linearVelocity);
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Impact(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        Impact(other);
    }

    private void Impact(Collider other)
    {
        // 同じフレームで複数のコライダーに当たっても1回だけ処理する
        if (hasHit) return;

        // 自分の戦車は無視
        if (owner != null && other.transform.IsChildOf(owner)) return;

        EnemyBase enemy = other.GetComponentInParent<EnemyBase>();

        // 敵以外のトリガー（エリア判定など）はすり抜ける
        if (enemy == null && other.isTrigger) return;

        hasHit = true;

        // 敵に当たったらHPを1減らす
        if (enemy != null)
        {
            enemy.TakeDamage();
        }

        SpawnHitEffect();

        if (SSoundManager.Instance != null)
        {
            SSoundManager.Instance.PlaySE_Impact();
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// 着弾位置に爆発パーティクルを出す（敵の弾の ShellExplosion と同じ見た目）
    /// </summary>
    private void SpawnHitEffect()
    {
        if (hitEffectPrefab == null) return;

        // 敵の弾と同じく、パーティクルが上向きに広がるよう X 軸 -90 度で出す
        ParticleSystem effect = Instantiate(
            hitEffectPrefab,
            transform.position,
            Quaternion.Euler(-90f, 0f, 0f)
        );

        effect.Play(true);

        // 再生が終わったら削除する
        Destroy(effect.gameObject, effect.main.duration);
    }
}
