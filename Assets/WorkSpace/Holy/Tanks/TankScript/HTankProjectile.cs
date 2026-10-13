using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TankProjectile : MonoBehaviour
{
    [Header("飛行設定")]
    [SerializeField]
    private float flightSpeed = 20f;

    [SerializeField]
    private float lifeTime = 10f;

    private Rigidbody rb;
    private Vector3 startPosition;
    private float straightFlightDistance;
    private bool isFalling;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    public void Launch(
     Vector3 aimPoint,
     float straightDistance,
     float speed)
    {
        startPosition = transform.position;
        straightFlightDistance = straightDistance;
        flightSpeed = speed;
        isFalling = false;

        Vector3 direction = (aimPoint - startPosition).normalized;

        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = transform.forward;
        }

        transform.rotation = Quaternion.LookRotation(direction);
        rb.useGravity = false;
        rb.linearVelocity = direction * flightSpeed;

        Destroy(gameObject, lifeTime);
    }
    private void FixedUpdate()
    {
        if (isFalling)
        {
            if (rb.linearVelocity.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(
                    rb.linearVelocity
                );
            }

            return;
        }

        float traveledDistance = Vector3.Distance(
            startPosition,
            transform.position
        );

        if (traveledDistance >= straightFlightDistance)
        {
            isFalling = true;
            rb.useGravity = true;
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
