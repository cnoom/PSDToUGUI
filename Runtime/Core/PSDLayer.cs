using System;
using System.Collections.Generic;

namespace CNoom.PSDToUGUI
{
    /// <summary>
    ///     PSD 图层数据模型
    /// </summary>
    [Serializable]
    public class PSDLayer
    {
        /// <summary>
        ///     图层唯一标识
        /// </summary>
        public string id;

        /// <summary>
        ///     图层名称
        /// </summary>
        public string name;

        /// <summary>
        ///     图层类型
        /// </summary>
        public LayerType type;

        /// <summary>
        ///     矩形区域（像素坐标，PS 坐标系：左上角为原点）
        /// </summary>
        public RectInfo rect;

        /// <summary>
        ///     不透明度 (0~1)
        /// </summary>
        public float opacity = 1f;

        /// <summary>
        ///     可见性
        /// </summary>
        public bool visible = true;

        /// <summary>
        ///     导出的纹理相对路径（仅 pixel/shape 类型）
        /// </summary>
        public string texture;

        /// <summary>
        ///     文本信息（仅 text 类型）
        /// </summary>
        public TextInfo text;

        /// <summary>
        ///     图层样式
        /// </summary>
        public LayerStyles styles;

        /// <summary>
        ///     子图层列表（仅 group 类型）
        /// </summary>
        public List<PSDLayer> children = new();

        /// <summary>
        ///     图层类型枚举
        /// </summary>
        [Serializable]
        public enum LayerType
        {
            Pixel,
            Text,
            Shape,
            Group
        }

        /// <summary>
        ///     矩形区域
        /// </summary>
        [Serializable]
        public class RectInfo
        {
            public int x;
            public int y;
            public int w;
            public int h;
        }

        /// <summary>
        ///     文本属性
        /// </summary>
        [Serializable]
        public class TextInfo
        {
            /// <summary>
            ///     文本内容
            /// </summary>
            public string content;

            /// <summary>
            ///     字体名称
            /// </summary>
            public string font;

            /// <summary>
            ///     字号
            /// </summary>
            public int fontSize;

            /// <summary>
            ///     字体颜色（十六进制，如 #FFFFFF）
            /// </summary>
            public string color;

            /// <summary>
            ///     对齐方式：left/center/right
            /// </summary>
            public string alignment = "left";

            /// <summary>
            ///     行高（像素）
            /// </summary>
            public int lineHeight;

            /// <summary>
            ///     是否粗体
            /// </summary>
            public bool bold;

            /// <summary>
            ///     是否斜体
            /// </summary>
            public bool italic;
        }

        /// <summary>
        ///     图层样式
        /// </summary>
        [Serializable]
        public class LayerStyles
        {
            /// <summary>
            ///     投影
            /// </summary>
            public ShadowStyle dropShadow;

            /// <summary>
            ///     描边
            /// </summary>
            public StrokeStyle stroke;
        }

        /// <summary>
        ///     投影样式
        /// </summary>
        [Serializable]
        public class ShadowStyle
        {
            public bool enabled;
            public string color;
            public int offsetX;
            public int offsetY;
            public int blur;
            public int spread;
        }

        /// <summary>
        ///     描边样式
        /// </summary>
        [Serializable]
        public class StrokeStyle
        {
            public bool enabled;
            public string color;
            public int size;
        }
    }
}
