using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

///<summary>
///文件名：ICurrent
///作用：静电计、万用表接口类，定义了外部读取电流的接口
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
    public interface ICurrent
    {
        /// <summary>
        /// 设置量程
        /// </summary>
        /// <param name="range">对应量程，auto-自动，除auto外，该参数必须为double数值(eg:2E-9)</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>0-成功 1-出错 2-不支持该功能</returns>
        int SetCurrentRange(string range, ref string errMsg);

        /// <summary>
        /// 设置静电计偏压
        /// </summary>
        /// <param name="biasVoltage">需要设置的偏压值(V),未特别制定时为-5v</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>0-成功 1-出错 2-不支持该功能</returns>
        int SetBiasVoltage(double biasVoltage, ref string errMsg);

        /// <summary>
        /// 读取当前电流值
        /// </summary>
        /// <param name="current">读取的电流值(nA)</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns></returns>
        int ReadCurrent(ref double current, ref string errMsg);

        /// <summary>
        /// 读取当前电流值
        /// </summary>
        /// <param name="range">对应量程，auto-自动，除auto外，该参数必须为double数值(eg:2E-9)</param>
        /// <param name="biasVoltage">设置静电计偏压(v),未特别制定时为-5v</param>
        /// <param name="current">读取的电流值(nA)</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>0-成功 1-出错 2-不支持该功能</returns>
        int GetCurrent(string range, double biasVoltage, ref double current, ref string errMsg);
    }
}
