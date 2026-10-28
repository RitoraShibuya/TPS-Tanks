using System;
using UnityEngine;

public class SCallingSelect : MonoBehaviour
{
    // ステージ選択要求イベント
    public event Action OnCallStageSelect;

    /// <summary>
    /// メインボタン押下時
    /// </summary>
    public void OnMainButtonClick()
    {
        // 決定音を再生
        if (SSoundManager.Instance != null)
        {
            SSoundManager.Instance.PlaySE_Decide();
        }

        // ゲームマネージャーへ通知
        OnCallStageSelect?.Invoke();
    }
}