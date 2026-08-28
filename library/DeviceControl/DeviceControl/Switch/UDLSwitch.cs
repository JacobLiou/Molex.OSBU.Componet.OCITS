using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MolexUtility.Device;
using UDL2_ServerLib;

namespace DeviceControl
{
    public class UDLSwitch: IUDLSwitch
    {
        /// <summary>
        /// switch GUID
        /// </summary>
        public int switchGUID { get; set; }

        /// <summary>
        /// 设置开关
        /// </summary>
        /// <param name="comPort">com端口</param>
        /// <param name="outPort">输出端口</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0--成功，1--失败，其他--出错信息</returns>
        public int SetSwitchPosition(int comPort, int outPort, ref string errMsg)
        {
            if(DeviceHandle.oswCtrl == null)
            {
                errMsg = "switch object is null.";
                return 1;
            }
            DeviceHandle.oswCtrl.SetSwitchPosition(switchGUID, comPort, outPort);
            DeviceHandle.GetUDLMessage(ref errMsg);
            if (errMsg.Length > 0)
                return 1;
            return 0;
        }
    }
}
