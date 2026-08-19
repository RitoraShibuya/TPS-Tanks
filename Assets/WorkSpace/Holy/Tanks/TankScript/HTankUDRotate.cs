using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// カメラのピッチ(上下)回転のみを担当するスクリプト。
/// Input Systemを使用し、マウスの縦方向移動、またはゲームパッド右スティックの上下入力で
/// 上下方向の視点操作を行う。
///
/// 役割分担:
/// - TankHead: ヨー(左右)の回転と、Bodyへの位置追従を担当する。
/// - このスクリプト(UDRotater): ピッチ(上下)の回転のみを担当する。
///
/// 動作仕様:
/// - Headの子オブジェクトとして配置し、ローカルX軸(ピッチ)のみを回転させる。
/// - ヨー(Y軸)・ロール(Z軸)には一切関与しない(親であるHeadのヨー回転がそのまま反映される)。
/// - キーボード+マウス操作: マウスでのピッチ操作は、これまで通り常にそのまま加算する
///   (オートセンタリングは行わない)。
/// - コントローラー操作: 右スティックの上下操作でピッチを変化させる。
///   スティックをニュートラルに戻すと、ピッチは自動で0度(水平)へ戻る(オートセンタリング)。
///   戻る速度は Return Speed で調整する。
///   ※ゲームパッドが実際に接続されている時のみオートセンタリングが働く
///   (未接続時はスティック値が常に0として読めてしまい、マウス操作と競合するのを防ぐため)。
///
/// セットアップ:
/// 1. Headの子として、このスクリプトをアタッチしたGameObject(UDRotater)を配置する。
/// 2. カメラ(Camera)は、このUDRotaterの子として配置する。
///    階層: Head(ヨー) > UDRotater(ピッチ) > Camera
/// </summary>
public class UDRotater : MonoBehaviour
{
    [Header("Input Action")]
    [Tooltip("マウス移動量に使用するInputAction(Vector2)。TankControls.inputactionsの「MouseLook」を割り当てる。" +
             "TankHeadと同じアクションを指定してよい。")]
    [SerializeField]
    private InputActionReference mouseLookActionReference;

    [Tooltip("ゲームパッド右スティックに使用するInputAction(Vector2)。TankControls.inputactionsの「StickLook」を割り当てる。" +
             "TankHeadと同じアクションを指定してよい。")]
    [SerializeField]
    private InputActionReference stickLookActionReference;

    [Header("ピッチ回転設定")]
    [Tooltip("上下(ピッチ)の可動範囲の下限(度)。俯角。パラメーター表: -10度")]
    [SerializeField]
    private float pitchMin = -10f;

    [Tooltip("上下(ピッチ)の可動範囲の上限(度)。仰角。パラメーター表: 30度")]
    [SerializeField]
    private float pitchMax = 30f;

    [Tooltip("チェックすると上下方向の入力を反転する")]
    [SerializeField]
    private bool invertPitch = false;

    [Header("マウス設定")]
    [Tooltip("マウスでのピッチ回転感度")]
    [SerializeField]
    private float mouseSensitivity = 0.2f;

    [Header("ゲームパッド設定")]
    [Tooltip("右スティック操作時のピッチ回転速度(度/秒)。パラメーター表の「上下操作時速度」: 60度")]
    [SerializeField]
    private float gamepadSensitivity = 60f;

    [Tooltip("スティックをニュートラルに戻した時、ピッチを0度へ戻す速度(度/秒)。" +
             "パラメーター表の「戻り時速度」: 90度")]
    [SerializeField]
    private float returnSpeed = 90f;

    [Tooltip("この大きさ未満の右スティック入力は無視する(遊び・誤差吸収用)")]
    [SerializeField]
    private float stickDeadzone = 0.1f;

    [Header("不感帯設定(斜め入力時の軸スナップ)")]
    [Tooltip("スティックの上下(Y)成分がこの値以内の場合、上下入力を無視して左右(ヨー)のみの動作にする。" +
             "仕様書の既定は「ナシ」(0)。まずは0で試し、必要になった場合のみ値を入れる。")]
    [SerializeField]
    private float verticalDeadzoneBand = 0f;

    [Header("エクセル連携")]
    [Tooltip("CSV(Excel)からインポートした調整値を反映するためのTankTuningConfig。" +
             "設定すると、起動時にこのアセットの値で上記のパラメーターが上書きされる。未設定ならこのInspectorの値をそのまま使用する。")]
    [SerializeField]
    private TankTuningConfig tuningConfig;

    private float pitch;

    private void Awake()
    {
        ApplyTuningConfig();

        // 初期回転をpitchに反映しておく(あらかじめ角度が付いている場合のズレ防止)
        pitch = NormalizePitch(transform.localEulerAngles.x);
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

        pitchMin = tuningConfig.ud_PitchMin;
        pitchMax = tuningConfig.ud_PitchMax;
        invertPitch = tuningConfig.ud_InvertPitch;
        mouseSensitivity = tuningConfig.ud_MouseSensitivity;
        gamepadSensitivity = tuningConfig.ud_GamepadSensitivity;
        returnSpeed = tuningConfig.ud_ReturnSpeed;
        stickDeadzone = tuningConfig.ud_StickDeadzone;
        verticalDeadzoneBand = tuningConfig.ud_VerticalDeadzoneBand;
    }

    private void OnEnable()
    {
        if (mouseLookActionReference != null)
        {
            mouseLookActionReference.action.Enable();
        }

        if (stickLookActionReference != null)
        {
            stickLookActionReference.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (mouseLookActionReference != null)
        {
            mouseLookActionReference.action.Disable();
        }

        if (stickLookActionReference != null)
        {
            stickLookActionReference.action.Disable();
        }
    }

    private void Update()
    {
        // マウス入力(1フレームあたりの移動量なので、そのまま感度を掛けて使用)
        Vector2 mouseDelta = mouseLookActionReference != null
            ? mouseLookActionReference.action.ReadValue<Vector2>()
            : Vector2.zero;

        // 右スティック入力(-1〜1の傾き具合なので、度/秒として時間を掛けて使用)
        Vector2 stickInput = stickLookActionReference != null
            ? stickLookActionReference.action.ReadValue<Vector2>()
            : Vector2.zero;

        if (stickInput.sqrMagnitude < stickDeadzone * stickDeadzone)
        {
            stickInput = Vector2.zero;
        }

        // 不感帯(軸スナップ): 上下(Y)成分が「上下幅」以内なら、
        // 斜め入力とみなさず左右(ヨー)のみの動作にする(上下を無視する)
        if (Mathf.Abs(stickInput.y) <= verticalDeadzoneBand)
        {
            stickInput.y = 0f;
        }

        float pitchSign = invertPitch ? 1f : -1f;

        // マウス操作(キーボード+マウス)は、常にそのまま加算する。元通り、自由に見られる。
        pitch += mouseDelta.y * mouseSensitivity * pitchSign;

        // オートセンタリングは「ゲームパッドが実際に接続されている場合」のみ行う。
        // ゲームパッドが無いと右スティック入力は常に0として読み取られてしまうため、
        // 接続判定を入れないとキーボード+マウス操作時にも毎フレーム0度へ戻ろうとして
        // マウス操作と competing してしまう(この不具合を修正した)。
        if (Gamepad.current != null)
        {
            if (stickInput.y != 0f)
            {
                // スティックに上下入力がある間は、通常通りその入力量で回転させる
                pitch += stickInput.y * gamepadSensitivity * Time.deltaTime * pitchSign;
            }
            else
            {
                // スティックがニュートラルに戻ったら、ピッチを0度へ戻り時速度で戻す(オートセンタリング)
                pitch = Mathf.MoveTowards(pitch, 0f, returnSpeed * Time.deltaTime);
            }
        }

        // 可動範囲を制限
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

        // 親(Head)のヨー回転はそのまま活かし、ローカルX軸(ピッチ)だけを回転させる
        transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    /// <summary>
    /// eulerAngles.x は 0〜360度で返ってくるため、-180〜180度の範囲に変換する。
    /// </summary>
    private float NormalizePitch(float angle)
    {
        if (angle > 180f)
        {
            angle -= 360f;
        }
        return angle;
    }
}