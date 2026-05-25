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

        /// <summary>旧版单文件 MPLUS 指令表（兼容）</summary>
        public const string InterleaverMplus1X16 = "interleaverSwitch-MPLUS";

        /// <summary>1×16 入光 MPLUS（COM1，TLS→DUT IN）</summary>
        public const string InterleaverMplus1X16In = "interleaverSwitch-MPLUS-IN";

        /// <summary>1×32 出光 MPLUS（COM2，DUT OUT→N7745C）</summary>
        public const string InterleaverMplus1X32Out = "interleaverSwitch-MPLUS-OUT";

        /// <summary>出光侧最大通道数（与模板 PORT/L 名映射）</summary>
        public const int MaxOutputSwitchChannels = 32;

        /// <summary>入光侧最大通道数（16 SN 槽位）</summary>
        public const int MaxInputSwitchChannels = 16;

        /// <summary>出光侧最大通道数（兼容旧代码引用）</summary>
        public const int MaxSwitchChannels = MaxOutputSwitchChannels;

        /// <summary>单端口模式下最多并行 SN 数（16 SN × 1 路）</summary>
        public const int MaxProductsSinglePort = 16;

        /// <summary>
        /// 规范化光开关指令文件名（去空格/BOM，修复 MPLUS 名称被误加空格）。
        /// </summary>
        public static string SanitizeMplusSwitchShowName(string showName)
        {
            if (string.IsNullOrWhiteSpace(showName))
                return showName ?? "";
            return showName.Trim().Replace(" ", "");
        }

        /// <summary>
        /// 迁移旧 ShowName；双 MPLUS 时仅将空名或旧单文件名按列表顺序映射为 IN/OUT。
        /// </summary>
        public static void NormalizeMplusSwitchShowName(List<List<DeviceConfig>> deviceConfigs)
        {
            if (deviceConfigs == null)
                return;

            foreach (List<DeviceConfig> devices in deviceConfigs)
            {
                if (devices == null)
                    continue;

                var mplusList = new List<DeviceConfig>();
                foreach (DeviceConfig cfg in devices)
                {
                    if (cfg != null &&
                        MplusSwitchType.Equals(cfg.ControlName, StringComparison.OrdinalIgnoreCase))
                        mplusList.Add(cfg);
                }

                if (mplusList.Count == 0)
                    continue;

                bool hasIn = mplusList.Exists(c =>
                    InterleaverMplus1X16In.Equals(c.ShowName, StringComparison.OrdinalIgnoreCase));
                bool hasOut = mplusList.Exists(c =>
                    InterleaverMplus1X32Out.Equals(c.ShowName, StringComparison.OrdinalIgnoreCase));

                int legacyIndex = 0;
                foreach (DeviceConfig cfg in mplusList)
                {
                    cfg.ShowName = SanitizeMplusSwitchShowName(cfg.ShowName);
                    if (string.IsNullOrWhiteSpace(cfg.ShowName) ||
                        InterleaverMplus1X16.Equals(cfg.ShowName, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!hasIn && legacyIndex == 0)
                        {
                            cfg.ShowName = InterleaverMplus1X16In;
                            hasIn = true;
                        }
                        else if (!hasOut)
                        {
                            cfg.ShowName = InterleaverMplus1X32Out;
                            hasOut = true;
                        }
                        legacyIndex++;
                    }
                }
            }
        }
    }
}
