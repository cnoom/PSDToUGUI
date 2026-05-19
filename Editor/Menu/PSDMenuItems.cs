using UnityEditor;
using UnityEngine;

namespace CNoom.PSDToUGUI.Editor
{
    /// <summary>
    ///     PSD To UGUI 菜单入口
    /// </summary>
    public static class PSDMenuItems
    {
        [MenuItem("Tools/PSD To UGUI/打开导入窗口", priority = 0)]
        public static void OpenImportWindow()
        {
            PSDImportWindow.ShowWindow();
        }

        [MenuItem("Tools/PSD To UGUI/创建默认设置", priority = 10)]
        public static void CreateDefaultSettings()
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "保存导入设置", "PSDImportSettings", "asset",
                "选择 PSD 导入设置的保存路径");

            if (string.IsNullOrEmpty(path)) return;

            var settings = ScriptableObject.CreateInstance<PSDImportSettings>();
            AssetDatabase.CreateAsset(settings, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }

        [MenuItem("Tools/PSD To UGUI/导出 PS 脚本", priority = 20)]
        public static void ExportPSScript()
        {
            var path = EditorUtility.SaveFilePanel(
                "保存 PS 导出脚本", "", "ExportPSD", "jsx");

            if (string.IsNullOrEmpty(path)) return;

            var script = PSDScriptExporter.GetScript();
            System.IO.File.WriteAllText(path, script);
            EditorUtility.RevealInFinder(path);
            Debug.Log($"[PSDToUGUI] PS 导出脚本已保存到: {path}");
        }

        [MenuItem("Tools/PSD To UGUI/关于", priority = 100)]
        public static void About()
        {
            EditorUtility.DisplayDialog("关于 PSD To UGUI",
                "PSD To UGUI v1.0.0\n\n" +
                "将 Photoshop 设计稿导出为 Unity UGUI 预制体。\n\n" +
                "工作流程：\n" +
                "1. 在 PS 中运行 ExportPSD.jsx 导出 JSON + PNG\n" +
                "2. 将导出文件放入 Unity 项目\n" +
                "3. 通过菜单导入生成 UGUI 预制体\n\n" +
                "作者: CNoom", "确定");
        }
    }
}
