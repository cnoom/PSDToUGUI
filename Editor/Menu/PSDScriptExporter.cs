namespace CNoom.PSDToUGUI.Editor
{
    /// <summary>
    ///     PS 导出脚本内容提供器
    ///     <para>将 JSX 脚本作为字符串内嵌，方便用户通过菜单导出</para>
    /// </summary>
    public static class PSDScriptExporter
    {
        /// <summary>
        ///     获取 PS 导出脚本内容
        /// </summary>
        public static string GetScript()
        {
            return s_script;
        }

        private static readonly string s_script = @"
// ============================================================
// PSD To UGUI 导出脚本
// 版本: 1.0.0
// 兼容: Photoshop CC 2015+
// 说明: 导出当前文档的图层结构和切图资源
// ============================================================

(function() {
    // 检查是否有打开的文档
    if (!app.documents.length) {
        alert('请先打开一个 PSD 文件！');
        return;
    }

    var doc = app.activeDocument;

    // 选择导出目录
    var exportDir = Folder.selectDialog('选择导出目录');
    if (!exportDir) return;

    // 创建资源目录
    var texturesDir = new Folder(exportDir.fsName + '/textures');
    if (!texturesDir.exists) texturesDir.create();

    // 导出设置
    var exportOptions = new PNGSaveOptions();
    exportOptions.compression = 9;

    // 收集所有图层信息
    var layers = [];
    var textureIndex = 0;

    function processLayer(layer) {
        var info = {};

        info.id = 'layer_' + ('0000' + textureIndex).slice(-4);
        info.name = layer.name;
        info.visible = layer.visible;
        info.opacity = layer.opacity / 100;

        // 确定图层类型
        if (layer.typename === 'LayerSet') {
            info.type = 'group';
        } else if (layer.kind === LayerKind.TEXT) {
            info.type = 'text';
        } else if (layer.kind === LayerKind.SOLIDFILL ||
                   layer.kind === LayerKind.GRADIENTFILL ||
                   layer.kind === LayerKind.PATTERNFILL) {
            info.type = 'shape';
        } else {
            info.type = 'pixel';
        }

        // 获取图层边界
        var bounds = layer.bounds;
        info.rect = {
            x: Math.round(bounds[0].value),
            y: Math.round(bounds[1].value),
            w: Math.round(bounds[2].value - bounds[0].value),
            h: Math.round(bounds[3].value - bounds[1].value)
        };

        // 文本信息
        if (layer.kind === LayerKind.TEXT && layer.textItem) {
            var ti = layer.textItem;
            info.text = {};

            try { info.text.content = ti.contents; } catch(e) { info.text.content = ''; }
            try { info.text.font = ti.font; } catch(e) { info.text.font = 'Arial'; }
            try { info.text.fontSize = Math.round(ti.size.value); } catch(e) { info.text.fontSize = 24; }

            // 颜色
            try {
                var c = ti.color;
                var r = Math.round(c.rgb.red);
                var g = Math.round(c.rgb.green);
                var b = Math.round(c.rgb.blue);
                info.text.color = '#' +
                    ('0' + r.toString(16)).slice(-2) +
                    ('0' + g.toString(16)).slice(-2) +
                    ('0' + b.toString(16)).slice(-2);
            } catch(e) {
                info.text.color = '#FFFFFF';
            }

            // 对齐
            try {
                var just = ti.justification;
                if (just == Justification.CENTER) info.text.alignment = 'center';
                else if (just == Justification.RIGHT) info.text.alignment = 'right';
                else info.text.alignment = 'left';
            } catch(e) {
                info.text.alignment = 'left';
            }

            try { info.text.lineHeight = Math.round(ti.leading.value); } catch(e) {}
            try { info.text.bold = ti.fauxBold; } catch(e) { info.text.bold = false; }
            try { info.text.italic = ti.fauxItalic; } catch(e) { info.text.italic = false; }
        }

        // 导出切图（非文本、非空图层）
        if (info.type !== 'group' && info.type !== 'text' && info.rect.w > 0 && info.rect.h > 0) {
            var texName = 'layer_' + ('0000' + textureIndex).slice(-4) + '.png';
            textureIndex++;

            // 保存并导出
            var savedState = doc.activeHistoryState;
            try {
                exportLayerTexture(layer, texturesDir.fsName + '/' + texName);
                info.texture = 'textures/' + texName;
            } catch(e) {
                info.texture = '';
            }
            doc.activeHistoryState = savedState;
        }

        // 图层样式
        info.styles = {};
        if (layer.kind !== LayerKind.TEXT && layer.kind !== LayerKind.NORMAL) {
            // 略，复杂样式后续版本支持
        }

        // 递归子图层
        if (layer.typename === 'LayerSet') {
            info.children = [];
            for (var i = layer.layers.length - 1; i >= 0; i--) {
                info.children.push(processLayer(layer.layers[i]));
            }
        }

        return info;
    }

    // 导出单个图层的纹理
    function exportLayerTexture(layer, filePath) {
        var file = new File(filePath);

        // 隐藏其他图层
        var layerVisibility = [];
        for (var i = 0; i < doc.layers.length; i++) {
            layerVisibility.push(doc.layers[i].visible);
            doc.layers[i].visible = false;
        }

        // 显示目标图层及其父级
        layer.visible = true;
        showParentLayers(layer);

        // 裁剪到图层边界并导出
        var bounds = layer.bounds;
        var x = Math.round(bounds[0].value);
        var y = Math.round(bounds[1].value);
        var w = Math.round(bounds[2].value - bounds[0].value);
        var h = Math.round(bounds[3].value - bounds[1].value);

        if (w > 0 && h > 0) {
            // 使用画布大小导出（保留位置信息）
            var pngOptions = new PNGSaveOptions();
            pngOptions.compression = 9;
            doc.saveAs(file, pngOptions, true, Extension.LOWERCASE);
        }

        // 恢复图层可见性
        for (var i = 0; i < doc.layers.length; i++) {
            doc.layers[i].visible = layerVisibility[i];
        }
    }

    // 显示图层的所有父级
    function showParentLayers(layer) {
        var parent = layer.parent;
        while (parent && parent !== doc) {
            if (parent.visible === false) {
                parent.visible = true;
            }
            parent = parent.parent;
        }
    }

    // 处理所有顶层图层（从上到下）
    for (var i = doc.layers.length - 1; i >= 0; i--) {
        layers.push(processLayer(doc.layers[i]));
    }

    // 构建 JSON 文档
    var document = {
        version: '1.0',
        canvas: {
            width: doc.width.value,
            height: doc.height.value
        },
        layers: layers
    };

    // 序列化 JSON（ExtendScript 手动实现）
    function toJson(obj, indent) {
        indent = indent || 0;
        var spaces = '';
        for (var s = 0; s < indent; s++) spaces += '    ';
        var result = '';

        if (obj === null || obj === undefined) {
            return 'null';
        }

        if (typeof obj === 'number') {
            return obj.toString();
        }

        if (typeof obj === 'boolean') {
            return obj ? 'true' : 'false';
        }

        if (typeof obj === 'string') {
            return '""' + obj.replace(/\\/g, '\\\\').replace(/""/g, '\\""').replace(/\n/g, '\\n') + '""';
        }

        if (obj instanceof Array) {
            if (obj.length === 0) return '[]';
            result = '[\n';
            for (var i = 0; i < obj.length; i++) {
                result += spaces + '    ' + toJson(obj[i], indent + 1);
                if (i < obj.length - 1) result += ',';
                result += '\n';
            }
            result += spaces + ']';
            return result;
        }

        if (typeof obj === 'object') {
            var keys = [];
            for (var key in obj) {
                if (obj.hasOwnProperty(key) && typeof obj[key] !== 'function') {
                    keys.push(key);
                }
            }
            if (keys.length === 0) return '{}';
            result = '{\n';
            for (var i = 0; i < keys.length; i++) {
                result += spaces + '    ""' + keys[i] + '"": ' + toJson(obj[keys[i]], indent + 1);
                if (i < keys.length - 1) result += ',';
                result += '\n';
            }
            result += spaces + '}';
            return result;
        }

        return obj.toString();
    }

    // 写入 JSON 文件
    var jsonFile = new File(exportDir.fsName + '/document.json');
    jsonFile.encoding = 'UTF-8';
    jsonFile.open('w');
    jsonFile.write(toJson(document));
    jsonFile.close();

    alert('导出完成！\n\n' +
          '文件位置: ' + exportDir.fsName + '\n' +
          '图层总数: ' + layers.length + '\n\n' +
          '请将整个目录复制到 Unity 项目中，\n然后通过菜单 Tools > PSD To UGUI > 导入 PSD 进行导入。');
})();
";
    }
}
