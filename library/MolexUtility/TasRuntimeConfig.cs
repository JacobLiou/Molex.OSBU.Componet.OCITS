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
        /// 光谱调节共享落盘根目录：读取 set\SpectrumShareRoot.txt 第一行非空路径（# 行为注释）；
        /// 同机推荐 D:\ITL_SpectrumShare；缺省为 {CurrentDirectory}\SpectrumShare。
        /// </summary>
        public static string GetSpectrumShareRoot()
        {
            string cfg = Path.Combine(Environment.CurrentDirectory, "set", "SpectrumShareRoot.txt");
            try
            {
                if (File.Exists(cfg))
                {
                    foreach (string line in File.ReadAllLines(cfg))
                    {
                        if (string.IsNullOrWhiteSpace(line))
                            continue;
                        string trimmed = line.Trim();
                        if (trimmed.StartsWith("#"))
                            continue;
                        return trimmed;
                    }
                }
            }
            catch
            {
            }
            return Path.Combine(Environment.CurrentDirectory, "SpectrumShare");
        }
    }
}
