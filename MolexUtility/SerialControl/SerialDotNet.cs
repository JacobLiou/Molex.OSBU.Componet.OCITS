using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Threading;

///<summary>
///文件名：SerialDotNet类
///作用：系统串口类，继承于ISerial接口，实现系统串口的所有操作
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
    public class SerialDotNet:ISerial
    {
        /// <summary>
        /// 串口操作对象
        /// </summary>
        private SerialPort serialSession = null;

        /// <summary>
        /// 线程读取数据事件
        /// </summary>
        public event SerialThreadRead ThreadReadEvent = null;

        /// <summary>
        /// 用来做互斥的对象
        /// lock允许同一时间只有一个线程执行。而ReaderWriterLock允许同一时间有多个线程可以执行读操作，或者只有一个有排它锁的线程执行写操作。
        /// </summary>
        //private ReaderWriterLock readerwritelock = new ReaderWriterLock();
        private object objLock = new object();

        /// <summary>
        /// 实时读取数据标志
        /// </summary>
        private bool isEndThreadRead = false;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="sourceName">串口名</param>
        /// <param name="baudrate">波特率</param>
        /// <param name="errMsg">错误信息</param>
        /// <param name="timeout">延时时间</param>
        /// <param name="isStartRead">开始实时读取数据的标志</param>
        public SerialDotNet(string sourceName, int baudrate, ref string errMsg, int timeout = 100,bool isStartRead=false)
        {
            OpenPort(sourceName, baudrate, timeout, ref errMsg,isStartRead);
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
        private int OpenPort(string sourceName, int baudRate, int timeout, ref string errMsg,bool isStartRead)
        {
            try
            {
                serialSession = new SerialPort();
                serialSession.PortName = sourceName;
                serialSession.BaudRate = baudRate;
                serialSession.StopBits = StopBits.One;
                serialSession.DataBits = 8;
                serialSession.Parity = Parity.None;
                //serialSession.DataReceived += SerialSession_DataReceived;
                serialSession.ReadTimeout = timeout;
                if (serialSession.IsOpen)
                {
                    serialSession.Close();
                }
                
                serialSession.Open();
                //isEndThreadRead = false;
                if (isStartRead)
                {
                    isEndThreadRead = false;
                    serialSession.DataReceived += SerialSession_DataReceived;
                }
                return 0;
            }
            catch (Exception ex)
            {
                errMsg += "打开串口:"+ sourceName + " 出错:" + ex.Message + "\r";
                return 1;
            }
        }

        /// <summary>
        /// 自动接收数据事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SerialSession_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string strResult = "";
            string errMsg = "";
            if (isEndThreadRead)
                return;
            try
            {
                lock (objLock)
                {
                    //readerwritelock.AcquireReaderLock(Timeout.Infinite);//获取读取锁，20毫秒超时
                    do
                    {
                        int count = serialSession.BytesToRead;
                        if (count <= 0)
                            break;
                        byte[] readBuffer = new byte[count];
                        System.Windows.Forms.Application.DoEvents();
                        serialSession.Read(readBuffer, 0, count);
                        strResult += System.Text.Encoding.Default.GetString(readBuffer);

                    } while (serialSession.BytesToRead > 0);
                    //readerwritelock.ReleaseReaderLock();// 释放读取锁
                }
            }
            catch (Exception ex)
            {
                errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                    + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + ex.Message + "\r";

            }
            if (ThreadReadEvent != null && (strResult.Length > 0 || errMsg.Length > 0))
                ThreadReadEvent(strResult, errMsg);
        }
   
        /// <summary>
        /// 结束读取数据
        /// </summary>
        public void SetEndThreadRead()
        {
            isEndThreadRead = true;
            serialSession.DiscardInBuffer();
            serialSession.DiscardOutBuffer();
            
        }

        /// <summary>
        /// 开始读取数据
        /// </summary>
        public void StartThreadRead()
        {
            isEndThreadRead = false;
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
                lock (objLock)
                {
                    //readerwritelock.AcquireReaderLock(Timeout.Infinite);//获取读取锁，20毫秒超时
                    int actualCount = 0;
                    actualCount = serialSession.Read(byteres, (int)alreadyRead, (int)readCount);
                    int beginTick = System.Environment.TickCount;
                    while (actualCount < readCount)
                    {
                        if (System.Environment.TickCount - beginTick > timeout)
                        {
                            errMsg += System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "."
                                + System.Reflection.MethodBase.GetCurrentMethod().Name + " error:" + "超时" + "\r";
                            return 3;
                        }
                        Thread.Sleep(20);
                        readCount -= actualCount;
                        if (ReadSerialBytes(readCount, ref byteres, ref errMsg, timeout, byteres.Length -readCount) != 0)
                            return 2;
                    }
                    //readerwritelock.ReleaseReaderLock();// 释放读取锁
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
                byte[] res = new byte[readCount];
                int actualCount = 0;
                lock (objLock)
                {
                    int nreadTime = 0;
                    while (serialSession.BytesToRead==0)
                    {
                        Thread.Sleep(5);
                        nreadTime++;
                        if (nreadTime > 20)
                        {
                            nreadTime = 0;                          
                            serialSession.DiscardInBuffer();
                            serialSession.DiscardOutBuffer();                          
                            Thread.Sleep(20);
                            break;
                        }
                        
                    }
                    //readerwritelock.AcquireReaderLock(Timeout.Infinite);//获取读取锁，20毫秒超时
                    int err = ReadSerialBytes(readCount, ref res, ref errMsg, timeout, actualCount);
                    if (err != 0)
                    {
                        return err;
                    }
                    for (int i = 0; i < readCount; i++)
                    {
                        strRes += string.Format("{0}", (char)res[i]);
                    }
                    //readerwritelock.ReleaseReaderLock();// 释放读取锁
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
        /// <returns>0-成功 1-错误</returns>
        public int ReadSerialString(out string strRes, ref string errMsg)
        {
            strRes = "";
            try
            {
                lock (objLock)
                {
                    //readerwritelock.AcquireReaderLock(Timeout.Infinite);//获取读取锁，20毫秒超时
                    strRes = serialSession.ReadExisting();
                    //readerwritelock.ReleaseReaderLock();// 释放读取锁
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
        /// <returns>0-成功 1-错误</returns>
        public int WriteSerailBytes(byte[] buffer, ref string errMsg)
        {
            try
            {
                lock (objLock)
                {
                    //readerwritelock.AcquireWriterLock(Timeout.Infinite);//获取写入锁 
                    serialSession.DiscardInBuffer();
                    serialSession.DiscardOutBuffer();                                                                   
                    serialSession.Write(buffer, 0, buffer.Length);
                    //readerwritelock.ReleaseWriterLock();// 释放写入锁
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
        /// <returns>0-成功 1-错误</returns>
        public int WriteSerialString(string buffer, ref string errMsg)
        {
            try
            {
                lock (objLock)
                {
                    //readerwritelock.AcquireWriterLock(Timeout.Infinite);//获取写入锁
                    serialSession.DiscardInBuffer();
                    serialSession.DiscardOutBuffer();
                    serialSession.Write(buffer);
                    //readerwritelock.ReleaseWriterLock();// 释放写入锁
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

        ~SerialDotNet()
        {
            isEndThreadRead = true;
            if (serialSession != null && serialSession.IsOpen)
            {
                serialSession.Close();
                serialSession = null;
            }
        }

        /// <summary>
        /// 关闭串口
        /// </summary>
        public void Close()
        {
            isEndThreadRead = true;
            if (serialSession != null && serialSession.IsOpen)
            {
                serialSession.Close();
                serialSession = null;
            }
        }
    }
}
