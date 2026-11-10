
using System;
using UnityEngine;

public class HHelth : MonoBehaviour
{
    [Header("体力")]
    [Tooltip("HP：弾が何発当たったら死亡するか")]
    [SerializeField] private int health = 3;


    // 死亡イベント
    public event Action OnDie;

    // 被弾イベント (引数は残り体力)
    public event Action<int> OnDamaged;

    private bool isDead = false;

    public int Health => health;
    public bool IsDead => isDead;

    /// <summary>
    /// 被弾処理（敵の弾から呼ばれる。1発につき体力を1減らす）
    /// </summary>
    public void TakeDamage()
    {
        if (isDead)
        {
            return;
        }

        health--;


        Debug.Log(gameObject.name + " 残り体力: " + health);

        // 被弾イベントを発行 (HPのUI更新など)
        OnDamaged?.Invoke(health);

        if (health <= 0)
        {
            isDead = true;

            // 死亡イベントを発行
            OnDie?.Invoke();
            
        }
    }
}
