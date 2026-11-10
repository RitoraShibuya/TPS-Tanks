
using Tanks.Complete;
using UnityEngine;

public abstract class EnemyBase : MonoBehaviour
{
    [System.Serializable]
    public class Stats
    {
        [Tooltip("HP：弾が何発当たったら破壊されるか")]
        public int maxHitCount = 3;
    }

    [Header("敵の基本ステータス")]
    [SerializeField] protected Stats statsData = new Stats();

    [Header("ターゲット設定")]
    [SerializeField] protected Transform target;
    [SerializeField] protected string playerTag = "Player";

    // 残りHP
    private int currentHp;

    public int CurrentHp => currentHp;
    public int MaxHp => statsData.maxHitCount;

    protected virtual void Awake()
    {
        currentHp = statsData.maxHitCount;

        // Playerタグのオブジェクトを自動検出
        if (target == null)
        {
            GameObject playerObj =
                GameObject.FindGameObjectWithTag(playerTag);

            if (playerObj != null)
            {
                target = playerObj.transform;
            }
        }

        EnsureCollider();
    }

    /// <summary>
    /// コライダーがない場合、自動追加
    /// </summary>
    private void EnsureCollider()
    {
        if (GetComponentInChildren<Collider>() != null) return;

        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0) return;

        Bounds localBounds = new Bounds();
        bool initialized = false;

        foreach (Renderer r in renderers)
        {
            Bounds b = r.localBounds;

            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = b.center + Vector3.Scale(
                    b.extents,
                    new Vector3(
                        (i & 1) == 0 ? -1f : 1f,
                        (i & 2) == 0 ? -1f : 1f,
                        (i & 4) == 0 ? -1f : 1f
                    )
                );

                Vector3 point = transform.InverseTransformPoint(
                    r.transform.TransformPoint(corner)
                );

                if (!initialized)
                {
                    localBounds = new Bounds(point, Vector3.zero);
                    initialized = true;
                }
                else
                {
                    localBounds.Encapsulate(point);
                }
            }
        }

        BoxCollider box = gameObject.AddComponent<BoxCollider>();
        box.center = localBounds.center;
        box.size = localBounds.size;

        Debug.LogWarning(
            $"[{gameObject.name}] BoxColliderを自動追加しました。"
        );
    }

    /// <summary>
    /// 弾の狙い位置を取得
    /// </summary>
    protected Vector3 GetTargetAimPoint()
    {
        if (target == null) return Vector3.zero;

        Bounds bounds = new Bounds();
        bool initialized = false;

        foreach (Collider col in target.GetComponentsInChildren<Collider>())
        {
            if (col.isTrigger || !col.enabled) continue;

            if (!initialized)
            {
                bounds = col.bounds;
                initialized = true;
            }
            else
            {
                bounds.Encapsulate(col.bounds);
            }
        }

        // コライダーがない場合はRendererの中心を使用
        if (!initialized)
        {
            foreach (Renderer r in target.GetComponentsInChildren<Renderer>())
            {
                if (!initialized)
                {
                    bounds = r.bounds;
                    initialized = true;
                }
                else
                {
                    bounds.Encapsulate(r.bounds);
                }
            }
        }

        return initialized ? bounds.center : target.position;
    }

    /// <summary>
    /// Playerの弾（HBullet）との衝突判定
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        // Playerの弾か確認
        Bullet bullet = collision.gameObject.GetComponent<Bullet>();

        // HBullet以外ならダメージを受けない
        if (bullet == null) return;

        TakeDamage();
    }

    /// <summary>
    /// 被弾処理（1発につきHPを1減らす）
    /// </summary>
    public virtual void TakeDamage()
    {
        // 破壊済みなら何もしない
        if (currentHp <= 0) return;

        currentHp--;

        Debug.Log(
            $"[{gameObject.name}] 被弾 / 残りHP: {currentHp}"
        );

        // 被弾音
        if (SSoundManager.Instance != null)
        {
            SSoundManager.Instance.PlaySE_Impact();
        }

        // HPが0になったら破壊
        if (currentHp <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// 死亡・破棄処理
    /// </summary>
    protected virtual void Die()
    {
        Debug.Log($"[{gameObject.name}] 破壊されました。");

        // 破壊音
        if (SSoundManager.Instance != null)
        {
            SSoundManager.Instance.PlaySE_TankDestroy();
        }

        Destroy(gameObject);
    }
}
