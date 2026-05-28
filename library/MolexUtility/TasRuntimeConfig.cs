using System;
using System.IO;

namespace MolexUtility
{
    /// <summary>
    /// 运行目录下与 USL.TAS / TMS 相关的可选开关。
    /// </summary>
    public static class TasRuntimeConfig
    {
        /// <summary>
        /// 若存在空标记文件 set\DisableUploadRefCalibrationToTms.txt，则归零完成后不调用
        /// UploadTestSystemCailbrationTime（例如 GDS 服务不可达、离线调试）。
        /// </summary>
        public static bool IsUploadRefCalibrationDisabled()
        {
            string flag = Path.Combine(Environment.CurrentDirectory, "set", "DisableUploadRefCalibrationToTms.txt");
            return File.Exists(flag);
        }

        /// <summary>
        /// 若存在空标记文件 set\DisableAutoSwitchDuringRef.txt，则系统归零时不自动切换光开关，
        /// 由操作员手动下发 MSW 后再继续扫描（光路/指令对比实验）。
        /// </summary>
        public static bool IsRefAutoSwitchDisabled()
        {
            string flag = Path.Combine(Environment.CurrentDirectory, "set", "DisableAutoSwitchDuringRef.txt");
            return File.Exists(flag);
        }

        /// <summary>
        /// 若存在空标记文件 set\DisableTccChamberCheck.txt，则测试前不校验 UDL 循环箱温度（无 TCC 现场调试）。
        /// </summary>
        public static bool IsTccChamberCheckDisabled()
        {
            string flag = Path.Combine(Environment.CurrentDirectory, "set", "DisableTccChamberCheck.txt");
            return File.Exists(flag);
        }

        /// <summary>
        /// 若存在空标记文件 set\RtOnlyTest.txt，则一键/单项测试仅调度常温项（约 20~30°C），跳过 LT/HT。
        /// </summary>
        public static bool IsRtOnlyTestMode()
        {
            string flag = Path.Combine(Environment.CurrentDirectory, "set", "RtOnlyTest.txt");
            return File.Exists(flag);
        }
    }
}
