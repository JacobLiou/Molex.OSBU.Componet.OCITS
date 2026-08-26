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
        private sealed class StringRef
        {
            public string Value;
        }

        private sealed class TempHolder
        {
            public int Res;
            public double Temp;
            public string Err;
        }

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
            if (UdlStaHost.IsOnHostThread())
                return GetCurrentTempImpl(out getTempr, ref errMsg);

            var holder = new TempHolder { Err = errMsg };
            int res = UdlStaHost.Invoke(() =>
            {
                double temp;
                string localErr = holder.Err;
                holder.Res = GetCurrentTempImpl(out temp, ref localErr);
                holder.Temp = temp;
                holder.Err = localErr;
                return holder.Res;
            });
            getTempr = holder.Temp;
            errMsg = holder.Err;
            return res;
        }

        private int GetCurrentTempImpl(out double getTempr, ref string errMsg)
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
            if (UdlStaHost.IsOnHostThread())
                return SetTempSetpointImpl(setTempr, ref errMsg);

            var errRef = new StringRef { Value = errMsg };
            int res = UdlStaHost.Invoke(() => SetTempSetpointImpl(setTempr, ref errRef.Value));
            errMsg = errRef.Value;
            return res;
        }

        private int SetTempSetpointImpl(double setTempr, ref string errMsg)
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

        /// <summary>
        /// 读取循环箱设定温度（setpoint）
        /// </summary>
        public int GetTempSetpoint(out double getTempr, ref string errMsg)
        {
            if (UdlStaHost.IsOnHostThread())
                return GetTempSetpointImpl(out getTempr, ref errMsg);

            var holder = new TempHolder { Err = errMsg };
            int res = UdlStaHost.Invoke(() =>
            {
                double temp;
                string localErr = holder.Err;
                holder.Res = GetTempSetpointImpl(out temp, ref localErr);
                holder.Temp = temp;
                holder.Err = localErr;
                return holder.Res;
            });
            getTempr = holder.Temp;
            errMsg = holder.Err;
            return res;
        }

        private int GetTempSetpointImpl(out double getTempr, ref string errMsg)
        {
            getTempr = CommonFunction.GetDefaultValue();
            if (DeviceHandle.tccCtrl == null)
            {
                errMsg = "TCC object is null.";
                return 1;
            }
            DeviceHandle.tccCtrl.GetTempSetpoint(deviceGUID, out getTempr);
            DeviceHandle.GetUDLMessage(ref errMsg);
            if (errMsg.Length > 0)
                return 1;
            return 0;
        }
    }
}
