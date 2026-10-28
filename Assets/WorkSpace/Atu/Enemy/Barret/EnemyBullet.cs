using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SharedBullet : MonoBehaviour
{
<<<<<<< HEAD
    public float Damage { get; private set; }

    [Header("着弾エフェクト（任意）")]
    [SerializeField] private GameObject hitEffectPrefab;

    private string shooterTag = "";
=======
    [Header("着弾エフェクト（任意）")]
    [SerializeField] private GameObject hitEffectPrefab;

    private Transform shooter;      // 発射した本人
    private bool isEnemyShot;       // 敵（EnemyBase）が撃った弾か
    private bool hasHit;
>>>>>>> Holy
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

<<<<<<< HEAD
    public void Launch(Vector3 direction, float speed, float attackPower, string shooterTag, float lifeTime = 3f)
    {
        Damage = attackPower;
        this.shooterTag = shooterTag;

        if (rb != null)
        {
=======
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
>>>>>>> Holy
            rb.linearVelocity = direction.normalized * speed;
        }

        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter(Collider other)
    {
<<<<<<< HEAD
        // 1. 発射した本人・同じ陣営（親・ルート階層含む）なら無視
        if (CheckTagInHierarchy(other, shooterTag))
=======
        if (hasHit) return;

        // 1. 発射した本人ならスルー（自爆防止）
        if (shooter != null && other.transform.IsChildOf(shooter))
>>>>>>> Holy
        {
            return;
        }

<<<<<<< HEAD
        // 2. プレイヤー（HHelth）への命中判定
        if (other.GetComponentInParent<HHelth>() is HHelth playerHealth)
        {
            playerHealth.TakeDamage((int)Damage);
=======
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

>>>>>>> Holy
            OnHit();
            return;
        }

<<<<<<< HEAD
        // 3. 敵（EnemyBase）への命中判定
        if (other.GetComponentInParent<EnemyBase>() is EnemyBase enemy)
        {
            enemy.TakeDamage(Damage);
            OnHit();
            return;
        }

        // 4. 壁や障害物（Trigger以外のコライダー）に着弾した場合
=======
        // 4. 壁や地面・障害物（Trigger以外のコライダー）に当たった場合
>>>>>>> Holy
        if (!other.isTrigger)
        {
            OnHit();
        }
    }

    private void OnHit()
    {
        hasHit = true;

        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }
<<<<<<< HEAD

    private bool CheckTagInHierarchy(Collider col, string targetTag)
    {
        if (string.IsNullOrEmpty(targetTag)) return false;

        if (col.CompareTag(targetTag)) return true;
        if (col.transform.parent != null && col.transform.parent.CompareTag(targetTag)) return true;
        if (col.transform.root != null && col.transform.root.CompareTag(targetTag)) return true;

        return false;
    }
}
=======
}
>>>>>>> Holy
