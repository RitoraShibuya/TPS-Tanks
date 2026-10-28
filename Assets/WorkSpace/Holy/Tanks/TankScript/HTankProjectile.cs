using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TankProjectile : MonoBehaviour
{
    [Header("飛行設定")]
    [SerializeField]
    private float flightSpeed = 20f;

    [SerializeField]
    private float lifeTime = 10f;

    [SerializeField]
    private float gravityMultiplier = 12f;

    private Rigidbody rb;

    // 発射した戦車（自分の弾で自分に当たらないようにする）
    private Transform owner;

    private float straightFlightDistance;
    private float straightFlightTime;
    private float flightElapsedTime;

    private bool isFalling;
    private bool hasHit;


    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        TankParameterManager manager = TankParameterManager.Instance;

        if (manager != null)
        {
            gravityMultiplier = manager.GetFloat(
                "gravityMultiplier",
                gravityMultiplier
            );
        }
    }


    public void Launch(
        Vector3 aimPoint,
        float straightDistance,
        float speed,
        Transform shooter = null)
    {
        owner = shooter;
        straightFlightDistance = straightDistance;
        flightSpeed = speed;

        flightElapsedTime = 0f;
        isFalling = false;

        Vector3 startPosition = transform.position;
        Vector3 direction = aimPoint - startPosition;

        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = transform.forward;
        }
        else
        {
            direction.Normalize();
        }

        transform.rotation = Quaternion.LookRotation(direction);

        straightFlightTime = flightSpeed > 0f
            ? straightFlightDistance / flightSpeed
            : 0f;

        rb.useGravity = false;
        rb.linearVelocity = direction * flightSpeed;

        Destroy(gameObject, lifeTime);
    }

    private void FixedUpdate()
    {
        if (rb == null)
        {
            return;
        }

        if (!isFalling)
        {
            flightElapsedTime += Time.fixedDeltaTime;

            if (flightElapsedTime >= straightFlightTime)
            {
                isFalling = true;
                rb.useGravity = false;
            }
        }

        if (isFalling)
        {
            rb.AddForce(
                Physics.gravity * (gravityMultiplier - 1f),
                ForceMode.Acceleration
            );

            if (rb.linearVelocity.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(
                    rb.linearVelocity
                );
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Impact(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        Impact(other);
    }

    private void Impact(Collider other)
    {
        // 同じフレームで複数のコライダーに当たっても1回だけ処理する
        if (hasHit)
        {
            return;
        }

        // 自分の戦車は無視
        if (owner != null && other.transform.IsChildOf(owner))
        {
            return;
        }

        hasHit = true;

        // 敵に当たったらHPを1減らす
        EnemyBase enemy = other.GetComponentInParent<EnemyBase>();
        if (enemy != null)
        {
            enemy.TakeDamage();
        }

        Destroy(gameObject);
    }
}