using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ivi.Visa;
using NationalInstruments.Visa;
using System.Threading;
using System.ComponentModel;

///<summary>
///文件名：SerialNI类
///作用：NI串口类，继承于ISerial接口，实现NI串口的所有操作
///作者：高鹏娟
///编写日期：2018-04-05
///修改记录
///R1：
///		修改作者：高鹏娟
///		修改日期：2018-04-18
///		修改内容：添加StartThreadRead接口
///		注意：如果实现一写一读的模式，必须先调SetEndThreadRead关闭实时读取数据线程/事件，然后写WriteSerailBytes、读ReadSerialBytes，最后调用StartThreadRead开启实时读数据线程/事件
///</summary>
namespace MolexUtility.SerialControl
{
    public class SerialNI:ISerial
    {
        /// <summary>
        /// 串口操作对象
        /// </summary>
        private SerialSession serialSession = null;

        /// <summary>
        /// 用来做互斥的对象
        /// </summary>
        //private ReaderWriterLock readerwritelock = new ReaderWriterLock();
        private object objLock = new object();

        /// <summary>
        /// 读取串口线程
        /// </summary>
        private Thread readThread = null;

        /// <summary>
        /// 实时读取数据标志
        /// </summary>
        private bool isEndThreadRead = false;

        /// <summary>
        /// 线程读取数据事件
        /// </summary>
        public event SerialThreadRead ThreadReadEvent = null;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="sourceName">串口资源名称</param>
        /// <param name="baudrate">波特率</param>
        /// <param name="errMsg">错误信息</param>
        /// <param name="timeout">操作串口超时时间，默认为100ms，单位为ms</param>
        /// <param name="isStartRead">开始实时读取数据的标志</param>
        public SerialNI(string sourceName, int baudrate, ref string errMsg, int timeout = 100, bool isStartRead=false)
        {
            OpenPort(sourceName, baudrate, timeout, ref errMsg,isStartRead);
        }

        /// <summary>
        /// 结束读取数据线程
        /// </summary>
        public void SetEndThreadRead()
        {
            serialSession.Flush(IOBuffers.ReadWrite,true);
            isEndThreadRead = true;
        }

        /// <summary>
        /// 开始读取数据线程
        /// </summary>
        public void StartThreadRead()
        {
            isEndThreadRead = false;
            readThread = new Thread(new ThreadStart(ReadThreadStart));
            readThread.Start();
        }

        /// <summary>
        /// 开始读取数据
        /// </summary>
        private void ReadThreadStart()
        {
            while (!isEndThreadRead)
            {
                string strRes = "";
                string errMsg = "";
                try
                {
                    lock (objLock)
                    {
                        //readerwritelock.AcquireReaderLock(Timeout.Infinite);
                        if (serialSession.BytesAvailable > 0)
                            strRes = serialSession.RawIO.ReadString(serialSession.BytesAvailable);
                        //readerwritelock.ReleaseReaderLock();
                    }
                }
                catch (Exception ex)
                {
                    errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                        + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                }
                if (ThreadReadEvent != null && (strRes.Length > 0 || errMsg.Length > 0))
                    ThreadReadEvent(strRes, errMsg);
            }
        }

        /// <summary>
        /// 打开串口
        /// </summary>
        /// <param name="sourceName">串口名</param>
        /// <param name="baudrate">波特率</param>
        /// <param name="timeout">延时时间</param>
        /// <param name="errMsg">错误信息</param>
        /// <param name="isStartRead">开始实时读取数据的标志</param>
        /// <returns>0-成功 1-出错</returns>
        private int OpenPort(string sourceName, int baudrate, int timeout, ref string errMsg, bool isStartRead)
        {
            using (var rmSession = new ResourceManager())
            {
                try
                {
                    serialSession = (SerialSession)rmSession.Open(sourceName);
                    serialSession.BaudRate = baudrate;
                    serialSession.DataBits = 8;
                    serialSession.Parity = SerialParity.None;
                    serialSession.StopBits = SerialStopBitsMode.One;
                    serialSession.TerminationCharacterEnabled = false;
                    serialSession.TimeoutMilliseconds = timeout;

                    if (isStartRead)
                    {
                        isEndThreadRead = false;
                        readThread = new Thread(new ThreadStart(ReadThreadStart));
                        readThread.Start();
                    }
                }
                catch (Exception ex)
                {
                    errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                        + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";
                    return 1;
                }
            }
            return 0;
        }

        /// <summary>
        /// 读取数据(读到字节数组)
        /// </summary>
        /// <param name="readCount">读取的字节数</param>
        /// <param name="byteres">读取到的字节数组</param>
        /// <param name="errMsg">错误信息</param>
        /// <param name="timeout">延时时间</param>
        /// <param name="alreadyRead">开始下标</param>
        /// <returns>0-读取数据成功 1-出错 2-读取数据失败 3-超时</returns>
        public int ReadSerialBytes(long readCount, ref byte[] byteres, ref string errMsg, int timeout = 1000, long alreadyRead = 0)
        {
            try
            {
                long actualCount = 0;
                ReadStatus readStatus = ReadStatus.Unknown;
                lock (objLock)
                {
                    //readerwritelock.AcquireReaderLock(Timeout.Infinite);//获取读取锁，20毫秒超时
                    serialSession.RawIO.Read(byteres, 0, readCount, out actualCount, out readStatus);
                    if (readStatus != ReadStatus.EndReceived)
                    {
                        int beginTick = System.Environment.TickCount;
                        while (actualCount != readCount)
                        {
                            if (System.Environment.TickCount - beginTick > timeout)
                            {
                                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + "超时" + "\r";
                                return 3;
                            }
                            Thread.Sleep(20);
                            readCount -= (int)actualCount;
                            if (ReadSerialBytes(readCount, ref byteres, ref errMsg, timeout, byteres.Length -readCount) != 0)
                                return 2;
                        }
                    }
                    //readerwritelock.ReleaseReaderLock();// 释放读取锁
                }
                if (readStatus != ReadStatus.Unknown)
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
        /// <param name="readCount">读取的字节数</param>
        /// <param name="strRes">读取到的字符串</param>
        /// <param name="errMsg">错误信息</param>
        /// <param name="timeout">延时时间</param>
        /// <param name="alreadyRead">开始下标</param>
        /// <returns>0-读取数据成功 1-出错 2-读取数据失败 3-超时</returns>
        public int ReadSerialString(int readCount, out string strRes, ref string errMsg, int timeout = 1000, long alreadyRead = 0)
        {
            strRes = "";
            try
            {
                lock (objLock)
                {
                    //readerwritelock.AcquireReaderLock(Timeout.Infinite);
                    byte[] res = new byte[readCount];
                    long actualCount = 0;
                    int err = ReadSerialBytes(readCount, ref res, ref errMsg, timeout, actualCount);
                    if (err != 0)
                    {
                        return err;
                    }
                    for (int i = 0; i < readCount; i++)
                    {
                        strRes += string.Format("{0}", (char)res[i]);
                    }
                    //readerwritelock.ReleaseReaderLock();
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
        /// 读取数据
        /// </summary>
        /// <param name="strRes">读取到的字符串</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0-成功 1-出错</returns>
        public int ReadSerialString(out string strRes, ref string errMsg)
        {
            strRes = "";
            try
            {
                lock (objLock)
                {
                    //readerwritelock.AcquireReaderLock(Timeout.Infinite);
                    strRes = serialSession.RawIO.ReadString();
                    //readerwritelock.ReleaseReaderLock();
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
        /// 写指令(字节数组)
        /// </summary>
        /// <param name="buffer">字节数组格式的指令</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>0-成功 1-出错</returns>
        public int WriteSerailBytes(byte[] buffer, ref string errMsg)
        {
            try
            {
                lock (objLock)
                {
                    //readerwritelock.AcquireWriterLock(Timeout.Infinite);
                    serialSession.RawIO.Write(buffer);
                    //readerwritelock.ReleaseWriterLock();
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
        /// 写指令(字符串)
        /// </summary>
        /// <param name="buffer">指令</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>0-成功 1-出错</returns>
        public int WriteSerialString(string buffer, ref string errMsg)
        {
            try
            {
                lock (objLock)
                {
                    //readerwritelock.AcquireWriterLock(Timeout.Infinite);
                    serialSession.RawIO.Write(buffer);
                    //readerwritelock.ReleaseWriterLock();
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
        /// 析构（关闭线程和串口）
        /// </summary>
        ~SerialNI()
        {
            if (readThread != null)
            {
                isEndThreadRead = true;
            }
            //readerwritelock = null;
            if (serialSession != null)
            {
                serialSession = null;
            }
        }

        public void Close()
        {
            if (readThread != null)
            {
                isEndThreadRead = true;
            }
            //readerwritelock = null;
            if (serialSession != null)
                serialSession = null;
        }
    }
}
