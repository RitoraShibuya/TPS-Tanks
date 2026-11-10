using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UI（StageSelectUIなど）へステージ情報を引き渡すための構造体
/// </summary>
[Serializable]
public struct StageStatusInfo
{
    public int StageID;
    public bool IsCleared;
    public bool IsUnlocked;
}

public class SProgressManager : MonoBehaviour
{
    public static SProgressManager SInstance { get; private set; }

    /// <summary>
    /// 本編ステージの数（ID 1 ～ SStageCount）。チュートリアルは ID 0
    /// ステージ数を変える時はここだけを変更する
    /// </summary>
    public const int SStageCount = 4;

    // ※進行状況はメモリ上のみで保持している（ゲームを終了するとリセットされる）

    // SIsTutorialCleared を廃止し、SStages のみに一本化
    public List<SStageData> SStages = new();

    private void Awake()
    {
        if (SInstance == null)
        {
            SInstance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddStageData(SStageData data)
    {
        if (GetStageData(data.SStageID) == null)
        {
            SStages.Add(data);
        }
    }

    /// <summary>
    /// 指定IDのステージがクリア済みか
    /// </summary>
    public bool IsStageCleared(int id)
    {
        SStageData data = GetStageData(id);
        return data != null && data.SIsCleared;
    }

    /// <summary>
    /// 指定IDのステージが解放されているか（1つ前のステージがクリア済みなら解放）
    /// </summary>
    public bool IsStageUnlocked(int id)
    {
        if (id <= 1) return true; // チュートリアル(ID:0)とステージ1(ID:1)は無条件で解放
        return IsStageCleared(id - 1); // 1つ前のステージがクリア済みなら解放
    }

    public SStageData GetStageData(int id)
    {
        return SStages.Find(stage => stage.SStageID == id);
    }

    /// <summary>
    /// 今後 UI 側でボタンのロック状態を生成するための全ステージ状態リストを取得
    /// </summary>
    public List<StageStatusInfo> GetStageStatusList()
    {
        // ID 0(チュートリアル) ～ SStageCount までを返す
        int maxStageCount = SStageCount + 1;

        List<StageStatusInfo> list = new List<StageStatusInfo>();
        for (int i = 0; i < maxStageCount; i++)
        {
            list.Add(new StageStatusInfo
            {
                StageID = i,
                IsCleared = IsStageCleared(i),
                IsUnlocked = IsStageUnlocked(i)
            });
        }
        return list;
    }
}