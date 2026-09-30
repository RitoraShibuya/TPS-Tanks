using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    [Header("ターゲット設定")]
    [Tooltip("攻撃対象（インスペクターで設定）")]
    [SerializeField] protected Transform target;

    [Header("現在のHP")]
    [SerializeField] protected float currentHealth;

    [Header("ダメージ判定設定")]
    [Tooltip("この名前を含むオブジェクトに当たったらダメージを受ける（例: Bullet, Turret, PlayerBullet など）")]
    [SerializeField] protected string targetObjectName = "Turret";

    public Transform Target => target;
    public float CurrentHealth => currentHealth;

    protected virtual void Awake()
    {
    }

    /// <summary>
    /// 被弾処理（ダメージを受けて死亡判定を行う共通処理）
    /// </summary>
    public virtual void TakeDamage(float damage)
    {
        currentHealth -= damage;
        Debug.Log($"{gameObject.name} が {damage} のダメージを受けました。残りHP: {currentHealth}");

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    /// <summary>
    /// 死亡処理
    /// </summary>
    protected virtual void Die()
    {
        Debug.Log($"{gameObject.name} が撃破されました。");
        Destroy(gameObject);
    }

    /// <summary>
    /// 衝突判定（Trigger判定の場合）
    /// </summary>
    protected virtual void OnTriggerEnter(Collider other)
    {
        // 当たったオブジェクトの名前に対象文字列が含まれているかチェック
        if (other.gameObject.name.Contains(targetObjectName))
        {
            // 例: ダメージ量を設定（あるいは弾側のコンポーネントからダメージ量を取得）
            float damage = 10f;

            // 弾側に DamageDealer などのスクリプトがついている場合はそこから取得も可能
            /*
            var bullet = other.GetComponent<Bullet>();
            if (bullet != null) damage = bullet.Damage;
            */

            TakeDamage(damage);

            // 当たった弾などのオブジェクトを消去（必要に応じて）
            // Destroy(other.gameObject);
        }
    }

    /// <summary>
    /// 物理衝突判定（Collision判定の場合）
    /// </summary>
    protected virtual void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.name.Contains(targetObjectName))
        {
            float damage = 10f;
            TakeDamage(damage);
        }
    }
}