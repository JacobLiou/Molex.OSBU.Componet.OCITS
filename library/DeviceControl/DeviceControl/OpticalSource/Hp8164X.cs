using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MolexUtility.Device;
using MolexUtility.SerialControl;
using System.Threading;
using System.IO.Ports;

namespace DeviceControl
{
    public class Hp8164X : MolexUtility.Device.IOpticalSource
    {

        /// <summary>
        /// 串口操作对象
        /// </summary>
        //private SerialPort serialSession = null;

        private ISerial niSerialSession = null;

        public Hp8164X(ref string errMsg, string com, string baudrate)
        {
            Open(ref errMsg, com, baudrate);
        }

        /// <summary>
        /// 打开功率计操作
        /// </summary>
        /// <param name="errMsg">错误信息</param>
        /// <param name="com">串口号</param>
        /// <param name="baudrate">波特率</param>
        ///<param name="timeout">延时</param>
        ///<param name="isStartRead">是否实时读取功率值</param>
        /// <returns>0-成功 1-出错</returns>
        private int Open(ref string errMsg, string com, string baudrate)
        {
            try
            {
                int baudrateInt = 0;
                Int32.TryParse(baudrate, out baudrateInt);
                /*serialSession = new SerialPort();
                serialSession.PortName = com;
                serialSession.BaudRate = baudrateInt;
                serialSession.StopBits = StopBits.One;
                serialSession.DataBits = 8;
                serialSession.Parity = Parity.None;
                serialSession.ReadTimeout = 1000;
                serialSession.DtrEnable = true;
                serialSession.RtsEnable = true;
                if (serialSession.IsOpen)
                {
                    serialSession.Close();
                }
                serialSession.Open();*/
                niSerialSession = new SerialNI(com, baudrateInt, ref errMsg,3000);
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
        /// 获取设备类型
        /// </summary>
        /// <returns>返回设备类型</returns>
        public Devices GetDeviceType()
        {
            return Devices.Opitical8164;
        }

        /// <summary>
        /// 激光器扫描功能
        /// </summary>
        /// <param name="param">扫描相关参数</param>
        /// <param name="dataPath">扫描结果数据，放在文件中</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>0-成功 1-失败 2-不支持</returns>
        public int DoScan(ScanParam param, out List<string> dataPath, ref string errMsg)
        {
            dataPath = new List<string>();
            return 2;
        }
        /// <summary>
        /// 切换激光器波长
        /// </summary>
        /// <param name="wavelength">波长点</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>0-成功 1-失败 2-不支持</returns>
        public int SetWavelength(double wavelength, ref string errMsg)
        {
            try
            {
                string sendData = string.Format("sour0:wav {0}nm\n", wavelength);
                if(niSerialSession.WriteSerialString(sendData, ref errMsg)!=0)
                {
                    Thread.Sleep(50);
                    niSerialSession.WriteSerialString(sendData, ref errMsg);
                }
                //serialSession.Write(sendData.ToArray(),0,sendData.Length);
                int nCount = 0;
                while (true)
                {
                    if(nCount==5)
                    {
                        if (niSerialSession.WriteSerialString(sendData, ref errMsg) != 0)
                        {
                            Thread.Sleep(50);
                            niSerialSession.WriteSerialString(sendData, ref errMsg);
                        }
                    }
                    Thread.Sleep(50);
                    //serialSession.DiscardInBuffer();
                    //serialSession.DiscardOutBuffer();
                    sendData = string.Format("sour0:wav?\n");
                    niSerialSession.WriteSerialString(sendData, ref errMsg);
                    //serialSession.Write(sendData.ToArray(), 0, sendData.Length);
                    Thread.Sleep(100);
                    string readData = "";
                    while (true)
                    {
                        string niRead = "";
                        niSerialSession.ReadSerialString(out niRead, ref errMsg);
                        readData += niRead;
                        if (readData.Contains("<END>"))
                            break;
                        /*if(serialSession.BytesToRead>0)
                        {
                            readData += serialSession.ReadExisting();
                            if(readData.Contains("<END>"))
                                break;
                        }*/
                    }
                    
                    readData = readData.Replace("<END>", "");
                    readData= readData.Replace("\n", "");
                    double dReadWL = 0;
                    if (readData.Contains("nm"))
                    {
                        readData= readData.Replace("nm", "");
                        dReadWL = Convert.ToDouble(readData);
                    }
                    else
                        dReadWL = Convert.ToDouble(readData)*1e9;
                    if (readData.Length > 0 && wavelength.CompareTo(dReadWL) == 0)
                        return 0;
                    else if (readData.Length > 0)
                    {
                        errMsg = "切换波长失败:"+ readData;
                        return 1;
                    }

                    nCount++;
                    if (nCount > 20)
                    {
                        errMsg = "无法读取激光器数据！";
                        return 1;
                    }
                }
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return 1;
            }
        }

        /// <summary>
        /// 设置光输出功率
        /// </summary>
        /// <param name="power">光输出功率</param>
        /// <param name="iUnit">光功率单位0：dbm 1：watt</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>0-成功 1-出错 2-不支持</returns>
        public int SetPower(double power, int iUnit, ref string errMsg)
        {
            return 2;
        }

        /// <summary>
        /// 设置光输出口，高功率或者低功率
        /// </summary>
        /// <param name="opticalOutput">光输出口 0-高功率口 1-低功率口</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>0-成功 1-出错 2-不支持</returns>
        public int SetOpticalOutput(long opticalOutput, ref string errMsg)
        {
            return 2;
        }
    }
}
