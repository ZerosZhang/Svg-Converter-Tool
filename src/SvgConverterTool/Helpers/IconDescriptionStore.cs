using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace SvgConverterTool.Helpers
{
    /// <summary>
    /// 图标备注存储：以「相对路径/图标名」为键，读写 Output 目录下的 descriptions.json
    /// </summary>
    public static class IconDescriptionStore
    {
        private static readonly string StorePath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output", "descriptions.json");

        private static readonly Dictionary<string, string> Descriptions = Load();

        /// <summary>
        /// 读取备注，没有时返回空字符串
        /// </summary>
        public static string Get(string key)
        {
            return Descriptions.TryGetValue(key, out string? text) ? text : string.Empty;
        }

        /// <summary>
        /// 写入备注，空内容会从文件中移除；写盘失败时静默忽略
        /// </summary>
        public static void Set(string key, string? text)
        {
            string value = text?.Trim() ?? string.Empty;

            if (value.Length == 0)
            {
                if (!Descriptions.Remove(key))
                    return;
            }
            else
            {
                if (Descriptions.TryGetValue(key, out string? current) && current == value)
                    return;

                Descriptions[key] = value;
            }

            Save();
        }

        /// <summary>
        /// 图标改名后迁移备注到新键
        /// </summary>
        public static void Rename(string oldKey, string newKey)
        {
            if (string.Equals(oldKey, newKey, StringComparison.OrdinalIgnoreCase))
                return;

            string text = Get(oldKey);
            Descriptions.Remove(oldKey);

            if (text.Length > 0)
                Descriptions[newKey] = text;

            Save();
        }

        /// <summary>
        /// 从 JSON 载入备注，文件缺失或损坏时返回空表
        /// </summary>
        private static Dictionary<string, string> Load()
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                if (!File.Exists(StorePath))
                    return result;

                var loaded = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(StorePath));
                if (loaded == null)
                    return result;

                foreach (var pair in loaded)
                {
                    if (!string.IsNullOrWhiteSpace(pair.Value))
                        result[pair.Key] = pair.Value;
                }
            }
            catch
            {
                // 文件损坏时按空表处理，避免影响页面加载
            }

            return result;
        }

        /// <summary>
        /// 按键名排序后写回 JSON
        /// </summary>
        private static void Save()
        {
            try
            {
                string? directory = Path.GetDirectoryName(StorePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                var ordered = Descriptions
                    .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(pair => pair.Key, pair => pair.Value);

                File.WriteAllText(StorePath,
                    JsonSerializer.Serialize(ordered, new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        // 中文备注保持可读，不转成 \uXXXX
                        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    }));
            }
            catch
            {
                // 写盘失败（占用、只读目录）时保留内存中的值
            }
        }
    }
}
