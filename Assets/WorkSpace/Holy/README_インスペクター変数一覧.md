# タンクスクリプト インスペクター変数 README

プレイヤー戦車まわりのスクリプトで、Inspector 上で編集できる変数をまとめたものです。
設定・調整時の参考にしてください。

> 以前の構成（TankHead / TankBody / UDRotater / TankWeapon / TankAimSystem / TankAimReticle）は削除済みです。
> 現在は下記の `TankMovement` が移動・砲塔・カメラ・発射をまとめて担当しています。

---

## TankMovement (Kominato/TankMovement.cs)
プレイヤー戦車の本体。移動・車体回転・砲塔/砲身の回転・カメラ追従・発射・着弾レティクル表示を担当。
`Rigidbody` が必要。`HHelth` が無い場合は自動で追加される。

| 変数名 | 型 | 既定値 | CSVキー | 説明 |
|---|---|---|---|---|
| Move / Aim / Fire Action | InputAction | - | - | 入力。バインド未設定なら L/R スティック・WASD/矢印キー・L1/R キーが自動で割り当てられる |
| Move Speed | float | 5 | moveSpeed | 前進の速度(m/s) |
| Rotation Speed | float | 360 | rotationSpeed | 入力方向へ向くまでの車体旋回速度(度/秒) |
| Dead Zone | float | 0.3 | deadZone | この大きさ未満のスティック入力は無視 |
| Aim Horizontal Speed | float | 120 | aimHorizontalSpeed | Rスティック左右での砲塔旋回速度(度/秒) |
| Aim Vertical Speed | float | 90 | aimVerticalSpeed | ※現在未使用（仰角はスティックの倒し量で直接決まる） |
| Max / Min Elevation | float | 30 / -10 | maxElevation / minElevation | 砲身の仰角・俯角の上限(度) |
| Auto Reset Speed | float | 90 | autoResetSpeed | スティックを離した時に仰角を0度へ戻す速度(度/秒) |
| Pitch Snap Angle | float | 30 | - | Rスティックを水平からこの角度以内に倒している時は上下入力を無視する |
| Camera Offset | Vector3 | (0, 3, -8) | cameraHeight / cameraDistance | 砲塔からのカメラ位置。CSVに値があればY/Zは上書きされる |
| Tank Body / Tank Turret / Main Camera | Transform | - | - | 車体・砲塔・カメラ。カメラ未設定なら Camera.main |
| UD Rotator / Barrel Parts / Fire Position | Transform | - | - | 砲身の上下回転パーツと弾の発射位置 |
| Bullet | GameObject | - | - | 発射する弾（`TankProjectile` か `SharedBullet` が付いたPrefab） |
| Bullet Flight Speed | float | 20 | bulletFlightSpeed | 弾の速度(m/s) |
| Bullet Straight Distance | float | 30 | bulletStraightDistance | 弾が重力なしで直進する距離(m)。レティクルの表示位置にも使う |
| Fire Cooldown | float | 0.3 | fireCooldown | 発射間隔(秒) |
| Ground Layer / Ray Length / Ray Offset Height / Ground Check Distance | - | - | - | 坂道・接地判定 |
| Tilt Smooth Speed | float | 120 | - | 坂に合わせて車体を傾ける速さ(度/秒) |
| Move Angle Threshold | float | 1 | - | 入力方向にこの角度以内まで向いたら前進を開始する |
| Landing Reticle / Reticle Canvas | RectTransform / Canvas | - | - | 着弾予測レティクル |
| Prediction Step / Prediction Layer Mask | float / LayerMask | 0.05 / 全て | - | 着弾予測の刻み幅と判定レイヤー |
| Health | HHelth | - | - | 体力。未設定なら自動取得 |

※プレイヤー死亡時のゲームオーバー通知は `SInGameManagerBase` が `HHelth.OnDie` を監視して行います。

---

## TankProjectile (Tanks/TankScript/TankProjectile.cs)
プレイヤーの弾。直進距離までは重力なしで飛び、その後 `Gravity Multiplier` 倍の重力で落下する。
敵（`EnemyBase`）に当たると HP を1減らして消える。敵以外のトリガーはすり抜ける。

| 変数名 | 型 | 既定値 | CSVキー | 説明 |
|---|---|---|---|---|
| Flight Speed | float | 20 | (発射時に TankMovement から渡される) | 直進時の速度(m/s) |
| Life Time | float | 10 | - | 何にも当たらなかった場合に自動で消えるまでの秒数 |
| Gravity Multiplier | float | 12 | gravityMultiplier | 落下時の重力の倍率（1 = 通常の重力） |

---

## TankParameterManager (NewTanks/TankParameterManager.cs)
`NewTanks/TankParameters.csv` を読み込み、上記スクリプトにパラメータを渡す。シーンに1つあればよい（重複分は自動で削除）。

| 変数名 | 型 | 説明 |
|---|---|---|
| Parameter Csv | TextAsset | 読み込むCSV（1列目がキー、2列目が値） |

CSVのうち `reticleOffset` / `aimCoefficient` / `aimSwitchPoint` / `fireEffectName` / `hitEffectName` は、現在どのスクリプトからも読まれていません。

---

## CameraOcclusionFade (NewTanks/CameraOcclusionFade.cs)
カメラとプレイヤーの間にある障害物を半透明にする。カメラに付ける。

| 変数名 | 型 | 既定値 | 説明 |
|---|---|---|---|
| Target | Transform | - | 見せたい対象（プレイヤー） |
| Obstacle Layers | LayerMask | 全て | 半透明にする対象のレイヤー |
| Ray Offset | float | 0.2 | 対象の足元からレイの到達点を上げる高さ |
| Transparent Alpha | float | 0.25 | 半透明時のアルファ値 |

---

## HHelth (NewTanks/HHelth.cs)
プレイヤーの体力。敵の弾（`SharedBullet`）から `TakeDamage()` が呼ばれる。

| 変数名 | 型 | 既定値 | 説明 |
|---|---|---|---|
| Health | int | 3 | 何発当たったら死亡するか |

---

## HPlayerHPUI (NewTanks/HPlayerHPUI.cs)
HPアイコン3つの表示。被弾するたびに左から `New Sprite` に切り替わる。
`HHelth.Health` を3以外にした場合は警告が出る（アイコン数と合わせること）。

| 変数名 | 型 | 説明 |
|---|---|---|
| Firest / Second / Thired Image | Image | HPアイコン |
| Current Sprite / New Sprite | Sprite | 残っているHP / 減ったHP の画像 |
| Player Health | HHelth | 監視する体力。未設定なら TankMovement から自動取得 |

---

## 未使用のスクリプト
- `Bullet` (NewTanks/Bullet.cs) … 旧方式の弾。現在のシーン・プレハブでは使われていない
- `TankReticle` (Tanks/TankScript/TankReticle.cs) … 旧方式のレティクル。現在は TankMovement の着弾レティクルを使用
