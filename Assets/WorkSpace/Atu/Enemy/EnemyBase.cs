using System;
using UnityEngine;

/// <summary>
/// 全エネミー共通の基底クラス
/// HP・ターゲット検出・視界判定・射撃など、敵の種類に関係なく共通の処理をまとめる
/// </summary>
public abstract class EnemyBase : MonoBehaviour
{
    public enum ModelForwardAxis { Forward, Right, Left, Back }
    public enum PatrolType { Loop, PingPong, Random }

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

    [Header("パーツ設定（共通）")]
    [SerializeField] protected Transform muzzleTransform;     // 砲口（発射位置）

    [Header("モデルの正面軸補正設定")]
    [Tooltip("モデルの正面が向いているローカル軸を指定")]
    [SerializeField] protected ModelForwardAxis modelForwardAxis = ModelForwardAxis.Forward;

    [Header("索敵・前方視界設定")]
    [SerializeField] protected float sightDistance = 20f;     // 正面視界の届く距離
    [SerializeField] protected float sightAngle = 120f;       // 視野角（例: 120度なら正面から左右60度ずつ）

    [Header("射撃パラメータ")]
    [SerializeField] protected GameObject bulletPrefab;
    [SerializeField] protected float bulletSpeed = 30f;
    [SerializeField] protected float attackRange = 20f;       // 射撃を開始する距離
    [SerializeField] protected float fireRate = 1.5f;         // 発射間隔（秒）

    [Header("遮蔽判定設定")]
    [Tooltip("壁や障害物のレイヤー。未設定(Nothing)の場合は Default を使う")]
    [SerializeField] protected LayerMask obstacleLayer;

    /// <summary>
    /// 破壊された瞬間に通知されるイベント（シーン切り替えによる破棄では呼ばれない）
    /// </summary>
    public event Action<EnemyBase> OnDied;

    // 残りHP（あと何発耐えられるか）
    private int currentHp;
    private float nextFireTime;

    public int CurrentHp => currentHp;
    public int MaxHp => statsData.maxHitCount;
    public bool IsDead => currentHp <= 0;

    protected virtual void Awake()
    {
        currentHp = statsData.maxHitCount;

        // Playerタグのオブジェクトを自動検出しターゲットに設定
        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObj != null)
            {
                target = playerObj.transform;
            }
        }

        // 遮蔽レイヤー未設定だと Raycast が何にも当たらず、壁越しに見えてしまうため保険を入れる
        if (obstacleLayer.value == 0)
        {
            obstacleLayer = LayerMask.GetMask("Default");
        }

        EnsureCollider();
    }

    /// <summary>
    /// コライダーが無いと弾が当たらないため、見た目の大きさに合わせたBoxColliderを自動で追加する
    /// （FBXをそのまま配置した敵用の保険。正式にはInspectorでコライダーを付けること）
    /// </summary>
    private void EnsureCollider()
    {
        if (GetComponentInChildren<Collider>() != null) return;

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        // 各Rendererのローカル境界の8頂点を、この敵のローカル空間にまとめる
        Bounds localBounds = new Bounds();
        bool initialized = false;

        foreach (Renderer r in renderers)
        {
            Bounds b = r.localBounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = b.center + Vector3.Scale(b.extents, new Vector3(
                    (i & 1) == 0 ? -1f : 1f,
                    (i & 2) == 0 ? -1f : 1f,
                    (i & 4) == 0 ? -1f : 1f));

                Vector3 point = transform.InverseTransformPoint(r.transform.TransformPoint(corner));

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

        Debug.LogWarning($"[{gameObject.name}] コライダーが無いため BoxCollider を自動追加しました。");
    }

    // ==========================================
    // 向き・視界判定（全エネミー共通）
    // ==========================================

    /// <summary>
    /// 指定トランスフォームの実際の見た目の正面方向を取得
    /// </summary>
    protected Vector3 GetModelForward(Transform t)
    {
        switch (modelForwardAxis)
        {
            case ModelForwardAxis.Right: return t.right;
            case ModelForwardAxis.Left: return -t.right;
            case ModelForwardAxis.Back: return -t.forward;
            default: return t.forward;
        }
    }

    /// <summary>
    /// LookRotation で作った回転を、モデルの正面軸に合わせて補正するための回転
    /// </summary>
    protected Quaternion GetAxisCorrection()
    {
        switch (modelForwardAxis)
        {
            case ModelForwardAxis.Right: return Quaternion.Euler(0, -90, 0);
            case ModelForwardAxis.Left: return Quaternion.Euler(0, 90, 0);
            case ModelForwardAxis.Back: return Quaternion.Euler(0, 180, 0);
            default: return Quaternion.identity;
        }
    }

    /// <summary>
    /// 視界判定（距離・モデル正面軸の視野角・壁遮蔽）
    /// </summary>
    protected bool IsTargetInSight()
    {
        if (target == null) return false;

        // 1. 距離判定
        Vector3 directionToTarget = target.position - transform.position;
        if (directionToTarget.magnitude > sightDistance) return false;

        // 2. 視野角判定（水平面で計算）
        Vector3 horizontalDir = directionToTarget;
        horizontalDir.y = 0f;

        Vector3 forward = GetModelForward(transform);
        forward.y = 0f;

        if (horizontalDir.sqrMagnitude > 0.0001f && forward.sqrMagnitude > 0.0001f)
        {
            float angle = Vector3.Angle(forward, horizontalDir);
            if (angle > (sightAngle / 2f)) return false;
        }

        // 3. 壁（障害物）の遮蔽チェック
        return HasLineOfSight();
    }

    /// <summary>
    /// 砲口からターゲットの中心までの視線が壁に遮られていないか
    /// </summary>
    protected bool HasLineOfSight()
    {
        if (target == null) return false;

        Vector3 origin = muzzleTransform != null
            ? muzzleTransform.position
            : transform.position + Vector3.up * 1f;

        Vector3 targetPoint = GetTargetAimPoint();
        Vector3 toTarget = targetPoint - origin;
        float dist = toTarget.magnitude;
        if (dist <= 0.0001f) return true;

        RaycastHit[] hits = Physics.RaycastAll(
            origin, toTarget / dist, dist, obstacleLayer, QueryTriggerInteraction.Ignore);

        foreach (RaycastHit hit in hits)
        {
            // 自分自身のコライダーは無視
            if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;

            // ターゲット（プレイヤー）に当たった場合は遮蔽物ではない
            if (hit.transform == target || hit.transform.IsChildOf(target) || hit.transform.CompareTag(playerTag)) continue;

            return false; // 壁に遮られている
        }

        return true;
    }

    /// <summary>
    /// 弾の狙い位置（ターゲットのコライダー全体の中心）を返す
    /// ※target.position は足元にあることが多いため、そのまま狙うと足元に飛んでしまう
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

        // コライダーが無ければ見た目（Renderer）の中心を使う
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

    // ==========================================
    // 射撃（全エネミー共通）
    // ==========================================

    /// <summary>
    /// 発射間隔が経過していれば発射する。発射したら true
    /// </summary>
    protected bool TryFire()
    {
        if (Time.time < nextFireTime) return false;
        if (muzzleTransform == null || bulletPrefab == null || target == null) return false;

        nextFireTime = Time.time + fireRate;
        FireAtTarget();
        return true;
    }

    /// <summary>
    /// ターゲットの中心に向けて弾（SharedBullet）を発射する
    /// </summary>
    private void FireAtTarget()
    {
        Vector3 shootDirection = (GetTargetAimPoint() - muzzleTransform.position).normalized;
        if (shootDirection == Vector3.zero) shootDirection = GetModelForward(muzzleTransform);

        GameObject bulletObj = Instantiate(
            bulletPrefab, muzzleTransform.position, Quaternion.LookRotation(shootDirection));

        if (bulletObj.TryGetComponent(out SharedBullet bullet))
        {
            bullet.Launch(shootDirection, bulletSpeed, gameObject);
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] bulletPrefab に SharedBullet が付いていません。");
        }

        if (SSoundManager.Instance != null)
        {
            SSoundManager.Instance.PlaySE_Shoot();
        }
    }

    // ==========================================
    // 被弾・死亡
    // ==========================================

    /// <summary>
    /// 被弾処理（1発につきHPを1減らす）
    /// </summary>
    public virtual void TakeDamage()
    {
        // 破壊済み（Destroy待ち）なら何もしない
        if (IsDead) return;

        currentHp--;
        Debug.Log($"[{gameObject.name}] 被弾 / 残りHP: {currentHp}");

        if (IsDead)
        {
            Die();
        }
    }

    /// <summary>
    /// 即座に破壊する（デバッグ用など）
    /// </summary>
    public void Kill()
    {
        if (IsDead) return;
        currentHp = 0;
        Die();
    }

    /// <summary>
    /// 死亡・破棄処理
    /// </summary>
    protected virtual void Die()
    {
        Debug.Log($"[{gameObject.name}] 破壊されました。");

        if (SSoundManager.Instance != null)
        {
            SSoundManager.Instance.PlaySE_TankDestroy();
        }

        OnDied?.Invoke(this);
        Destroy(gameObject);
    }

    // ==========================================
    // Gizmo
    // ==========================================

    protected virtual void OnDrawGizmosSelected()
    {
        // 正面軸補正を考慮した視界範囲（黄色）
        Gizmos.color = Color.yellow;
        Vector3 forward = GetModelForward(transform);
        Vector3 leftRay = Quaternion.Euler(0, -sightAngle / 2f, 0) * forward;
        Vector3 rightRay = Quaternion.Euler(0, sightAngle / 2f, 0) * forward;

        Gizmos.DrawRay(transform.position, leftRay * sightDistance);
        Gizmos.DrawRay(transform.position, rightRay * sightDistance);
        Gizmos.DrawRay(transform.position, forward * sightDistance);

        // 攻撃距離（赤色）
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
