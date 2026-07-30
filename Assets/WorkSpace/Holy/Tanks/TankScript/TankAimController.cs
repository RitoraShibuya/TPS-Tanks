using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// PC仕様「② カメラ回転/上下＋砲台旋回/砲身上下」を担当。
///
/// 想定する既存アセットの階層:
///   Tank(本体, TankMovementが付く)
///     └ Turret(砲台。左右旋回はここでワールド基準の角度を独立して制御)
///         └ UDRotator(砲身の上下ピボット)
///             ├ Muzzle
///             ├ Barrel
///             └ BrlShadow
/// Muzzle/Barrel/BrlShadowはUDRotatorの子として一緒に回転するだけなので、
/// このスクリプトではUDRotatorの回転だけを操作すればよい。
///
/// 仕様まとめ:
/// ・Rスティック入力値30%以上で、左右→カメラとTurretを回転、上下→カメラとUDRotator(砲身)を上下(仰角リミットあり・上下別)。
/// ・カメラの基準位置: Turret原点中心の半径6mの球上、水平から20°、Turretの真後ろ、Turret原点からY+1.5mを注視。
/// ・上下操作はスティックがニュートラルに戻ると基準角度(20°)へ追従して戻る(=蓄積せず係数で決まる)。
/// ・左右(旋回)は蓄積式(押している間、回り続ける)。
/// ・Turretは本体(Tank)の回転に影響されず、常にワールド基準の砲台角度を維持する
///   (本体が回転してもTurretのワールド向きは変わらないよう、ローカル回転を都度補正する)。
/// </summary>
public class TankAimController : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private Transform tankBody;      // 回転補正用に本体のTransformが必要
    [SerializeField] private Transform turret;        // 砲台(左右旋回)
    [SerializeField] private Transform udRotator;     // 砲身上下ピボット(Muzzle/Barrel/BrlShadowの親)
    [SerializeField] private Transform cameraTransform;

    [Header("入力 (Inspectorで調整可)")]
    [SerializeField, Range(0f, 1f)] private float aimInputDeadzone = 0.3f;
    [Tooltip("★仕様書指示によりデフォルトは未使用。斜め入力の軸スナップが必要になったらON。")]
    [SerializeField] private bool useAxisSnapDeadzone = false;
    [SerializeField] private float axisSnapVerticalBand = 0.15f;
    [SerializeField] private float axisSnapHorizontalBand = 0.15f;

    [Header("カメラ基準位置 (仕様書指定値)")]
    [SerializeField] private float orbitRadius = 6f;          // 半径6mの球上
    [SerializeField] private float baseElevationDeg = 20f;    // 水平から20°
    [SerializeField] private float lookHeightOffset = 1.5f;   // Turret原点からY+1.5mを注視

    [Header("旋回・仰角パラメータ (仮値)")]
    [SerializeField] private float turretYawSpeedDegPerSec = 180f;
    [Tooltip("上下スティック値に掛ける係数。仰角 = baseElevationDeg + stickY * elevationCoefficient")]
    [SerializeField] private float elevationCoefficient = 45f;
    [Tooltip("仰角の上限(見上げ方向)。上下でリミットが別、という仕様に対応。")]
    [SerializeField] private float maxElevationDeg = 60f;
    [Tooltip("仰角の下限(見下ろし方向)。マイナス値。")]
    [SerializeField] private float minElevationDeg = -10f;
    [SerializeField] private float accelTime = 0.03f;

    /// <summary>現在の砲台ワールドYaw角(度)。蓄積式。</summary>
    private float turretWorldYaw;
    private float currentYawRate;
    private float currentElevationDeg;

    private void Awake()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
        turretWorldYaw = tankBody != null ? tankBody.eulerAngles.y : 0f;
        currentElevationDeg = baseElevationDeg;
    }

    private void LateUpdate()
    {
        Vector2 rawInput = ReadRightStick();
        Vector2 input = DeadzoneUtils.ApplyMagnitudeDeadzone(rawInput, aimInputDeadzone);
        if (useAxisSnapDeadzone && input.sqrMagnitude > 0f)
        {
            input = DeadzoneUtils.ApplyAxisSnapDeadzone(input, axisSnapVerticalBand, axisSnapHorizontalBand);
        }

        UpdateYaw(input.x);
        UpdateElevation(input.y);
        ApplyTurretRotation();
        ApplyCameraTransform();
    }

    /// <summary>左右入力で砲台(および同期するカメラ方位)を蓄積回転させる。</summary>
    private void UpdateYaw(float horizontalInput)
    {
        float targetYawRate = horizontalInput * turretYawSpeedDegPerSec;
        currentYawRate = DeadzoneUtils.ApplyAcceleration(currentYawRate, targetYawRate, turretYawSpeedDegPerSec, accelTime, Time.deltaTime);
        turretWorldYaw += currentYawRate * Time.deltaTime;
    }

    /// <summary>
    /// 上下入力で仰角を係数式に決定する(蓄積しない)。
    /// スティックがニュートラルに戻れば基準角度(baseElevationDeg)に戻る。
    /// </summary>
    private void UpdateElevation(float verticalInput)
    {
        float targetElevation = Mathf.Clamp(
            baseElevationDeg + verticalInput * elevationCoefficient,
            minElevationDeg,
            maxElevationDeg);
        currentElevationDeg = DeadzoneUtils.ApplyAcceleration(
            currentElevationDeg, targetElevation, maxElevationDeg - minElevationDeg, accelTime, Time.deltaTime);
    }

    /// <summary>
    /// Turretは本体の回転と独立してワールド基準の向きを保つため、
    /// ローカル回転 = ワールド目標Yaw - 本体Yaw で補正する。
    /// UDRotatorはTurretのローカル子として仰角のみ持たせる。
    /// </summary>
    private void ApplyTurretRotation()
    {
        if (turret == null) return;
        float bodyYaw = tankBody != null ? tankBody.eulerAngles.y : 0f;
        float turretLocalYaw = Mathf.DeltaAngle(0f, turretWorldYaw - bodyYaw);
        turret.localRotation = Quaternion.Euler(0f, turretLocalYaw, 0f);

        if (udRotator != null)
        {
            // Unityは+X回転で下向きになるため、見上げを正にするには-currentElevationDegを使う
            udRotator.localRotation = Quaternion.Euler(-currentElevationDeg, 0f, 0f);
        }
    }

    /// <summary>
    /// カメラをTurret原点中心・半径orbitRadiusの球上、Turretの真後ろ・現在の仰角の位置へ配置し、
    /// Turret原点からY+lookHeightOffsetの点を注視させる。
    /// </summary>
    private void ApplyCameraTransform()
    {
        if (cameraTransform == null || turret == null) return;

        Vector3 turretOrigin = turret.position;
        // 「真後ろ」= 砲台の正面と反対方向(yaw + 180°)
        float azimuthDeg = turretWorldYaw + 180f;
        float azimuthRad = azimuthDeg * Mathf.Deg2Rad;
        float elevationRad = currentElevationDeg * Mathf.Deg2Rad;

        // 球面座標 → ワールド座標(Y-upのUnity座標系)
        float horizontalDist = orbitRadius * Mathf.Cos(elevationRad);
        Vector3 offset = new Vector3(
            horizontalDist * Mathf.Sin(azimuthRad),
            orbitRadius * Mathf.Sin(elevationRad),
            horizontalDist * Mathf.Cos(azimuthRad));

        cameraTransform.position = turretOrigin + offset;

        Vector3 lookTarget = turretOrigin + Vector3.up * lookHeightOffset;
        cameraTransform.LookAt(lookTarget, Vector3.up);
    }

    private Vector2 ReadRightStick()
    {
        var pad = Gamepad.current;
        return pad != null ? pad.rightStick.ReadValue() : Vector2.zero;
    }

    /// <summary>照準UI等の外部スクリプトから現在の仰角(度)を参照するためのアクセサ。</summary>
    public float CurrentElevationDeg => currentElevationDeg;

    /// <summary>照準UI等の外部スクリプトから現在の砲台ワールドYaw(度)を参照するためのアクセサ。</summary>
    public float TurretWorldYaw => turretWorldYaw;
}