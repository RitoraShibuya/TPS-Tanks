using System.Collections.Generic;
using UnityEngine;

public class SProgressManager : MonoBehaviour
{
    public static SProgressManager SInstance { get; private set; }

    public bool SIsTutorialCleared = false;
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
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void AddStageData(SStageData data)
    {
        if (GetStageData(data.SStageID) == null)
        {
            SStages.Add(data);
        }
    }

    public bool IsStageCleared(int id)
    {
        SStageData data = GetStageData(id);
        return data != null && data.SIsCleared;
    }

    public SStageData GetStageData(int id)
    {
        return SStages.Find(stage => stage.SStageID == id);
    }

}
