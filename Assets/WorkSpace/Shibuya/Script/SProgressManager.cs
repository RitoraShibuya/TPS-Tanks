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
    /// 指定IDのステージが解放されているか（1つ前のステージがクリア済み、またはID0なら解放）
    /// </summary>
    public bool IsStageUnlocked(int id)
    {
        if (id <= 0) return true; // チュートリアル(ID:0)は無条件で解放
        return IsStageCleared(id - 1); // 1つ前のステージがクリア済みなら解放
    }

    public SStageData GetStageData(int id)
    {
        return SStages.Find(stage => stage.SStageID == id);
    }

    /// <summary>
    /// 今後 UI 側でボタンのロック状態を生成するための全ステージ状態リストを取得
    /// </summary>
    public List<StageStatusInfo> GetStageStatusList(int maxStageCount = 5)
    {
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