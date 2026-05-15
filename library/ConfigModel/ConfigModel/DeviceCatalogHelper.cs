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
        private const string MplusSwitchType = "MPLUSSwitch";

        public static void EnsureMplusSwitchInCatalog(List<string> deviceNameList, List<List<DeviceConfig>> allDeviceConfig)
        {
            if (deviceNameList == null || allDeviceConfig == null)
                return;

            for (int i = 0; i < deviceNameList.Count && i < allDeviceConfig.Count; i++)
            {
                if (!IsOpticalSwitchCategory(deviceNameList[i], allDeviceConfig[i]))
                    continue;

                if (allDeviceConfig[i].Any(d =>
                    MplusSwitchType.Equals(d.ControlName, StringComparison.OrdinalIgnoreCase)))
                    continue;

                DeviceConfig template = allDeviceConfig[i].FirstOrDefault(d =>
                    "OMSSwitch".Equals(d.ControlName, StringComparison.OrdinalIgnoreCase))
                    ?? allDeviceConfig[i].FirstOrDefault(d =>
                        d.ControlName != null &&
                        d.ControlName.EndsWith("Switch", StringComparison.OrdinalIgnoreCase));

                DeviceConfig mplus = template != null ? template.Clone() : CreateDefaultMplusSwitchTemplate();
                mplus.ShowName = "MPLUS 1X16光开关";
                mplus.ControlName = MplusSwitchType;
                ClearControlValues(mplus);
                allDeviceConfig[i].Add(mplus);
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

        private static DeviceConfig CreateDefaultMplusSwitchTemplate()
        {
            var cfg = new DeviceConfig();
            cfg.ShowName = "MPLUS 1X16光开关";
            cfg.ControlName = Devices.MPLUSSwitch.GetAdditional();
            cfg.ControlKey[0] = "COM";
            cfg.ControlKey[1] = "波特率";
            cfg.Control[1] = "9600";
            return cfg;
        }

        private static void ClearControlValues(DeviceConfig config)
        {
            for (int i = 0; i < config.Control.Length; i++)
                config.Control[i] = "";
        }
    }
}
