using System;
using UnityEngine;

public class HHelth : MonoBehaviour
{
    [Header("ダメージ対象のタグ（相手の弾のタグなど）")]
    [SerializeField] private string targetTag = "Enemy";

    [Header("体力")]
    [SerializeField] private int health = 3;

    // 死亡イベント
    public event Action OnDie;

    private bool isDead = false;

    public bool IsTarget(GameObject target)
    {
        return target.CompareTag(targetTag);
    }

    /// <summary>
    /// 弾側から呼び出す被ダメージ処理
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (isDead) return;

        health -= damage;
        Debug.Log(gameObject.name + " 残り体力: " + health);

        if (health <= 0)
        {
            isDead = true;
            Debug.Log(gameObject.name + " が破壊されました。");

            // 死亡イベントを発行
            OnDie?.Invoke();

            // プレイヤーオブジェクトの破棄（必要に応じて）
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isDead) return;

        // タグチェック（相手の陣営かどうか）
        if (!IsTarget(other.gameObject) && !CheckTagInHierarchy(other, targetTag))
        {
            return;
        }

        // SharedBullet経由でダメージが来る場合はOnTriggerEnterを弾側に任せるため、
        // 直接当たった場合（コライダー衝突等）の保険処理として以下を残します
        if (other.TryGetComponent(out SharedBullet bullet))
        {
            TakeDamage(1); // 1ダメージ
        }
    }

    private bool CheckTagInHierarchy(Collider col, string tag)
    {
        if (col.CompareTag(tag)) return true;
        if (col.transform.parent != null && col.transform.parent.CompareTag(tag)) return true;
        if (col.transform.root != null && col.transform.root.CompareTag(tag)) return true;
        return false;
    }
}