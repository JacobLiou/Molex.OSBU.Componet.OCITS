using System;
using System.Collections.Generic;
using System.Linq;
using MolexUtility;
using MolexUtility.Device;

namespace ConfigModel
{
    /// <summary>
    /// 维护 AllDevice.xml 设备清单：在光源盒分类中补全 MPLUSSwitch 等新增类型。
    /// </summary>
    internal static class DeviceCatalogHelper
    {
        private const string MplusSwitchType = OpticalSwitchConfigNames.MplusSwitchType;

        public static void EnsureMplusSwitchInCatalog(List<string> deviceNameList, List<List<DeviceConfig>> allDeviceConfig)
        {
            if (deviceNameList == null || allDeviceConfig == null)
                return;

            for (int i = 0; i < deviceNameList.Count && i < allDeviceConfig.Count; i++)
            {
                if (!IsOpticalSwitchCategory(deviceNameList[i], allDeviceConfig[i]))
                    continue;

                DeviceConfig template = allDeviceConfig[i].FirstOrDefault(d =>
                    "OMSSwitch".Equals(d.ControlName, StringComparison.OrdinalIgnoreCase))
                    ?? allDeviceConfig[i].FirstOrDefault(d =>
                        d.ControlName != null &&
                        d.ControlName.EndsWith("Switch", StringComparison.OrdinalIgnoreCase));

                if (!allDeviceConfig[i].Any(d =>
                    MplusSwitchType.Equals(d.ControlName, StringComparison.OrdinalIgnoreCase) &&
                    OpticalSwitchConfigNames.InterleaverMplus1X16In.Equals(
                        d.ShowName, StringComparison.OrdinalIgnoreCase)))
                {
                    DeviceConfig input = template != null ? template.Clone() : CreateDefaultMplusInputTemplate();
                    input.ShowName = OpticalSwitchConfigNames.InterleaverMplus1X16In;
                    input.ControlName = MplusSwitchType;
                    ClearControlValues(input);
                    allDeviceConfig[i].Add(input);
                }

                if (!allDeviceConfig[i].Any(d =>
                    MplusSwitchType.Equals(d.ControlName, StringComparison.OrdinalIgnoreCase) &&
                    OpticalSwitchConfigNames.InterleaverMplus1X32Out.Equals(
                        d.ShowName, StringComparison.OrdinalIgnoreCase)))
                {
                    DeviceConfig output = template != null ? template.Clone() : CreateDefaultMplusOutputTemplate();
                    output.ShowName = OpticalSwitchConfigNames.InterleaverMplus1X32Out;
                    output.ControlName = MplusSwitchType;
                    ClearControlValues(output);
                    allDeviceConfig[i].Add(output);
                }
            }
        }

        private static bool IsOpticalSwitchCategory(string categoryName, List<DeviceConfig> devices)
        {
            if (categoryName != null &&
                (categoryName.Contains("光源盒") || categoryName.Contains("光开关")))
                return true;

            return devices.Any(d =>
                d.ControlName != null &&
                (d.ControlName.EndsWith("Switch", StringComparison.OrdinalIgnoreCase) ||
                 d.ControlName.IndexOf("Switch", StringComparison.OrdinalIgnoreCase) >= 0));
        }

        public static void NormalizeMplusSwitchShowName(List<List<DeviceConfig>> deviceConfigs)
        {
            if (deviceConfigs == null)
                return;

            foreach (List<DeviceConfig> devices in deviceConfigs)
            {
                if (devices == null)
                    continue;

                bool hasIn = devices.Any(d =>
                    d != null &&
                    MplusSwitchType.Equals(d.ControlName, StringComparison.OrdinalIgnoreCase) &&
                    OpticalSwitchConfigNames.InterleaverMplus1X16In.Equals(
                        d.ShowName, StringComparison.OrdinalIgnoreCase));
                bool hasOut = devices.Any(d =>
                    d != null &&
                    MplusSwitchType.Equals(d.ControlName, StringComparison.OrdinalIgnoreCase) &&
                    OpticalSwitchConfigNames.InterleaverMplus1X32Out.Equals(
                        d.ShowName, StringComparison.OrdinalIgnoreCase));

                int legacyIndex = 0;
                foreach (DeviceConfig cfg in devices)
                {
                    if (cfg == null ||
                        !MplusSwitchType.Equals(cfg.ControlName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (string.IsNullOrWhiteSpace(cfg.ShowName) ||
                        OpticalSwitchConfigNames.InterleaverMplus1X16.Equals(
                            cfg.ShowName, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!hasIn && legacyIndex == 0)
                        {
                            cfg.ShowName = OpticalSwitchConfigNames.InterleaverMplus1X16In;
                            hasIn = true;
                        }
                        else if (!hasOut)
                        {
                            cfg.ShowName = OpticalSwitchConfigNames.InterleaverMplus1X32Out;
                            hasOut = true;
                        }
                        legacyIndex++;
                    }
                }
            }
        }

        private static DeviceConfig CreateDefaultMplusInputTemplate()
        {
            var cfg = new DeviceConfig();
            cfg.ShowName = OpticalSwitchConfigNames.InterleaverMplus1X16In;
            cfg.ControlName = Devices.MPLUSSwitch.GetAdditional();
            cfg.ControlKey[0] = "COM";
            cfg.ControlKey[1] = "波特率";
            cfg.Control[1] = "115200";
            cfg.CheckCmd = "MSW 1,1,2;9,1,1;";
            return cfg;
        }

        private static DeviceConfig CreateDefaultMplusOutputTemplate()
        {
            var cfg = new DeviceConfig();
            cfg.ShowName = OpticalSwitchConfigNames.InterleaverMplus1X32Out;
            cfg.ControlName = Devices.MPLUSSwitch.GetAdditional();
            cfg.ControlKey[0] = "COM";
            cfg.ControlKey[1] = "波特率";
            cfg.Control[1] = "115200";
            cfg.CheckCmd = "MSW 1,1,2;9,1,1;";
            return cfg;
        }

        private static void ClearControlValues(DeviceConfig config)
        {
            for (int i = 0; i < config.Control.Length; i++)
                config.Control[i] = "";
        }
    }
}
