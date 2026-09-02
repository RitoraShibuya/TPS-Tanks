using UnityEngine;

/// <summary>
/// タンク(Player)の調整値をまとめて保持するScriptableObject。
///
/// 位置付け:
/// - このアセット1つが「エクセル(CSV)からインポートされた最新の調整値」を保持する。
/// - 各スクリプト(TankHead / TankAimReticle / TankAimSystem / TankBody /
///   TankProjectile / UDRotater / TankWeapon)は、Inspector上の「Tuning Config」欄に
///   このアセットを登録しておくと、起動時(Awake)にここの値で自動的に上書きされる。
/// - Tuning Configを未設定のままにしておけば、従来通り各スクリプトのInspectorの値が
///   そのまま使われる(後方互換)。
///
/// 運用フロー:
/// 1. Assets/TankTuning/TankTuningConfig.asset を作成する
///    (メニュー: Assets > Create > Tank > Tuning Config)。
/// 2. 各タンクスクリプトのInspectorの「Tuning Config」欄に、このアセットをドラッグ&ドロップする。
/// 3. Player_TankTuning.xlsx をExcelで開き、「現在値」列を編集する。
/// 4. Excelで「名前を付けて保存」→ CSV UTF-8(コンマ区切り) 形式で保存する
///    (列構成を変えないこと。Key列・列の並び順は変更しないでください)。
/// 5. Unityメニュー: Tools > Tank > Tuning CSVをインポート… から、保存したCSVを選択する。
///    → このアセットの値が一括更新され、Playすれば即座に反映される。
///
/// 注意:
/// - フィールド名(例: head_MouseSensitivity)は、CSVの「Key」列と完全一致している必要がある。
///   インポーターはリフレクションでこの名前を頼りに値を書き込むため、フィールド名を変更する場合は
///   CSV側のKey列も同時に変更すること。
/// - InputActionReferenceやLayerMask、Transform、Cameraなどの「参照」系の項目はCSVでは管理しない
///   (Excelのセルにドラッグ&ドロップの参照を入れることはできないため)。これらは引き続き各スクリプトの
///   Inspectorで直接設定する。
///
/// 【変更履歴】
/// - reticle_VerticalStickCoefficient / reticle_StickDeadzone を廃止し、
///   reticle_PixelsPerPitchDegree に置き換えました(TankAimReticleがスティック生入力ではなく、
///   UDRotaterの実際のピッチ角度を参照する方式に変更されたため)。
///   Player_TankTuning.xlsx側のKey列も、古い2項目を削除して
///   reticle_PixelsPerPitchDegree の1行に置き換えてください。古いKey名のままだと、
///   インポート時に「該当フィールドが無い」として無視されるだけで、エラーにはなりません。
/// </summary>
[CreateAssetMenu(fileName = "TankTuningConfig", menuName = "Tank/Tuning Config")]
public class TankTuningConfig : ScriptableObject
{
    [Header("TankHead (HTankAimController.cs)")]
    public float head_MouseSensitivity = 0.2f;
    public float head_GamepadSensitivity = 180f;
    public float head_StickDeadzone = 0.1f;
    public float head_HorizontalDeadzoneBand = 0f;

    [Header("TankAimReticle (HTankAimReticle.cs)")]
    public float reticle_YScreenOffset = 0f;
    public float reticle_PixelsPerPitchDegree = 4f;

    [Header("TankAimSystem (HTankAimSystem.cs)")]
    public float aim_MaxAimDistance = 12f;

    [Header("TankBody (HTankMovement.cs)")]
    public float body_MoveSpeed = 5f;
    public float body_RotateSpeed = 180f;
    public float body_MoveAngleThreshold = 5f;
    public float body_InputDeadzone = 0.1f;

    [Header("TankProjectile (HTankProjectile.cs)")]
    public float projectile_FlightSpeed = 20f;
    public float projectile_LifeTime = 10f;

    [Header("UDRotater (HTankUDRotate.cs)")]
    public float ud_PitchMin = -10f;
    public float ud_PitchMax = 30f;
    public bool ud_InvertPitch = false;
    public float ud_MouseSensitivity = 0.2f;
    public float ud_GamepadSensitivity = 60f;
    public float ud_ReturnSpeed = 90f;
    public float ud_StickDeadzone = 0.1f;
    public float ud_VerticalDeadzoneBand = 0f;

    [Header("TankWeapon (HTankWeapon.cs)")]
    public float weapon_FireInterval = 0.5f;
}