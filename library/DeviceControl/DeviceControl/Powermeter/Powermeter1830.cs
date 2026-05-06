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
///作用：1830功率计类，继承于IPowermeter接口，实现1830的所有操作
///作者：阮锦芳
///编写日期：2018-01-22
///修改记录
///R1：
///		修改作者：高鹏娟
///		修改日期：2018-04-10
///		修改内容：实现接口
///</summary>

namespace DeviceControl
{
    public class Powermeter1830:IPowermeter 
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
        /// 串口操作对象
        /// </summary>
        private SerialPort serialSession = null;

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
        public Powermeter1830(ref string errMsg, string com, string baudrate)
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
                /*baseSession = new SerialDotNet(com, baudrateInt, ref errMsg, 1000, true);
                //baseSession = new SerialNI(com, baudrateInt, ref errMsg, 1000, true);
                //baseSession.ThreadReadEvent += BaseSession_ThreadReadEvent;*/

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
                ResetPowermeter(ref errMsg);
                return 0;
            }
            catch(Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName+"."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name+" error:" + ex.Message + "\r";
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
                    string writeCMD = "W" + centerWL + "\n";
                    serialSession.Write(writeCMD);
                    Thread.Sleep(50);
                    writeCMD = "W?\n";
                    serialSession.Write(writeCMD);
                    Thread.Sleep(50);
                    int nreadTime = 0;
                    int nReWriteTime = 0;
                    string result = "";
                    bool bEnd = false;
                    while (!bEnd)
                    {
                        if (serialSession.BytesToRead > 0)
                        {
                            result += serialSession.ReadExisting();
                            if (result.Contains("\n"))
                                break;
                        }
                        Thread.Sleep(5);
                        nreadTime++;
                        if (nreadTime > 20)
                        {
                            nreadTime = 0;
                            nReWriteTime++;
                            serialSession.DiscardInBuffer();
                            serialSession.DiscardOutBuffer();
                            serialSession.Write(writeCMD);
                            Thread.Sleep(20);
                        }
                        if (nReWriteTime > 5)
                        {
                            return 2;
                        }
                    }                    
                    if (result.Contains(centerWL.ToString()))
                    {
                        isReading = false;
                        Thread.Sleep(3000);
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
                            serialSession.Write("U1\n");  //Watts
                            break;
                        case "DB":
                            serialSession.Write("U2\n");  //dB
                            break;
                        case "DBM":
                            serialSession.Write("U3\n"); //dBm
                            break;
                        case "REF":
                            serialSession.Write("U4\n");  //REF
                            break;
                        default:
                            break;
                    }
                    Thread.Sleep(50);
                    string writeCMD = "U?\n";
                    serialSession.Write(writeCMD);
                    Thread.Sleep(50);
                    int nreadTime = 0;
                    int nReWriteTime = 0;
                    string result = "";
                    bool bEnd = false;
                    while (!bEnd)
                    {
                        if (serialSession.BytesToRead > 0)
                        {
                            result += serialSession.ReadExisting();
                            if (result.Contains("\n"))
                                break;
                        }
                        Thread.Sleep(5);
                        nreadTime++;
                        if (nreadTime > 20)
                        {
                            nreadTime = 0;
                            nReWriteTime++;
                            serialSession.DiscardInBuffer();
                            serialSession.DiscardOutBuffer();
                            serialSession.Write(writeCMD);
                            Thread.Sleep(20);
                        }
                        if (nReWriteTime > 5)
                        {
                            return 2;
                        }
                    }

                    
                    
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
                    //baseSession.SetEndThreadRead();
                    serialSession.Write("Z?\n");
                    Thread.Sleep(50);
                    string result = serialSession.ReadExisting();
                    //baseSession.ReadSerialString(out result, ref errMsg);
                    //baseSession.StartThreadRead();
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

        private int errFlag = 0;
        private string strFlag = "";
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

                    //baseSession.SetEndThreadRead();
                    for (int index = 0; index < (avgSample + 10); index++)
                    {
                        serialSession.DiscardInBuffer();
                        serialSession.DiscardOutBuffer();
                        string writeCMD = string.Format("D?\n");
                        byte[] bWrCMD = new byte[3];
                        bWrCMD[0] = Convert.ToByte('D');
                        bWrCMD[1] = Convert.ToByte('?');
                        bWrCMD[2] = Convert.ToByte('\n');

                        serialSession.Write(bWrCMD,0, bWrCMD.Length);
                        errFlag = 1;
                        Thread.Sleep(20);
                        int nreadTime = 0;
                        int nReWriteTime = 0;
                        bool bEnd = false;
                        string result = "";
                        while (!bEnd)
                        {
                            if(serialSession.BytesToRead>0)
                            {
                                result += serialSession.ReadExisting();
                                if (result.Contains("\n"))
                                    break;
                            }
                            Thread.Sleep(5);
                            nreadTime++;
                            if (nreadTime > 20)
                            {
                                nreadTime = 0;
                                nReWriteTime++;
                                serialSession.DiscardInBuffer();
                                serialSession.DiscardOutBuffer();
                                serialSession.Write(bWrCMD, 0, bWrCMD.Length);
                                Thread.Sleep(20);
                            }
                            if (nReWriteTime > 5)
                            {
                                return 2;
                            }
                        }
                        
                        //string result = serialSession.ReadExisting();
                        errFlag = 2;
                        strFlag = result;
                        if (result.Length>0)
                        {
                            nReadErrorNum = 0;                           
                             result = result.Substring(0, result.Length - 1);
                             dPower = Convert.ToDouble(result);
                                                    
                        }
                        else
                        {
                            if (nReadErrorNum > 3)//连续错误3次
                            {
                                isReading = false;
                                return 2;
                            }
                            nReadErrorNum++;
                            continue;
                        }
                        errFlag = 3;
                        if (dPowerOld < MolexUtility.CommonFunction.GetDefaultValue() && dPower < (dPowerOld - 6.0))
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
                            errFlag = 4;
                            return 0;
                        }

                        if (index > (Number + 9))   //错误超过9次
                        {
                            errMsg = "错误超过9次!";
                            isReading = false;
                            return 2;
                        }
                    }
                    //baseSession.StartThreadRead();

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
                    errMsg = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
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
                        int errCount = 0;
                        for (int index = 0; index < totalCount; index++)
                        {
                            int result = -1;
                            result = ReadPowerAvg(ref errMsg, out powerNew, isGetAllChannel: isGetAllChannel, specialChannel: specialChannel);
                            for (int j = 0; j < ChannelCount; j++)
                                powers[j].Add(powerNew[j]);
                            if (result != 0)
                            {
                                errCount++;
                                totalCount++;
                                if (errCount > 10)
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
                        if (totalCount < 100)
                            totalCount = 100;
                        else if (totalCount > 1300)
                            totalCount = 1300;
                        errMsg = "";
                        List<double> powerNew = new List<double>();
                        List<double> powers = new List<double>();
                        int errCount = 0;
                        for (int index = 0; index < totalCount; index++)
                        {
                            int result = -1;
                            result = ReadPowerAvg(ref errMsg, out powerNew, isGetAllChannel: isGetAllChannel, specialChannel: specialChannel);
                            powers.Add(powerNew[0]);
                            if (result != 0)
                            {
                                errCount++;
                                totalCount++;
                                if (errCount > 10)
                                {
                                    errMsg = "读功率计出错！";
                                    return 2;
                                }
                                continue;
                            }
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
                    //baseSession.SetEndThreadRead();
                    //1、Average of the measurements,same as Filter (F1:16点, F2:4点, F3:1点)
                    serialSession.Write("F2\n");//medunm 
                    Thread.Sleep(50);
                    serialSession.Write("F?\n");
                    Thread.Sleep(50);
                    string filter = serialSession.ReadExisting();
                    
                    Thread.Sleep(50);
                    //2、Units(U1:Watts, U2:dB, U3:dBm, U4:REF)
                    serialSession.Write("U2\n");//dB 
                    serialSession.Write("U?\n");
                    Thread.Sleep(50);
                    string units = serialSession.ReadExisting();
                    
                    Thread.Sleep(50);
                    //4、Set Range of the input signal (R0,R1,...R8)
                    serialSession.Write("R0\n");//Auto
                    serialSession.Write("R?\n");
                    Thread.Sleep(50);
                    string range = serialSession.ReadExisting();
                    
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
            /*if (baseSession != null)
            {
                baseSession.SetEndThreadRead();
                baseSession = null;
            }*/
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
