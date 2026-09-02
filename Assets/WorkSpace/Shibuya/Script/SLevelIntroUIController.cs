using System.Collections;
using UnityEngine;
using TMPro;

public class SLevelIntroUIController : MonoBehaviour
{
    [Header("UI Objects")]
    [SerializeField] private GameObject SStageUI;
    [SerializeField] private GameObject SStartUI;

    [Header("UI Text")]
    [SerializeField] private TextMeshProUGUI SStageText;

    [Header("Display Duration Settings")]
    [SerializeField] private float SStageUIDisplayTime = 1.5f; // Stage UI の表示時間（秒）
    [SerializeField] private float SStartUIDisplayTime = 0.5f; // Start UI の表示時間（秒）

    // GameManagerから受け取るための変数
    private int SCurrentStageID;

    // ★Start()を削除し、外部から呼ばれる専用メソッドを用意する
    public void SetupAndPlay(int stageID)
    {
        SCurrentStageID = stageID; // 渡されたIDをセット

        // 値をセットし終わってから、演出をスタート！
        StartCoroutine(PlayIntroSequence());
    }

    private IEnumerator PlayIntroSequence()
    {
        if (SStartUI != null) SStartUI.SetActive(false);

        if (SStageUI != null)
        {
            if (SStageText != null)
            {
                // ★流し込まれた SCurrentStageID を使ってテキストを変更
                if (SCurrentStageID == 0)
                {
                    SStageText.text = $"Tutorial";
                }
                else
                {
                    SStageText.text = $"STAGE {SCurrentStageID}";
                }
            }

            SStageUI.SetActive(true);
        }

        // 指定した秒数だけ待機
        yield return new WaitForSeconds(SStageUIDisplayTime);

        // Stage UI を消し、Start UI を表示
        if (SStageUI != null) SStageUI.SetActive(false);
        if (SStartUI != null) SStartUI.SetActive(true);

        // 指定した秒数だけ待機
        yield return new WaitForSeconds(SStartUIDisplayTime);

        // Start UI を消す
        if (SStartUI != null) SStartUI.SetActive(false);

        // ★演出が終わったら、自分自身（Canvasごと）を削除して画面を綺麗にする
        if (SUIManager.SInstance != null)
        {
            SUIManager.SInstance.SHideUI(gameObject);
        }
    }
}