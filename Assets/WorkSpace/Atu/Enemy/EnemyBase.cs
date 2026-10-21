using Tanks.Complete;
using UnityEngine;

public abstract class EnemyBase : MonoBehaviour
{
    [System.Serializable]
    public class Stats
    {
        public float maxHp = 100f;
        public float currentHp = 100f;
        public float attackPower = 10f;
    }

    [Header("敵の基本ステータス")]
    [SerializeField] protected Stats statsData = new Stats();

    [Header("ターゲット設定")]
    [SerializeField] protected Transform target;
    [SerializeField] protected string playerTag = "Player";


    //[SerializeField] private HHelth health;

    public float CurrentHp => statsData.currentHp;
    public float MaxHp => statsData.maxHp;

    protected virtual void Awake()
    {
        statsData.currentHp = statsData.maxHp;

        // Playerタグのオブジェクトを自動検出しターゲットに設定
        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObj != null)
            {
                target = playerObj.transform;
            }
        }

        //if(TankHealth == null)
        //{
        //    TankHealth = GetComponent<HHelth>();
        //}
    }

    //private void OnEnable()
    //{
    //    if(TankHealth != null)
    //    {
    //        TankHealth.OnDie += Die();
    //    }
    //}

    //private void OnDisable()
    //{
    //    if (TankHealth != null)
    //    {
    //        TankHealth.OnDie -= Die;
    //    }
    //}


    /// <summary>
    /// 被ダメージ処理
    /// </summary>
    public virtual void TakeDamage(float amount)
    {
        statsData.currentHp -= amount;
        Debug.Log($"[{gameObject.name}] 被ダメージ: {amount} / 残りHP: {statsData.currentHp}");

        if (statsData.currentHp <= 0)
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
        Destroy(gameObject);
    }
}