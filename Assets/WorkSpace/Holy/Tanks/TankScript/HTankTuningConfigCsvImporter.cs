#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Excelから書き出したCSVを TankTuningConfig アセットに読み込むエディタ拡張。
///
/// 【重要】このファイルは、Unityプロジェクト内の名前が「Editor」のフォルダの中に
/// 置いてください(例: Assets/Editor/HTankTuningConfigCsvImporter.cs)。
/// 「Editor」フォルダに入れないと、実行用ビルドに混入してエラーになります。
///
/// CSVの列構成(1行目はヘッダーとして無視されます):
///   [0] Script      … スクリプト名(表示用、インポートには使用しない)
///   [1] 変数名(和名) … 表示用ラベル(インポートには使用しない)
///   [2] Key         … TankTuningConfigのフィールド名(これでマッチングする。変更しないこと)
///   [3] 現在値       … 実際にインポートされる値(Excelで編集する列)
///   [4] 説明        … 表示用の説明(インポートには使用しない)
///
/// 使い方:
///   Unityメニュー: Tools > Tank > Tuning CSVをインポート…
///   → CSVファイルを選択すると、Assets内から TankTuningConfig アセットを自動検索して
///     (複数ある場合は最初に見つかったもの)値を書き込む。見つからない場合は
///     Assets/TankTuning/TankTuningConfig.asset を新規作成する。
/// </summary>
public static class TankTuningConfigCsvImporter
{
    private const string DefaultAssetFolder = "Assets/TankTuning";
    private const string DefaultAssetPath = DefaultAssetFolder + "/TankTuningConfig.asset";

    [MenuItem("Tools/Tank/Tuning CSVをインポート…")]
    public static void ImportCsv()
    {
        string path = EditorUtility.OpenFilePanel("Tuning CSVを選択", Application.dataPath, "csv");
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        TankTuningConfig config = FindOrCreateConfig();
        if (config == null)
        {
            EditorUtility.DisplayDialog("インポート失敗", "TankTuningConfigアセットの取得/作成に失敗しました。", "OK");
            return;
        }

        List<(string key, string value)> rows;
        try
        {
            rows = ParseCsv(path);
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("インポート失敗", "CSVの読み込みに失敗しました:\n" + e.Message, "OK");
            return;
        }

        int updatedCount = 0;
        List<string> unknownKeys = new List<string>();

        Undo.RecordObject(config, "Import Tank Tuning CSV");

        foreach (var (key, value) in rows)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            FieldInfo field = typeof(TankTuningConfig).GetField(key, BindingFlags.Public | BindingFlags.Instance);
            if (field == null)
            {
                unknownKeys.Add(key);
                continue;
            }

            if (!TrySetField(config, field, value))
            {
                unknownKeys.Add(key + " (値の変換に失敗: " + value + ")");
                continue;
            }

            updatedCount++;
        }

        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string message = $"{updatedCount} 件の値を更新しました。\n対象アセット: {AssetDatabase.GetAssetPath(config)}";
        if (unknownKeys.Count > 0)
        {
            message += "\n\n以下のKeyは無視されました(TankTuningConfigに該当フィールドが無いか、値の形式が不正です):\n"
                       + string.Join("\n", unknownKeys);
        }

        Debug.Log("[TankTuningConfigCsvImporter] " + message);
        EditorUtility.DisplayDialog("インポート完了", message, "OK");
    }

    private static TankTuningConfig FindOrCreateConfig()
    {
        string[] guids = AssetDatabase.FindAssets("t:TankTuningConfig");
        if (guids.Length > 0)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<TankTuningConfig>(assetPath);
        }

        if (!AssetDatabase.IsValidFolder(DefaultAssetFolder))
        {
            AssetDatabase.CreateFolder("Assets", "TankTuning");
        }

        TankTuningConfig config = ScriptableObject.CreateInstance<TankTuningConfig>();
        AssetDatabase.CreateAsset(config, DefaultAssetPath);
        AssetDatabase.SaveAssets();
        Debug.Log("[TankTuningConfigCsvImporter] TankTuningConfigが見つからなかったため、新規作成しました: " + DefaultAssetPath);
        return config;
    }

    private static bool TrySetField(TankTuningConfig config, FieldInfo field, string rawValue)
    {
        string value = rawValue.Trim();

        if (field.FieldType == typeof(float))
        {
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float f))
            {
                field.SetValue(config, f);
                return true;
            }
            return false;
        }

        if (field.FieldType == typeof(int))
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int i))
            {
                field.SetValue(config, i);
                return true;
            }
            return false;
        }

        if (field.FieldType == typeof(bool))
        {
            string normalized = value.ToLowerInvariant();
            if (normalized == "true" || normalized == "1" || normalized == "on" || normalized == "yes")
            {
                field.SetValue(config, true);
                return true;
            }
            if (normalized == "false" || normalized == "0" || normalized == "off" || normalized == "no")
            {
                field.SetValue(config, false);
                return true;
            }
            return false;
        }

        // 未対応の型(string等)はそのまま文字列として設定を試みる
        if (field.FieldType == typeof(string))
        {
            field.SetValue(config, value);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 簡易CSVパーサー。ダブルクォート囲み("...")とその中のカンマ・改行に対応する。
    /// 1行目(ヘッダー)は読み飛ばす。各行から (Key列, 現在値列) のペアを取り出す。
    /// </summary>
    private static List<(string key, string value)> ParseCsv(string path)
    {
        var result = new List<(string, string)>();

        // Excel保存のCSVは Shift_JIS または UTF-8(BOM付き)のことが多いため、
        // UTF-8を優先しつつ、文字化けが疑われる場合はShift_JISで読み直す。
        string text = File.ReadAllText(path, Encoding.UTF8);
        if (text.Contains("\uFFFD"))
        {
            try
            {
                Encoding sjis = Encoding.GetEncoding("Shift_JIS");
                text = File.ReadAllText(path, sjis);
            }
            catch (ArgumentException)
            {
                // Shift_JISが環境に無ければUTF-8のまま続行
            }
        }

        List<List<string>> table = ParseCsvText(text);

        for (int row = 1; row < table.Count; row++) // 0行目はヘッダーなのでスキップ
        {
            List<string> cols = table[row];
            if (cols.Count < 4)
            {
                continue;
            }

            string key = cols[2];
            string value = cols[3];

            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            result.Add((key, value));
        }

        return result;
    }

    private static List<List<string>> ParseCsvText(string text)
    {
        var rows = new List<List<string>>();
        var currentRow = new List<string>();
        var field = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    field.Append(c);
                }
                continue;
            }

            switch (c)
            {
                case '"':
                    inQuotes = true;
                    break;
                case ',':
                    currentRow.Add(field.ToString());
                    field.Clear();
                    break;
                case '\r':
                    break;
                case '\n':
                    currentRow.Add(field.ToString());
                    field.Clear();
                    rows.Add(currentRow);
                    currentRow = new List<string>();
                    break;
                default:
                    field.Append(c);
                    break;
            }
        }

        // 末尾に改行が無い最終行を回収
        if (field.Length > 0 || currentRow.Count > 0)
        {
            currentRow.Add(field.ToString());
            rows.Add(currentRow);
        }

        return rows;
    }
}
#endif