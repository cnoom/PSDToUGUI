using UnityEngine;

namespace CNoom.PSDToUGUI
{
    /// <summary>
    ///     PSD 导入配置
    ///     <para>ScriptableObject，可在 Inspector 中配置</para>
    /// </summary>
    [CreateAssetMenu(fileName = "PSDImportSettings", menuName = "PSDToUGUI/导入设置")]
    public class PSDImportSettings : ScriptableObject
    {
        [Header("画布参考尺寸")]
        [Tooltip("设计稿参考宽度")]
        public int referenceWidth = 1920;

        [Tooltip("设计稿参考高度")]
        public int referenceHeight = 1080;

        [Header("文本设置")]
        [Tooltip("是否使用 TextMeshPro（需安装 TMP 包）")]
        public bool useTextMeshPro = true;

        [Tooltip("TMP 字体资源查找路径（Resources 下的相对路径）")]
        public string tmpFontResourcePath = "Fonts";

        [Header("锚点策略")]
        [Tooltip("锚点适配模式")]
        public AnchorMode anchorMode = AnchorMode.Relative;

        [Header("资源设置")]
        [Tooltip("纹理的 Pixels Per Unit")]
        public float pixelsPerUnit = 100f;

        [Tooltip("精灵图过滤模式")]
        public FilterMode filterMode = FilterMode.Bilinear;

        [Header("输出设置")]
        [Tooltip("生成预制体的输出根路径")]
        public string outputPrefabPath = "Assets/PSDOutput/Prefabs";

        [Tooltip("生成精灵图的输出根路径")]
        public string outputSpritePath = "Assets/PSDOutput/Sprites";

        /// <summary>
        ///     锚点适配模式
        /// </summary>
        public enum AnchorMode
        {
            /// <summary>
            ///     相对锚点适配（根据位置计算 anchors）
            /// </summary>
            Relative,

            /// <summary>
            ///     固定像素定位（左上角锚点）
            /// </summary>
            FixedTopLeft
        }

        /// <summary>
        ///     默认配置
        /// </summary>
        public static PSDImportSettings Default
        {
            get
            {
                var settings = CreateInstance<PSDImportSettings>();
                settings.name = "PSDImportSettings";
                return settings;
            }
        }
    }
}
