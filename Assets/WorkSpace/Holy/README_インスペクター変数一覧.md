# タンクスクリプト インスペクター変数 README

各スクリプトでInspector上に表示され、編集可能な変数(`[SerializeField]`が付いたもの)を
スクリプトごとにまとめたものです。設定・調整時の参考にしてください。

---

## TankHead (HTankAimController.cs)
Head(カメラ土台)のヨー(左右)回転を担当。

| 変数名 | 型 | 既定値 | 説明 |
|---|---|---|---|
| Mouse Look Action Reference | InputActionReference | - | マウス移動量に使うInputAction(Vector2)。「MouseLook」を割り当てる |
| Stick Look Action Reference | InputActionReference | - | ゲームパッド右スティックに使うInputAction(Vector2)。「StickLook」を割り当てる |
| Mouse Sensitivity | float | 0.2 | マウスでのヨー回転感度 |
| Gamepad Sensitivity | float | 180 | 右スティックでのヨー回転感度(度/秒) |
| Stick Deadzone | float | 0.1 | この大きさ未満の右スティック入力は無視(遊び・誤差吸収用) |
| Horizontal Deadzone Band | float | 0 | 斜め入力時の軸スナップ用。スティックX成分がこの値以内なら左右入力を無視(上下のみ動作)。既定は「ナシ」 |

---

## TankAimReticle (HTankAimReticle.cs)
照準UI(レティクル)の画面表示を担当。

| 変数名 | 型 | 既定値 | 説明 |
|---|---|---|---|
| Stick Look Action Reference | InputActionReference | - | 右スティック入力。TankHead/UDRotaterと同じアクションでOK |
| Aim System | TankAimSystem | - | 狙点計算に使うTankAimSystem(Muzzleに付いているもの) |
| Render Camera | Camera | - | 狙点をスクリーン座標に変換する際に使うカメラ(未設定時はCamera.main) |
| Y Screen Offset | float | 0 | スクリーン座標へのY軸オフセット(ピクセル) |
| Vertical Stick Coefficient | float | 50 | Rスティック上下操作(仰角)に対する追加の上下移動係数(ピクセル) |
| Stick Deadzone | float | 0.1 | この大きさ未満の右スティック入力は無視 |

---

## TankAimSystem (HTankAimSystem.cs)
照準点(狙点)の計算を担当。Muzzleにアタッチ。

| 変数名 | 型 | 既定値 | 説明 |
|---|---|---|---|
| Max Aim Distance | float | 12 | 狙点までの最大距離(m)。**弾(TankProjectile)が直線で飛ぶ距離(落下開始距離)としても共有される唯一の設定値**。距離を変えたい場合はここだけを変更する |
| Aim Layer Mask | LayerMask | 全レイヤー | レイキャストで当たり判定を取るレイヤー。戦車自身や弾自身のレイヤーは含めないよう注意 |

---

## TankBody (HTankMovement.cs)
Body(車体)の移動・回転を担当。

| 変数名 | 型 | 既定値 | 説明 |
|---|---|---|---|
| Move Action Reference | InputActionReference | - | 移動入力(Vector2)。「Move」を割り当てる |
| Camera Transform | Transform | - | 移動方向の基準にするカメラ(Head)のTransform。未設定時はワールド座標基準 |
| Move Speed | float | 5 | 前進の速度(m/s) |
| Rotate Speed | float | 180 | 入力方向へ向くまでの旋回速度(度/秒) |
| Move Angle Threshold | float | 5 | この角度(度)以内まで入力方向を向いたら前進開始。大きくすると多少ズレていても前進しやすい |
| Input Deadzone | float | 0.1 | この大きさ未満のスティック入力は無視 |

---

## TankProjectile (HTankProjectile.cs)
弾の挙動を担当。

| 変数名 | 型 | 既定値 | 説明 |
|---|---|---|---|
| Flight Speed | float | 20 | 直線飛行時の速度(m/s)。仕様書の数値確定までの暫定値 |
| Life Time | float | 10 | 発射から何秒後に自動で消えるか(何にも当たらなかった場合の保険) |

※直線飛行距離はこのスクリプトに項目がなく、`TankAimSystem.Max Aim Distance` から発射時に渡される。

---

## TankReticle (HTankRetecl.cs)
※`TankAimReticle`とは別の、もう一つの照準UI実装。

| 変数名 | 型 | 既定値 | 説明 |
|---|---|---|---|
| Aim Camera | Camera | - | 投影計算に使うカメラ |
| Muzzle | Transform | - | 画面内2D座標の基準にするMuzzleのTransform |
| Reticle Rect | RectTransform | - | 移動させるレティクルUIのRectTransform |
| Screen Y Offset Pixels | float | 40 | Muzzle投影座標に加算するY軸ピクセルオフセット |
| Vertical Stick Coefficient Pixels | float | 120 | Rスティック上下値に掛ける追加の照準移動係数(ピクセル/入力値) |
| Aim Input Deadzone | float (Range 0–1) | 0.3 | 右スティック入力の不感帯 |

---

## UDRotater (HTankUDRotate.cs)
カメラのピッチ(上下)回転のみを担当。Headの子に配置。

| 変数名 | 型 | 既定値 | 説明 |
|---|---|---|---|
| Mouse Look Action Reference | InputActionReference | - | マウス移動量。TankHeadと同じアクションでOK |
| Stick Look Action Reference | InputActionReference | - | 右スティック入力。TankHeadと同じアクションでOK |
| Pitch Min | float | -10 | 上下(ピッチ)可動範囲の下限(度)。俯角 |
| Pitch Max | float | 30 | 上下(ピッチ)可動範囲の上限(度)。仰角 |
| Invert Pitch | bool | false | チェックすると上下方向の入力を反転する |
| Mouse Sensitivity | float | 0.2 | マウスでのピッチ回転感度 |
| Gamepad Sensitivity | float | 60 | 右スティック操作時のピッチ回転速度(度/秒) |
| Return Speed | float | 90 | スティックがニュートラルに戻った時、ピッチを0度へ戻す速度(度/秒)。ゲームパッド接続時のみ有効 |
| Stick Deadzone | float | 0.1 | この大きさ未満の右スティック入力は無視 |
| Vertical Deadzone Band | float | 0 | 斜め入力時の軸スナップ用。スティックY成分がこの値以内なら上下入力を無視(左右のみ動作)。既定は「ナシ」 |

---

## TankWeapon (HTankWeapon.cs)
弾の発射制御を担当。Muzzleにアタッチ。

| 変数名 | 型 | 既定値 | 説明 |
|---|---|---|---|
| Fire Action Reference | InputActionReference | - | 発射入力(Button)。「Fire」を割り当てる(L1ボタン/マウス左クリック) |
| Aim System | TankAimSystem | - | 狙点計算に使うTankAimSystem。未設定時は自身から取得を試みる |
| Projectile Prefab | GameObject | - | 発射する弾のPrefab(TankProjectileが付いているもの) |
| Muzzle Point | Transform | - | 弾の発射位置・向きの基準にするTransform。未設定時は自身のTransform |
| Fire Interval | float | 0.5 | 発射間隔(秒)。仕様書の数値確定までの暫定値 |

---

## 補足:スクリプト間で共有・連動している設定値

- **`TankAimSystem.Max Aim Distance`**は、`TankWeapon`経由で`TankProjectile.Launch()`に渡され、
  弾の直線飛行距離としてもそのまま使われます。照準の表示距離と弾の落下開始距離を必ず一致させるため、
  距離を変更したい場合は**この一箇所だけ**を変更してください(`TankProjectile`側には距離設定項目はありません)。
- `MouseLookActionReference` / `StickLookActionReference` は、`TankHead`・`UDRotater`・`TankAimReticle`の
  複数スクリプトで同じInputActionを共有して登録できます。
