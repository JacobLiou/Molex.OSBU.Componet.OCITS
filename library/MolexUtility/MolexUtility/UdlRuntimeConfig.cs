using System;
using System.IO;

namespace MolexUtility
{
    /// <summary>
    /// 运行目录下与 UDL2_Engine 相关的约定（DeviceHandle、部分 UI 模块会读取）。
    /// </summary>
    public static class UdlRuntimeConfig
    {
        /// <summary>
        /// 若存在空的标记文件 set\DisableUDLEngine.txt，则跳过加载 UDL2_Engine 及 UDLConfig.xml
        ///（例如本地调试无硬件、或 UDLConfig 不完整导致 DevKey1 解析失败时使用）。
        /// </summary>
        public static bool IsUdlEngineLoadDisabled()
        {
            string flag = Path.Combine(Environment.CurrentDirectory, "set", "DisableUDLEngine.txt");
            return File.Exists(flag);
        }
    }
}
