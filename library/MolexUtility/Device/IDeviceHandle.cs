using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MolexUtility.Device
{
    public interface IDeviceHandle
    {
        /// <summary>
        /// 初始化设备
        /// </summary>
        /// <param name="errMsg">出错信息</param>
        /// <returns></returns>s
        int InitDeviceByConfig(ref string errMsg);

        /// <summary>
        /// 关闭所有设备
        /// </summary>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0-正常 1-出错</returns>
        int CloseAllDevice(ref string errMsg);

        /// <summary>
        /// 根据功率计index，获取功率计的对象，通道
        /// </summary>
        /// <param name="index">功率计index，从1开始</param>
        /// <param name="channel">功率计通道</param>
        /// <param name="curPowermeter">功率计对象</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>0--成功  1--出错</returns>
        int GetPowermeterByIndex(int index, ref int channel, ref IPowermeter desPowermeter, ref string errMsg);

        /// <summary>
        /// 根据类型获取光源盒对象
        /// </summary>
        /// <param name="type">光源盒类型，与配置的指令配置文件名称一致</param>
        /// <param name="desSwitch">获取的光源盒对象</param>
        /// <param name="errMsg">具体错误信息</param>
        /// <returns>0--正确  1--出错</returns>
        int GetSwitchByType(string type, ref IOpticalSwitch desSwitch, ref string errMsg);

        /// <summary>
        /// 根据类型获取光源盒对象
        /// </summary>
        /// <param name="idx">设备Index，从1开始</param>
        /// <param name="desSwitch">获取的光源盒对象</param>
        /// <param name="errMsg">具体错误信息</param>
        /// <returns>0--正确  1--出错</returns>
        int GetSwitchByIndex(int idx, ref IOpticalSwitch desSwitch, ref string errMsg);

        /// <summary>
        /// 获取静电计或者万用表对象
        /// </summary>
        /// <param name="index">设备Index，从1开始</param>
        /// <param name="desCurrent">静电计或者万用表对象</param>
        /// <param name="errMsg">出错具体信息</param>
        /// <returns>0--正确  1--出错</returns>
        int GetCurrentByIndex(int index, ref ICurrent desCurrent, ref string errMsg);

        /// <summary>
        /// 获取偏振控制器对象
        /// </summary>
        /// <param name="index">设备Index，从1开始</param>
        /// <param name="pdlCtrl">偏振控制器对象</param>
        /// <param name="errMsg">出错具体信息</param>
        /// <returns>0--正确  1--出错</returns>
        int GetPDLControllerByIdx(int nIdx, ref IPDLController pdlCtrl, ref string errMsg);

        /// <summary>
        /// 根据波长和光源类型进行查找
        /// </summary>
        /// <param name="index">设备Index，从1开始</param>
        /// <param name="desOptical">光源对象</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0--成功  1--出错</returns>
        int GetOpticalSourceByWaveAndType(int index, ref IOpticalSource desOptical, ref string errMsg);

        /// <summary>
        /// 根据标准获取要扫描的对象，
        /// </summary>
        /// <param name="flag">scan的按配置顺序的index，从1开始</param>
        /// <param name="desScan">获取到的扫描对象</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0--成功  1--出错</returns>
        int GetInterleaverScanByFlag(int flag, ref IInterleaverScan desScan, ref string errMsg);

        int GetAutomationInIndex(int index, ref IAutomation automation, ref string errMsg);

        int GetCDScanByIndex(int index, ref ICDScan cdScan, ref string errMsg);

        int GetFSTPScanByType(int nType, ref IFSTPScan fstpScan, ref string errMsg);

        int GetUDLFstpByGUID(int guid, ref IUDLFSTP fstpScan, ref string errMsg);

        int GetUDLSwitchByGUID(int guid, ref IUDLSwitch switchObj, ref string errMsg);

        int GetUDLTCCByGUID(int guid, ref IUDLTCC tccObj, ref string errMsg);
    }
}
