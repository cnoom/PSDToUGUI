using System.IO;
using UnityEditor;
using UnityEngine;

namespace CNoom.PSDToUGUI.Editor
{
    /// <summary>
    ///     PSD 导入预览窗口
    ///     <para>提供 JSON 选择、预览、导入选项配置</para>
    /// </summary>
    public class PSDImportWindow : EditorWindow
    {
        private const string JsonFileKey = "PSDToUGUI_JsonPath";
        private const string SettingsKey = "PSDToUGUI_SettingsPath";

        private string _jsonPath;
        private PSDImportSettings _settings;
        private PSDDocument _document;
        private Vector2 _scrollPos;
        private bool _imported;
        private string _statusMessage;

        [MenuItem("Tools/PSD To UGUI/导入 PSD", priority = 0)]
        public static void ShowWindow()
        {
            var window = GetWindow<PSDImportWindow>("PSD 导入");
            window.minSize = new Vector2(400, 500);
        }

        private void OnEnable()
        {
            _jsonPath = EditorPrefs.GetString(JsonFileKey, "");
            var settingsPath = EditorPrefs.GetString(SettingsKey, "");
            if (!string.IsNullOrEmpty(settingsPath))
            {
                _settings = AssetDatabase.LoadAssetAtPath<PSDImportSettings>(settingsPath);
            }

            if (_settings == null)
            {
                _settings = PSDImportSettings.Default;
            }

            TryLoadDocument();
        }

        private void OnDisable()
        {
            EditorPrefs.SetString(JsonFileKey, _jsonPath);
            if (_settings != null)
            {
                var path = AssetDatabase.GetAssetPath(_settings);
                if (!string.IsNullOrEmpty(path))
                {
                    EditorPrefs.SetString(SettingsKey, path);
                }
            }
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawJsonSelector();
            DrawSettings();
            DrawPreview();
            DrawImportButton();
            DrawStatusBar();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(8);
            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("PSD → UGUI 导入工具", style);
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "选择 PS 导出的 JSON 文件，配置导入选项后点击导入。\n" +
                "生成的预制体不包含 Canvas，需要放在已有 Canvas 下使用。",
                MessageType.Info);
            EditorGUILayout.Space(4);
        }

        private void DrawJsonSelector()
        {
            EditorGUILayout.LabelField("JSON 文件", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            _jsonPath = EditorGUILayout.TextField(_jsonPath);
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                var path = EditorUtility.OpenFilePanel("选择 PS 导出的 JSON", "", "json");
                if (!string.IsNullOrEmpty(path))
                {
                    // 转换为相对路径
                    if (path.StartsWith(Application.dataPath))
                    {
                        path = "Assets" + path[Application.dataPath.Length..];
                    }

                    _jsonPath = path;
                    TryLoadDocument();
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4);
        }

        private void DrawSettings()
        {
            EditorGUILayout.LabelField("导入设置", EditorStyles.boldLabel);
            var newSettings = (PSDImportSettings)EditorGUILayout.ObjectField(
                "配置文件", _settings, typeof(PSDImportSettings), false);
            if (newSettings != null && newSettings != _settings)
            {
                _settings = newSettings;
            }

            if (_settings != null)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField($"参考尺寸: {_settings.referenceWidth} x {_settings.referenceHeight}");
                EditorGUILayout.LabelField($"文本方案: {(_settings.useTextMeshPro ? "TextMeshPro" : "UGUI Text")}");
                EditorGUILayout.LabelField($"锚点模式: {_settings.anchorMode}");
                EditorGUILayout.LabelField($"预制体路径: {_settings.outputPrefabPath}");
                EditorGUILayout.LabelField($"精灵图路径: {_settings.outputSpritePath}");
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(4);
        }

        private void DrawPreview()
        {
            if (_document == null) return;

            EditorGUILayout.LabelField("文档预览", EditorStyles.boldLabel);
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.ExpandHeight(true));

            EditorGUILayout.LabelField($"版本: {_document.version}");

            if (_document.canvas != null)
            {
                EditorGUILayout.LabelField($"画布: {_document.canvas.width} x {_document.canvas.height}");
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("图层列表:");
            EditorGUI.indentLevel++;
            DrawLayerTree(_document.layers, 0);
            EditorGUI.indentLevel--;

            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space(4);
        }

        private void DrawLayerTree(System.Collections.Generic.List<PSDLayer> layers, int depth)
        {
            if (layers == null) return;
            foreach (var layer in layers)
            {
                var icon = layer.type switch
                {
                    PSDLayer.LayerType.Group => "📁",
                    PSDLayer.LayerType.Text => "📝",
                    PSDLayer.LayerType.Pixel => "🖼",
                    PSDLayer.LayerType.Shape => "⬜",
                    _ => "?"
                };

                var visible = layer.visible ? "●" : "○";
                var label = $"{icon} {visible} {layer.name}";

                if (layer.type == PSDLayer.LayerType.Group && layer.children?.Count > 0)
                {
                    label += $" ({layer.children.Count})";
                }

                EditorGUILayout.LabelField(label);

                if (layer.children?.Count > 0)
                {
                    EditorGUI.indentLevel++;
                    DrawLayerTree(layer.children, depth + 1);
                    EditorGUI.indentLevel--;
                }
            }
        }

        private void DrawImportButton()
        {
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(_jsonPath) || _document == null);
            if (GUILayout.Button("导入生成 UGUI", GUILayout.Height(36)))
            {
                DoImport();
            }

            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space(4);
        }

        private void DrawStatusBar()
        {
            if (!string.IsNullOrEmpty(_statusMessage))
            {
                EditorGUILayout.HelpBox(_statusMessage, _imported ? MessageType.Info : MessageType.Warning);
            }
        }

        private void TryLoadDocument()
        {
            _document = null;
            _statusMessage = "";
            _imported = false;

            if (string.IsNullOrEmpty(_jsonPath)) return;

            var fullPath = _jsonPath;
            if (_jsonPath.StartsWith("Assets"))
            {
                fullPath = Application.dataPath + _jsonPath[6..];
            }

            if (!File.Exists(fullPath))
            {
                _statusMessage = $"文件不存在: {fullPath}";
                return;
            }

            try
            {
                var json = File.ReadAllText(fullPath);
                _document = JsonUtility.FromJson<PSDDocument>(json);
                if (_document != null)
                {
                    _statusMessage = $"加载成功: {_document.layers?.Count ?? 0} 个顶层图层";
                }
                else
                {
                    _statusMessage = "JSON 解析失败";
                }
            }
            catch (System.Exception e)
            {
                _statusMessage = $"加载失败: {e.Message}";
            }
        }

        private void DoImport()
        {
            if (_document == null || _settings == null) return;

            try
            {
                // 确保输出目录存在
                EnsureDirectory(_settings.outputPrefabPath);
                EnsureDirectory(_settings.outputSpritePath);

                // 创建预制体根节点
                var rootObj = new GameObject(_document.canvas != null
                    ? $"PSD_{_document.canvas.width}x{_document.canvas.height}"
                    : "PSD_Export");

                // 生成 UGUI 层级
                var generator = new UGUIGenerator(_document, _settings);
                generator.Generate(rootObj.transform);

                // 保存为预制体
                var prefabPath = GetUniquePrefabPath(rootObj.name);
                PrefabUtility.SaveAsPrefabAsset(rootObj, prefabPath);

                // 销毁场景中的临时对象
                DestroyImmediate(rootObj);

                _imported = true;
                _statusMessage = $"导入成功！预制体已保存到: {prefabPath}";

                // 刷新资源数据库
                AssetDatabase.Refresh();

                // 选中生成的预制体
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
            }
            catch (System.Exception e)
            {
                _imported = false;
                _statusMessage = $"导入失败: {e.Message}";
                Debug.LogException(e);
            }
        }

        private static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private string GetUniquePrefabPath(string name)
        {
            var path = Path.Combine(_settings.outputPrefabPath, $"{name}.prefab");
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            return path;
        }
    }
}
