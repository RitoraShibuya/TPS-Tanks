using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyBullet : MonoBehaviour
{
    // EnemyBaseからダメージ量を参照できるように public（getter）にする
    public float Damage { get; private set; }

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Launch(Vector3 direction, float speed, float attackPower, float lifeTime = 2f)
    {
        Damage = attackPower;

        rb.linearVelocity = direction.normalized * speed;

        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // 自分自身（発射した本体など）との接触を無視したい場合は、Tagではなく必要に応じて調整

        Debug.Log($"{other.name} に命中！ ダメージ: {Damage}");

        // ※ EnemyBase 側で OnTriggerEnter(Collider other) による TakeDamage を行っている場合は、
        //   弾側では Destroy だけ行えば EnemyBase 側でダメージ処理が実行されます。
        Destroy(gameObject);
    }
}