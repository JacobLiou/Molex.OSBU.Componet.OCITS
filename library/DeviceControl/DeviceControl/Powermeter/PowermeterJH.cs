using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MolexUtility.Device;
using MolexUtility.SerialControl;
using System.Threading;
using System.IO.Ports;

///<summary>
///文件名：ICurrent
///作用：嘉惠功率计类，继承于IPowermeter接口，实现嘉惠的所有操作
///作者：阮锦芳
///编写日期：2018-01-24
///修改记录
///R1：
///		修改作者：高鹏娟
///		修改日期：2018-04-10
///		修改内容：实现接口
///</summary>

namespace DeviceControl
{
    public class PowermeterJH : MolexUtility.Device.IPowermeter
    {
        /// <summary>
        /// 功率计总共有多少个通道
        /// </summary>
        public int ChannelCount { get; set; }

        /// <summary>
        /// 串口操作对象
        /// </summary>
        private SerialPort serialSession = null;

        /// <summary>
        /// 串口
        /// </summary>
        private ISerial baseSession = null;

        /// <summary>
        /// 用来做互斥的锁对象
        /// </summary>
        private object lockObj = new object();
        private object lockObj1 = new object();
        private bool isReading = false;

        /// <summary>
        /// 存储功率计值
        /// </summary>
        private List<double>[] tempArr;

        /// <summary>
        /// 结束读取多个功率值的标志
        /// </summary>
        bool isEndReadMltiPowers = false;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="errMsg">错误信息</param>
        /// <param name="com">串口号，格式“ASRL1::INSTR”/“COM1”</param>
        /// <param name="baudrate">波特率</param>
        public PowermeterJH(ref string errMsg, string com, string baudrate)
        {
            ChannelCount = 2;
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
                serialSession = new SerialPort();
                serialSession.PortName = com;
                serialSession.BaudRate = baudrateInt;
                serialSession.StopBits = StopBits.One;
                serialSession.DataBits = 8;
                serialSession.Parity = Parity.None;
                serialSession.ReadTimeout = 1000;
                if (serialSession.IsOpen)
                {
                    serialSession.Close();
                }
                serialSession.Open();
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
                    int res = -1;
                    int res2 = -1;
                    res = SetWavelength(centerWL, 7, ref errMsg);
                    res2 = SetWavelength(centerWL, 8, ref errMsg);
                    isReading = false;
                    if (res == 0 && res2 == 0)
                        return 0;
                    else
                        return 2;
                }
                catch (Exception ex)
                {
                    errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                        + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                    return 1;
                }
            }
        }

        /// <summary>
        /// 设置光功率计单位
        /// </summary>
        /// <param name="errMsg">错误信息</param>
        /// <param name="units">dBm，Watts，dB，REF</param>
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
                    byte unitIndex = 0;
                    units = units.ToUpper();
                    switch (units)
                    {
                        case "DBM":
                            unitIndex = 0;
                            break;
                        case "WATTS":
                            unitIndex = 1;
                            break;
                        case "DB":
                            unitIndex = 2;
                            break;
                        case "REF":
                            unitIndex = 3;
                            break;
                        default:
                            unitIndex = 2;
                            break;
                    }
                    int res = -1;
                    int res2 = -1;
                    res = SetUnits(ref errMsg, unitIndex, 9);
                    res2 = SetUnits(ref errMsg, unitIndex, 10);
                    isReading = false;
                    if (res == 0 && res2 == 0)
                        return 0;
                    return 2;
                }
                catch (Exception ex)
                {
                    errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                        + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                    return 1;
                }
            }
        }

        /// <summary>
        /// 是否已经清理
        /// </summary>
        /// <param name="errMsg">错误信息</param>
        /// <returns>0-已清理 1-出错 2-不支持该功能</returns>
        public int GetZeroControl(ref string errMsg)
        {
            try
            {
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
        /// 光功率计复位
        /// </summary>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0-复位成功 1-出错 2-复位失败</returns>
        public int ResetPowermeter(ref string errMsg)
        {
            try
            {
                return SetPMUnits(ref errMsg, "dB");
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return 1;
            }
        }

        int oldTickCount = System.Environment.TickCount;

        string oldSpecialChannel = "";

        List<double> oldPowerArray = new List<double>();

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
                    if (isReading)
                        return 2;
                    isReading = true;

                    try
                    {
                        List<double> dAvg = new List<double>(ChannelCount);
                        int Number = 0; //实际采样记数
                        List<double> dPower = new List<double>(ChannelCount);
                        int nReadErrorNum = 0;
                        List<double> dPowerOld = new List<double>(ChannelCount);
                        for (int i = 0; i < ChannelCount; i++)
                        {
                            dPowerOld.Add(MolexUtility.CommonFunction.GetDefaultValue());
                            dAvg.Add(0);
                            powerArray.Add(MolexUtility.CommonFunction.GetDefaultValue());
                        }

                        for (int index = 0; index < (avgSample + 10); index++)
                        {
                            if (GetPMPower(ref dPower, ref errMsg) == 0)
                            {
                                nReadErrorNum = 0;
                            }
                            else
                            {
                                nReadErrorNum++;
                                if (nReadErrorNum > 3)
                                {
                                    isReading = false;
                                    return 2;
                                }
                                continue;
                            }
                            if ((dPowerOld[0] < 1000.0 && dPower[0] < (dPowerOld[0] - 6.0)) || (dPowerOld[1] < 1000.0 && dPower[1] < (dPowerOld[1] - 6.0)))
                            {//突变6dB
                                Thread.Sleep(550);  //等待稳定                    
                                dPowerOld = dPower;
                                continue;
                            }

                            dPowerOld = dPower;

                            if (avgSample <= 0)    //单次采样
                            {
                                powerArray = dPowerOld;
                                isReading = false;
                                return 0;
                            }

                            for (int i = 0; i < ChannelCount; i++)
                                dAvg[i] += dPowerOld[i];////////////
                            Number++;

                            if (Number >= avgSample)
                            {
                                for (int i = 0; i < ChannelCount; i++)
                                    powerArray[i] = Math.Round((dAvg[i] / Number), 3);
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

                        isReading = false;
                        if (Number > 0)
                        {
                            for (int i = 0; i < ChannelCount; i++)
                                powerArray[i] = Math.Round((dAvg[i] / Number), 3);
                            return 0;
                        }
                        else
                            return 2;
                    }
                    catch (Exception ex)
                    {
                        errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                            + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                        isReading = false;
                        return 1;
                    }
                }
                else
                {
                    powerArray = new List<double>(1);
                    if (isReading)
                        return 2;
                    isReading = true;
                    try
                    {
                        int scIndex = 0;
                        scIndex = Convert.ToInt32(specialChannel);
                        if ((System.Environment.TickCount - oldTickCount) < 200)
                        {
                            if (oldSpecialChannel == "0" && specialChannel == "1")
                            {
                                powerArray.Add(oldPowerArray[scIndex]);
                                isReading = false;
                                return 0;
                            }
                        }

                        List<double> dAvg = new List<double>();
                        int Number = 0; //实际采样记数
                        List<double> dPower = new List<double>();
                        int nReadErrorNum = 0;
                        List<double> dPowerOld = new List<double>();
                        for (int i = 0; i < ChannelCount; i++)
                        {
                            //dPower.Add(MolexUtility.CommonFunction.GetDefaultValue());
                            //dPowerOld.Add(MolexUtility.CommonFunction.GetDefaultValue());
                            dAvg.Add(0);
                        }

                        for (int index = 0; index < (avgSample + 10); index++)
                        {
                            dPower.Clear();
                            if (GetPMPower(ref dPower, ref errMsg) == 0)
                            {
                                nReadErrorNum = 0;
                            }
                            else
                            {
                                nReadErrorNum++;
                                if (nReadErrorNum > 3)
                                {
                                    isReading = false;
                                    return 2;
                                }
                                continue;
                            }

                            if(dPowerOld.Count> 0 && dPowerOld[scIndex] < 1000.0 && dPower[scIndex] < (dPowerOld[scIndex] - 6.0))
                            {//突变6dB
                                Thread.Sleep(550);  //等待稳定
                                dPowerOld.Clear();
                                foreach (double value in dPower)
                                {
                                    dPowerOld.Add(value);
                                }
                                continue;
                            }

                            dPowerOld.Clear();
                            foreach (double value in dPower)
                            {
                                dPowerOld.Add(value);
                            }

                            if (avgSample <= 0)    //单次采样
                            {
                                powerArray.Add(dPowerOld[scIndex]);
                                oldPowerArray = dPowerOld;
                                isReading = false;
                                return 0;
                            }

                            for (int i = 0; i < ChannelCount; i++)
                                dAvg[i] += dPowerOld[i];
                            Number++;

                            if (Number >= avgSample)
                            {
                                powerArray.Add(Math.Round((dAvg[scIndex] / Number), 3));
                                oldPowerArray.Clear();
                                for (int i = 0; i < ChannelCount; i++)
                                    oldPowerArray.Add(Math.Round((dAvg[i] / Number), 3));
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
                            powerArray.Clear();
                            powerArray.Add(Math.Round((dAvg[scIndex] / Number), 3));
                            oldPowerArray.Clear();
                            for (int i = 0; i < ChannelCount; i++)
                                oldPowerArray.Add(Math.Round((dAvg[i] / Number), 3));
                            isReading = false;
                            return 0;
                        }
                        else
                            return 2;
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
                        List<double> powerNew = new List<double>();
                        List<double>[] powers = new List<double>[ChannelCount];
                        for (int j = 0; j < ChannelCount; j++)
                            powers[j] = new List<double>();
                        if (totalCount < 100)
                            totalCount = 100;
                        else if (totalCount > 1300)
                            totalCount = 1300;
                        int nerrCount = 0;
                        for (int index = 0; index < totalCount; index++)
                        {
                            int result = -1;
                            result = ReadPowerAvg(ref errMsg, out powerNew, isGetAllChannel: isGetAllChannel, specialChannel: specialChannel);
                            for (int j = 0; j < ChannelCount; j++)
                                powers[j].Add(powerNew[j]);
                            if (result != 0)
                            {
                                nerrCount++;
                                totalCount++;
                                if (nerrCount > 10)
                                {
                                    errMsg = "读功率计出错！";
                                    return 2;
                                }
                                continue;
                            }
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
                        int scIndex = 0;
                        scIndex = Convert.ToInt32(specialChannel);
                        List<double> powerNew = new List<double>();
                        List<double> powers = new List<double>();
                        if (totalCount < 100)
                            totalCount = 100;
                        else if (totalCount > 1300)
                            totalCount = 1300;
                        int nerrCount = 0;
                        for (int index = 0; index < totalCount; index++)
                        {
                            int result = -1;
                            result = ReadPowerAvg(ref errMsg, out powerNew, isGetAllChannel: isGetAllChannel, specialChannel: specialChannel);
                            powers.Add(powerNew[scIndex]);
                            if (result != 0)
                            {
                                nerrCount++;
                                totalCount++;
                                if (nerrCount > 10)
                                {
                                    errMsg = "读功率计出错！";
                                    return 2;
                                }
                                continue;
                            }
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
            lock (obj) {
                if (isGetAllChannel)
                {
                    powerArray = new List<List<double>>(ChannelCount);

                    try
                    {
                        if (isReading)
                            return 2;
                        isReading = true;
                        lock (lockObj)
                        {
                            isEndReadMltiPowers = true;
                        }
                        for (int j = 0; j < ChannelCount; j++)
                            powerArray.Add(tempArr[j]);
                        isReading = false;
                        return 0;
                    }
                    catch (Exception ex)
                    {
                        errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                            + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                        isReading = false;
                        return 1;
                    }
                }
                else
                {
                    powerArray = new List<List<double>>(1);

                    try
                    {
                        if (isReading)
                            return 2;
                        isReading = true;
                        lock (lockObj)
                        {
                            isEndReadMltiPowers = true;
                        }
                        powerArray.Add(tempArr[0]);
                        isReading = false;
                        return 0;
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

        /// <summary>
        /// 设置波长
        /// </summary>
        /// <param name="centerWL">中心波长</param>
        /// <param name="channelIndex">通道下标</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0-成功 1-出错 2-失败</returns>
        private int SetWavelength(double centerWL,byte channelIndex, ref string errMsg)
        {
            try
            {
                centerWL = (int)(centerWL + 0.5);//四舍五入
                byte[] sendBuf = new byte[7];

                sendBuf[0] = 0xaa;
                sendBuf[1] = 0xbb;
                sendBuf[2] = 0xcc;
                sendBuf[3] = channelIndex;
                sendBuf[4] = (byte)(centerWL / 256);
                sendBuf[5] = (byte)(centerWL % 256);
                sendBuf[6] = GetCheckXor(sendBuf, 1, 5);

                serialSession.Write(sendBuf, 0, sendBuf.Length);
                Thread.Sleep(50);
                int nreadTime = 0;
                int nReWriteTime = 0;
                while (serialSession.BytesToRead != 9)
                {
                    Thread.Sleep(5);
                    nreadTime++;
                    if (nreadTime > 20)
                    {
                        nreadTime = 0;
                        nReWriteTime++;
                        serialSession.DiscardInBuffer();
                        serialSession.DiscardOutBuffer();
                        serialSession.Write(sendBuf, 0, sendBuf.Length);
                        Thread.Sleep(20);
                    }
                    if (nReWriteTime > 5)
                    {
                        return 2;
                    }
                }

                byte[] resultBuf = new byte[9];
                for (int i = 0; i < 9; i++)
                {
                    serialSession.Read(resultBuf, i, 1);
                }
                if (resultBuf[0] == 0x55 && GetCheckXor(resultBuf, 1, 7) == resultBuf[8]) //数据校验成功
                {
                    if (centerWL != (resultBuf[2] * 256 + resultBuf[3]))
                        return 2;
                    else
                        return 0;
                }
                else
                    return 2;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return 1;
            }
        }

        /// <summary>
        /// 设置单位
        /// </summary>
        /// <param name="errMsg">出错信息</param>
        /// <param name="unitsIndex">单位下标</param>
        /// <param name="channelIndex">通道下标</param>
        /// <returns>0-成功 1-出错 2-失败</returns>
        private int SetUnits(ref string errMsg, byte unitsIndex, byte channelIndex)
        {
            try
            {
                byte[] sendBuf = new byte[7];
                sendBuf[0] = 0xaa;
                sendBuf[1] = 0xbb;
                sendBuf[2] = 0xcc;
                sendBuf[3] = channelIndex;
                sendBuf[4] = unitsIndex;
                sendBuf[5] = 0x0;
                sendBuf[6] = GetCheckXor(sendBuf, 1, 5);
                serialSession.Write(sendBuf, 0, sendBuf.Length);
                Thread.Sleep(50);
                byte[] resultBuf = new byte[9];
                for (int i = 0; i < 9; i++)
                {
                    serialSession.Read(resultBuf, i, 1);
                }

                if (GetCheckXor(resultBuf, 1, 7) == resultBuf[8] && resultBuf[0] == 0x55)
                {
                    if (resultBuf[4] != unitsIndex)
                        return 2;
                    else
                        return 0;
                }
                else
                    return 2;
            }
            catch (Exception ex)
            {
                errMsg += "嘉惠功率计设置单位出错 error:" + ex.Message + "\r";
                return 1;
            }
        }

        /// <summary>
        /// 读取功率值
        /// </summary>
        /// <param name="powerArray">读到的功率值</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0-成功 1-出错 2-失败</returns>
        private int GetPMPower(ref List <double > powerArray, ref string errMsg)
        {
            try
            {
                int res = -1;
                int res2 = -1;
                res = StartReadPower(ref errMsg);
                res2 = ReadPowerValue(ref powerArray, ref errMsg);
                if (res2 == 0 && res == 0)
                    return 0;
                else
                    return 2;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return 1;
            }
        }

        /// <summary>
        /// 开始读取数据
        /// </summary>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0-成功 1-出错 2-失败</returns>
        private int StartReadPower(ref string errMsg)
        {
            try
            {
                serialSession.DiscardInBuffer();
                serialSession.DiscardOutBuffer();
                byte[] sendBuf = new byte[7];
                sendBuf[0] = 0xaa;
                sendBuf[1] = 0x08;
                sendBuf[2] = 0x0;
                sendBuf[3] = 0x0;
                sendBuf[4] = 0x0;
                sendBuf[5] = 0x0;
                sendBuf[6] = 0x08;
                serialSession.Write(sendBuf, 0, sendBuf.Length);
                Thread.Sleep(10);
                int nreadTime = 0;
                int nReWriteTime = 0;
                while (serialSession.BytesToRead != 9)
                {
                    Thread.Sleep(5);
                    nreadTime++;
                    if (nreadTime > 20)
                    {
                        nreadTime = 0;
                        nReWriteTime++;
                        serialSession.DiscardInBuffer();
                        serialSession.DiscardOutBuffer();
                        serialSession.Write(sendBuf, 0, sendBuf.Length);
                        Thread.Sleep(20);
                    }
                    if(nReWriteTime>5)
                    {
                        return 2;
                    }
                }
                byte[] resultBuf = new byte[9];
                for (int i = 0; i < 9; i++)
                {
                    serialSession.Read(resultBuf, i, 1);
                }
                if (resultBuf[1] == 0x05 && resultBuf[2] == 0x01 && resultBuf[8] == GetCheckXor(resultBuf, 1, 7))
                    return 0;
                return 2;
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return 1;
            }
        }

        /// <summary>
        /// 读取数据
        /// </summary>
        /// <param name="powerArray">读到的功率值</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0-成功 1-出错 2-失败</returns>
        private int ReadPowerValue(ref List<double> powerArray, ref string errMsg)
        {
            try
            {
                byte[] send_buf = new byte[7];
                send_buf[0] = 0xaa;
                send_buf[1] = 0x07;
                send_buf[2] = 0x0;
                send_buf[3] = 0x0;
                send_buf[4] = 0x0;
                send_buf[5] = 0x0;
                send_buf[6] = 0x07;
                serialSession.Write(send_buf, 0, send_buf.Length);
                Thread.Sleep(10);
                int nreadTime = 0;
                int nReWriteTime = 0;
                while (serialSession.BytesToRead < 9)
                {
                    Thread.Sleep(1);
                    nreadTime++;
                    if (nreadTime > 20)
                    {
                        nReWriteTime++;
                           nreadTime = 0;
                        serialSession.Write(send_buf, 0, send_buf.Length);
                        Thread.Sleep(20);
                    }
                    if (nReWriteTime > 5)
                        return 2;
                }
                byte[] resultBuf = new byte[9];
                for (int i = 0; i < 9; i++)
                {
                    serialSession.Read(resultBuf, i, 1);
                }


                //取字符串4~7这4位，index从0开始
                if (resultBuf[8] == GetCheckXor(resultBuf, 1, 7) && resultBuf[1] == 0x11)
                {
                    byte[] byValue = new byte[4];
                    byValue[0] = resultBuf[4];
                    byValue[1] = resultBuf[5];
                    byValue[2] = resultBuf[6];
                    byValue[3] = resultBuf[7];
                    powerArray.Add(Math.Round(BitConverter.ToSingle(byValue, 0), 3));
                }
                else
                    powerArray.Add(MolexUtility.CommonFunction.GetDefaultValue());

                while (serialSession.BytesToRead < 9)
                {
                    Thread.Sleep(1);
                }
                resultBuf = new byte[9];
                for (int i = 0; i < 9; i++)
                {
                    serialSession.Read(resultBuf, i, 1);
                }

                //取字符串4~7这4为，index从0开始
                if (resultBuf[8] == GetCheckXor(resultBuf, 1, 7) && resultBuf[1] == 0x12)
                {
                    byte[] byValue = new byte[4];
                    byValue[0] = resultBuf[4];
                    byValue[1] = resultBuf[5];
                    byValue[2] = resultBuf[6];
                    byValue[3] = resultBuf[7];
                    powerArray.Add(Math.Round(BitConverter.ToSingle(byValue, 0), 3));
                }
                else
                    powerArray.Add(MolexUtility.CommonFunction.GetDefaultValue());
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
        /// 停止读取数据
        /// </summary>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0-成功 1-出错 2-失败</returns>
        private int StopReadPower(ref string errMsg)
        {
            try
            {
                byte[] sendBuf = new byte[7];
                sendBuf[0] = 0xaa;
                sendBuf[1] = 0x09;
                sendBuf[2] = 0x0;
                sendBuf[3] = 0x0;
                sendBuf[4] = 0x0;
                sendBuf[5] = 0x0;
                sendBuf[6] = 0x09;
                serialSession.Write(sendBuf, 0, sendBuf.Length);
                /*baseSession.SetEndThreadRead();
                baseSession.WriteSerailBytes(sendBuf, ref errMsg);*/

                Thread.Sleep(20);
                byte[] resultBuf = new byte[9];
                for (int i = 0; i < 9; i++)
                {
                    serialSession.Read(resultBuf, i, 1);
                }

                if (resultBuf[1] == 0x05 && resultBuf[2] == 0x00 && resultBuf[8] == GetCheckXor(resultBuf, 1, 7))
                    return 0;
                return 2;

            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                return 1;
            }
        }

        /// <summary>
        /// 获取校验值
        /// </summary>
        /// <param name="buffer">接收的数据</param>
        /// <param name="startIndex">开始下标</param>
        /// <param name="stopIndex">结束下标</param>
        /// <returns>校验值</returns>
        private byte GetCheckXor(byte[] buffer, int startIndex, int stopIndex)
        {
            if (buffer.Length == 0)
                return 0;
            byte byRes = buffer[startIndex];
            for (int i = startIndex + 1; i <= stopIndex; i++)
                byRes = Convert.ToByte(byRes ^ buffer[i]);
            return byRes;
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
    }
}
