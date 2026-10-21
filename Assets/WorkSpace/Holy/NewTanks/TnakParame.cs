
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class TankParameterManager : MonoBehaviour
{
    [Header("パラメータCSV")]
    [SerializeField]
    private TextAsset parameterCsv;

    public static TankParameterManager Instance { get; private set; }

    private readonly Dictionary<string, string> parameters =
        new Dictionary<string, string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        LoadParameters();
    }

    private void LoadParameters()
    {
        TextAsset csv = parameterCsv;

        if (csv == null)
        {
            Debug.LogError("TankParameters.csv が見つかりません！");
            return;
        }

        string[] lines = csv.text.Split(
            new[] { '\r', '\n' },
            System.StringSplitOptions.RemoveEmptyEntries
        );

        for (int i = 1; i < lines.Length; i++)
        {
            string[] columns = lines[i].Split(',');

            if (columns.Length < 2)
            {
                continue;
            }

            string key = columns[0].Trim();
            string value = columns[1].Trim();

            if (!string.IsNullOrEmpty(key))
            {
                parameters[key] = value;
            }
        }

        Debug.Log("ゲームパラメータを読み込みました。");
    }

    public float GetFloat(string key, float defaultValue)
    {
        if (parameters.TryGetValue(key, out string value) &&
            float.TryParse(
                value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float result))
        {
            return result;
        }

        Debug.LogWarning($"パラメータ '{key}' がないため既定値を使用します。");
        return defaultValue;
    }

    public string GetString(string key, string defaultValue)
    {
        if (parameters.TryGetValue(key, out string value))
        {
            return value;
        }

        return defaultValue;
    }
}
