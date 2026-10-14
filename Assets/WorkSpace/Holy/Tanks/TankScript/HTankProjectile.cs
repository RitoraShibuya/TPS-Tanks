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

    private float straightFlightDistance;
    private float straightFlightTime;
    private float flightElapsedTime;

    private bool isFalling;

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
        float speed)
    {
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
            // Unity標準の重力に追加して、指定倍率の重力を適用
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
        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        Destroy(gameObject);
    }

    
}