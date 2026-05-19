using System.IO;
using UnityEditor;
using UnityEngine;

namespace CNoom.PSDToUGUI.Editor
{
    /// <summary>
    ///     PSD JSON 文件的自定义导入器
    ///     <para>双击 JSON 文件可直接打开导入窗口</para>
    /// </summary>
    public class PSDAssetImporter : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (var path in importedAssets)
            {
                if (!path.EndsWith(".json")) continue;

                // 检查是否为 PSD 导出的 JSON
                var fullPath = System.IO.Path.Combine(
                    Application.dataPath[..^"Assets".Length], path);
                if (!File.Exists(fullPath)) continue;

                try
                {
                    var json = File.ReadAllText(fullPath);
                    var doc = JsonUtility.FromJson<PSDDocument>(json);
                    if (doc != null && doc.version == "1.0" && doc.canvas != null)
                    {
                        // 检测到 PSD 导出文件，提示用户
                        Debug.Log($"[PSDToUGUI] 检测到 PSD 导出文件: {path}");
                    }
                }
                catch
                {
                    // 忽略非 PSD JSON
                }
            }
        }
    }
}
