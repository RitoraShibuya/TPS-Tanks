
using System;
using UnityEngine;

public class HHelth : MonoBehaviour
{
    [Header("ダメージ対象のタグ")]
    [SerializeField] private string targetTag = "Enemy";

    [Header("ダメージを与える弾")]
    [SerializeField] private GameObject Bullet;

    [Header("体力")]
    [SerializeField] private int health = 3;


    // 死亡イベント
    public event Action OnDie;

    private bool isDead = false;

    public bool IsTarget(GameObject target)
    {
        return target.CompareTag(targetTag);
    }

    private void OnTriggerEnter(Collider other)
    {
        
        if (isDead)
        {
            return;
        }
        
        // タグがEnemyでなければ無視
        if (!IsTarget(other.gameObject))
        {
            return;
        }
        Debug.Log("HitT");
        // Inspectorで設定した弾でなければ無視
        if (Bullet == null || other.gameObject != Bullet)
        {
            return;
        }
        Debug.Log("HitB");
        health--;
        
        Debug.Log(gameObject.name + " 残り体力: " + health);

        if (health <= 0)
        {
            isDead = true;

            // 死亡イベントを発行
            OnDie?.Invoke();
        }
    }
}
