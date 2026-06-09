using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UDL2_ServerLib;

namespace MolexUtility.Device
{
    public interface IUDLSwitch
    {
        /// <summary>
        /// switch GUID
        /// </summary>
        int switchGUID { get; set; }

        /// <summary>
        /// 设置开关
        /// </summary>
        /// <param name="comPort">com端口</param>
        /// <param name="outPort">输出端口</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0--成功，1--失败，其他--出错信息</returns>
        int SetSwitchPosition(int comPort, int outPort, ref string errMsg);
    }
}
