using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

///<summary>
///文件名：ScanParam
///作用：扫描相关参数
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
    public class ScanParam
    {
        public ulong Size { get; set; }
        public double StartWavelength { get; set; }
        public double StopWavelength { get; set; }
        public double Step { get; set; }
        public double TLSPower { get; set; }
        public double PWMPower { get; set; }
        public ulong NumberOfScan { get; set; }
        public ulong ChannelNum { get; set; }
        public ulong ChannelCfgHigh { get; set; }
        public ulong ChannelCfgLow { get; set; }
        public ulong OpticalHighOrLow { get; set; }
        public ulong SampleCount { get; set; }


    }
}
