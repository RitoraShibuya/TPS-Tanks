using UnityEngine;

/// <summary>
/// 照準点(狙点)の計算を行うスクリプト。
/// 発射処理(TankWeapon)と照準UI表示(TankAimReticle)の両方から参照される、
/// 「今どこを狙っているか」を計算する共通ロジック。
///
/// 仕様:
/// ① 照準(Muzzleの向いている方向)内に当たり判定付きオブジェクトが存在する場合
///    → そのオブジェクトに当たった地点を狙点とする。
/// ② 存在しない場合
///    → Muzzleから真っ直ぐ Max Aim Distance 先の地点を狙点とする。
///
/// 重要: この Max Aim Distance は、弾(TankProjectile)が直線で飛ぶ距離(落下を始める距離)
/// と同じ値として共有される。TankWeaponがこの値を弾のLaunch()に渡すため、
/// 「照準の表示位置」と「実際に弾が落下し始める距離」は常に一致する。
/// 距離を変更したい場合は、このコンポーネントの Max Aim Distance だけを変更すればよい
/// (TankProjectile側には距離の設定項目は無い)。
///
/// セットアップ:
/// - Muzzle(砲口)のGameObjectにアタッチする。
/// - Muzzleの正面方向(transform.forward)を狙う方向として使用する。
/// </summary>
public class TankAimSystem : MonoBehaviour
{
    [Header("照準設定")]
    [Tooltip("狙点までの最大距離(m)。弾(TankProjectile)が直線で飛ぶ距離(落下を始める距離)としても" +
             "共有される、唯一の距離設定。パラメーター表の「直線距離」に対応。")]
    [SerializeField]
    private float maxAimDistance = 12f;

    [Tooltip("レイキャストで当たり判定を取るレイヤー。" +
             "戦車自身(Body/Turret/Muzzleなど)や弾自身(Bulletレイヤーなど)は" +
             "含めないようにレイヤー分けしてください。")]
    [SerializeField]
    private LayerMask aimLayerMask = ~0; // 既定は全レイヤー。プロジェクトに合わせて調整してください。

    [Header("エクセル連携")]
    [Tooltip("CSV(Excel)からインポートした調整値を反映するためのTankTuningConfig。" +
             "設定すると、起動時にこのアセットの値でMax Aim Distanceが上書きされる。未設定ならこのInspectorの値をそのまま使用する。")]
    [SerializeField]
    private TankTuningConfig tuningConfig;

    /// <summary>
    /// 狙点までの最大距離(=弾の直線飛行距離)。TankWeaponがLaunch()に渡す際に使用する。
    /// </summary>
    public float MaxAimDistance => maxAimDistance;

    private void Awake()
    {
        ApplyTuningConfig();
    }

    /// <summary>
    /// TankTuningConfig(CSV/Excelからインポートされた値)が設定されている場合、
    /// そちらの値でこのスクリプトのパラメーターを上書きする。
    /// </summary>
    private void ApplyTuningConfig()
    {
        if (tuningConfig == null)
        {
            return;
        }

        maxAimDistance = tuningConfig.aim_MaxAimDistance;
    }

    /// <summary>
    /// 現在の狙点(ワールド座標)を取得する。
    /// </summary>
    /// <param name="hitSomething">当たり判定付きオブジェクトに命中したかどうか</param>
    public Vector3 GetAimWorldPoint(out bool hitSomething)
    {
        Ray ray = new Ray(transform.position, transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, maxAimDistance, aimLayerMask, QueryTriggerInteraction.Ignore))
        {
            hitSomething = true;
            return hit.point;
        }

        hitSomething = false;
        return transform.position + transform.forward * maxAimDistance;
    }
}