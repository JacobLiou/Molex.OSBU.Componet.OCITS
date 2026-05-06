using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MolexUtility.Device;
using MolexUtility.SerialControl;
using System.Threading;

///<summary>
///文件名：PowermeterOplink1830
///作用：自制1830功率计类，继承于IPowermeter接口，实现自制1830的所有操作
///作者：高鹏娟
///编写日期：2018-11-27
///修改记录
///R1：
///		修改作者：
///		修改日期：
///		修改内容：
///</summary>
namespace DeviceControl
{
    public class PowermeterOplink1830 : IPowermeter
    {
        /// <summary>
        /// 功率计总共有多少个通道
        /// </summary>
        public int ChannelCount { get; set; }

        /// <summary>
        /// 串口
        /// </summary>
        private ISerial baseSession = null;

        /// <summary>
        /// 用来做互斥的锁对象
        /// </summary>
        private object lockObj = new object();
        private object lockObj1 = new object();

        /// <summary>
        /// 结束读取多个功率值的标志
        /// </summary>
        private bool isEndReadMltiPowers = false;
        private bool isReading = false;
        /// <summary>
        /// 存储功率计值
        /// </summary>
        private List<double>[] tempArr;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="errMsg">错误信息</param>
        /// <param name="com">串口号，格式“COM1”</param>
        /// <param name="baudrate">波特率</param>
        public PowermeterOplink1830(ref string errMsg, string com, string baudrate)
        {
            ChannelCount = 1;
            Open(ref errMsg, com, baudrate);
        }

        /// <summary>
        /// 打开功率计操作
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
                baseSession = new SerialDotNet(com, baudrateInt, ref errMsg, 1000, true);
                //baseSession = new SerialNI(com, baudrateInt, ref errMsg, 1000, true);
                baseSession.ThreadReadEvent += BaseSession_ThreadReadEvent;
                ResetPowermeter(ref errMsg);
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
        /// 设置功率计所有通道中心波长
        /// </summary>
        /// <param name="centerWL">需要设置的中心波长</param>
        /// <returns>0-成功 1-出错 2-失败</returns>
        public int SetPMWavelength(double centerWL, ref string errMsg)
        {
            lock (lockObj)
            {
                try
                {
                    if (isReading)
                        return 2;
                    isReading = true;
                    centerWL = (int)(centerWL + 0.5);//四舍五入
                    baseSession.WriteSerialString("W" + centerWL + "\n", ref errMsg);
                    Thread.Sleep(50);
                    baseSession.SetEndThreadRead();
                    baseSession.WriteSerialString("W?\n", ref errMsg);
                    Thread.Sleep(50);
                    string result = "";
                    baseSession.ReadSerialString(out result, ref errMsg);
                    baseSession.StartThreadRead();
                    if (result.Contains(centerWL.ToString()))
                    {
                        isReading = false;
                        return 0;
                    }
                    else
                    {
                        isReading = false;
                        return 2;
                    }
                }
                catch (Exception ex)
                {
                    errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                        + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                    isReading = false;
                    return 1;
                }
            }
        }

        /// <summary>
        /// 设置光功率计单位
        /// </summary>
        /// <param name="errMsg">错误信息</param>
        /// <param name="units">Watts，dB，dBm，REF</param>
        /// <returns>0-成功 1-出错 2-失败</returns>
        public int SetPMUnits(ref string errMsg, string units)
        {
            lock (lockObj)
            {
                try
                {
                    if (isReading)
                        return 2;
                    isReading = true;
                    units = units.ToUpper();
                    switch (units)
                    {
                        case "WATTS":
                            baseSession.WriteSerialString("U1\n", ref errMsg);  //Watts
                            break;
                        case "DB":
                            baseSession.WriteSerialString("U2\n", ref errMsg);  //dB
                            break;
                        case "DBM":
                            baseSession.WriteSerialString("U3\n", ref errMsg); //dBm
                            break;
                        case "REF":
                            baseSession.WriteSerialString("U4\n", ref errMsg);  //REF
                            break;
                        default:
                            break;
                    }
                    Thread.Sleep(50);
                    baseSession.SetEndThreadRead();
                    baseSession.WriteSerialString("U?\n", ref errMsg);
                    Thread.Sleep(50);
                    string result = "";
                    baseSession.ReadSerialString(out result, ref errMsg);
                    baseSession.StartThreadRead();
                    result = result.Substring(0, 1);
                    switch (result)
                    {
                        case "1":
                            result = "WATTS";
                            break;
                        case "2":
                            result = "DB";
                            break;
                        case "3":
                            result = "DBM";
                            break;
                        case "4":
                            result = "REF";
                            break;
                        default:
                            break;
                    }
                    if (result == units)
                    {
                        isReading = false;
                        return 0;
                    }
                    else
                    {
                        isReading = false;
                        return 2;
                    }
                }
                catch (Exception ex)
                {
                    errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                        + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                    isReading = false;
                    return 1;
                }
            }
        }

        /// <summary>
        /// 是否已经清理
        /// </summary>
        /// <param name="errMsg">错误信息</param>
        /// <returns>0-已清零 1-出错 2-未清零</returns>
        public int GetZeroControl(ref string errMsg)
        {
            lock (lockObj)
            {
                try
                {
                    if (isReading)
                        return 2;
                    isReading = true;
                    baseSession.SetEndThreadRead();
                    baseSession.WriteSerialString("Z?\n", ref errMsg);
                    Thread.Sleep(50);
                    string result = "";
                    baseSession.ReadSerialString(out result, ref errMsg);
                    baseSession.StartThreadRead();
                    if (result == "0\n")
                    {
                        isReading = false;
                        return 0;
                    }
                    else
                    {
                        isReading = false;
                        return 2;
                    }
                }
                catch (Exception ex)
                {
                    errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                        + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                    isReading = false;
                    return 1;
                }
            }
        }

        /// <summary>
        /// 读取功率平均值
        /// </summary>
        /// <param name="errMsg">出错信息</param>
        /// <param name="powerArray">读取到的功率值</param>
        /// <param name="avgSample">采样多少个点取平均值</param>
        /// <param name="isGetAllChannel">一个通道或者所有通道</param>
        /// <param name="specialChannel">如果isGetAllChannel为false，有效，指定要读取功率的通道</param>
        /// <returns>0-成功 1-出错 2-数据错误</returns>
        public int ReadPowerAvg(ref string errMsg, out List<double> powerArray, int avgSample = 1, bool isGetAllChannel = false, string specialChannel = "0")
        {
            lock (lockObj)
            {
                if (isGetAllChannel)
                {
                    powerArray = new List<double>(ChannelCount);
                }
                else
                {
                    powerArray = new List<double>(1);
                }
                try
                {
                    if (isReading)
                        return 2;
                    isReading = true;
                    double dAvg = 0.0;
                    int Number = 0; //实际采样记数
                    double dPower = MolexUtility.CommonFunction.GetDefaultValue();
                    int nReadErrorNum = 0;
                    double dPowerOld = MolexUtility.CommonFunction.GetDefaultValue();

                    for (int index = 0; index < (avgSample + 10); index++)
                    {
                        baseSession.SetEndThreadRead();
                        baseSession.WriteSerialString("D?\n", ref errMsg);
                        Thread.Sleep(100);
                        string result = "";
                        int err = 0;
                        err = baseSession.ReadSerialString(out result, ref errMsg);
                        baseSession.StartThreadRead();
                        if (err == 0)
                        {
                            nReadErrorNum = 0;
                            //if (result.Contains('\n')&&result.Substring ((result.ToLower().IndexOf ('e'))).Length ==5)
                            if (result.Contains('\n') && result.Substring((result.ToLower().IndexOf('e'))).Length == 5)
                            {
                                result = result.Replace('\n', ' ');
                                dPower = Convert.ToDouble(result);
                            }
                        }
                        else
                        {
                            nReadErrorNum++;
                            if (nReadErrorNum > 3)//连续错误3次
                            {
                                isReading = false;
                                return 2;
                            }
                        }

                        if (dPowerOld < 1000.0 && dPower < (dPowerOld - 6.0))
                        {//突变6dB
                            Thread.Sleep(550);  //等待稳定                    
                            dPowerOld = dPower;
                            continue;
                        }

                        dPowerOld = dPower;

                        if (avgSample <= 0)    //单次采样
                        {
                            powerArray.Add(dPowerOld);
                            isReading = false;
                            return 0;
                        }

                        dAvg += dPowerOld;
                        Number++;

                        if (Number >= avgSample)
                        {
                            powerArray.Add(Math.Round((dAvg / Number), 3));
                            isReading = false;
                            return 0;
                        }

                        if (index > (Number + 9))   //错误超过9次
                        {
                            errMsg = "错误超过9次!";
                            isReading = false;
                            return 2;
                        }
                    }

                    if (Number > 0)
                    {
                        powerArray.Add(Math.Round((dAvg / Number), 3));
                        isReading = false;
                        return 0;
                    }
                    else
                    {
                        isReading = false;
                        return 2;
                    }
                }
                catch (Exception ex)
                {
                    errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                        + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                    isReading = false;
                    return 1;
                }
            }
        }

        /// <summary>
        /// 读取多个功率值
        /// </summary>
        /// <param name="errMsg">出错信息</param>
        /// <param name="powerArray">存放功率值数据</param>
        /// <param name="timeInternal">两次读取数值间间隔</param>
        /// <param name="totalCount">每个通道读取功率点数</param>
        /// <param name="isGetAllChannel">一个通道或者所有通道</param>
        /// <param name="specialChannel">如果isGetAllChannel为false，有效，指定要读取功率的通道</param>
        /// <returns>0-成功 1-出错 2-数据错误</returns>
        public int GetMultiPowers(ref string errMsg, out List<List<double>> powerArray, int timeInternal, int totalCount, bool isGetAllChannel = false, string specialChannel = "0")
        {
            lock (lockObj1)
            {
                if (isGetAllChannel)
                {
                    powerArray = new List<List<double>>(ChannelCount);
                    try
                    {
                        if (totalCount < 100)
                            totalCount = 100;
                        else if (totalCount > 1300)
                            totalCount = 1300;
                        errMsg = "";
                        List<double> powerNew = new List<double>();
                        List<double>[] powers = new List<double>[ChannelCount];
                        for (int j = 0; j < ChannelCount; j++)
                            powers[j] = new List<double>();
                        //int errCount = 0;
                        for (int index = 0; index < totalCount; index++)
                        {
                            int result = -1;
                            result = ReadPowerAvg(ref errMsg, out powerNew, isGetAllChannel: isGetAllChannel, specialChannel: specialChannel);
                            for (int j = 0; j < ChannelCount; j++)
                                powers[j].Add(powerNew[j]);
                            //if (result != 0)
                            //{
                            //    errCount++;
                            //    totalCount++;
                            //    if (errCount > 10)
                            //    {
                            //        errMsg = "读功率计出错！";
                            //        return 2;
                            //    }
                            //    continue;
                            //}
                            Thread.Sleep(timeInternal);
                        }
                        for (int k = 0; k < ChannelCount; k++)
                            powerArray.Add(powers[k]);
                        return 0;
                    }
                    catch (Exception ex)
                    {
                        errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                            + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                        return 1;
                    }
                }
                else
                {
                    powerArray = new List<List<double>>(1);
                    try
                    {
                        if (totalCount < 100)
                            totalCount = 100;
                        else if (totalCount > 1300)
                            totalCount = 1300;
                        errMsg = "";
                        List<double> powerNew = new List<double>();
                        List<double> powers = new List<double>();
                        //int errCount = 0;
                        for (int index = 0; index < totalCount; index++)
                        {
                            int result = -1;
                            result = ReadPowerAvg(ref errMsg, out powerNew, isGetAllChannel: isGetAllChannel, specialChannel: specialChannel);
                            if (powerNew.Count > 0)
                                powers.Add(powerNew[0]);
                            //if (result != 0)
                            //{
                            //    errCount++;
                            //    totalCount++;
                            //    if (errCount > 10)
                            //    {
                            //        errMsg = "读功率计出错！";
                            //        return 2;
                            //    }
                            //    continue;
                            //}
                            Thread.Sleep(timeInternal);
                        }
                        powerArray.Add(powers);
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

        /// <summary>
        /// 光功率计复位
        /// </summary>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0-复位成功 1-出错 2-复位失败</returns>
        public int ResetPowermeter(ref string errMsg)
        {
            lock (lockObj)
            {
                try
                {
                    if (isReading)
                        return 2;
                    isReading = true;
                    baseSession.SetEndThreadRead();
                    //1、Average of the measurements,same as Filter (F1:16点, F2:4点, F3:1点)
                    baseSession.WriteSerialString("F2\n", ref errMsg);//medunm 
                    Thread.Sleep(50);
                    baseSession.WriteSerialString("F?\n", ref errMsg);
                    Thread.Sleep(50);
                    string filter = "";
                    baseSession.ReadSerialString(out filter, ref errMsg);
                    Thread.Sleep(50);
                    //2、Units(U1:Watts, U2:dB, U3:dBm, U4:REF)
                    baseSession.WriteSerialString("U2\n", ref errMsg);//dB 
                    baseSession.WriteSerialString("U?\n", ref errMsg);
                    Thread.Sleep(50);
                    string units = "";
                    baseSession.ReadSerialString(out units, ref errMsg);
                    Thread.Sleep(50);
                    //4、Set Range of the input signal (R0,R1,...R8)
                    baseSession.WriteSerialString("R0\n", ref errMsg);//Auto
                    baseSession.WriteSerialString("R?\n", ref errMsg);
                    Thread.Sleep(50);
                    string range = "";
                    baseSession.ReadSerialString(out range, ref errMsg);
                    baseSession.StartThreadRead();
                    if (filter == "2\n" && units == "2\n")
                    {
                        isReading = false;
                        return 0;
                    }
                    else
                    {
                        isReading = false;
                        return 2;
                    }
                }
                catch (Exception ex)
                {
                    errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                        + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                    isReading = false;
                    return 1;
                }
            }
        }

        /// <summary>
        /// 开始读取多个功率值
        /// </summary>
        /// <param name="errMsg">出错信息</param>
        /// <param name="timeInternal">两次读取数值间间隔</param>
        /// <param name="isGetAllChannel">一个通道或者所有通道</param>
        /// <param name="specialChannel">如果isGetAllChannel为false，有效，指定要读取功率的通道</param>
        /// <returns>0-成功 1-出错</returns>
        public int BeginReadMltiPowers(ref string errMsg, int timeInternal, bool isGetAllChannel = false, string specialChannel = "0")
        {
            lock (lockObj1)
            {
                if (isGetAllChannel)
                {
                    tempArr = new List<double>[ChannelCount];
                    for (int i = 0; i < ChannelCount; i++)
                        tempArr[i] = new List<double>();
                    try
                    {
                        isEndReadMltiPowers = false;
                        List<double> powerNew = new List<double>();
                        while (true)
                        {
                            if (isEndReadMltiPowers)
                            {
                                break;
                            }
                            int result = -1;
                            result = ReadPowerAvg(ref errMsg, out powerNew, isGetAllChannel: isGetAllChannel, specialChannel: specialChannel);
                            if (result != 0)
                            {
                                continue;
                            }
                            for (int j = 0; j < ChannelCount; j++)
                                tempArr[j].Add(powerNew[j]);
                            Thread.Sleep(timeInternal);
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
                else
                {
                    tempArr = new List<double>[1];
                    try
                    {
                        isEndReadMltiPowers = false;
                        List<double> powerNew = new List<double>();
                        while (true)
                        {
                            if (isEndReadMltiPowers)
                                break;
                            int result = -1;
                            result = ReadPowerAvg(ref errMsg, out powerNew, isGetAllChannel: isGetAllChannel, specialChannel: specialChannel);
                            if (result != 0)
                                continue;
                            tempArr[0].Add(powerNew[0]);
                            Thread.Sleep(timeInternal);
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

        /// <summary>
        /// 结束读取多个功率值
        /// </summary>
        /// <param name="errMsg">出错信息</param>
        /// <param name="powerArray">存放功率值数据</param>
        /// <param name="isGetAllChannel">一个通道或者所有通道，如果为所有通道，则停止所有通道数据，并返回结果</param>
        /// <param name="specialChannel">如果isGetAllChannel为false，有效，停止读取该通道功率，并返回所有读取结果</param>
        /// <returns>0-成功 1-出错</returns>
        public int EndReadMultiPowers(ref string errMsg, out List<List<double>> powerArray, bool isGetAllChannel = false, string specialChannel = "0")
        {
            object obj = new object();
            lock (obj)
            {
                if (isGetAllChannel)
                {
                    powerArray = new List<List<double>>(ChannelCount);
                    try
                    {
                        lock (lockObj)
                        {
                            isEndReadMltiPowers = true;
                        }
                        for (int j = 0; j < ChannelCount; j++)
                            powerArray.Add(tempArr[j]);
                        return 0;
                    }
                    catch (Exception ex)
                    {
                        errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                            + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                        return 1;
                    }
                }
                else
                {
                    powerArray = new List<List<double>>(1);
                    try
                    {
                        lock (lockObj)
                        {
                            isEndReadMltiPowers = true;
                        }
                        powerArray.Add(tempArr[0]);
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

        /// <summary>
        /// 关闭功率计
        /// </summary>
        public void PowermeterClose()
        {
            if (baseSession != null)
            {
                baseSession.SetEndThreadRead();
                baseSession = null;
            }
        }

        /// <summary>
        /// 实时读取数据事件
        /// </summary>
        /// <param name="readStr"></param>
        /// <param name="errMsg"></param>
        private void BaseSession_ThreadReadEvent(string readStr, string errMsg)
        {
            //if (ReadDataEvent != null)
            //    ReadDataEvent(readStr, errMsg);
        }
    }
}
