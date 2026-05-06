using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MolexUtility;
using MolexUtility.Device;

namespace DeviceControl
{
    public class UDLTccCtrl:IUDLTCC
    {
        /// <summary>
        /// 设备GUID
        /// </summary>
        public int deviceGUID { get; set; }

        /// <summary>
        /// 读取循环箱温度
        /// </summary>
        /// <param name="getTempr">读取的温度</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0--成功，1--失败，其他--奔溃</returns>
        public int GetCurrentTemp(out double getTempr, ref string errMsg)
        {
            getTempr = CommonFunction.GetDefaultValue();
            if (DeviceHandle.tccCtrl == null)
            {
                errMsg = "TCC object is null.";
                return 1;
            }
            DeviceHandle.tccCtrl.GetCurrentTemp(deviceGUID, out getTempr);
            DeviceHandle.GetUDLMessage(ref errMsg);
            if (errMsg.Length > 0)
                return 1;
            return 0;
        }

        /// <summary>
        /// 设置循环箱温度
        /// </summary>
        /// <param name="setTempr">设置循环箱温度</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0--成功，1--失败，其他--奔溃</returns>
        public int SetTempSetpoint(double setTempr, ref string errMsg)
        {
            if (DeviceHandle.tccCtrl == null)
            {
                errMsg = "TCC object is null.";
                return 1;
            }
            DeviceHandle.tccCtrl.SetTempSetpoint(deviceGUID, setTempr);
            DeviceHandle.GetUDLMessage(ref errMsg);
            if (errMsg.Length > 0)
            {
                double getTempr = -1000;
                DeviceHandle.tccCtrl.GetTempSetpoint(deviceGUID, out getTempr);
                if (setTempr.CompareTo(getTempr) == 0)
                    return 0;
                return 1;
            }
            return 0;
        }
    }
}
