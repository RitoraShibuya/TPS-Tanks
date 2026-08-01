using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// PC仕様「照準について」を担当。
/// 「TurretのMuzzleの画面内2D座標(画面に投影した時の座標)に対してUIレイヤーで表示・
/// 　Y軸オフセットをもって表示する。Rスティックの上下操作(仰角)に対し、
/// 　係数を持って照準も上下に移動する」を実装する。
///
/// UIのRectTransform(reticleRect)をCanvas(Screen Space - Overlay/Camera)上で動かす想定。
/// </summary>
public class TankReticle : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private Camera aimCamera;
    [SerializeField] private Transform muzzle;
    [SerializeField] private RectTransform reticleRect;

    [Header("オフセット (Inspectorで調整可)")]
    [Tooltip("Muzzle投影座標に加算するY軸ピクセルオフセット。")]
    [SerializeField] private float screenYOffsetPixels = 40f;
    [Tooltip("Rスティック上下値に掛ける追加の照準移動係数(ピクセル/入力値)。")]
    [SerializeField] private float verticalStickCoefficientPixels = 120f;
    [SerializeField, Range(0f, 1f)] private float aimInputDeadzone = 0.3f;

    private void LateUpdate()
    {
        if (aimCamera == null || muzzle == null || reticleRect == null) return;

        Vector3 screenPoint = aimCamera.WorldToScreenPoint(muzzle.position);

        // カメラの後方(画面外)にある場合は表示しない
        bool visible = screenPoint.z > 0f;
        reticleRect.gameObject.SetActive(visible);
        if (!visible) return;

        float verticalInput = ReadRightStickVertical();
        screenPoint.y += screenYOffsetPixels + verticalInput * verticalStickCoefficientPixels;

        reticleRect.position = new Vector3(screenPoint.x, screenPoint.y, 0f);
    }

    private float ReadRightStickVertical()
    {
        var pad = Gamepad.current;
        if (pad == null) return 0f;
        Vector2 raw = pad.rightStick.ReadValue();
        Vector2 input = DeadzoneUtils.ApplyMagnitudeDeadzone(raw, aimInputDeadzone);
        return input.y;
    }
}