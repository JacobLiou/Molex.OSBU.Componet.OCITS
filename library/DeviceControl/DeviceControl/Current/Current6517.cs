using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MolexUtility.Device;
using MolexUtility.SerialControl;
using System.Threading;

///<summary>
///文件名：Cruuent6517
///作用：6517静电计类，继承于ICurrent接口，实现6517静电计的所有操作
///作者：马永华
///编写日期：2018-04-27
///修改记录
///R1：
///		修改作者：xxx
///		修改日期：2018-xx-xx
///		修改内容：xxx
///</summary>

namespace DeviceControl
{
    public class Current6517 : MolexUtility.Device.ICurrent
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="errMsg">错误信息</param>
        /// <param name="com">串口号，格式“COM1”</param>
        /// <param name="baudrate">波特率</param>
        public Current6517(ref string errMsg, string com, string baudrate)
        {
            Open(ref errMsg, com, baudrate);
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~Current6517()
        {
            if (baseSession != null)
            {
                baseSession = null;
            }
        }

        /// <summary>
        /// 串口
        /// </summary>
        private ISerial baseSession = null;

        /// <summary>
        /// 打开静电计
        /// </summary>
        /// <param name="errMsg">错误信息</param>
        /// <param name="com">串口号</param>
        /// <param name="baudrate">波特率</param>
        /// <returns>0-成功 1-出错</returns>
        private int Open(ref string errMsg, string com, string baudrate)
        {
            try
            {
                int baudrateInt = 0;
                Int32.TryParse(baudrate, out baudrateInt);
                baseSession = new SerialDotNet(com, baudrateInt, ref errMsg, 1000, false);
                if (errMsg.Contains("error:"))
                {
                    return 1;
                }
                if (InitCurrent(ref errMsg) != 0)
                {
                    return 1;
                }
                return 0;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return 1;
            }
        }

        /// <summary>
        /// 初始化静电计
        /// </summary>
        /// <param name="errMsg">错误信息</param>
        /// <returns>0-成功 1-出错</returns>
        private int InitCurrent(ref string errMsg)
        {
            try
            {
                baseSession.WriteSerialString(":syst:zch on\n", ref errMsg);
                Thread.Sleep(50);
 
                baseSession.WriteSerialString(":syst:zcor 1\n", ref errMsg);
                Thread.Sleep(50);

                baseSession.WriteSerialString(":syst:zch off\n", ref errMsg);
                Thread.Sleep(50);

                baseSession.WriteSerialString(":sour:volt:mcon on\n", ref errMsg);
                Thread.Sleep(50);
 
                baseSession.WriteSerialString(":sour:volt:lev:imm:ampl " + -5 + "\n", ref errMsg);
                Thread.Sleep(50);
    
                baseSession.WriteSerialString(":outp:stat on\n", ref errMsg);
                Thread.Sleep(50);

                baseSession.WriteSerialString(":func 'curr:dc'\n", ref errMsg);
                Thread.Sleep(50);

                baseSession.WriteSerialString(":curr:dc:rang " + Convert.ToString(2E-9) + "\n", ref errMsg);
                Thread.Sleep(50);

                return 0;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return 1;
            }
        }

        /// <summary>
        /// 设置量程
        /// </summary>
        /// <param name="range">对应量程，auto-自动，除auto外，该参数必须为double数值(eg:2E-9)</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>0-成功 1-出错 2-不支持该功能</returns>
        public int SetCurrentRange(string range, ref string errMsg)
        {
            try
            {
                 switch (range)
                {
                    case "auto":
                        baseSession.WriteSerialString(":CURR:DC:RANGE:AUTO ON\n", ref errMsg);
                        Thread.Sleep(50);
                        break;
                    default:
                        baseSession.WriteSerialString(":curr:dc:rang " + range + "\n", ref errMsg);
                        Thread.Sleep(50);
                        break;
                }                
                return 0;

            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return 1;
            }
        }

        /// <summary>
        /// 设置静电计偏压
        /// </summary>
        /// <param name="biasVoltage">需要设置的偏压值(V),未特别制定时为-5v</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>0-成功 1-出错 2-不支持该功能</returns>
        public int SetBiasVoltage(double biasVoltage, ref string errMsg)
        {
            try
            {
                baseSession.WriteSerialString(":sour:volt:lev:imm:ampl " + biasVoltage + "\n", ref errMsg);
                Thread.Sleep(50);
                return 0;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return 1;
            }
        }

        /// <summary>
        /// 读取当前电流值
        /// </summary>
        /// <param name="current">读取的电流值(nA)</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>0-成功 1-出错 2-不支持该功能</returns>
        public int ReadCurrent(ref double current, ref string errMsg)
        {
            try
            {
                string result = "";
                string strCurrent = "";

                baseSession.WriteSerialString("FETCH?\n", ref errMsg);
                Thread.Sleep(300);
                baseSession.ReadSerialString(out result, ref errMsg);
                strCurrent = result.Substring(0, result.IndexOf("NADC"));
                current = Convert.ToDouble(strCurrent) *1E9;
                return 0;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return 1;
            }

        }

        /// <summary>
        /// 读取当前电流值
        /// </summary>
        /// <param name="range">对应量程，auto-自动，除auto外，该参数必须为double数值(eg:2E-9)</param>
        /// <param name="biasVoltage">设置静电计偏压(v),未特别制定时为-5v</param>
        /// <param name="current">读取的电流值(nA)</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>0-成功 1-出错 2-不支持该功能</returns>
        public int GetCurrent(string range, double biasVoltage, ref double current, ref string errMsg)
        {
            try
            {
                if (SetBiasVoltage(biasVoltage, ref errMsg) != 0)
                {
                    return 1;
                }
                if (SetCurrentRange(range, ref errMsg) != 0)
                {
                    return 1;
                }
                if (ReadCurrent(ref current, ref errMsg) != 0)
                {
                    return 1;
                }
                return 0;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return 1;
            }

        }

    }
}
