using UnityEngine;

/// <summary>
/// PC仕様書「不感帯について」を実装した共通ユーティリティ。
/// ・入力値30%未満は無視する（①②共通の起動しきい値）
/// ・青枠(クロス状)の不感帯内では、上下/左右いずれか一方の軸のみ有効にする
///   （★仕様書の指示通りデフォルトは「ナシ」。使う場合は useAxisSnap を true にする）
/// </summary>
public static class DeadzoneUtils
{
    /// <summary>
    /// スティックの入力値が起動しきい値(既定30%)未満なら (0,0) を返す。
    /// </summary>
    public static Vector2 ApplyMagnitudeDeadzone(Vector2 raw, float magnitudeThreshold)
    {
        return raw.magnitude < magnitudeThreshold ? Vector2.zero : raw;
    }

    /// <summary>
    /// クロス状(十字)の不感帯。中心付近の帯域(verticalBandWidth/horizontalBandWidth)に
    /// 入っている軸をゼロにし、斜め入力による意図しないブレを防ぐ。
    /// 仕様書指示により既定では未使用（呼び出し側で useAxisSnap=false のときはスキップすること）。
    /// </summary>
    public static Vector2 ApplyAxisSnapDeadzone(Vector2 input, float verticalBandWidth, float horizontalBandWidth)
    {
        bool xInBand = Mathf.Abs(input.x) <= horizontalBandWidth;
        bool yInBand = Mathf.Abs(input.y) <= verticalBandWidth;

        if (xInBand && !yInBand) return new Vector2(0f, input.y);
        if (yInBand && !xInBand) return new Vector2(input.x, 0f);
        return input; // 両方帯域内(中心付近) or 両方帯域外(斜め方向) はそのまま
    }

    /// <summary>
    /// 仕様書の加減速ルール共通実装。
    /// 「単位時間0.03秒・加減同数値・ゼロで等速」を Time.deltaTime ベースで滑らかに追従させる。
    /// accelTime が 0 以下の場合は等速（即座にtargetへ到達）とする。
    /// </summary>
    public static float ApplyAcceleration(float current, float target, float maxValue, float accelTime, float deltaTime)
    {
        if (accelTime <= 0f) return target; // ゼロで等速(=即応答)
        float accelPerSecond = maxValue / accelTime; // 0.03秒でmaxValueに到達する変化率
        return Mathf.MoveTowards(current, target, accelPerSecond * deltaTime);
    }
}
