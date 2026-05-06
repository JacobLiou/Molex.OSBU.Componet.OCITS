using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;

///<summary>
///文件名：ISerial接口
///作用：串口接口，定义了外部串口读写等功能的接口
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
    /// <summary>
    /// 读取到数据代理
    /// </summary>
    /// <param name="readStr">读取到的数据</param>
    /// <param name="errMsg">错误信息</param>
    public delegate void SerialThreadRead(string readStr, string errMsg);

    public interface ISerial
    {
        /// <summary>
        /// 线程读取数据事件
        /// </summary>
        event SerialThreadRead ThreadReadEvent;

        /// <summary>
        /// 结束实时读取数据
        /// 如果使用NI实时读取数据，结束时必须调用此函数结束线程。
        /// </summary>
        /// <returns></returns>
        void SetEndThreadRead();

        /// <summary>
        /// 开始实时读取数据
        /// </summary>
        void StartThreadRead();

        /// <summary>
        /// 读取数据(读到字节数组)
        /// </summary>
        /// <param name="readCount">读取的字节数</param>
        /// <param name="byteres">读取到的字节数组</param>
        /// <param name="errMsg">错误信息</param>
        /// <param name="timeout">延时时间</param>
        /// <param name="alreadyRead">开始下标</param>
        /// <returns>0-成功 1-失败</returns>
        int ReadSerialBytes(long readCount, ref byte[] byteRes, ref string errMsg, int timeout = 1000, long alreadyRead = 0);

        /// <summary>
        /// 读取数据
        /// </summary>
        /// <param name="readCount">读取的字节数</param>
        /// <param name="result">读取到的字符串</param>
        /// <param name="errMsg">错误信息</param>
        /// <param name="timeout">延时时间</param>
        /// <param name="alreadyRead">开始下标</param>
        /// <returns>0-成功 1-失败</returns>
        int ReadSerialString(int readCount, out string result, ref string errMsg, int timeout = 1000, long alreadyRead = 0);

        /// <summary>
        /// 读取数据
        /// </summary>
        /// <param name="result">读取到的字符串</param>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0-成功 1-失败</returns>
        int ReadSerialString(out string result, ref string errMsg);

        /// <summary>
        /// 写指令(字节数组)
        /// </summary>
        /// <param name="buffer">字节数组格式的指令</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>0-成功 1-失败</returns>
        int WriteSerailBytes(byte[] buffer, ref string errMsg);

        /// <summary>
        /// 写指令(字符串)
        /// </summary>
        /// <param name="buffer">指令</param>
        /// <param name="errMsg">错误信息</param>
        /// <returns>0-成功 1-失败</returns>
        int WriteSerialString(string buffer, ref string errMsg);

        /// <summary>
        /// 关闭串口
        /// </summary>
        void Close();
    }
}
