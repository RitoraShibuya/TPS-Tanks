using UnityEngine;

/// <summary>
/// Body / Head の参照を自動で配線するセットアップスクリプト。
///
/// HeadのヨーはBody(親)の回転に関係なく、ワールド回転を直接指定する方式のため、
/// グラフィッカーから届く「Bodyの子としてHead」という構造をそのまま使うことができる
/// (切り離し処理は不要)。
///
/// このスクリプトは、TankBodyの「Camera Transform」にHeadを自動で登録するだけを行う。
///
/// 使い方:
/// 1. 届いたモデル(Body → Head の親子構造のまま)をシーンに配置する。
/// 2. Body の GameObject に TankBody と、このスクリプト(TankRigSetup)をアタッチする。
/// 3. Inspector上の「Head Transform」に、モデル内のHead(砲塔)のTransformを
///    ドラッグ&ドロップで登録する(Bodyの子のままでよい)。
/// 4. Head側に TankHead コンポーネントが付いていなければ自動で追加される。
/// 5. 実行すると、Awakeのタイミングで自動的に
///    TankBody.SetCameraTransform(Head) を呼び、参照を設定する。
/// </summary>
[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(TankBody))]
public class TankRigSetup : MonoBehaviour
{
    [Tooltip("モデル内でBodyの子になっているHead(砲塔)のTransform。Bodyの子のままでよい。")]
    [SerializeField]
    private Transform headTransform;

    private void Awake()
    {
        if (headTransform == null)
        {
            Debug.LogWarning("[TankRigSetup] Head Transform が設定されていません。", this);
            return;
        }

        // TankHeadコンポーネントが無ければ追加する
        if (headTransform.GetComponent<TankHead>() == null)
        {
            headTransform.gameObject.AddComponent<TankHead>();
        }

        TankBody tankBody = GetComponent<TankBody>();
        if (tankBody != null)
        {
            tankBody.SetCameraTransform(headTransform);
        }
    }
}