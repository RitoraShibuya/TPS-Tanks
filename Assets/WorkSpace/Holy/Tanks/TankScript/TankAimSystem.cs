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
///    → Muzzleから真っ直ぐ Max Aim Distance(既定 6m)先の地点を狙点とする。
///
/// セットアップ:
/// - Muzzle(砲口)のGameObjectにアタッチする。
/// - Muzzleの正面方向(transform.forward)を狙う方向として使用する。
/// </summary>
public class TankAimSystem : MonoBehaviour
{
    [Header("照準設定")]
    [Tooltip("狙点までの最大距離(m)。仕様書記載の既定値は6m。")]
    [SerializeField]
    private float maxAimDistance = 6f;

    [Tooltip("レイキャストで当たり判定を取るレイヤー。" +
             "戦車自身(Body/Turret/Muzzleなど)は含めないようにレイヤー分けしてください。")]
    [SerializeField]
    private LayerMask aimLayerMask = ~0; // 既定は全レイヤー。プロジェクトに合わせて調整してください。

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

    /// <summary>
    /// 狙点までの距離を取得する(弾の直線飛行距離の計算に使用)。
    /// </summary>
    public float GetAimDistance(out bool hitSomething)
    {
        Vector3 aimPoint = GetAimWorldPoint(out hitSomething);
        return Vector3.Distance(transform.position, aimPoint);
    }
}