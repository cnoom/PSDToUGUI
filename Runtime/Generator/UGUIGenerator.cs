using System;
using System.Collections.Generic;
using UnityEngine;

namespace CNoom.PSDToUGUI
{
    /// <summary>
    ///     UGUI 预制体生成器
    ///     <para>将 PSDDocument 转换为 UGUI GameObject 层级</para>
    ///     <para>生成纯 UI 元素层级，不含 Canvas，需要放在已有 Canvas 下使用</para>
    /// </summary>
    public class UGUIGenerator
    {
        private readonly PSDImportSettings _settings;
        private readonly PSDDocument _document;
        private readonly Dictionary<string, Sprite> _spriteCache = new();

        public UGUIGenerator(PSDDocument document, PSDImportSettings settings)
        {
            _document = document;
            _settings = settings ?? PSDImportSettings.Default;
        }

        /// <summary>
        ///     生成 UGUI 层级根节点
        /// </summary>
        /// <param name="parent">父节点（应为 Canvas 下的节点）</param>
        /// <returns>生成的根节点</returns>
        public GameObject Generate(Transform parent)
        {
            var root = new GameObject("PSDRoot");
            var rectTransform = root.AddComponent<RectTransform>();
            rectTransform.SetParent(parent, false);

            // 根节点铺满父容器
            SetStretchAnchor(rectTransform);

            // 递归生成子图层
            foreach (var layer in _document.layers)
            {
                ProcessLayer(layer, root.transform, _document.canvas);
            }

            return root;
        }

        /// <summary>
        ///     处理单个图层
        /// </summary>
        private void ProcessLayer(PSDLayer layer, Transform parent, PSDDocument.CanvasInfo canvas)
        {
            if (!layer.visible) return;

            GameObject go;

            switch (layer.type)
            {
                case PSDLayer.LayerType.Group:
                    go = ProcessGroupLayer(layer, parent, canvas);
                    break;
                case PSDLayer.LayerType.Pixel:
                    go = ProcessPixelLayer(layer, parent, canvas);
                    break;
                case PSDLayer.LayerType.Text:
                    go = ProcessTextLayer(layer, parent, canvas);
                    break;
                case PSDLayer.LayerType.Shape:
                    go = ProcessShapeLayer(layer, parent, canvas);
                    break;
                default:
                    return;
            }

            // 设置通用属性
            if (go != null)
            {
                go.name = layer.name;
                var canvasGroup = go.GetComponent<CanvasGroup>();
                if (canvasGroup != null && Mathf.Abs(layer.opacity - 1f) > 0.001f)
                {
                    canvasGroup.alpha = layer.opacity;
                }
            }
        }

        /// <summary>
        ///     处理图层组 → 空 GameObject
        /// </summary>
        private GameObject ProcessGroupLayer(PSDLayer layer, Transform parent, PSDDocument.CanvasInfo canvas)
        {
            var go = new GameObject(layer.name);
            var rectTransform = go.AddComponent<RectTransform>();
            rectTransform.SetParent(parent, false);

            SetLayerRect(rectTransform, layer, canvas);

            if (Mathf.Abs(layer.opacity - 1f) > 0.001f)
            {
                go.AddComponent<CanvasGroup>().alpha = layer.opacity;
            }

            // 递归处理子图层
            foreach (var child in layer.children)
            {
                ProcessLayer(child, go.transform, canvas);
            }

            return go;
        }

        /// <summary>
        ///     处理像素图层 → UGUI Image
        /// </summary>
        private GameObject ProcessPixelLayer(PSDLayer layer, Transform parent, PSDDocument.CanvasInfo canvas)
        {
            var go = new GameObject(layer.name);
            var rectTransform = go.AddComponent<RectTransform>();
            rectTransform.SetParent(parent, false);

            SetLayerRect(rectTransform, layer, canvas);

            var image = go.AddComponent<UnityEngine.UI.Image>();
            image.raycastTarget = false;

            // 加载纹理
            if (!string.IsNullOrEmpty(layer.texture))
            {
                var sprite = LoadSprite(layer.texture);
                if (sprite != null)
                {
                    image.sprite = sprite;
                    image.preserveAspect = true;
                }
            }

            // 应用图层样式
            ApplyStyles(go, layer.styles);

            return go;
        }

        /// <summary>
        ///     处理文本图层 → TMP_Text 或 UGUI Text
        /// </summary>
        private GameObject ProcessTextLayer(PSDLayer layer, Transform parent, PSDDocument.CanvasInfo canvas)
        {
            var go = new GameObject(layer.name);
            var rectTransform = go.AddComponent<RectTransform>();
            rectTransform.SetParent(parent, false);

            SetLayerRect(rectTransform, layer, canvas);

            if (layer.text != null)
            {
                if (_settings.useTextMeshPro)
                {
                    CreateTMText(go, layer.text);
                }
                else
                {
                    CreateUGUIText(go, layer.text);
                }
            }

            ApplyStyles(go, layer.styles);
            return go;
        }

        /// <summary>
        ///     处理形状图层 → UGUI Image（纯色/切片）
        /// </summary>
        private GameObject ProcessShapeLayer(PSDLayer layer, Transform parent, PSDDocument.CanvasInfo canvas)
        {
            var go = new GameObject(layer.name);
            var rectTransform = go.AddComponent<RectTransform>();
            rectTransform.SetParent(parent, false);

            SetLayerRect(rectTransform, layer, canvas);

            var image = go.AddComponent<UnityEngine.UI.Image>();
            image.raycastTarget = false;

            if (!string.IsNullOrEmpty(layer.texture))
            {
                var sprite = LoadSprite(layer.texture);
                if (sprite != null)
                {
                    image.sprite = sprite;
                }
            }

            ApplyStyles(go, layer.styles);
            return go;
        }

        /// <summary>
        ///     创建 TMP 文本组件
        /// </summary>
        private void CreateTMText(GameObject go, PSDLayer.TextInfo textInfo)
        {
            try
            {
                var tmpType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
                if (tmpType == null)
                {
                    // TMP 未安装，回退到 UGUI Text
                    CreateUGUIText(go, textInfo);
                    return;
                }

                var tmp = go.AddComponent(tmpType) as MonoBehaviour;
                if (tmp == null) return;

                // 通过反射设置属性（避免编译时依赖）
                SetTMPProperty(tmp, "text", textInfo.content);
                SetTMPProperty(tmp, "fontSize", (float)textInfo.fontSize);
                SetTMPProperty(tmp, "color", ParseColor(textInfo.color));
                SetTMPProperty(tmp, "alignment", GetTMPAlignment(textInfo.alignment));
                SetTMPProperty(tmp, "richText", false);
                SetTMPProperty(tmp, "raycastTarget", false);

                // 尝试加载字体
                TryLoadTMPFont(tmp, textInfo.font);
            }
            catch (Exception)
            {
                CreateUGUIText(go, textInfo);
            }
        }

        /// <summary>
        ///     创建 UGUI Text 组件（回退方案）
        /// </summary>
        private void CreateUGUIText(GameObject go, PSDLayer.TextInfo textInfo)
        {
            var text = go.AddComponent<UnityEngine.UI.Text>();
            text.text = textInfo.content;
            text.fontSize = textInfo.fontSize;
            text.color = ParseColor(textInfo.color);
            text.alignment = GetTextAnchor(textInfo.alignment);
            text.raycastTarget = false;
            text.horizontalOverflow = UnityEngine.UI.HorizontalWrapMode.Overflow;

            if (textInfo.bold && textInfo.italic)
            {
                text.fontStyle = FontStyle.BoldAndItalic;
            }
            else if (textInfo.bold)
            {
                text.fontStyle = FontStyle.Bold;
            }
            else if (textInfo.italic)
            {
                text.fontStyle = FontStyle.Italic;
            }
        }

        /// <summary>
        ///     设置图层的 RectTransform（相对锚点适配）
        /// </summary>
        private void SetLayerRect(RectTransform rt, PSDLayer layer, PSDDocument.CanvasInfo canvas)
        {
            if (layer.rect == null || canvas == null) return;

            var refW = (float)canvas.width;
            var refH = (float)canvas.height;
            if (_settings != null)
            {
                refW = _settings.referenceWidth;
                refH = _settings.referenceHeight;
            }

            var x = layer.rect.x;
            var y = layer.rect.y;
            var w = layer.rect.w;
            var h = layer.rect.h;

            if (_settings != null && _settings.anchorMode == PSDImportSettings.AnchorMode.Relative)
            {
                // 相对锚点：将 PS 坐标转为 0~1 的归一化比例
                var xMin = x / refW;
                var yMin = 1f - (y + h) / refH; // PS 坐标 Y 轴翻转
                var xMax = (x + w) / refW;
                var yMax = 1f - y / refH;

                rt.anchorMin = new Vector2(xMin, yMin);
                rt.anchorMax = new Vector2(xMax, yMax);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
            else
            {
                // 固定像素定位（左上角锚点）
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(x, -y);
                rt.sizeDelta = new Vector2(w, h);
            }
        }

        /// <summary>
        ///     设置铺满父容器的锚点
        /// </summary>
        private static void SetStretchAnchor(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        /// <summary>
        ///     应用图层样式（投影 → Shadow，描边 → Outline）
        /// </summary>
        private void ApplyStyles(GameObject go, PSDLayer.Styles styles)
        {
            if (styles == null) return;

            // 投影 → Shadow 组件
            if (styles.dropShadow is { enabled: true })
            {
                var shadow = go.AddComponent<UnityEngine.UI.Shadow>();
                shadow.effectColor = ParseColor(styles.dropShadow.color);
                shadow.effectDistance = new Vector2(
                    styles.dropShadow.offsetX,
                    -styles.dropShadow.offsetY // PS Y 轴翻转
                );
            }

            // 描边 → Outline 组件
            if (styles.stroke is { enabled: true })
            {
                var outline = go.AddComponent<UnityEngine.UI.Outline>();
                outline.effectColor = ParseColor(styles.stroke.color);
                outline.effectDistance = new Vector2(styles.stroke.size, styles.stroke.size);
            }
        }

        /// <summary>
        ///     加载精灵图
        /// </summary>
        private Sprite LoadSprite(string texturePath)
        {
            if (string.IsNullOrEmpty(texturePath)) return null;
            if (_spriteCache.TryGetValue(texturePath, out var cached)) return cached;

            // 尝试通过 Resources 加载
            var fileName = System.IO.Path.GetFileNameWithoutExtension(texturePath);
            var sprite = Resources.Load<Sprite>(fileName);
            if (sprite != null)
            {
                _spriteCache[texturePath] = sprite;
            }

            return sprite;
        }

        /// <summary>
        ///     尝试加载 TMP 字体
        /// </summary>
        private void TryLoadTMPFont(MonoBehaviour tmp, string fontName)
        {
            if (string.IsNullOrEmpty(fontName)) return;

            var path = string.IsNullOrEmpty(_settings.tmpFontResourcePath)
                ? fontName
                : $"{_settings.tmpFontResourcePath}/{fontName}";

            var fontAsset = Resources.Load(path);
            if (fontAsset != null)
            {
                SetTMPProperty(tmp, "font", fontAsset);
            }
        }

        #region 反射辅助方法

        private static void SetTMPProperty(MonoBehaviour tmp, string propertyName, object value)
        {
            try
            {
                var prop = tmp.GetType().GetProperty(propertyName);
                prop?.SetValue(tmp, value);
            }
            catch
            {
                // 忽略属性设置失败
            }
        }

        private static object GetTMPAlignment(string alignment)
        {
            try
            {
                var enumType = Type.GetType("TMPro.TextAlignmentOptions, Unity.TextMeshPro");
                if (enumType == null) return 0;

                return alignment switch
                {
                    "left" => Enum.Parse(enumType, "Left"),
                    "center" => Enum.Parse(enumType, "Center"),
                    "right" => Enum.Parse(enumType, "Right"),
                    _ => Enum.Parse(enumType, "Left")
                };
            }
            catch
            {
                return 0;
            }
        }

        #endregion

        #region 工具方法

        /// <summary>
        ///     解析十六进制颜色字符串
        /// </summary>
        public static Color ParseColor(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return Color.white;

            hex = hex.TrimStart('#');
            if (hex.Length == 6)
            {
                var r = Convert.ToInt32(hex[..2], 16) / 255f;
                var g = Convert.ToInt32(hex[2..4], 16) / 255f;
                var b = Convert.ToInt32(hex[4..6], 16) / 255f;
                return new Color(r, g, b);
            }

            if (hex.Length == 8)
            {
                var r = Convert.ToInt32(hex[..2], 16) / 255f;
                var g = Convert.ToInt32(hex[2..4], 16) / 255f;
                var b = Convert.ToInt32(hex[4..6], 16) / 255f;
                var a = Convert.ToInt32(hex[6..8], 16) / 255f;
                return new Color(r, g, b, a);
            }

            return Color.white;
        }

        /// <summary>
        ///     文本对齐字符串转 TextAnchor
        /// </summary>
        public static TextAnchor GetTextAnchor(string alignment)
        {
            return alignment switch
            {
                "left" => TextAnchor.MiddleLeft,
                "center" => TextAnchor.MiddleCenter,
                "right" => TextAnchor.MiddleRight,
                _ => TextAnchor.MiddleLeft
            };
        }

        #endregion
    }
}
