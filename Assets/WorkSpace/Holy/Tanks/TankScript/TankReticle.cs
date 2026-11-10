
using UnityEngine;
using UnityEngine.InputSystem;

public class TankReticle : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private Camera aimCamera;
    [SerializeField] private Transform muzzle;
    [SerializeField] private RectTransform reticleRect;

    [Header("レティクル位置設定")]
    [Tooltip("砲口のスクリーン座標から上下にずらすピクセル数")]
    [SerializeField] private float screenYOffsetPixels = 40f;

    [Tooltip("Rスティック上下入力による移動量")]
    [SerializeField] private float verticalStickCoefficientPixels = 120f;

    [SerializeField, Range(0f, 1f)]
    private float aimInputDeadzone = 0.3f;

    private void LateUpdate()
    {
        if (aimCamera == null ||
            muzzle == null ||
            reticleRect == null)
        {
            return;
        }

        // 砲口のワールド座標をスクリーン座標に変換
        Vector3 screenPoint =
            aimCamera.WorldToScreenPoint(muzzle.position);

        // カメラの後方にある場合は非表示
        bool visible = screenPoint.z > 0f;

        if (reticleRect.gameObject.activeSelf != visible)
        {
            reticleRect.gameObject.SetActive(visible);
        }

        if (!visible)
        {
            return;
        }

        // Rスティックの上下入力を取得
        float verticalInput = ReadRightStickVertical();

        // 固定オフセットとスティック入力を反映
        screenPoint.y += screenYOffsetPixels
            + verticalInput * verticalStickCoefficientPixels;

        // レティクルを配置
        reticleRect.position = new Vector3(
            screenPoint.x,
            screenPoint.y,
            0f
        );
    }

    private float ReadRightStickVertical()
    {
        Gamepad pad = Gamepad.current;

        if (pad == null)
        {
            return 0f;
        }

        Vector2 rawInput = pad.rightStick.ReadValue();

        // デッドゾーンを適用
        if (rawInput.magnitude < aimInputDeadzone)
        {
            return 0f;
        }

        float adjustedMagnitude =
            (rawInput.magnitude - aimInputDeadzone)
            / (1f - aimInputDeadzone);

        Vector2 adjustedInput =
            rawInput.normalized * adjustedMagnitude;

        return adjustedInput.y;
    }
}
