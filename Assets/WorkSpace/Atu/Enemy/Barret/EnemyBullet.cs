using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SharedBullet : MonoBehaviour
{
    // ダメージ量（他のスクリプトから参照可能）
    public float Damage { get; private set; }

    [Header("着弾エフェクト（任意）")]
    [SerializeField] private GameObject hitEffectPrefab;

    private string shooterTag = ""; // 発射した側のTag（"Player" または "Enemy"）
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    /// <summary>
    /// 弾の発射初期化処理（発射元のTagを渡す）
    /// </summary>
    public void Launch(Vector3 direction, float speed, float attackPower, string shooterTag, float lifeTime = 3f)
    {
        Damage = attackPower;
        this.shooterTag = shooterTag; // 撃った側のTagを記録

        if (rb != null)
        {
            // 進行方向に物理速度を与える
            rb.linearVelocity = direction.normalized * speed;
        }

        // 指定時間後に自動消滅
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // 1. 発射した本人・同じ陣営（自分と同じTag）ならスルー（自爆・誤爆防止）
        if (CheckTagInHierarchy(other, shooterTag))
        {
            return;
        }

        // 2. 敵対陣営に当たった場合
        if (shooterTag == "Enemy" && CheckTagInHierarchy(other, "Player"))
        {
            Debug.Log($"[SharedBullet] 敵の弾が Player ({other.name}) に命中！ ダメージ: {Damage}");
            // ※ Player側にダメージ処理スクリプトがある場合はここで実行してください
            // 例: other.GetComponentInParent<PlayerHealth>()?.TakeDamage(Damage);

            OnHit();
            return;
        }
        else if (shooterTag == "Player" && CheckTagInHierarchy(other, "Enemy"))
        {
            Debug.Log($"[SharedBullet] Playerの弾が 敵 ({other.name}) に命中！ ダメージ: {Damage}");

            // EnemyBase の TakeDamage を呼び出す
            if (other.TryGetComponent(out EnemyBase enemy))
            {
                enemy.TakeDamage(Damage);
            }
            else if (other.GetComponentInParent<EnemyBase>() is EnemyBase parentEnemy)
            {
                parentEnemy.TakeDamage(Damage);
            }

            OnHit();
            return;
        }

        // 3. 壁や地面・障害物（Trigger以外のコライダー）に当たった場合
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
        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }

    /// <summary>
    /// 当たったコライダー（またはその親・ルート階層）に指定タグがあるか判定
    /// </summary>
    private bool CheckTagInHierarchy(Collider col, string targetTag)
    {
        if (string.IsNullOrEmpty(targetTag)) return false;

        if (col.CompareTag(targetTag)) return true;
        if (col.transform.parent != null && col.transform.parent.CompareTag(targetTag)) return true;
        if (col.transform.root != null && col.transform.root.CompareTag(targetTag)) return true;

        return false;
    }
}