using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyBullet : MonoBehaviour
{
    private float damage;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Launch(Vector3 direction, float speed, float attackPower, float lifeTime = 2f)
    {
        damage = attackPower;

        rb.linearVelocity = direction.normalized * speed;

        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy")) return;

        // if (other.TryGetComponent(out IDamageable target))
        // {
        //     target.TakeDamage(damage);
        // }

        Debug.Log($"{other.name} �ɒ��e�I �_���[�W: {damage}");


        Destroy(gameObject);
    }
}