using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UDL2_ServerLib;

namespace MolexUtility.Device
{
    public interface IUDLTCC
    {
        /// <summary>
        /// 设备GUID
        /// </summary>
        int deviceGUID { get; set; }

        /// <summary>
        /// 读取循环箱温度
        /// </summary>
        /// <param name="getTempr">读取的温度</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0--成功，1--失败，其他--奔溃</returns>
        int GetCurrentTemp(out double getTempr, ref string errMsg);

        /// <summary>
        /// 设置循环箱温度
        /// </summary>
        /// <param name="setTempr">设置循环箱温度</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0--成功，1--失败，其他--奔溃</returns>
        int SetTempSetpoint(double setTempr, ref string errMsg);
    }
}
