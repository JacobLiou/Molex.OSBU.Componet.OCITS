using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

///<summary>
///文件名：IOpticalSource
///作用：光源接口，定义了外部切换波长、扫描等操作接口
///作者：阮锦芳
///编写日期：2018-01-22
///修改记录
///R1：
///		修改作者：作者中文名
///		修改日期：<模块创建日期，格式：YYYY-MM-DD>
///		修改内容：xxx
///</summary>

namespace MolexUtility.Device
{
    public interface IOpticalSource
    {
        /// <summary>
        /// 多台激光器扫描时开关对象
        /// </summary>
        /// <param name="tlsSwitch">开关对象</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>0-成功  1-出错</returns>
        //int SetMultiTLSSwitch(IOpticalSwitch tlsSwitch, ref string errMsg);

        /// <summary>
        /// 获取设备类型
        /// </summary>
        /// <returns>返回设备类型</returns>
        Devices GetDeviceType();

        /// <summary>
        /// 激光器扫描功能
        /// </summary>
        /// <param name="param">扫描相关参数</param>
        /// <param name="dataPath">扫描结果数据，放在文件中</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>0-成功 1-失败 2-不支持</returns>
        int DoScan(ScanParam param, out List<string> dataPath,ref string errMsg);

        /// <summary>
        /// 切换激光器波长
        /// </summary>
        /// <param name="wavelength">波长点</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>0-成功 1-失败 2-不支持</returns>
        int SetWavelength(double wavelength, ref string errMsg);

        /// <summary>
        /// 设置光输出功率
        /// </summary>
        /// <param name="power">光输出功率</param>
        /// <param name="iUnit">光功率单位0：dbm 1：watt</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>0-成功 1-出错 2-不支持</returns>
        int SetPower(double power, int iUnit,ref string errMsg);

        /// <summary>
        /// 设置光输出口，高功率或者低功率
        /// </summary>
        /// <param name="opticalOutput">光输出口 0-高功率口 1-低功率口</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>0-成功 1-出错 2-不支持</returns>
        int SetOpticalOutput(long opticalOutput, ref string errMsg);
    }
}
