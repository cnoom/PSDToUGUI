using System;
using System.Collections.Generic;
using UnityEngine;

namespace CNoom.PSDToUGUI
{
    /// <summary>
    ///     PSD 文档数据模型
    ///     <para>对应 PS 导出的 JSON 根对象</para>
    /// </summary>
    [Serializable]
    public class PSDDocument
    {
        /// <summary>
        ///     格式版本号
        /// </summary>
        public string version = "1.0";

        /// <summary>
        ///     画布信息
        /// </summary>
        public CanvasInfo canvas;

        /// <summary>
        ///     图层列表（顶层）
        /// </summary>
        public List<PSDLayer> layers = new();

        /// <summary>
        ///     画布信息
        /// </summary>
        [Serializable]
        public class CanvasInfo
        {
            /// <summary>
            ///     画布宽度（像素）
            /// </summary>
            public int width;

            /// <summary>
            ///     画布高度（像素）
            /// </summary>
            public int height;
        }
    }
}
