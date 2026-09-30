using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class TankMovement : MonoBehaviour
{
    [Header("入力設定 (Input System)")]
    public InputAction moveAction = new InputAction("Move");
    public InputAction aimAction = new InputAction("Aim");

    [Header("入力設定Keyborad (Input System)")]
    private InputActionReference moveActionKeyboard;
    private InputActionReference aimActionKeyboard;

    [Header("移動・回転設定 (Lスティック)")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 360f;
    public float deadZone = 0.3f;

    [Header("砲台・カメラ設定 (Rスティック)")]
    public float aimHorizontalSpeed = 120f;
    public float aimVerticalSpeed = 90f;
    public float maxElevation = 30f;
    public float minElevation = -10f;
    public float autoResetSpeed = 5f;

    [Header("カメラのオフセット位置")]
    public Vector3 cameraOffset = new Vector3(0f, 3f, -8f);

    [Header("参照オブジェクト (同階層)")]
    public Transform tankBody;
    public Transform tankTurret;
    public Transform mainCamera;

    [Header("エイム・砲身パーツ (同階層)")]
    public Transform udRotator;
    public Transform[] barrelParts;

    [Header("坂道判定")]
    public LayerMask groundLayer = -1;
    public float rayLength = 2.0f;
    public float rayOffsetHeight = 1.0f;

    private Rigidbody rb;
    private Vector3 currentTurretForward;
    private float currentPitch = 0f;

    [Header("値チェック")]
    public  float Val1= 0;


    private void Awake()
    {
        moveAction.AddBinding("<Gamepad>/leftStick");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        aimAction.AddBinding("<Gamepad>/rightStick");
        aimAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");
    }

    private void OnEnable()
    {
        moveAction.Enable();
        aimAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        aimAction.Disable();
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        if (mainCamera == null)
        {
            mainCamera = Camera.main.transform;
        }

        // 初期方向の取得（安全対策として値がゼロにならないよう保証）
        if (tankTurret != null)
        {
            Vector3 projected = Vector3.ProjectOnPlane(tankTurret.forward, Vector3.up);
            if (projected.sqrMagnitude > 0.01f)
                currentTurretForward = -projected.normalized; // 180度反転モデルなのでマイナス
            else
                currentTurretForward = transform.forward;
        }
        else
        {
            currentTurretForward = transform.forward;
        }
    }

    void Update()
    {
        // ============================================
        // Rスティック処理 (エイム計算)
        // ============================================
        Vector2 rightInput = aimAction.ReadValue<Vector2>();

        if (rightInput.magnitude >= deadZone)
        {
            // 左右：論理的な「向いている方向」を回転
            Quaternion yawRot = Quaternion.AngleAxis(rightInput.x * aimHorizontalSpeed * Time.deltaTime, Vector3.up);
            currentTurretForward = yawRot * currentTurretForward;

            // 上下：ピッチ角度の更新
            currentPitch -= rightInput.y * aimVerticalSpeed * Time.deltaTime;
            currentPitch = Mathf.Clamp(currentPitch, minElevation, maxElevation);
        }
        else
        {
            // ニュートラル時に水平へ戻る
            currentPitch = Mathf.Lerp(currentPitch, 0f, autoResetSpeed * Time.deltaTime);
        }
    }

    void FixedUpdate()
    {
        // ============================================
        // Lスティック処理 (移動・車体回転)
        // ============================================
        Vector2 leftInput = moveAction.ReadValue<Vector2>();
        bool isMoving = leftInput.magnitude >= deadZone;

        // ブレーキ処理（移動していない時はXとZをロック）
        if (!isMoving)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
        }
        else
        {
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }


        // ============================================
        // 坂道判定 (停止中も砲台の傾きのために常に計算する)
        // ============================================
        Vector3 groundNormal = Vector3.up;
        Vector3 rayStart = transform.position + Vector3.up * rayOffsetHeight;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, rayLength, groundLayer))
        {
            groundNormal = hit.normal;
        }

        //坂道に入ったら重力を無効化
        float dot = Vector3.Dot(hit.normal, Vector3.up);
        Val1 = dot;
        if (dot > 0.99f || dot <= 0)
        {
            rb.useGravity = true;
        }
        else
        {
            rb.useGravity = false;
        }

        Quaternion tiltRotation = Quaternion.FromToRotation(Vector3.up, groundNormal);
        Quaternion offsetRot = Quaternion.Euler(0, 180, 0); // モデルの180度反転オフセット

        // ============================================
        // 移動と車体の回転 (Lスティック入力時のみ実行)
        // ============================================
        if (isMoving)
        {
            Vector2 moveInput = leftInput.normalized;
            Vector3 camForward = mainCamera.forward;
            Vector3 camRight = mainCamera.right;

            camForward.y = 0;
            camRight.y = 0;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 baseMoveDir = camForward * moveInput.y + camRight * moveInput.x;

            // 車体の回転
            if (baseMoveDir != Vector3.zero && tankBody != null)
            {
                Quaternion bodyYaw = Quaternion.LookRotation(baseMoveDir, Vector3.up);
                Quaternion bodyTargetRot = tiltRotation * bodyYaw * offsetRot;

                tankBody.rotation = Quaternion.RotateTowards(
                    tankBody.rotation,
                    bodyTargetRot,
                    rotationSpeed * Time.fixedDeltaTime
                );

                float angle = Quaternion.Angle(
                    tankBody.rotation,
                    bodyTargetRot
                );

                if (angle < 1.0f)
                {
                    Vector3 moveDir = Vector3.ProjectOnPlane(
                        baseMoveDir,
                        groundNormal
                    ).normalized;

                    Vector3 nextPosition =
                        rb.position + moveDir * moveSpeed * Time.fixedDeltaTime;

                    rb.MovePosition(nextPosition);
                }
            }
        }

        // ============================================
        // 砲台・砲身の回転 (停止中も含め、毎フレーム常に実行)
        // ============================================

        // ★砲台（Turret）の回転
        Quaternion logicalYawRot = Quaternion.LookRotation(currentTurretForward, Vector3.up);
        Quaternion turretBaseRot = tiltRotation * logicalYawRot;

        if (tankTurret != null)
        {
            tankTurret.rotation = turretBaseRot * offsetRot;
        }

        // ★UDRotator と 砲身パーツの回転 UDRotatorの回転はModelの問題であっていないかも
        if (udRotator != null)
        {
            Quaternion udTargetRot = turretBaseRot * Quaternion.Euler(currentPitch, 0, 0) * offsetRot;
            udRotator.rotation = udTargetRot;

            if (barrelParts != null)
            {
                foreach (var part in barrelParts)
                {
                    if (part != null)
                    {
                        part.rotation = udTargetRot;
                    }
                }
            }
        }
    }

    void LateUpdate()
    {
        // ============================================
        // カメラの追従処理
        // ============================================
        if (mainCamera != null && tankTurret != null)
        {
            // ★坂道の傾き（tiltRotation）を一切使わず、Y軸（Yaw）と ピッチ（上下）のみで計算
            Quaternion camBaseYaw = Quaternion.LookRotation(currentTurretForward, Vector3.up);
            Quaternion camTargetRot = camBaseYaw * Quaternion.Euler(currentPitch, 0, 0);

            mainCamera.rotation = camTargetRot;
            mainCamera.position = tankTurret.position + camTargetRot * cameraOffset;
        }
    }
}