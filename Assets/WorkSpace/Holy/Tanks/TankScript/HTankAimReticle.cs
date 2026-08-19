using UnityEngine;

/// <summary>
/// 照準UI(レティクル)を画面に表示するスクリプト。
///
/// 仕様:
/// - 狙点(TankAimSystemが計算するワールド座標)を画面上の2D座標に変換し、
///   UIレイヤーで表示する。
/// - Y軸オフセットを加えて表示する。
/// - 上下(仰角)の追加移動は、UDRotaterが持つ「実際のピッチ角度(CurrentPitch)」を
///   直接参照して計算する。
///   ※以前は右スティックの生入力(stickInput.y)をそのまま係数倍していたが、
///     これだとマウス操作時に全く反応しない上、UDRotater側の可動範囲クランプや
///     オートセンタリング(戻り速度)を無視した動きになり、実際のカメラの傾きと
///     レティクルの見た目がズレる不具合があった。UDRotaterのCurrentPitchを見る
///     方式にすることで、マウス/スティックどちらの操作でも、また可動範囲の端や
///     オートセンタリング中でも、常に実際のピッチ角度と完全に一致した動きになる。
///
/// セットアップ:
/// - Canvas(Screen Space - Overlay 推奨)の下にUI Imageを配置し、
///   そのUI ImageのGameObjectにこのスクリプトをアタッチする。
/// - 「Aim System」に、Muzzleに付いている TankAimSystem を登録する。
/// - 「Render Camera」に、実際に描画しているカメラ(UDRotaterの子のCameraなど)を登録する。
/// - 「Pitch Source」に、UDRotaterをアタッチしたGameObjectを登録する。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class TankAimReticle : MonoBehaviour
{
    [Header("参照")]
    [Tooltip("狙点の計算に使用する TankAimSystem")]
    [SerializeField]
    private TankAimSystem aimSystem;

    [Tooltip("狙点をスクリーン座標に変換する際に使用するカメラ")]
    [SerializeField]
    private Camera renderCamera;

    [Tooltip("上下(仰角)の追加移動量の計算に使用する UDRotater。" +
             "実際に反映されているピッチ角度(CurrentPitch)をそのまま参照するため、" +
             "マウス/スティックどちらの操作でも、可動範囲クランプやオートセンタリング中でも" +
             "レティクルの動きが実際のカメラの傾きと一致する。")]
    [SerializeField]
    private UDRotater pitchSource;

    [Header("表示調整")]
    [Tooltip("スクリーン座標へのY軸オフセット(ピクセル)")]
    [SerializeField]
    private float yScreenOffset = 0f;

    [Tooltip("ピッチ角度1度あたりの追加上下移動量(ピクセル)。" +
             "UDRotaterのCurrentPitch(度)に、この値を掛けて画面上のオフセットにする。")]
    [SerializeField]
    private float pixelsPerPitchDegree = 4f;

    [Header("エクセル連携")]
    [Tooltip("CSV(Excel)からインポートした調整値を反映するためのTankTuningConfig。" +
             "設定すると、起動時にこのアセットの値で上記のパラメーターが上書きされる。未設定ならこのInspectorの値をそのまま使用する。" +
             "【要対応】TankTuningConfig側の項目名を reticle_VerticalStickCoefficient から" +
             "reticle_PixelsPerPitchDegree 等に変更し、下のApplyTuningConfig()を対応させてください" +
             "(stickDeadzoneの項目は本スクリプトでは不要になったため参照しません)。")]
    [SerializeField]
    private TankTuningConfig tuningConfig;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        ApplyTuningConfig();

        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (renderCamera == null)
        {
            renderCamera = Camera.main;
        }
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

        yScreenOffset = tuningConfig.reticle_YScreenOffset;
        pixelsPerPitchDegree = tuningConfig.reticle_PixelsPerPitchDegree;
    }

    private void LateUpdate()
    {
        if (aimSystem == null || renderCamera == null)
        {
            return;
        }

        Vector3 aimWorldPoint = aimSystem.GetAimWorldPoint(out _);
        Vector3 screenPoint = renderCamera.WorldToScreenPoint(aimWorldPoint);

        // カメラの後ろ(画面に映らない位置)にある場合は非表示にする
        bool isBehindCamera = screenPoint.z <= 0f;
        SetVisible(!isBehindCamera);
        if (isBehindCamera)
        {
            return;
        }

        // UDRotaterの実際のピッチ角度(マウス/スティック入力・可動範囲クランプ・
        // オートセンタリングを全て反映済みの値)を直接参照して上下オフセットを計算する。
        // これにより、操作方法や状態(端に張り付いている/戻っている最中)に関わらず、
        // レティクルの上下位置が常に実際のカメラの傾きと一致する。
        float pitchOffset = pitchSource != null
            ? pitchSource.CurrentPitch * pixelsPerPitchDegree
            : 0f;

        screenPoint.y += yScreenOffset + pitchOffset;

        rectTransform.position = new Vector3(screenPoint.x, screenPoint.y, 0f);
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
        }
        else
        {
            // 【注意】CanvasGroupが無いと、非表示にする際にGameObject自体をSetActive(false)する。
            // これによりOnDisable()が呼ばれるが、上記の通りOnDisable()では何もしないよう変更済みなので
            // 現在は問題ないはず。ただし、このGameObjectにアタッチされた他のコンポーネントの
            // 動作(Update等)も一緒に止まってしまう点は変わらないため、
            // 可能であればこのGameObjectにCanvasGroupコンポーネントを追加し、
            // こちらのSetActive経路を使わないようにすることを推奨する。
            gameObject.SetActive(visible);
        }
    }
}