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
        /// <summary>串行化 TCC UDL 调用，减轻并发导致的缓存溢出。</summary>
        private static readonly object TccUdlLock = new object();

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
            lock (TccUdlLock)
            {
                getTempr = CommonFunction.GetDefaultValue();
                if (DeviceHandle.tccCtrl == null)
                {
                    errMsg = "TCC object is null.";
                    return 1;
                }
                errMsg = "";
                DeviceHandle.tccCtrl.GetCurrentTemp(deviceGUID, out getTempr);
                DeviceHandle.GetUDLMessage(ref errMsg);
                if (errMsg.Length > 0)
                    return 1;
                return 0;
            }
        }

        /// <summary>
        /// 设置循环箱温度。UDL 软错误（缓存溢出/格式）原样返回，由业务层用「朝目标升温」判定。
        /// </summary>
        /// <param name="setTempr">设置循环箱温度</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0--成功，1--失败，其他--奔溃</returns>
        public int SetTempSetpoint(double setTempr, ref string errMsg)
        {
            lock (TccUdlLock)
            {
                if (DeviceHandle.tccCtrl == null)
                {
                    errMsg = "TCC object is null.";
                    return 1;
                }
                errMsg = "";
                DeviceHandle.tccCtrl.SetTempSetpoint(deviceGUID, setTempr);
                DeviceHandle.GetUDLMessage(ref errMsg);
                if (errMsg.Length == 0)
                    return 0;

                // 若读回设定值已与目标一致，可确认写成功（不依赖当前实测温）
                try
                {
                    double getSetpoint = -1000;
                    string spErr = "";
                    DeviceHandle.tccCtrl.GetTempSetpoint(deviceGUID, out getSetpoint);
                    DeviceHandle.GetUDLMessage(ref spErr);
                    if (spErr.Length == 0 && Math.Abs(getSetpoint - setTempr) < 0.05)
                    {
                        CommonFunction.WriteLog(string.Format(
                            "TCC SetTempSetpoint: setpoint matched after udlErr. set={0:F1}, getSP={1:F1}, udlErr={2}",
                            setTempr, getSetpoint, errMsg));
                        errMsg = "";
                        return 0;
                    }
                }
                catch (Exception ex)
                {
                    CommonFunction.WriteLog("TCC GetTempSetpoint after Set fail: " + ex.Message);
                }

                // 软错误码留给业务：朝目标升温 / 操作员确认（驱动不在此用 GetCurrentTemp 强行洗白）
                return 1;
            }
        }
    }
}
