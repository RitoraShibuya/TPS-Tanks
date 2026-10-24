using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SharedBullet : MonoBehaviour
{
    public float Damage { get; private set; }

    [Header("着弾エフェクト（任意）")]
    [SerializeField] private GameObject hitEffectPrefab;

    private string shooterTag = "";
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Launch(Vector3 direction, float speed, float attackPower, string shooterTag, float lifeTime = 3f)
    {
        Damage = attackPower;
        this.shooterTag = shooterTag;

        if (rb != null)
        {
            rb.linearVelocity = direction.normalized * speed;
        }

        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // 1. 発射した本人・同じ陣営（親・ルート階層含む）なら無視
        if (CheckTagInHierarchy(other, shooterTag))
        {
            return;
        }

        // 2. プレイヤー（HHelth）への命中判定
        if (other.GetComponentInParent<HHelth>() is HHelth playerHealth)
        {
            playerHealth.TakeDamage((int)Damage);
            OnHit();
            return;
        }

        // 3. 敵（EnemyBase）への命中判定
        if (other.GetComponentInParent<EnemyBase>() is EnemyBase enemy)
        {
            enemy.TakeDamage(Damage);
            OnHit();
            return;
        }

        // 4. 壁や障害物（Trigger以外のコライダー）に着弾した場合
        if (!other.isTrigger)
        {
            OnHit();
        }
    }

    private void OnHit()
    {
        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }

    private bool CheckTagInHierarchy(Collider col, string targetTag)
    {
        if (string.IsNullOrEmpty(targetTag)) return false;

        if (col.CompareTag(targetTag)) return true;
        if (col.transform.parent != null && col.transform.parent.CompareTag(targetTag)) return true;
        if (col.transform.root != null && col.transform.root.CompareTag(targetTag)) return true;

        return false;
    }
}