using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SharedBullet : MonoBehaviour
{
    [Header("着弾エフェクト（任意）")]
    [SerializeField] private GameObject hitEffectPrefab;

    private Transform shooter;      // 発射した本人
    private bool isEnemyShot;       // 敵（EnemyBase）が撃った弾か
    private bool hasHit;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    /// <summary>
    /// 弾の発射初期化処理（発射元の GameObject を渡す）
    /// ※陣営はタグではなく EnemyBase / HHelth コンポーネントの有無で判定する
    /// ※当たった相手のHPは1発につき1減る
    /// </summary>
    public void Launch(Vector3 direction, float speed, GameObject shooterObject, float lifeTime = 3f)
    {
        shooter = shooterObject != null ? shooterObject.transform : null;
        isEnemyShot = shooterObject != null && shooterObject.GetComponentInParent<EnemyBase>() != null;

        if (rb != null)
        {
            if (isEnemyShot)
            {
                // 敵の弾は重力・減速なしでまっすぐ飛ばす
                rb.useGravity = false;
                rb.linearDamping = 0f;
                rb.angularVelocity = Vector3.zero;
                transform.rotation = Quaternion.LookRotation(direction);
            }

            // 進行方向に物理速度を与える
            rb.linearVelocity = direction.normalized * speed;
        }

        // 指定時間後に自動消滅
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        // 1. 発射した本人ならスルー（自爆防止）
        if (shooter != null && other.transform.IsChildOf(shooter))
        {
            return;
        }

        EnemyBase enemy = other.GetComponentInParent<EnemyBase>();
        HHelth player = other.GetComponentInParent<HHelth>();

        // 2. 同じ陣営ならスルー（誤爆防止）
        if ((isEnemyShot && enemy != null) || (!isEnemyShot && player != null))
        {
            return;
        }

        // 3. 敵対陣営に当たった場合
        if (isEnemyShot && player != null)
        {
            Debug.Log($"[SharedBullet] 敵の弾が Player ({other.name}) に命中！");
            player.TakeDamage();

            OnHit();
            return;
        }
        else if (!isEnemyShot && enemy != null)
        {
            Debug.Log($"[SharedBullet] Playerの弾が 敵 ({other.name}) に命中！");
            enemy.TakeDamage();

            OnHit();
            return;
        }

        // 4. 壁や地面・障害物（Trigger以外のコライダー）に当たった場合
        if (!other.isTrigger)
        {
            Debug.Log($"[SharedBullet] 障害物 ({other.name}) に着弾。");
            OnHit();
        }
    }

    /// <summary>
    /// 着弾・消滅共通処理
    /// </summary>
    private void OnHit()
    {
        hasHit = true;

        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }
}
