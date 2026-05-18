using System;
using System.Collections.Generic;

namespace MolexUtility.Device
{
    /// <summary>
    /// 光开关指令配置文件名（运行目录 switch\ 下无扩展名文件）及类型常量。
    /// </summary>
    public static class OpticalSwitchConfigNames
    {
        public const string MplusSwitchType = "MPLUSSwitch";

        /// <summary>1×16 MPLUS 终测光开关指令表</summary>
        public const string InterleaverMplus1X16 = "interleaverSwitch-MPLUS";

        /// <summary>1×16 工位最大开关通道数</summary>
        public const int MaxSwitchChannels = 16;

        /// <summary>单端口模式下最多并行 SN 数（16 SN × 1 路）</summary>
        public const int MaxProductsSinglePort = 16;

        public static void NormalizeMplusSwitchShowName(List<List<DeviceConfig>> deviceConfigs)
        {
            if (deviceConfigs == null)
                return;

            foreach (List<DeviceConfig> devices in deviceConfigs)
            {
                if (devices == null)
                    continue;
                foreach (DeviceConfig cfg in devices)
                {
                    if (cfg == null ||
                        !MplusSwitchType.Equals(cfg.ControlName, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (!InterleaverMplus1X16.Equals(cfg.ShowName, StringComparison.OrdinalIgnoreCase))
                        cfg.ShowName = InterleaverMplus1X16;
                }
            }
        }
    }
}
