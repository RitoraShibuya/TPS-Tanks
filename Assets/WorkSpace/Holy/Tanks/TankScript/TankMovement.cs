using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// タンク型の移動を行うコントローラー。
/// Input Systemを使用し、キーボード(WASD)とゲームパッド(左スティック)の両方に対応。
///
/// 動作仕様:
/// - W / 左スティック上  : 前進
/// - S / 左スティック下  : 後退
/// - A / 左スティック左  : 左旋回(その場で回転)
/// - D / 左スティック右  : 右旋回(その場で回転)
///
/// 事前準備:
/// 1. Edit > Project Settings > Player > Active Input Handling を
///    "Input System Package (New)" または "Both" に設定してください。
/// 2. Package Manager で "Input System" パッケージがインストールされていることを確認してください。
/// 3. このスクリプトをタンクのGameObjectにアタッチしてください(Rigidbodyが自動で追加されます)。
/// 4. Rigidbodyの Constraints で Freeze Rotation X, Z にチェックを入れると
///    タンクが倒れずに安定して動きます(お好みで調整してください)。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class TankController : MonoBehaviour
{
    [Header("移動設定")]
    [Tooltip("前後移動の速度 (m/s)")]
    [SerializeField]
    private float moveSpeed = 5f;

    [Tooltip("旋回速度 (度/秒)")]
    [SerializeField]
    private float rotateSpeed = 100f;

    private Rigidbody rb;
    private InputAction moveAction;
    private Vector2 moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Move用のInputActionをコード上で定義
        // (Input Actionアセットを別途作らなくても動作する簡易構成)
        moveAction = new InputAction(
            name: "Move",
            type: InputActionType.Value,
            expectedControlType: "Vector2"
        );

        // キーボード WASD を Vector2 として合成
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        // ゲームパッドの左スティックをそのままバインド
        moveAction.AddBinding("<Gamepad>/leftStick");
    }

    private void OnEnable()
    {
        moveAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
    }

    private void Update()
    {
        // 入力値を毎フレーム取得 (x: 旋回, y: 前後)
        moveInput = moveAction.ReadValue<Vector2>();
    }

    private void FixedUpdate()
    {
        // 前後移動(タンクの正面方向へ移動)
        Vector3 forwardMove = transform.forward * moveInput.y * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + forwardMove);

        // 旋回(その場でY軸回転)
        float rotationAmount = moveInput.x * rotateSpeed * Time.fixedDeltaTime;
        Quaternion turnRotation = Quaternion.Euler(0f, rotationAmount, 0f);
        rb.MoveRotation(rb.rotation * turnRotation);
    }
}