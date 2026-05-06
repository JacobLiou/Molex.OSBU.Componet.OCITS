using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

///<summary>
///文件名：IPowermeter
///作用：功率计接口类，定义了外部读功率等功率计操作的接口
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
    public interface IPowermeter
    {
        /// <summary>
        /// 功率计总共有多少个通道
        /// </summary>
        int ChannelCount { get; set; }

        /// <summary>
        /// 设置功率计所有通道中心波长
        /// </summary>
        /// <param name="dCenterWL">需要设置的中心波长</param>
        /// <returns>0-成功 1-设置信息 2-不支持该功能</returns>
        int SetPMWavelength(double dCenterWL, ref string errMsg);

        /// <summary>
        /// 设置光功率计单位
        /// </summary>
        /// <param name="errMsg">错误信息</param>
        /// <param name="units">Watts，dB，dBm，REF</param>
        /// <returns>0-成功 1-设置信息 2-不支持该功能</returns>
        int SetPMUnits(ref string errMsg, string units);

        /// <summary>
        /// 是否已经清理
        /// </summary>
        /// <param name="errMsg">错误信息</param>
        /// <returns>0-已清理 1-出错 2-不支持该功能</returns>
        int GetZeroControl(ref string errMsg);

        /// <summary>
        /// 光功率计复位
        /// </summary>
        /// <param name="errMsg">出错信息</param>
        /// <returns>0-复位成功 1-出错 2-不支持该功能</returns>
        int ResetPowermeter(ref string errMsg);

        /// <summary>
        /// 读取功率平均值
        /// </summary>
        /// <param name="errMsg">出错信息</param>
        /// <param name="powerArray">读取到的功率值</param>
        /// <param name="nAvgSample">采样多少个点取平均值</param>
        /// <param name="isGetAllChannel">一个通道或者所有通道</param>
        /// <param name="specialChannel">如果isGetAllChannel为false，有效，指定要读取功率的通道</param>
        /// <returns>0-成功 1-出错</returns>
        int ReadPowerAvg(ref string errMsg, out List<double> powerArray, int nAvgSample = 1, bool isGetAllChannel = false, string specialChannel = "0");

        /// <summary>
        /// 读取多个功率值
        /// </summary>
        /// <param name="errMsg">出错信息</param>
        /// <param name="powerArray">存放功率值数据</param>
        /// <param name="timeInternal">两次读取数值间间隔</param>
        /// <param name="totalCount">每个通道读取功率点数</param>
        /// <param name="isGetAllChannel">一个通道或者所有通道</param>
        /// <param name="specialChannel">如果isGetAllChannel为false，有效，指定要读取功率的通道</param>
        /// <returns>0-成功 1-出错</returns>
        int GetMultiPowers(ref string errMsg, out List<List<double>> powerArray, int timeInternal, int totalCount, bool isGetAllChannel = false, string specialChannel = "0");

        /// <summary>
        /// 开始读取多个功率值
        /// </summary>
        /// <param name="errMsg">出错信息</param>
        /// <param name="timeInternal">两次读取数值间间隔</param>
        /// <param name="isGetAllChannel">一个通道或者所有通道</param>
        /// <param name="specialChannel">如果isGetAllChannel为false，有效，指定要读取功率的通道</param>
        /// <returns>0-成功 1-出错</returns>
        int BeginReadMltiPowers(ref string errMsg, int timeInternal, bool isGetAllChannel = false, string specialChannel = "0");

        /// <summary>
        /// 结束读取多个功率值
        /// </summary>
        /// <param name="errMsg">出错信息</param>
        /// <param name="powerArray">存放功率值数据</param>
        /// <param name="isGetAllChannel">一个通道或者所有通道，如果为所有通道，则停止所有通道数据，并返回结果</param>
        /// <param name="specialChannel">如果isGetAllChannel为false，有效，停止读取该通道功率，并返回所有读取结果</param>
        /// <returns>0-成功 1-出错</returns>
        int EndReadMultiPowers(ref string errMsg, out List<List<double>> powerArray, bool isGetAllChannel = false, string specialChannel = "0");

        void PowermeterClose();
    }
}
